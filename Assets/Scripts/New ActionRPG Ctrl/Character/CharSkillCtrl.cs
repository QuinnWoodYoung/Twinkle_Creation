using System.Collections.Generic;
using UnityEngine;

public class CharSkillCtrl : MonoBehaviour
{
    [Header("Skill List")]
    [Tooltip("技能列表顺序需要和输入槽位一一对应。")]
    public List<SkillData> skills = new List<SkillData>();

    [Header("Targeting")]
    public LayerMask groundLayer;

    private readonly List<float> _skillCooldowns = new List<float>();

    private CharCtrl _charCtrl;
    private CharActionCtrl _actionCtrl;
    private CharBlackBoard _blackBoard;
    private CharSignalReader _signalReader;
    private SkillPreviewController _previewCtrl;

    private bool _hasPendingCast;
    private int _pendingSkillIndex = -1;
    private SkillData _pendingSkill;
    private CastContext _pendingContext;

    private bool _hasQueuedCastRelease;
    private int _queuedSkillIndex = -1;
    private SkillData _queuedSkill;
    private CastContext _queuedContext;
    private float _queuedCastRemain;

    private bool _isChanneling;
    private int _channelSkillIndex = -1;
    private SkillData _channelSkill;
    private CastContext _channelContext;
    private float _channelRemain;
    private float _channelTickRemain;
    private int _heldGamepadSkillIndex = -1;
    private SkillData _heldGamepadSkill;

    private void Awake()
    {
        _charCtrl = GetComponent<CharCtrl>();
        _actionCtrl = GetComponent<CharActionCtrl>();
        _blackBoard = GetComponent<CharBlackBoard>();
        _signalReader = GetComponent<CharSignalReader>();
        _previewCtrl = GetComponent<SkillPreviewController>();
        if (_previewCtrl == null)
        {
            _previewCtrl = gameObject.AddComponent<SkillPreviewController>();
        }

        if (_charCtrl == null || _charCtrl.Param == null)
        {
            return;
        }

        _charCtrl.Param.SkillInputDown.Clear();
        _charCtrl.Param.SkillInputStates.Clear();
        _skillCooldowns.Clear();

        for (int i = 0; i < skills.Count; i++)
        {
            _charCtrl.Param.SkillInputDown.Add(false);
            _charCtrl.Param.SkillInputStates.Add(default);
            _skillCooldowns.Add(0f);
        }

        SyncBlackBoardSkillData();
    }

    private void OnEnable()
    {
        if (_actionCtrl == null)
        {
            _actionCtrl = GetComponent<CharActionCtrl>();
        }

        if (_actionCtrl != null)
        {
            _actionCtrl.ActionEnd += OnActionEnd;
            _actionCtrl.ActionIntd += OnActionInterrupted;
        }
    }

    private void OnDisable()
    {
        if (_actionCtrl != null)
        {
            _actionCtrl.ActionEnd -= OnActionEnd;
            _actionCtrl.ActionIntd -= OnActionInterrupted;
        }

        if (_previewCtrl != null)
        {
            _previewCtrl.CancelPreview();
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

        UpdateQueuedCastRelease();
        UpdateChanneling();
        ProcessSkillInputs();
        SyncBlackBoardSkillData();
    }

    private void LateUpdate()
    {
        UpdatePendingCast();
    }

    private void ProcessSkillInputs()
    {
        if (UpdateHeldGamepadSkill())
        {
            return;
        }

        if (IsPreviewing())
        {
            ProcessPreviewInputs();
            return;
        }

        if (HasSkillFlowInProgress() || _charCtrl == null || _charCtrl.Param == null)
        {
            return;
        }

        if (IsUsingGamepadSkillMode())
        {
            ProcessGamepadSkillInputs();
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

    private void ProcessGamepadSkillInputs()
    {
        int inputCount = _charCtrl.Param.SkillInputStates.Count;
        for (int i = 0; i < skills.Count; i++)
        {
            if (i >= inputCount)
            {
                continue;
            }

            if (_charCtrl.Param.SkillInputStates[i].isDown)
            {
                BeginHeldGamepadSkill(i);
                return;
            }
        }
    }

    public void TryCastSkill(int skillIndex)
    {
        if (IsPreviewing() || HasSkillFlowInProgress() || !CanCastByState())
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

        if (!CharResourceResolver.HasEnoughEnergy(gameObject, currentSkill.energyCost))
        {
            return;
        }

        TargetInfo info = TargetingUtil.Collect(_charCtrl, groundLayer);
        if (ShouldUseAimPreview(currentSkill))
        {
            _previewCtrl.BeginPreview(skillIndex, currentSkill, groundLayer);
            return;
        }

        TryCastSkillWithTargetInfo(skillIndex, currentSkill, info);
    }

    private bool TryCastSkillWithTargetInfo(int skillIndex, SkillData currentSkill, TargetInfo info)
    {
        CastContext context = new CastContext(gameObject, info);
        if (!currentSkill.CanActivate(context))
        {
            return false;
        }

        if (currentSkill.requireFacingBeforeCast && TryBeginPendingCast(skillIndex, currentSkill, context))
        {
            return true;
        }

        BeginSkillFlow(skillIndex, currentSkill, context, false);
        return true;
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
            CharActionReq req = new CharActionReq
            {
                type = CharActionType.Cast,
                state = CharActionState.CastPoint,
                src = skill,
                dur = ResolveCastPointDuration(skill.castAnimDur),
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

        bool reuseExistingCastAction =
            _actionCtrl != null &&
            _actionCtrl.CurReq != null &&
            _actionCtrl.CurReq.type == CharActionType.Cast &&
            _actionCtrl.CurReq.src == skill;

        ClearPendingCast(!reuseExistingCastAction);
        BeginSkillFlow(skillIndex, skill, context, reuseExistingCastAction);
    }

    private void BeginSkillFlow(int skillIndex, SkillData skill, CastContext context, bool reuseExistingCastAction)
    {
        if (skill == null || context == null)
        {
            return;
        }

        float castPointDuration = ResolveCastPointDuration(skill.castAnimDur);
        if (skill.UsesCastPointRelease())
        {
            if (!reuseExistingCastAction && !TryStartCastAnim(skill))
            {
                return;
            }

            if (castPointDuration <= 0f)
            {
                if (skill.IsChannelSkill())
                {
                    StartChannel(skillIndex, skill, context);
                }
                else
                {
                    ExecuteSkill(skillIndex, skill, context);
                }

                return;
            }

            QueueCastRelease(skillIndex, skill, context, reuseExistingCastAction);
            return;
        }

        if (!reuseExistingCastAction && !TryStartCastAnim(skill))
        {
            return;
        }

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
            dur = ResolveCastPointDuration(skill.castAnimDur),
            lockMove = skill.lockMoveOnCastAnim,
            lockRotate = skill.lockRotateOnCastAnim,
            interruptible = true,
            animKey = skill.GetCastAnimKey(),
        };

        return _actionCtrl.TryStart(req);
    }

    private void QueueCastRelease(int skillIndex, SkillData skill, CastContext context, bool reuseExistingCastAction)
    {
        _hasQueuedCastRelease = true;
        _queuedSkillIndex = skillIndex;
        _queuedSkill = skill;
        _queuedContext = context;
        _queuedCastRemain = reuseExistingCastAction || _actionCtrl != null
            ? 0f
            : ResolveCastPointDuration(skill.castAnimDur);
    }

    private void UpdateQueuedCastRelease()
    {
        if (!_hasQueuedCastRelease || _queuedSkill == null || _queuedContext == null)
        {
            return;
        }

        if (!CanCastByState())
        {
            ClearQueuedCastRelease();
            return;
        }

        if (_actionCtrl != null)
        {
            return;
        }

        if (_queuedCastRemain > 0f)
        {
            _queuedCastRemain -= Time.deltaTime;
            if (_queuedCastRemain > 0f)
            {
                return;
            }
        }

        ResolveQueuedCastRelease();
    }

    private void ResolveQueuedCastRelease()
    {
        int skillIndex = _queuedSkillIndex;
        SkillData skill = _queuedSkill;
        CastContext context = _queuedContext;

        ClearQueuedCastRelease();

        if (skill == null || context == null)
        {
            return;
        }

        if (skill.IsChannelSkill())
        {
            StartChannel(skillIndex, skill, context);
            return;
        }

        ExecuteSkill(skillIndex, skill, context);
    }

    private bool StartChannel(int skillIndex, SkillData skill, CastContext context)
    {
        if (!ExecuteSkill(skillIndex, skill, context))
        {
            return false;
        }

        float channelDuration = ResolveChannelDuration(skill.channelDuration);
        if (channelDuration <= 0f)
        {
            ExecuteChannelEndEffects(skill, context);
            return true;
        }

        if (_actionCtrl != null)
        {
            CharActionReq req = new CharActionReq
            {
                type = CharActionType.Channel,
                state = CharActionState.Channeling,
                src = skill,
                dur = channelDuration,
                lockMove = skill.lockMoveDuringChannel,
                lockRotate = skill.lockRotateDuringChannel,
                interruptible = true,
                animKey = skill.GetCastAnimKey(),
            };

            if (!_actionCtrl.TryStart(req))
            {
                ExecuteChannelEndEffects(skill, context);
                return true;
            }
        }
        else if (_charCtrl != null)
        {
            _charCtrl.SetMovementLocked(skill.lockMoveDuringChannel);
        }

        _isChanneling = true;
        _channelSkillIndex = skillIndex;
        _channelSkill = skill;
        _channelContext = context;
        _channelRemain = channelDuration;
        _channelTickRemain = skill.channelTickInterval;

        if (skill.triggerChannelTickImmediately)
        {
            ExecuteChannelTick(skill, context);
        }

        return true;
    }

    private void UpdateChanneling()
    {
        if (!_isChanneling || _channelSkill == null || _channelContext == null)
        {
            return;
        }

        if (!CanCastByState())
        {
            EndChannel(true, true);
            return;
        }

        if (_actionCtrl == null)
        {
            _channelRemain -= Time.deltaTime;
            if (_channelRemain <= 0f)
            {
                EndChannel(false, false);
                return;
            }
        }
        else
        {
            _channelRemain = Mathf.Max(0f, _channelRemain - Time.deltaTime);
            if (!IsActiveChannelReq(_actionCtrl.CurReq))
            {
                EndChannel(false, false);
                return;
            }
        }

        float interval = _channelSkill.channelTickInterval;
        if (interval <= 0f || _channelSkill.channelTickEffects == null || _channelSkill.channelTickEffects.Count == 0)
        {
            return;
        }

        _channelTickRemain -= Time.deltaTime;
        while (_channelTickRemain <= 0f)
        {
            ExecuteChannelTick(_channelSkill, _channelContext);
            _channelTickRemain += interval;
        }
    }

    private void EndChannel(bool interrupted, bool controlAction)
    {
        SkillData skill = _channelSkill;
        CastContext context = _channelContext;
        bool unlockMovement = _actionCtrl == null && _charCtrl != null && skill != null && skill.lockMoveDuringChannel;
        bool shouldControlAction = controlAction && _actionCtrl != null && IsActiveChannelReq(_actionCtrl.CurReq);

        ClearChannelState();

        if (shouldControlAction)
        {
            if (interrupted)
            {
                _actionCtrl.Interrupt("channel");
            }
            else
            {
                _actionCtrl.EndCur();
            }
        }

        if (unlockMovement)
        {
            _charCtrl.SetMovementLocked(false);
        }

        ExecuteChannelEndEffects(skill, context);
    }

    private void ExecuteChannelTick(SkillData skill, CastContext context)
    {
        if (skill == null || context == null || skill.channelTickEffects == null || skill.channelTickEffects.Count == 0)
        {
            return;
        }

        SkillEffectUtility.ExecuteEffects(skill.channelTickEffects, context.Snapshot());
    }

    private void ExecuteChannelEndEffects(SkillData skill, CastContext context)
    {
        if (skill == null || context == null || skill.channelEndEffects == null || skill.channelEndEffects.Count == 0)
        {
            return;
        }

        SkillEffectUtility.ExecuteEffects(skill.channelEndEffects, context.Snapshot());
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
    }

    private void ClearQueuedCastRelease()
    {
        _hasQueuedCastRelease = false;
        _queuedSkillIndex = -1;
        _queuedSkill = null;
        _queuedContext = null;
        _queuedCastRemain = 0f;
    }

    private void ClearChannelState()
    {
        _isChanneling = false;
        _channelSkillIndex = -1;
        _channelSkill = null;
        _channelContext = null;
        _channelRemain = 0f;
        _channelTickRemain = 0f;
    }

    private bool ExecuteSkill(int skillIndex, SkillData skill, CastContext context)
    {
        if (skill == null || context == null)
        {
            return false;
        }

        if (!skill.Activate(context))
        {
            return false;
        }

        if (!CharResourceResolver.TrySpendEnergy(gameObject, skill.energyCost))
        {
            return false;
        }

        _skillCooldowns[skillIndex] = skill.cooldown;
        return true;
    }

    private void OnActionEnd(CharActionReq req)
    {
        if (req == null)
        {
            return;
        }

        if (_hasQueuedCastRelease && req.type == CharActionType.Cast && req.src == _queuedSkill)
        {
            ResolveQueuedCastRelease();
            return;
        }

        if (_isChanneling && req.type == CharActionType.Channel && req.src == _channelSkill)
        {
            EndChannel(false, false);
        }
    }

    private void OnActionInterrupted(CharActionReq req, string reason)
    {
        if (req == null)
        {
            return;
        }

        if (_hasQueuedCastRelease && req.type == CharActionType.Cast && req.src == _queuedSkill)
        {
            ClearQueuedCastRelease();
            return;
        }

        if (_isChanneling && req.type == CharActionType.Channel && req.src == _channelSkill)
        {
            EndChannel(true, false);
        }
    }

    private bool HasSkillFlowInProgress()
    {
        return _hasPendingCast || _hasQueuedCastRelease || _isChanneling || _heldGamepadSkill != null;
    }

    private bool IsPreviewing()
    {
        return _previewCtrl != null && _previewCtrl.IsPreviewing;
    }

    private bool IsUsingGamepadSkillMode()
    {
        return PlayerInputManager.instance != null
            && PlayerInputManager.instance.IsUsingGamepadInput
            && IsPlayerControlledForPreview();
    }

    private void BeginHeldGamepadSkill(int skillIndex)
    {
        if (_heldGamepadSkill != null || !CanCastByState())
        {
            return;
        }

        if (skillIndex < 0 || skillIndex >= skills.Count)
        {
            return;
        }

        SkillData skill = skills[skillIndex];
        if (skill == null || _skillCooldowns[skillIndex] > 0f)
        {
            return;
        }

        if (!CharResourceResolver.HasEnoughEnergy(gameObject, skill.energyCost))
        {
            return;
        }

        _heldGamepadSkillIndex = skillIndex;
        _heldGamepadSkill = skill;

        if (ShouldShowHeldGamepadPreview(skill) && _previewCtrl != null)
        {
            _previewCtrl.BeginPreview(skillIndex, skill, groundLayer);
        }
    }

    private bool UpdateHeldGamepadSkill()
    {
        if (_heldGamepadSkill == null)
        {
            return false;
        }

        if (_charCtrl == null || _charCtrl.Param == null || !CanCastByState())
        {
            ClearHeldGamepadSkill();
            return true;
        }

        if (_heldGamepadSkillIndex < 0 || _heldGamepadSkillIndex >= _charCtrl.Param.SkillInputStates.Count)
        {
            ClearHeldGamepadSkill();
            return true;
        }

        ButtonInputState heldState = _charCtrl.Param.SkillInputStates[_heldGamepadSkillIndex];
        if (heldState.isHeld)
        {
            if (ShouldShowHeldGamepadPreview(_heldGamepadSkill)
                && _previewCtrl != null
                && (!_previewCtrl.IsPreviewing || _previewCtrl.ActiveSkillIndex != _heldGamepadSkillIndex))
            {
                _previewCtrl.BeginPreview(_heldGamepadSkillIndex, _heldGamepadSkill, groundLayer);
            }

            return true;
        }

        int skillIndex = _heldGamepadSkillIndex;
        SkillData skill = _heldGamepadSkill;
        bool hasPreviewContext =
            _previewCtrl != null &&
            _previewCtrl.IsPreviewing &&
            _previewCtrl.ActiveSkillIndex == skillIndex &&
            _previewCtrl.CurrentContext != null;
        bool isPreviewValid = !hasPreviewContext || _previewCtrl.IsCurrentContextValid;
        bool modifierHeld = PlayerInputManager.instance != null &&
                            PlayerInputManager.instance.playerInputSkillModifierValue;
        TargetInfo releaseTarget = hasPreviewContext
            ? _previewCtrl.CurrentContext.rawTarget
            : TargetingUtil.Collect(_charCtrl, groundLayer);

        ClearHeldGamepadSkill();

        if (modifierHeld && (heldState.isUp || !heldState.isHeld) && isPreviewValid)
        {
            TryCastSkillWithTargetInfo(skillIndex, skill, releaseTarget);
        }

        return true;
    }

    private bool ShouldShowHeldGamepadPreview(SkillData skill)
    {
        if (skill == null)
        {
            return false;
        }

        return skill.targetingMode != SkillTargetMode.NoTarget || skill.useAimPreview;
    }

    private void ClearHeldGamepadSkill()
    {
        _heldGamepadSkillIndex = -1;
        _heldGamepadSkill = null;

        if (_previewCtrl != null && _previewCtrl.IsPreviewing)
        {
            _previewCtrl.CancelPreview(true);
        }
    }

    private bool ShouldUseAimPreview(SkillData skill)
    {
        return skill != null
            && skill.useAimPreview
            && IsPlayerControlledForPreview()
            && !IsUsingGamepadSkillMode();
    }

    private void ProcessPreviewInputs()
    {
        if (_previewCtrl == null || !_previewCtrl.IsPreviewing || _charCtrl == null || _charCtrl.Param == null)
        {
            return;
        }

        if (!IsPlayerControlledForPreview())
        {
            _previewCtrl.CancelPreview();
            return;
        }

        if (!CanCastByState())
        {
            _previewCtrl.CancelPreview();
            return;
        }

        if (IsPreviewConfirmPressed())
        {
            ConfirmPreviewCast();
            return;
        }

        if (IsPreviewCancelPressed())
        {
            _previewCtrl.CancelPreview(true);
            return;
        }

        int activeSkillIndex = _previewCtrl.ActiveSkillIndex;
        int inputCount = _charCtrl.Param.SkillInputDown.Count;
        for (int i = 0; i < inputCount; i++)
        {
            if (!_charCtrl.Param.SkillInputDown[i])
            {
                continue;
            }

            if (i == activeSkillIndex)
            {
                return;
            }

            _previewCtrl.CancelPreview();
            TryCastSkill(i);
            return;
        }
    }

    private void ConfirmPreviewCast()
    {
        if (_previewCtrl == null || !_previewCtrl.IsPreviewing)
        {
            return;
        }

        CastContext previewContext = _previewCtrl.CurrentContext;
        SkillData skill = _previewCtrl.ActiveSkill;
        int skillIndex = _previewCtrl.ActiveSkillIndex;
        bool isValid = _previewCtrl.IsCurrentContextValid;

        _previewCtrl.CancelPreview(true);

        if (!isValid || skill == null || previewContext == null)
        {
            return;
        }

        TryCastSkillWithTargetInfo(skillIndex, skill, previewContext.rawTarget);
    }

    private bool IsActiveChannelReq(CharActionReq req)
    {
        return req != null &&
               req.type == CharActionType.Channel &&
               req.src == _channelSkill;
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

    private bool IsPlayerControlledForPreview()
    {
        if (_blackBoard != null)
        {
            return _blackBoard.Identity.isPlayerControlled;
        }

        return _signalReader == null || _signalReader.IsPlayerControlled;
    }

    private bool IsPreviewConfirmPressed()
    {
        return Input.GetMouseButtonDown(0);
    }

    private bool IsPreviewCancelPressed()
    {
        return Input.GetMouseButtonDown(1);
    }

    private float ResolveCastPointDuration(float baseDuration)
    {
        if (baseDuration <= 0f)
        {
            return 0f;
        }

        float castSpeed = CharRuntimeResolver.GetCastSpeed(gameObject);
        return Mathf.Max(0f, baseDuration / Mathf.Max(castSpeed, 0.01f));
    }

    private static float ResolveChannelDuration(float baseDuration)
    {
        return Mathf.Max(0f, baseDuration);
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

        _blackBoard.Skills.pendingCast = _hasPendingCast || _hasQueuedCastRelease;
        _blackBoard.Skills.pendingSlot = _hasPendingCast ? _pendingSkillIndex : _queuedSkillIndex;
        _blackBoard.Skills.channeling = _isChanneling;
        _blackBoard.Skills.channelSlot = _channelSkillIndex;
        _blackBoard.Skills.channelRemain = _channelRemain;
        _blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Skills);
    }
}
