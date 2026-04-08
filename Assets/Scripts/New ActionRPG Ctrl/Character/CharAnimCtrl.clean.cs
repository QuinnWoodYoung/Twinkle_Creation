using UnityEngine;
#if false

[DisallowMultipleComponent]
public class CharAnimCtrl : MonoBehaviour
{
    [Header("本体 Animator")]
    [Tooltip("角色本体使用的 Animator。留空时会自动查找，并尽量跳过武器 Animator。")]
    [SerializeField] private Animator _bodyAnim;

    [Header("移动参数")]
    [Tooltip("横向移动参数名。")]
    [SerializeField] private string _xVelParam = "xVelocity";
    [Tooltip("纵向移动参数名。")]
    [SerializeField] private string _zVelParam = "zVelocity";
    [Tooltip("移动参数阻尼。")]
    [SerializeField] private float _moveDamp = 0.1f;

    [Header("状态参数")]
    [Tooltip("死亡 Bool 参数名。")]
    [SerializeField] private string _deadBool = "dead";
    [Tooltip("近战武器姿态 Bool 参数名。当前项目中 Sword / Axe 会写入 true。")]
    [SerializeField] private string _swordBool = "isSword";
    [Tooltip("弓武器姿态 Bool 参数名。当前项目中 Bow 会写入 true。")]
    [SerializeField] private string _archerBool = "isArcher";

    [Header("动作 Trigger")]
    [Tooltip("普通攻击默认 Trigger。动作请求未指定 animKey 时使用它。")]
    [SerializeField] private string _atkTrig = "Attack";
    [Tooltip("受击默认 Trigger。动作请求未指定 animKey 时使用它。")]
    [SerializeField] private string _hitTrig = "";

    [Header("武器姿态同步")]
    [Tooltip("启用后，会按 WeaponType 自动切换角色 Animator Layer。")]
    [SerializeField] private bool _autoWeaponLayer = true;
    [Tooltip("启用后，会按 WeaponType 自动同步角色 Animator Bool。")]
    [SerializeField] private bool _autoWeaponBool = true;

    private CharActionCtrl _actionCtrl;
    private CharWeaponCtrl _weaponCtrl;

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
    }

    private void OnValidate()
    {
        CacheBodyAnim();
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

        // 非锁定视角下只关心前后移动，但这里仍主动把横向速度清零，避免残留旧值。
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

            case CharActionType.HitReact:
                PlayHit(req.animKey);
                break;
        }
    }

    private void OnWeaponChanged(WeaponType weaponType)
    {
        ApplyWeaponState(weaponType);
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

    private void SetFloatSafe(Animator animator, string paramName, float value)
    {
        if (HasAnimatorCtrl(animator) && HasParam(animator, paramName, AnimatorControllerParameterType.Float))
        {
            animator.SetFloat(paramName, value, _moveDamp, Time.deltaTime);
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

        if (_actionCtrl == null)
        {
            _actionCtrl = GetComponent<CharActionCtrl>();
        }

        if (_weaponCtrl == null)
        {
            _weaponCtrl = GetComponent<CharWeaponCtrl>();
        }
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
#endif
