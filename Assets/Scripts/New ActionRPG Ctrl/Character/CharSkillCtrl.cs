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
    [Header("Skill List")]
    public List<SkillData> skills = new List<SkillData>();

    /// <summary>
    /// 用于目标采集的 Layer。
    /// groundLayer 识别地面，unitLayer 识别可被技能选中的单位。
    /// </summary>
    [Header("Targeting Layers")]
    public LayerMask groundLayer;
    public LayerMask unitLayer;

    /// <summary>
    /// 每个技能槽位剩余冷却时间。
    /// </summary>
    private readonly List<float> _skillCooldowns = new List<float>();
    private CharCtrl _charCtrl;
    private StateManager _stateManager;
    private bool _hasPendingCast;
    private int _pendingSkillIndex = -1;
    private SkillData _pendingSkill;
    private CastContext _pendingContext;

    private void Awake()
    {
        _charCtrl = GetComponent<CharCtrl>();
        _stateManager = GetComponent<StateManager>();

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
        if (_stateManager != null && !_stateManager.CanCastSkills)
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

        TargetInfo info = TargetingUtil.Collect(_charCtrl, groundLayer, unitLayer);
        CastContext context = new CastContext(gameObject, info);

        if (!currentSkill.CanActivate(context))
        {
            return;
        }

        if (currentSkill.requireFacingBeforeCast && TryBeginPendingCast(skillIndex, currentSkill, context))
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

        _hasPendingCast = true;
        _pendingSkillIndex = skillIndex;
        _pendingSkill = skill;
        _pendingContext = context;

        if (_charCtrl != null)
        {
            if (skill.lockMovementDuringFacing)
            {
                _charCtrl.SetMovementLocked(true);
            }

            _charCtrl.BeginSkillFacing(facingDirection);
        }

        return true;
    }

    private void UpdatePendingCast()
    {
        if (!_hasPendingCast || _pendingSkill == null || _pendingContext == null)
        {
            return;
        }

        if (_stateManager != null && !_stateManager.CanCastSkills)
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

        ClearPendingCast();
        ExecuteSkill(skillIndex, skill, context);
    }

    private void ClearPendingCast()
    {
        if (_charCtrl != null)
        {
            _charCtrl.EndSkillFacing();
            _charCtrl.SetMovementLocked(false);
        }

        _hasPendingCast = false;
        _pendingSkillIndex = -1;
        _pendingSkill = null;
        _pendingContext = null;
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
}
