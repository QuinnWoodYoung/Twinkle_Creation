using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色技能控制器。
/// 负责管理技能列表、冷却、输入读取，以及为每次施法创建 CastContext。
/// 具体技能逻辑由 SkillData 和 SkillEffect 处理。
/// </summary>
public class CharSkillCtrl : MonoBehaviour
{
    /// <summary>
    /// 当前角色拥有的技能列表。
    /// 顺序与技能输入槽位对应。
    /// </summary>
    [Header("技能列表")]
    [Tooltip("角色当前拥有的技能槽。索引顺序必须与输入槽顺序一致。")]
    public List<SkillData> skills = new List<SkillData>();

    /// <summary>
    /// 用于目标采集的 Layer。
    /// groundLayer 只用于地面点选，单位识别已经不再依赖单位 Layer。
    /// </summary>
    [Header("选目标层")]
    [Tooltip("地面选目标使用的 LayerMask。")]
    public LayerMask groundLayer;

    /// <summary>
    /// 每个技能槽位剩余冷却时间。
    /// </summary>
    private readonly List<float> _skillCooldowns = new List<float>();
    private CharCtrl _charCtrl;
    private CharActionCtrl _actionCtrl;
    private CharBlackBoard _blackBoard;
    private bool _hasPendingCast;
    private int _pendingSkillIndex = -1;
    private SkillData _pendingSkill;
    private CastContext _pendingContext;

    private void Awake()
    {
        _charCtrl = GetComponent<CharCtrl>();
        _actionCtrl = GetComponent<CharActionCtrl>();
        _blackBoard = GetComponent<CharBlackBoard>();

        if (_charCtrl == null || _charCtrl.Param == null)
        {
            return;
        }

        _charCtrl.Param.SkillInputDown.Clear();
        _skillCooldowns.Clear();

        for (int i = 0; i < skills.Count; i++)
        {
            _charCtrl.Param.SkillInputDown.Add(false);
            _skillCooldowns.Add(0f);
        }

        SyncBlackBoardSkillData();
    }

    private void Update()
    {
        for (int i = 0; i < _skillCooldowns.Count; i++)
        {
            if (_skillCooldowns[i] > 0f)
            {
                _skillCooldowns[i] -= Time.deltaTime;
            }
        }

        ProcessSkillInputs();
        SyncBlackBoardSkillData();
    }

    private void LateUpdate()
    {
        UpdatePendingCast();
    }

    /// <summary>
    /// 读取技能按键信号并触发对应技能。
    /// </summary>
    private void ProcessSkillInputs()
    {
        if (_hasPendingCast || _charCtrl == null || _charCtrl.Param == null)
        {
            return;
        }

        int inputCount = _charCtrl.Param.SkillInputDown.Count;

        for (int i = 0; i < skills.Count; i++)
        {
            if (i >= inputCount)
            {
                continue;
            }

            if (_charCtrl.Param.SkillInputDown[i])
            {
                TryCastSkill(i);
            }
        }
    }

    /// <summary>
    /// 尝试释放指定槽位的技能。
    /// 成功时才进入冷却。
    /// </summary>
    public void TryCastSkill(int skillIndex)
    {
        if (!CanCastByState())
        {
            return;
        }

        if (skillIndex < 0 || skillIndex >= skills.Count)
        {
            return;
        }

        SkillData currentSkill = skills[skillIndex];
        if (currentSkill == null)
        {
            return;
        }

        if (_skillCooldowns[skillIndex] > 0f)
        {
            return;
        }

        TargetInfo info = TargetingUtil.Collect(_charCtrl, groundLayer);
        CastContext context = new CastContext(gameObject, info);

        if (!currentSkill.CanActivate(context))
        {
            return;
        }

        if (currentSkill.requireFacingBeforeCast && TryBeginPendingCast(skillIndex, currentSkill, context))
        {
            return;
        }

        if (!TryStartCastAnim(currentSkill))
        {
            return;
        }

        ExecuteSkill(skillIndex, currentSkill, context);
    }

    private bool TryBeginPendingCast(int skillIndex, SkillData skill, CastContext context)
    {
        Vector3 facingDirection = GetCastFacingDirection(context);
        if (facingDirection.sqrMagnitude < 0.001f)
        {
            return false;
        }

        if (_actionCtrl != null)
        {
            // 当前俯视角 ARPG 版本还没有做完整施法阶段图。
            // 这里只先保留一个“先转向再施法”的动作入口。
            CharActionReq req = new CharActionReq
            {
                type = CharActionType.Cast,
                state = CharActionState.CastPoint,
                src = skill,
                dur = ResolveCastActionDuration(skill.castAnimDur),
                lockMove = skill.lockMovementDuringFacing || skill.lockMoveOnCastAnim,
                lockRotate = skill.lockRotateOnCastAnim,
                interruptible = true,
                animKey = skill.GetCastAnimKey(),
                waitFace = true,
                faceDir = facingDirection,
                faceTol = skill.castFacingAngleTolerance,
            };

            if (!_actionCtrl.TryStart(req))
            {
                return false;
            }
        }
        else if (_charCtrl != null)
        {
            if (skill.lockMovementDuringFacing)
            {
                _charCtrl.SetMovementLocked(true);
            }

            _charCtrl.BeginSkillFacing(facingDirection);
        }

        _hasPendingCast = true;
        _pendingSkillIndex = skillIndex;
        _pendingSkill = skill;
        _pendingContext = context;
        SyncBlackBoardSkillData();

        return true;
    }

    private void UpdatePendingCast()
    {
        if (!_hasPendingCast || _pendingSkill == null || _pendingContext == null)
        {
            return;
        }

        if (!CanCastByState())
        {
            ClearPendingCast();
            return;
        }

        if (_actionCtrl != null && _actionCtrl.CurReq == null)
        {
            ClearPendingCast();
            return;
        }

        Vector3 facingDirection = GetCastFacingDirection(_pendingContext);
        if (facingDirection.sqrMagnitude < 0.001f)
        {
            ExecutePendingCast();
            return;
        }

        if (_actionCtrl != null)
        {
            if (_actionCtrl.IsWaitingFace())
            {
                return;
            }

            // 转向完成后，立刻继续原本的技能释放逻辑。
            ExecutePendingCast();
            return;
        }

        if (_charCtrl != null)
        {
            _charCtrl.BeginSkillFacing(facingDirection);

            if (!_charCtrl.IsFacingDirection(facingDirection, _pendingSkill.castFacingAngleTolerance))
            {
                return;
            }
        }

        ExecutePendingCast();
    }

    private void ExecutePendingCast()
    {
        int skillIndex = _pendingSkillIndex;
        SkillData skill = _pendingSkill;
        CastContext context = _pendingContext;

        bool keepCastAction = _actionCtrl != null && _actionCtrl.CurReq != null && _actionCtrl.CurReq.src == skill;
        ClearPendingCast(!keepCastAction);
        ExecuteSkill(skillIndex, skill, context);
    }

    private bool TryStartCastAnim(SkillData skill)
    {
        if (skill == null || _actionCtrl == null)
        {
            return true;
        }

        CharActionReq req = new CharActionReq
        {
            type = CharActionType.Cast,
            state = CharActionState.CastPoint,
            src = skill,
            dur = ResolveCastActionDuration(skill.castAnimDur),
            lockMove = skill.lockMoveOnCastAnim,
            lockRotate = skill.lockRotateOnCastAnim,
            interruptible = true,
            animKey = skill.GetCastAnimKey(),
        };

        return _actionCtrl.TryStart(req);
    }

    private void ClearPendingCast(bool endCur = true)
    {
        if (_actionCtrl != null)
        {
            if (endCur)
            {
                _actionCtrl.EndCur();
            }
        }
        else if (_charCtrl != null)
        {
            _charCtrl.EndSkillFacing();
            _charCtrl.SetMovementLocked(false);
        }

        _hasPendingCast = false;
        _pendingSkillIndex = -1;
        _pendingSkill = null;
        _pendingContext = null;
        SyncBlackBoardSkillData();
    }

    private void ExecuteSkill(int skillIndex, SkillData skill, CastContext context)
    {
        if (skill == null || context == null)
        {
            return;
        }

        if (skill.Activate(context))
        {
            _skillCooldowns[skillIndex] = skill.cooldown;
            SyncBlackBoardSkillData();
        }
    }

    private Vector3 GetCastFacingDirection(CastContext context)
    {
        if (context == null || context.caster == null)
        {
            return Vector3.zero;
        }

        Vector3 direction = context.rawTarget.direction;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = context.rawTarget.position - context.caster.transform.position;
            direction.y = 0f;
        }

        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
    }

    private bool CanCastByState()
    {
        return CharRuntimeResolver.CanCast(gameObject);
    }

    private float ResolveCastActionDuration(float baseDuration)
    {
        if (baseDuration <= 0f)
        {
            return 0f;
        }

        float castSpeed = CharRuntimeResolver.GetCastSpeed(gameObject);
        return Mathf.Max(0f, baseDuration / Mathf.Max(castSpeed, 0.01f));
    }

    private void SyncBlackBoardSkillData()
    {
        if (_blackBoard == null || !_blackBoard.Features.useSkills)
        {
            return;
        }

        List<SkillData> skillSlots = _blackBoard.Skills.slots;
        skillSlots.Clear();
        for (int i = 0; i < skills.Count; i++)
        {
            skillSlots.Add(skills[i]);
        }

        List<float> cooldowns = _blackBoard.Skills.cooldowns;
        cooldowns.Clear();
        for (int i = 0; i < _skillCooldowns.Count; i++)
        {
            cooldowns.Add(_skillCooldowns[i]);
        }

        _blackBoard.Skills.pendingCast = _hasPendingCast;
        _blackBoard.Skills.pendingSlot = _pendingSkillIndex;
        _blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Skills);
    }
}
