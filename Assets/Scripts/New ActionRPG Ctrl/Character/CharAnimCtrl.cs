using UnityEngine;

[DisallowMultipleComponent]
public class CharAnimCtrl : MonoBehaviour
{
    [Header("Body Animator")]
    [SerializeField] private Animator _bodyAnim;

    [Header("Movement Params")]
    [SerializeField] private string _xVelParam = "xVelocity";
    [SerializeField] private string _zVelParam = "zVelocity";
    [SerializeField] private float _moveDamp = 0.1f;

    [Header("State Params")]
    [SerializeField] private string _deadBool = "dead";
    [SerializeField] private string _swordBool = "isSword";
    [SerializeField] private string _archerBool = "isArcher";

    [Header("Action Triggers")]
    [SerializeField] private string _atkTrig = "Attack";
    [SerializeField] private string _castTrig = "Cast";
    [SerializeField] private string _shootCastTrig = "Shoot";
    [SerializeField] private string _buffCastTrig = "Buff";
    [SerializeField] private string _dashCastTrig = "Dash";
    [SerializeField] private string _hitTrig = "";

    [Header("Attack Params")]
    [SerializeField] private string _meleeRepeatSpeedParam = "meleeSpeed";

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

    private void LateUpdate()
    {
        // Blackboard events are now the preferred sync path.
    }

    public void SetMove(Vector3 moveDir, bool canMove, bool isLock, Transform lockedTarget)
    {
        Animator animator = _bodyAnim;
        if (!HasAnimatorCtrl(animator))
        {
            return;
        }

        Vector3 animMoveDir = canMove ? moveDir : Vector3.zero;
        Vector3 animDir = animMoveDir.sqrMagnitude > 0.001f ? animMoveDir.normalized : Vector3.zero;

        // In free-look mode only the forward component matters.
        if (!isLock && lockedTarget != null)
        {
            SetFloatSafe(animator, _xVelParam, 0f);
            SetFloatSafe(animator, _zVelParam, Vector3.Dot(animDir, transform.forward));
            return;
        }

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
        string finalTrig = string.IsNullOrEmpty(trig) ? _atkTrig : trig;
        SetTriggerSafe(_bodyAnim, finalTrig);
    }

    public void PlayHit(string trig = null)
    {
        string finalTrig = string.IsNullOrEmpty(trig) ? _hitTrig : trig;
        SetTriggerSafe(_bodyAnim, finalTrig);
    }

    public void PlayCast(string trig = null)
    {
        string finalTrig = ResolveCastTrig(trig);
        SetTriggerSafe(_bodyAnim, finalTrig);
    }

    public bool SetMeleeRepeatSpeed(float speed)
    {
        Animator animator = _bodyAnim;
        if (!HasAnimatorCtrl(animator) || !HasParam(animator, _meleeRepeatSpeedParam, AnimatorControllerParameterType.Float))
        {
            return false;
        }

        animator.SetFloat(_meleeRepeatSpeedParam, Mathf.Max(0.01f, speed));
        return true;
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

        string activeLayer = Weapon.GetAnimLayerName(weaponType);
        for (int i = 0; i < Weapon.AnimLayers.Length; i++)
        {
            string layerName = Weapon.AnimLayers[i];
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

        bool isSword = weaponType == WeaponType.Sword || weaponType == WeaponType.Axe;
        bool isArcher = weaponType == WeaponType.Bow;

        if (HasParam(animator, _swordBool, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(_swordBool, isSword);
        }

        if (HasParam(animator, _archerBool, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(_archerBool, isArcher);
        }
    }

    private string ResolveCastTrig(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return _castTrig;
        }

        switch (key)
        {
            case "Cast":
                return _castTrig;
            case "Shoot":
                return _shootCastTrig;
            case "Buff":
                return _buffCastTrig;
            case "Dash":
                return _dashCastTrig;
            default:
                return key;
        }
    }

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
