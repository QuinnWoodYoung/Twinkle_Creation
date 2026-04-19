using UnityEngine;

[DisallowMultipleComponent]
/// <summary>
/// 身体动画控制器。
/// 它只负责角色本体 Animator，不负责武器 prefab 上的独立动画。
/// </summary>
public class CharAnimCtrl : MonoBehaviour
{
    [System.Serializable]
    private sealed class WeaponAnimBinding
    {
        public WeaponType weaponType = WeaponType.None;
        public string layerName;
        public string locomotionBoolName;
    }

    [Header("Body Animator")]
    [SerializeField] private Animator _bodyAnim;

    [Header("Locomotion")]
    [Tooltip("角色是否拥有八向移动动画（xVelocity/zVelocity blend tree）。关闭后攻击时将锁定移动。")]
    [SerializeField] private bool _has8DirLocomotion = true;

    [Header("Movement Params")]
    [SerializeField] private string _xVelParam = "xVelocity";
    [SerializeField] private string _zVelParam = "zVelocity";
    [SerializeField] private float _moveDamp = 0.1f;

    [Header("Weapon Presentation")]
    [Tooltip("每个角色自己配置：某种武器启用哪个攻击层，以及默认移动姿态使用哪个 Bool。")]
    [SerializeField] private WeaponAnimBinding[] _weaponBindings;

    [Header("State Params")]
    [SerializeField] private string _deadBool = "dead";

    [Header("Action Triggers")]
    [SerializeField] private string _atkTrig = "Attack";
    [SerializeField] private string _castTrig = "Cast";
    [SerializeField] private string _shootCastTrig = "Shoot";
    [SerializeField] private string _buffCastTrig = "Buff";
    [SerializeField] private string _dashCastTrig = "Dash";
    [SerializeField] private string _hitTrig = "";
    [SerializeField] private string _channelBool = "Channeling";

    [Header("Status Layer")]
    [SerializeField] private bool _autoStatusAnim = true;
    [SerializeField] private string _stunBool = "StunState";
    [SerializeField] private string _sleepBool = "SleepState";
    [SerializeField] private string _rootBool = "RootState";
    [SerializeField] private string _silenceBool = "SilenceState";

    [Header("Weapon Sync")]
    [SerializeField] private bool _autoWeaponLayer = true;
    [SerializeField] private bool _autoWeaponBool = true;

    private CharActionCtrl _actionCtrl;
    private CharWeaponCtrl _weaponCtrl;
    private CharStatusCtrl _statusCtrl;
    private CharBlackBoard _blackBoard;

    public Animator BodyAnim => _bodyAnim;
    public bool Has8DirLocomotion => _has8DirLocomotion;

    private void Awake()
    {
        CacheRefs();
    }

    private void OnEnable()
    {
        CacheRefs();

        if (_actionCtrl != null)
        {
            _actionCtrl.ActionStart += OnActionStart;
        }

        if (_weaponCtrl != null)
        {
            _weaponCtrl.WeaponChanged += OnWeaponChanged;
            ApplyWeaponState(_weaponCtrl.CurWeapon);
        }

        if (_blackBoard != null)
        {
            _blackBoard.RuntimeChanged += OnBlackBoardChanged;
            PollBlackBoardState();
        }

        if (_statusCtrl != null)
        {
            _statusCtrl.SnapUpd += OnSnapUpd;
            ApplyStatusState(_statusCtrl.Snap);
        }
    }

    private void OnDisable()
    {
        if (_actionCtrl != null)
        {
            _actionCtrl.ActionStart -= OnActionStart;
        }

        if (_weaponCtrl != null)
        {
            _weaponCtrl.WeaponChanged -= OnWeaponChanged;
        }

        if (_blackBoard != null)
        {
            _blackBoard.RuntimeChanged -= OnBlackBoardChanged;
        }

        if (_statusCtrl != null)
        {
            _statusCtrl.SnapUpd -= OnSnapUpd;
        }
    }

    private void OnValidate()
    {
        CacheBodyAnim();
    }

    /// <summary>
    /// 写入移动动画参数。
    /// 八方向角色会区分前后左右，非八方向角色只保留前进强度。
    /// </summary>
    public void SetMove(Vector3 moveDir, bool canMove, bool isLock, Transform lockedTarget)
    {
        Animator animator = _bodyAnim;
        if (!HasAnimatorCtrl(animator))
        {
            return;
        }

        Vector3 animMoveDir = canMove ? moveDir : Vector3.zero;
        Vector3 animDir = animMoveDir.sqrMagnitude > 0.001f ? animMoveDir.normalized : Vector3.zero;
        if (!_has8DirLocomotion)
        {
            // 非八向角色：始终面朝移动方向，只需驱动前进/停止，无侧移
            SetFloatSafe(animator, _xVelParam, 0f);
            SetFloatSafe(animator, _zVelParam, animDir.magnitude);
            return;
        }

        // 八向角色：非锁定时只写前进分量
        if (!isLock || lockedTarget == null)
        {
            SetFloatSafe(animator, _xVelParam, 0f);
            SetFloatSafe(animator, _zVelParam, Vector3.Dot(animDir, transform.forward));
            return;
        }

        // 八向角色 + 锁定：完整的侧移/前进 blend
        SetFloatSafe(animator, _xVelParam, Vector3.Dot(animDir, transform.right));
        SetFloatSafe(animator, _zVelParam, Vector3.Dot(animDir, transform.forward));
    }

    public void SetDead(bool isDead)
    {
        if (HasAnimatorCtrl(_bodyAnim) && HasParam(_bodyAnim, _deadBool, AnimatorControllerParameterType.Bool))
        {
            _bodyAnim.SetBool(_deadBool, isDead);
        }
    }

    public void PlayAtk(string trig = null)
    {
        string finalTrig = string.IsNullOrEmpty(trig) ? ResolveDefaultTrigger(_atkTrig, "Attack") : trig;
        SetTriggerSafe(_bodyAnim, finalTrig);
    }

    public void PlayHit(string trig = null)
    {
        string finalTrig = string.IsNullOrEmpty(trig) ? ResolveDefaultTrigger(_hitTrig, string.Empty) : trig;
        SetTriggerSafe(_bodyAnim, finalTrig);
    }

    public void PlayCast(string trig = null)
    {
        string finalTrig = ResolveCastTrig(trig);
        SetTriggerSafe(_bodyAnim, finalTrig);
    }

    public void ApplyWeaponState(WeaponType weaponType)
    {
        if (_autoWeaponLayer)
        {
            ApplyWeaponLayer(_bodyAnim, weaponType);
        }

        if (_autoWeaponBool)
        {
            ApplyWeaponBool(_bodyAnim, weaponType);
        }
    }

    /// <summary>
    /// 动作开始时，根据动作类型触发攻击/施法/受击动画。
    /// </summary>
    private void OnActionStart(CharActionReq req)
    {
        if (req == null)
        {
            return;
        }

        switch (req.type)
        {
            case CharActionType.Atk:
                PlayAtk(req.animKey);
                break;

            case CharActionType.Cast:
                PlayCast(req.animKey);
                break;

            case CharActionType.HitReact:
                PlayHit(req.animKey);
                break;
        }
    }

    private void OnWeaponChanged(WeaponType weaponType)
    {
        ApplyWeaponState(weaponType);
    }

    private void OnSnapUpd(CharStateSnap snap)
    {
        ApplyStatusState(snap);
    }

    private void OnBlackBoardChanged(CharBlackBoard board, CharBlackBoardChangeMask changeMask)
    {
        if (board != _blackBoard)
        {
            return;
        }

        if ((changeMask & (CharBlackBoardChangeMask.Action | CharBlackBoardChangeMask.Resources | CharBlackBoardChangeMask.Status | CharBlackBoardChangeMask.Equipment)) == 0)
        {
            return;
        }

        PollBlackBoardState();
    }

    private void ApplyWeaponLayer(Animator animator, WeaponType weaponType)
    {
        if (!HasAnimatorCtrl(animator))
        {
            return;
        }

        string[] weaponLayers = GetConfiguredWeaponLayerNames();
        if (weaponLayers.Length == 0)
        {
            return;
        }

        string activeLayer = ResolveWeaponLayerName(weaponType);

        for (int i = 0; i < weaponLayers.Length; i++)
        {
            string layerName = weaponLayers[i];
            if (string.IsNullOrEmpty(layerName) || IsDuplicateName(weaponLayers, i))
            {
                continue;
            }

            int layerIndex = animator.GetLayerIndex(layerName);
            if (layerIndex < 0)
            {
                continue;
            }

            animator.SetLayerWeight(layerIndex, layerName == activeLayer ? 1f : 0f);
        }
    }

    private void ApplyWeaponBool(Animator animator, WeaponType weaponType)
    {
        if (!HasAnimatorCtrl(animator))
        {
            return;
        }

        string[] locomotionBools = GetConfiguredLocomotionBoolNames();
        if (locomotionBools.Length == 0)
        {
            return;
        }

        string activeBool = ResolveLocomotionBoolName(weaponType);
        for (int i = 0; i < locomotionBools.Length; i++)
        {
            string boolName = locomotionBools[i];
            if (string.IsNullOrEmpty(boolName) || IsDuplicateName(locomotionBools, i))
            {
                continue;
            }

            if (HasParam(animator, boolName, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(boolName, boolName == activeBool);
            }
        }
    }

    private string ResolveCastTrig(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return ResolveDefaultTrigger(_castTrig, "Cast");
        }

        switch (key)
        {
            case "Cast":
                return ResolveDefaultTrigger(_castTrig, "Cast");
            case "Shoot":
                return ResolveDefaultTrigger(_shootCastTrig, "Shoot");
            case "Buff":
                return ResolveDefaultTrigger(_buffCastTrig, "Buff");
            case "Dash":
                return ResolveDefaultTrigger(_dashCastTrig, "Dash");
            default:
                return key;
        }
    }

    private static string ResolveDefaultTrigger(string configured, string fallback)
    {
        return string.IsNullOrEmpty(configured) ? fallback : configured;
    }

    /// <summary>
    /// 根据状态快照写入身体 Animator 的状态层布尔值。
    /// </summary>
    private void ApplyStatusState(CharStateSnap snap)
    {
        if (!_autoStatusAnim)
        {
            return;
        }

        Animator animator = _bodyAnim;
        if (!HasAnimatorCtrl(animator))
        {
            return;
        }

        CharStateTag domTag = snap != null ? snap.domCtrlTag : CharStateTag.None;
        if (domTag == CharStateTag.Dead)
        {
            domTag = CharStateTag.None;
        }

        bool isSoftCtrlAction =
            snap != null &&
            snap.actionState != CharActionState.None &&
            snap.actionState != CharActionState.Idle &&
            snap.actionState != CharActionState.Moving;

        bool useStun = domTag == CharStateTag.Stun;
        bool useSleep = domTag == CharStateTag.Sleep;
        bool useRoot = domTag == CharStateTag.Root && !isSoftCtrlAction;
        bool useSilence = domTag == CharStateTag.Silence && !isSoftCtrlAction;

        SetBoolSafe(animator, _stunBool, useStun);
        SetBoolSafe(animator, _sleepBool, useSleep);
        SetBoolSafe(animator, _rootBool, useRoot);
        SetBoolSafe(animator, _silenceBool, useSilence);
    }

    private void ApplyActionState(CharActionSlice action)
    {
        if (!HasAnimatorCtrl(_bodyAnim))
        {
            return;
        }

        bool isChanneling = action != null && action.state == CharActionState.Channeling;
        SetBoolSafe(_bodyAnim, _channelBool, isChanneling);
    }

    private void SetFloatSafe(Animator animator, string paramName, float value)
    {
        if (HasAnimatorCtrl(animator) && HasParam(animator, paramName, AnimatorControllerParameterType.Float))
        {
            animator.SetFloat(paramName, value, _moveDamp, Time.deltaTime);
        }
    }

    private void SetBoolSafe(Animator animator, string paramName, bool value)
    {
        if (HasAnimatorCtrl(animator) && HasParam(animator, paramName, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(paramName, value);
        }
    }

    private void SetTriggerSafe(Animator animator, string trig)
    {
        if (HasAnimatorCtrl(animator) && HasParam(animator, trig, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(trig);
        }
    }

    private bool HasParam(Animator animator, string paramName, AnimatorControllerParameterType paramType)
    {
        if (!HasAnimatorCtrl(animator) || string.IsNullOrEmpty(paramName))
        {
            return false;
        }

        for (int i = 0; i < animator.parameterCount; i++)
        {
            AnimatorControllerParameter param = animator.parameters[i];
            if (param.type == paramType && param.name == paramName)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasAnimatorCtrl(Animator animator)
    {
        return animator != null && animator.runtimeAnimatorController != null;
    }

    private string ResolveWeaponLayerName(WeaponType weaponType)
    {
        WeaponAnimBinding binding = FindWeaponBinding(weaponType);
        return binding != null ? binding.layerName : string.Empty;
    }

    private string ResolveLocomotionBoolName(WeaponType weaponType)
    {
        WeaponAnimBinding binding = FindWeaponBinding(weaponType);
        return binding != null ? binding.locomotionBoolName : string.Empty;
    }

    private WeaponAnimBinding FindWeaponBinding(WeaponType weaponType)
    {
        if (_weaponBindings == null)
        {
            return null;
        }

        for (int i = 0; i < _weaponBindings.Length; i++)
        {
            WeaponAnimBinding binding = _weaponBindings[i];
            if (binding != null && binding.weaponType == weaponType)
            {
                return binding;
            }
        }

        return null;
    }

    private string[] GetConfiguredWeaponLayerNames()
    {
        if (_weaponBindings == null || _weaponBindings.Length == 0)
        {
            return new string[0];
        }

        System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>(_weaponBindings.Length);
        for (int i = 0; i < _weaponBindings.Length; i++)
        {
            WeaponAnimBinding binding = _weaponBindings[i];
            if (binding != null && !string.IsNullOrEmpty(binding.layerName))
            {
                result.Add(binding.layerName);
            }
        }

        return result.ToArray();
    }

    private string[] GetConfiguredLocomotionBoolNames()
    {
        if (_weaponBindings == null || _weaponBindings.Length == 0)
        {
            return new string[0];
        }

        System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>(_weaponBindings.Length);
        for (int i = 0; i < _weaponBindings.Length; i++)
        {
            WeaponAnimBinding binding = _weaponBindings[i];
            if (binding != null && !string.IsNullOrEmpty(binding.locomotionBoolName))
            {
                result.Add(binding.locomotionBoolName);
            }
        }

        return result.ToArray();
    }

    private bool IsDuplicateName(string[] names, int index)
    {
        string name = names[index];
        for (int i = 0; i < index; i++)
        {
            if (names[i] == name)
            {
                return true;
            }
        }

        return false;
    }

    private void CacheRefs()
    {
        CacheBodyAnim();

        if (_blackBoard == null)
        {
            _blackBoard = GetComponent<CharBlackBoard>();
        }

        if (_actionCtrl == null)
        {
            _actionCtrl = GetComponent<CharActionCtrl>();
        }

        if (_weaponCtrl == null)
        {
            _weaponCtrl = GetComponent<CharWeaponCtrl>();
        }

        if (_statusCtrl == null)
        {
            _statusCtrl = GetComponent<CharStatusCtrl>();
        }
    }

    /// <summary>
    /// 从黑板拉取装备、状态和死亡结果来刷新表现。
    /// </summary>
    private void PollBlackBoardState()
    {
        if (_blackBoard == null)
        {
            return;
        }

        // Animation is a pure reader here: it only consumes folded runtime data.
        if (_blackBoard.Features.useEquipment)
        {
            ApplyWeaponState(_blackBoard.Equipment.weaponType);
        }

        if (_blackBoard.Features.useStatus)
        {
            ApplyStatusState(_blackBoard.Status.snapshot);
        }

        ApplyActionState(_blackBoard.Action);

        bool isDead = _blackBoard.Action.isDead;
        if (_blackBoard.Features.useResources && _blackBoard.Resources.hasHealth)
        {
            isDead = _blackBoard.Resources.hp <= 0f;
        }

        SetDead(isDead);
    }

    private void CacheBodyAnim()
    {
        if (_bodyAnim != null && !IsWeaponAnimator(_bodyAnim))
        {
            return;
        }

        Animator[] animators = GetComponentsInChildren<Animator>(true);
        if (animators == null || animators.Length == 0)
        {
            _bodyAnim = null;
            return;
        }

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator != null && animator.avatar != null && !IsWeaponAnimator(animator))
            {
                _bodyAnim = animator;
                return;
            }
        }

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator != null && !IsWeaponAnimator(animator))
            {
                _bodyAnim = animator;
                return;
            }
        }

        _bodyAnim = animators[0];
    }

    private bool IsWeaponAnimator(Animator animator)
    {
        return animator != null && animator.GetComponentInParent<WeaponAnimCtrl>() != null;
    }
}
