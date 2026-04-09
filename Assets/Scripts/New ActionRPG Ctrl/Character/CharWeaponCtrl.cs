using UnityEngine;

[DisallowMultipleComponent]
public class CharWeaponCtrl : MonoBehaviour
{
    // Fired after the logical weapon state is fully synchronized.
    public event System.Action<WeaponType> WeaponChanged;

    private CharCtrl _charCtrl;
    private CharAnimCtrl _animCtrl;
    private WeaponVisualCtrl _weaponVisualCtrl;
    private CharActionCtrl _actionCtrl;
    private CharBlackBoard _blackBoard;
    private WeaponType _lastWeapon = (WeaponType)(-1);
    private float _attackCooldownRemain;
    private BasicAttackMode _resolvedAttackMode = BasicAttackMode.MeleeRepeat;
    private int _comboIndex;
    private float _comboResetRemain;
    private bool _isChargingBasicAttack;
    private float _chargeHoldTime;

    [SerializeField] private Transform _weaponRoot;
    [SerializeField] private Animator _weaponAnimator;
    [SerializeField] private WeaponAnimCtrl _weaponAnimCtrl;
    [SerializeField] public WeaponType _currentWeapon;

    public WeaponType CurWeapon => _currentWeapon;

    [Header("Legacy Projectile Fallback")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private string _projectileSpawnPointName = "Arrow Create";

    [Header("Debug")]
    [SerializeField] private bool _debugWeaponAnim = true;

    [Header("Light Attack")]
    [SerializeField] private string _lightAtkTrig = "Attack";
    [SerializeField] private string _comboAtkTrig = "Attack";
    [SerializeField] private string _meleeRepeatTrig = "melee";
    [SerializeField] private float _lightAtkDur = 0.35f;
    [SerializeField] private bool _lightAtkLockMove = true;
    [SerializeField] private bool _lightAtkLockRotate;


    [Header("Weapon Animation")]
    [SerializeField] private bool _autoWeaponCfg = true;

    [Header("Attack Input")]
    [Tooltip("近战默认按住持续普攻，偏向 Hades 的手感。")]
    [SerializeField] private bool _repeatMeleeAttackWhileHeld = true;
    [Tooltip("远程默认按住持续普攻。松开后立刻停止后续连射。")]
    [SerializeField] private bool _repeatRangedAttackWhileHeld = true;

    [Header("Melee Assist")]
    [Tooltip("近战在攻击开始前，轻微朝最近目标修正朝向。")]
    [SerializeField] private bool _useMeleeTargetAssist = true;
    [Tooltip("近战目标辅助的搜索角度。")]
    [SerializeField] private float _meleeAssistAngle = 40f;
    [Tooltip("近战 repeat 模式里，位移锁持续到一次攻击周期的哪个比例。1 表示整段周期都锁住。")]
    [SerializeField] private float _meleeRepeatLockMoveFraction = 0.75f;
    [Tooltip("近战 repeat 优先使用角色 Animator 的 meleeSpeed 参数，而不是全局 Animator.speed。")]
    [SerializeField] private bool _useBodyMeleeSpeedParam = true;
    [Tooltip("长按 repeat 时，动作结束后至少保留这么久的可移动空窗，避免同帧续刀吞掉位移。")]
    [SerializeField] private float _meleeRepeatHoldRestartDelay = 0.02f;
    [Tooltip("近战 repeat 链中如果正在移动，则不驱动 locomotion 动画，只保留纯位移。")]
    [SerializeField] private bool _suppressMoveAnimDuringMeleeRepeat = true;
    [Tooltip("近战 repeat 输入断开后，仍然保留多久的盯鼠标状态，用来把连按和长按统一成同一段连打意图。")]
    [SerializeField] private float _meleeRepeatAimGraceTime = 0.16f;
    [Tooltip("近战 combo 链中如果正在移动，则不驱动 locomotion 动画，只保留纯位移。")]
    [SerializeField] private bool _suppressMoveAnimDuringMeleeCombo = true;
    [Tooltip("近战 combo 两次点击之间，仍然保留多久的盯鼠标状态。")]
    [SerializeField] private float _meleeComboAimGraceTime = 0.16f;

    [Header("Ranged Targeting")]
    [Tooltip("远程普攻默认瞄准模式。FreeAim 为纯自由攻击，SoftLock 为方向附近软索敌。")]
    [SerializeField] private BasicAttackTargetingMode _rangedTargetingMode = BasicAttackTargetingMode.SoftLock;
    [Tooltip("存在硬锁定目标时，远程普攻优先攻击锁定目标。")]
    [SerializeField] private bool _preferLockedTarget = true;
    [Tooltip("软索敌允许的最大偏角。")]
    [SerializeField] private float _rangedAssistAngle = 25f;
    [Tooltip("远程普攻在发射前是否强制面向攻击方向。")]
    [SerializeField] private bool _faceAttackDirection = true;
    [Tooltip("近战 repeat 且允许移动时，移动中不再每刀强制抢朝向，避免边走边砍时抽搐。")]
    [SerializeField] private bool _suppressRepeatFaceWhileMoving = true;
    [Tooltip("用于朝向修正的短暂锁向时长。")]
    [SerializeField] private float _attackFaceLockDuration = 0.08f;

    private float _activeAttackAnimSpeed = 1f;
    private bool _keepRepeatAttackAnimSpeed;
    private float _meleeRepeatHoldRestartBlockRemain;
    private float _meleeRepeatAimGraceRemain;
    private Vector3 _meleeRepeatAimDirection = Vector3.forward;
    private float _meleeComboAimGraceRemain;
    private Vector3 _meleeComboAimDirection = Vector3.forward;

    protected void Awake()
    {
        _charCtrl = GetComponent<CharCtrl>();
        _animCtrl = GetComponent<CharAnimCtrl>();
        _weaponVisualCtrl = GetComponent<WeaponVisualCtrl>();
        _actionCtrl = GetComponent<CharActionCtrl>();
        _blackBoard = GetComponent<CharBlackBoard>();

        if (_animCtrl == null)
        {
            _animCtrl = gameObject.AddComponent<CharAnimCtrl>();
        }

        CacheWeaponRoot();
        CacheWeaponAnimator();
        CacheWeaponAnimCtrl();
        SyncWeaponCfg();
    }

    private void Start()
    {
        RefreshWeaponAnim();
        SyncWeaponCfg();
        SyncBlackBoardWeapon();
    }

    private void OnEnable()
    {
        if (_actionCtrl == null)
        {
            _actionCtrl = GetComponent<CharActionCtrl>();
        }

        if (_actionCtrl != null)
        {
            _actionCtrl.ActionStart += OnActionStart;
            _actionCtrl.ActionEnd += OnActionEnd;
            _actionCtrl.ActionIntd += OnActionInterrupted;
        }
    }

    private void OnDisable()
    {
        if (_actionCtrl != null)
        {
            _actionCtrl.ActionStart -= OnActionStart;
            _actionCtrl.ActionEnd -= OnActionEnd;
            _actionCtrl.ActionIntd -= OnActionInterrupted;
        }
    }

    private void Update()
    {
        if (_attackCooldownRemain > 0f)
        {
            _attackCooldownRemain -= Time.deltaTime;
        }

        if (_meleeRepeatHoldRestartBlockRemain > 0f)
        {
            _meleeRepeatHoldRestartBlockRemain -= Time.deltaTime;
        }

        if (_meleeRepeatAimGraceRemain > 0f)
        {
            _meleeRepeatAimGraceRemain -= Time.deltaTime;
        }

        if (_meleeComboAimGraceRemain > 0f)
        {
            _meleeComboAimGraceRemain -= Time.deltaTime;
        }

        if (_comboResetRemain > 0f)
        {
            _comboResetRemain -= Time.deltaTime;
            if (_comboResetRemain <= 0f)
            {
                _comboResetRemain = 0f;
                _comboIndex = 0;
            }
        }

        SyncResolvedAttackProfile();

        if (_lastWeapon != _currentWeapon)
        {
            SyncWeaponCfg();
            NotifyWeaponChanged();
        }

        UpdateAtkInput_New();
        UpdateRepeatAttackAnimSpeedState();
    }

    private void UpdateAtkInput_New()
    {
        if (_charCtrl == null || _charCtrl.Param == null)
        {
            return;
        }

        AttackInputState input = _charCtrl.Param.AttackState;
        AttackData_SO attackProfile = ResolveAttackProfile();

        switch (_resolvedAttackMode)
        {
            case BasicAttackMode.MeleeCombo:
                if (input.isDown)
                {
                    TryComboAtk_New(attackProfile);
                }

                return;

            case BasicAttackMode.RangedChargeRelease:
                UpdateChargeReleaseInput(input, attackProfile);
                return;
        }

        if (ShouldRepeatAttackWhileHeld(_resolvedAttackMode))
        {
            if (!input.isHeld && !input.isDown)
            {
                return;
            }

            if (_resolvedAttackMode == BasicAttackMode.MeleeRepeat &&
                !input.isDown &&
                _meleeRepeatHoldRestartBlockRemain > 0f)
            {
                return;
            }

            TryLightAtk_New(attackProfile, ResolveDefaultAttackAnimKey(attackProfile));
            return;
        }

        if (input.isDown)
        {
            TryLightAtk_New(attackProfile, ResolveDefaultAttackAnimKey(attackProfile));
        }
    }

    private void OnActionStart(CharActionReq req)
    {
        if (req == null || req.type != CharActionType.Atk || req.src != this)
        {
            return;
        }

        // Presentation waits until the action request is really accepted.
        PlayAtk_New(req.animKey);
    }

    private void OnActionEnd(CharActionReq req)
    {
        if (req == null || req.type != CharActionType.Atk || req.src != this)
        {
            return;
        }

        if (_resolvedAttackMode == BasicAttackMode.MeleeRepeat)
        {
            _meleeRepeatHoldRestartBlockRemain = Mathf.Max(
                _meleeRepeatHoldRestartBlockRemain,
                Mathf.Max(_attackCooldownRemain, _meleeRepeatHoldRestartDelay));
            return;
        }

        ResetAttackAnimSpeed();
    }

    private void OnActionInterrupted(CharActionReq req, string reason)
    {
        if (req == null || req.type != CharActionType.Atk || req.src != this)
        {
            return;
        }

        _keepRepeatAttackAnimSpeed = false;
        _meleeRepeatHoldRestartBlockRemain = 0f;
        _meleeRepeatAimGraceRemain = 0f;
        _meleeComboAimGraceRemain = 0f;
        ResetAttackAnimSpeed();
    }

    private bool TryLightAtk_New(AttackData_SO attackProfile, string animKey = null)
    {
        string finalAnimKey = string.IsNullOrEmpty(animKey) ? _lightAtkTrig : animKey;
        float attackCycleDuration = ResolveAttackCycleDuration(attackProfile, _resolvedAttackMode);
        float attackDuration = ResolveActionDuration(attackProfile, _resolvedAttackMode);
        bool lockMove = _autoWeaponCfg ? !CanMoveOnAtkByWeapon() : _lightAtkLockMove;
        if (!TryStartAtk_New(finalAnimKey, attackDuration, lockMove, _lightAtkLockRotate, true, false, attackCycleDuration))
        {
            return false;
        }

        if (_actionCtrl == null)
        {
            PlayAtk_New(finalAnimKey);
        }

        return true;
    }

    private bool TryStartAtk_New(
        string animKey,
        float dur,
        bool lockMove,
        bool lockRotate,
        bool applyCooldown = true,
        bool allowReplaceActiveAttack = false,
        float cooldownFallbackDuration = -1f)
    {
        if (!CanAtkByState_New())
        {
            return false;
        }

        // 只有在动作控制器真的处于非 Idle 状态时，才认为当前被动作占用。
        // 这样可以兜住场景序列化残留造成的“CurReq 非空但实际上空闲”的情况。
        if (_actionCtrl != null &&
            _actionCtrl.CurReq != null &&
            _actionCtrl.State != CharActionState.Idle &&
            !CanReplaceActiveAttack(allowReplaceActiveAttack))
        {
            return false;
        }

        if (applyCooldown && _attackCooldownRemain > 0f)
        {
            return false;
        }

        if (_actionCtrl == null)
        {
            if (applyCooldown)
            {
                BeginAttackCooldown(cooldownFallbackDuration >= 0f ? cooldownFallbackDuration : dur);
            }
            return true;
        }

        float actionDuration = Mathf.Max(0.01f, dur);

        CharActionReq req = new CharActionReq
        {
            type = CharActionType.Atk,
            state = CharActionState.AtkWindup,
            src = this,
            dur = actionDuration,
            lockMove = lockMove,
            lockRotate = lockRotate,
            interruptible = true,
            animKey = animKey,
        };

        if (!_actionCtrl.TryStart(req))
        {
            return false;
        }

        if (applyCooldown)
        {
            BeginAttackCooldown(cooldownFallbackDuration >= 0f ? cooldownFallbackDuration : actionDuration);
        }
        return true;
    }

    private bool CanAtkByState_New()
    {
        return CharRuntimeResolver.CanAttack(gameObject);
    }

    private void PlayAtk_New(string trig)
    {
        AttackData_SO attackProfile = ResolveAttackProfile();
        ApplyAttackAnimSpeed(attackProfile, _resolvedAttackMode);
        if (_resolvedAttackMode == BasicAttackMode.MeleeRepeat)
        {
            _keepRepeatAttackAnimSpeed = true;
        }

        BasicAttackTargetInfo targetInfo = ResolveBasicAttackTarget(attackProfile, _resolvedAttackMode);
        UpdateBlackBoardAttackTarget(targetInfo);

        if (ShouldForceFaceAttackDirection(targetInfo))
        {
            _charCtrl.ForceBasicAttackFaceDirection(
                targetInfo.attackDirection,
                ResolveAttackFaceLockDuration(attackProfile),
                true);
        }

        if (_weaponAnimCtrl == null)
        {
            RefreshWeaponAnim();
            SetAttackAnimSpeed(_activeAttackAnimSpeed);
        }

        if (_weaponAnimCtrl != null)
        {
            if (_debugWeaponAnim)
            {
                Debug.Log($"[CharWeaponCtrl] PlayAtk trig={trig} root={_weaponRoot?.name} weaponAnim={_weaponAnimCtrl.name} curWeapon={_currentWeapon}", this);
            }

            _weaponAnimCtrl.PlayAtk(trig);
        }
        else if (_debugWeaponAnim)
        {
            Debug.LogWarning($"[CharWeaponCtrl] PlayAtk failed, no WeaponAnimCtrl. root={_weaponRoot?.name} animator={_weaponAnimator?.name} curWeapon={_currentWeapon}", this);
        }

        if (IsRangedAttackMode(_resolvedAttackMode))
        {
            Shoot(targetInfo, attackProfile, _resolvedAttackMode);
        }
    }

    public void SetWeapon(WeaponType weaponType)
    {
        if (_currentWeapon == weaponType)
        {
            return;
        }

        _currentWeapon = weaponType;
        SyncWeaponCfg();
        SyncBlackBoardWeapon();
        NotifyWeaponChanged();
    }

    public void BindWeaponRoot(Transform weaponRoot)
    {
        _weaponRoot = weaponRoot;
        _weaponAnimator = null;
        _weaponAnimCtrl = null;

        CacheWeaponAnimator();
        CacheWeaponAnimCtrl();

        if (_weaponAnimCtrl != null)
        {
            _weaponAnimCtrl.Bind(gameObject, _currentWeapon);
        }

        if (_debugWeaponAnim)
        {
            Debug.Log($"[CharWeaponCtrl] BindWeaponRoot root={_weaponRoot?.name} anim={_weaponAnimator?.name} ctrl={_weaponAnimCtrl?.name} curWeapon={_currentWeapon}", this);
        }

        SyncBlackBoardWeapon();
    }

    public void ClearWeaponRoot()
    {
        if (_weaponAnimCtrl != null)
        {
            _weaponAnimCtrl.Unbind();
        }

        _weaponRoot = null;
        _weaponAnimator = null;
        _weaponAnimCtrl = null;
        SyncBlackBoardWeapon();
    }

    public void RefreshWeaponAnim()
    {
        CacheWeaponRoot();
        if (_weaponRoot == null)
        {
            _weaponAnimator = null;
            _weaponAnimCtrl = null;
            SyncBlackBoardWeapon();
            return;
        }

        Animator bodyAnimator = _animCtrl != null ? _animCtrl.BodyAnim : null;
        Animator[] animators = _weaponRoot.GetComponentsInChildren<Animator>(true);
        _weaponAnimator = null;

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator != null && animator != bodyAnimator)
            {
                _weaponAnimator = animator;
                break;
            }
        }

        _weaponAnimCtrl = null;
        CacheWeaponAnimCtrl();

        if (_weaponAnimCtrl != null)
        {
            _weaponAnimCtrl.Bind(gameObject, _currentWeapon);
        }

        if (_debugWeaponAnim)
        {
            Debug.Log($"[CharWeaponCtrl] RefreshWeaponAnim root={_weaponRoot?.name} anim={_weaponAnimator?.name} ctrl={_weaponAnimCtrl?.name} curWeapon={_currentWeapon}", this);
        }

        SyncBlackBoardWeapon();
    }

    private void SyncWeaponCfg()
    {
        _lastWeapon = _currentWeapon;

        if (_autoWeaponCfg)
        {
            // Attack keys stay simple; weapon difference is mostly movement and layers.
            _lightAtkTrig = Weapon.GetAtkAnimName(_currentWeapon);
        }

        if (_weaponRoot == null)
        {
            CacheWeaponRoot();
        }

        if (_weaponAnimator == null)
        {
            CacheWeaponAnimator();
        }

        if (_weaponAnimCtrl == null)
        {
            CacheWeaponAnimCtrl();
        }

        if (_weaponAnimCtrl != null)
        {
            _weaponAnimCtrl.Bind(gameObject, _currentWeapon);
        }

        SyncBlackBoardWeapon();
    }

    private bool CanMoveOnAtkByWeapon()
    {
        // Current simplified rule: axe attack locks movement, most others do not.
        return Weapon.CanMoveAtk(_currentWeapon);
    }

    private bool ShouldRepeatAttackWhileHeld(BasicAttackMode attackMode)
    {
        switch (attackMode)
        {
            case BasicAttackMode.MeleeRepeat:
                return _repeatMeleeAttackWhileHeld;

            case BasicAttackMode.RangedStraight:
            case BasicAttackMode.RangedHoming:
                return _repeatRangedAttackWhileHeld;

            default:
                return false;
        }
    }

    private BasicAttackTargetInfo ResolveBasicAttackTarget(AttackData_SO attackProfile, BasicAttackMode attackMode)
    {
        if (_charCtrl == null)
        {
            return default;
        }

        bool isRanged = IsRangedAttackMode(attackMode);
        float range = isRanged
            ? CharResourceResolver.GetMaxAttackRange(gameObject)
            : CharResourceResolver.GetAttackRange(gameObject);

        if (range <= 0f)
        {
            range = isRanged ? 8f : 2f;
        }

        BasicAttackTargetingMode targetingMode = ResolveTargetingMode(attackMode, attackProfile);
        float assistAngle = isRanged ? _rangedAssistAngle : _meleeAssistAngle;
        bool preferLockedTarget = ResolvePreferLockedTarget(attackMode, attackProfile);
        bool useLockedAim = attackMode != BasicAttackMode.RangedStraight;

        return CharBasicAttackTargeting.Resolve(
            gameObject,
            _charCtrl,
            targetingMode,
            range,
            assistAngle,
            preferLockedTarget,
            useLockedAim);
    }

    private void BeginAttackCooldown(float fallbackDuration)
    {
        float cooldown = ResolveAttackInterval(fallbackDuration, _resolvedAttackMode);
        _attackCooldownRemain = Mathf.Max(0f, cooldown);
    }

    private float ResolveAttackInterval(float fallbackDuration, BasicAttackMode attackMode)
    {
        float attackSpeed = CharResourceResolver.GetAttackSpeed(gameObject);
        float explicitCooldown = CharResourceResolver.GetAttackCooldown(gameObject);

        // Repeat attack cadence should primarily follow attack speed. Cooldown is
        // kept as fallback so old weapon data still works when attackSpeed is 0.
        if (attackMode == BasicAttackMode.MeleeRepeat)
        {
            if (attackSpeed > 0f)
            {
                return 1f / attackSpeed;
            }

            if (explicitCooldown > 0f)
            {
                return explicitCooldown;
            }
        }

        // Non-repeat modes keep authored cooldown priority to preserve existing
        // behavior for ranged/manual timing setups.
        if (explicitCooldown > 0f)
        {
            return explicitCooldown;
        }

        if (attackSpeed > 0f)
        {
            return 1f / attackSpeed;
        }

        return Mathf.Max(0f, fallbackDuration);
    }

    private void CacheWeaponAnimator()
    {
        if (_weaponRoot != null)
        {
            Animator[] rootAnimators = _weaponRoot.GetComponentsInChildren<Animator>(true);
            Animator bodyAnimator = _animCtrl != null ? _animCtrl.BodyAnim : null;
            for (int i = 0; i < rootAnimators.Length; i++)
            {
                Animator animator = rootAnimators[i];
                if (animator != null && animator != bodyAnimator)
                {
                    _weaponAnimator = animator;
                    return;
                }
            }
        }

        Animator[] animators = GetComponentsInChildren<Animator>(true);
        Animator ownBodyAnimator = _animCtrl != null ? _animCtrl.BodyAnim : null;
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator != null && animator != ownBodyAnimator)
            {
                _weaponAnimator = animator;
                return;
            }
        }
    }

    private void CacheWeaponAnimCtrl()
    {
        if (_weaponAnimCtrl != null)
        {
            return;
        }

        if (_weaponRoot != null)
        {
            _weaponAnimCtrl = _weaponRoot.GetComponent<WeaponAnimCtrl>();
            if (_weaponAnimCtrl == null)
            {
                _weaponAnimCtrl = _weaponRoot.GetComponentInChildren<WeaponAnimCtrl>(true);
            }

            if (_weaponAnimCtrl == null)
            {
                _weaponAnimCtrl = _weaponRoot.gameObject.AddComponent<WeaponAnimCtrl>();
            }

            return;
        }

        if (_weaponAnimator != null)
        {
            _weaponAnimCtrl = _weaponAnimator.GetComponent<WeaponAnimCtrl>();
            if (_weaponAnimCtrl == null)
            {
                _weaponAnimCtrl = _weaponAnimator.GetComponentInParent<WeaponAnimCtrl>();
            }

            return;
        }

        WeaponAnimCtrl[] ctrls = GetComponentsInChildren<WeaponAnimCtrl>(true);
        for (int i = 0; i < ctrls.Length; i++)
        {
            if (ctrls[i] != null)
            {
                _weaponAnimCtrl = ctrls[i];
                return;
            }
        }
    }

    private void CacheWeaponRoot()
    {
        if (_weaponVisualCtrl == null)
        {
            _weaponVisualCtrl = GetComponent<WeaponVisualCtrl>();
        }

        _weaponRoot = CharEquipmentResolver.ResolveWeaponRoot(
            gameObject,
            _blackBoard,
            _weaponVisualCtrl,
            _animCtrl,
            _currentWeapon);
    }

    private void SyncBlackBoardWeapon()
    {
        if (_blackBoard == null || !_blackBoard.Features.useEquipment)
        {
            return;
        }

        // Weapon ownership still lives here. The blackboard only mirrors the result.
        _blackBoard.Equipment.weaponType = _currentWeapon;
        _blackBoard.Equipment.weaponRoot = _weaponRoot;
        CharBlackBoardChangeMask changeMask = CharBlackBoardChangeMask.Equipment;
        if (_blackBoard.Features.useTargeting)
        {
            changeMask |= CharBlackBoardChangeMask.Targeting;
        }

        _blackBoard.MarkRuntimeChanged(changeMask);
    }

    private void NotifyWeaponChanged()
    {
        WeaponChanged?.Invoke(_currentWeapon);
    }

    private void Shoot(BasicAttackTargetInfo targetInfo, AttackData_SO attackProfile, BasicAttackMode attackMode)
    {
        if (_currentWeapon != WeaponType.Bow || !IsRangedAttackMode(attackMode))
        {
            return;
        }

        GameObject projectilePrefab = ResolveProjectilePrefab(attackProfile);
        float projectileSpeed = ResolveProjectileSpeed(attackProfile);
        float homingTurnSpeed = ResolveProjectileTurnSpeed(attackProfile);
        Transform projectileSpawnPoint = ResolveProjectileSpawnPoint();

        if (projectilePrefab == null || projectileSpawnPoint == null)
        {
            return;
        }

        Vector3 fireDirection = targetInfo.attackDirection.sqrMagnitude > 0.001f
            ? targetInfo.attackDirection
            : projectileSpawnPoint.forward;

        GameObject newBullet = Instantiate(
            projectilePrefab,
            projectileSpawnPoint.position,
            Quaternion.LookRotation(fireDirection));

        Rigidbody rb = newBullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = fireDirection * projectileSpeed;
        }

        Bullet bullet = newBullet.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.launcher = gameObject;
            bullet.SetMoveSpeed(projectileSpeed);

            if ((attackMode == BasicAttackMode.RangedHoming || attackMode == BasicAttackMode.RangedChargeRelease) &&
                targetInfo.targetUnit != null)
            {
                bullet.SetHomingTarget(targetInfo.targetUnit.transform, projectileSpeed, homingTurnSpeed);
            }
            else
            {
                bullet.ClearHoming();
            }
        }
    }

    private void SyncResolvedAttackProfile()
    {
        AttackData_SO attackProfile = ResolveAttackProfile();
        _resolvedAttackMode = ResolveBasicAttackMode(attackProfile);

        string resolvedAttackAnimKey = ResolveDefaultAttackAnimKey(attackProfile);
        if (!string.IsNullOrEmpty(resolvedAttackAnimKey))
        {
            _lightAtkTrig = resolvedAttackAnimKey;
        }

        if (_resolvedAttackMode != BasicAttackMode.MeleeCombo)
        {
            _comboIndex = 0;
            _comboResetRemain = 0f;
        }

        if (_resolvedAttackMode != BasicAttackMode.RangedChargeRelease)
        {
            ResetChargeAttack();
        }

        if (_resolvedAttackMode != BasicAttackMode.MeleeCombo)
        {
            _meleeComboAimGraceRemain = 0f;
        }

        if (_resolvedAttackMode != BasicAttackMode.MeleeRepeat)
        {
            _meleeRepeatHoldRestartBlockRemain = 0f;
            _meleeRepeatAimGraceRemain = 0f;
            _keepRepeatAttackAnimSpeed = false;
            ResetAttackAnimSpeed();
        }
    }

    private AttackData_SO ResolveAttackProfile()
    {
        return CharResourceResolver.GetAttackData(gameObject);
    }

    private BasicAttackMode ResolveBasicAttackMode(AttackData_SO attackProfile)
    {
        bool isBow = _currentWeapon == WeaponType.Bow;
        if (attackProfile == null)
        {
            return isBow
                ? BasicAttackMode.RangedHoming
                : BasicAttackMode.MeleeRepeat;
        }

        if (isBow && attackProfile.basicAttackMode == BasicAttackMode.MeleeRepeat)
        {
            return BasicAttackMode.RangedHoming;
        }

        if (!isBow && IsRangedAttackMode(attackProfile.basicAttackMode))
        {
            return BasicAttackMode.MeleeRepeat;
        }

        return attackProfile.basicAttackMode;
    }

    private string ResolveDefaultAttackAnimKey(AttackData_SO attackProfile)
    {
        if (_currentWeapon != WeaponType.Bow)
        {
            if (_resolvedAttackMode == BasicAttackMode.MeleeRepeat)
            {
                return _meleeRepeatTrig;
            }

            if (_resolvedAttackMode == BasicAttackMode.MeleeCombo)
            {
                return _comboAtkTrig;
            }
        }

        if (attackProfile != null && !string.IsNullOrEmpty(attackProfile.attackAnimKey))
        {
            return attackProfile.attackAnimKey;
        }

        if (_autoWeaponCfg)
        {
            return Weapon.GetAtkAnimName(_currentWeapon);
        }

        return _lightAtkTrig;
    }

    private float ResolveAttackDuration(AttackData_SO attackProfile)
    {
        if (attackProfile != null && attackProfile.attackTime > 0f)
        {
            return attackProfile.attackTime;
        }

        return _lightAtkDur;
    }

    private GameObject ResolveProjectilePrefab(AttackData_SO attackProfile)
    {
        if (attackProfile != null && attackProfile.projectilePrefab != null)
        {
            return attackProfile.projectilePrefab;
        }

        return bulletPrefab;
    }

    private float ResolveProjectileSpeed(AttackData_SO attackProfile)
    {
        if (attackProfile != null && attackProfile.projectileSpeed > 0f)
        {
            return attackProfile.projectileSpeed;
        }

        return bulletSpeed;
    }

    private float ResolveProjectileTurnSpeed(AttackData_SO attackProfile)
    {
        if (attackProfile != null && attackProfile.homingTurnSpeed > 0f)
        {
            return attackProfile.homingTurnSpeed;
        }

        return 18f;
    }

    private Transform ResolveProjectileSpawnPoint()
    {
        if (bulletSpawnPoint != null)
        {
            return bulletSpawnPoint;
        }

        if (_weaponRoot != null)
        {
            Transform namedSpawnPoint = FindChildRecursive(_weaponRoot, _projectileSpawnPointName);
            if (namedSpawnPoint != null)
            {
                return namedSpawnPoint;
            }

            return _weaponRoot;
        }

        return transform;
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void TryComboAtk_New(AttackData_SO attackProfile)
    {
        string comboAnimKey = ResolveComboAnimKey(attackProfile);
        bool lockMove = _autoWeaponCfg ? !CanMoveOnAtkByWeapon() : _lightAtkLockMove;
        if (!TryStartAtk_New(
                comboAnimKey,
                Mathf.Max(0.01f, _lightAtkDur),
                lockMove,
                _lightAtkLockRotate,
                false,
                true))
        {
            return;
        }

        if (_actionCtrl == null)
        {
            ResetAttackAnimSpeed();
            PlayAtk_New(comboAnimKey);
        }

        int comboCount = attackProfile != null && attackProfile.comboAnimKeys != null
            ? attackProfile.comboAnimKeys.Length
            : 0;

        if (comboCount > 0)
        {
            _comboIndex = (_comboIndex + 1) % comboCount;
        }

        _comboResetRemain = ResolveComboResetTime(attackProfile);
    }

    private string ResolveComboAnimKey(AttackData_SO attackProfile)
    {
        if (attackProfile == null || attackProfile.comboAnimKeys == null || attackProfile.comboAnimKeys.Length == 0)
        {
            return _comboAtkTrig;
        }

        int comboSlot = _comboResetRemain > 0f ? _comboIndex : 0;
        comboSlot = Mathf.Clamp(comboSlot, 0, attackProfile.comboAnimKeys.Length - 1);
        string comboAnimKey = attackProfile.comboAnimKeys[comboSlot];
        return string.IsNullOrEmpty(comboAnimKey)
            ? _comboAtkTrig
            : comboAnimKey;
    }

    private float ResolveComboResetTime(AttackData_SO attackProfile)
    {
        if (attackProfile != null && attackProfile.comboResetTime > 0f)
        {
            return attackProfile.comboResetTime;
        }

        return 0.6f;
    }

    private void UpdateChargeReleaseInput(AttackInputState input, AttackData_SO attackProfile)
    {
        if (input.isDown)
        {
            TryBeginChargeAttack();
        }

        if (_isChargingBasicAttack && input.isHeld)
        {
            float maxChargeTime = ResolveMaxChargeTime(attackProfile);
            _chargeHoldTime += Time.deltaTime;
            if (maxChargeTime > 0f)
            {
                _chargeHoldTime = Mathf.Min(_chargeHoldTime, maxChargeTime);
            }

            return;
        }

        if (_isChargingBasicAttack && input.isUp)
        {
            if (_chargeHoldTime >= ResolveMinChargeTime(attackProfile))
            {
                TryLightAtk_New(attackProfile, ResolveChargeReleaseAnimKey(attackProfile));
            }

            ResetChargeAttack();
        }
    }

    private void TryBeginChargeAttack()
    {
        if (_attackCooldownRemain > 0f)
        {
            return;
        }

        if (_actionCtrl != null &&
            _actionCtrl.CurReq != null &&
            _actionCtrl.State != CharActionState.Idle)
        {
            return;
        }

        _isChargingBasicAttack = true;
        _chargeHoldTime = 0f;
    }

    private void ResetChargeAttack()
    {
        _isChargingBasicAttack = false;
        _chargeHoldTime = 0f;
    }

    private float ResolveMaxChargeTime(AttackData_SO attackProfile)
    {
        return attackProfile != null && attackProfile.maxChargeTime > 0f
            ? attackProfile.maxChargeTime
            : 1.2f;
    }

    private float ResolveMinChargeTime(AttackData_SO attackProfile)
    {
        return attackProfile != null && attackProfile.minChargeTime > 0f
            ? attackProfile.minChargeTime
            : 0.15f;
    }

    private string ResolveChargeReleaseAnimKey(AttackData_SO attackProfile)
    {
        if (attackProfile != null && !string.IsNullOrEmpty(attackProfile.chargeReleaseAnimKey))
        {
            return attackProfile.chargeReleaseAnimKey;
        }

        return ResolveDefaultAttackAnimKey(attackProfile);
    }

    private bool IsRangedAttackMode(BasicAttackMode attackMode)
    {
        switch (attackMode)
        {
            case BasicAttackMode.RangedStraight:
            case BasicAttackMode.RangedHoming:
            case BasicAttackMode.RangedChargeRelease:
                return true;

            default:
                return false;
        }
    }

    private BasicAttackTargetingMode ResolveTargetingMode(BasicAttackMode attackMode, AttackData_SO attackProfile)
    {
        switch (attackMode)
        {
            case BasicAttackMode.RangedStraight:
                return BasicAttackTargetingMode.FreeAim;

            case BasicAttackMode.RangedHoming:
            case BasicAttackMode.RangedChargeRelease:
                if (attackProfile != null && !attackProfile.enableSoftLock)
                {
                    return BasicAttackTargetingMode.FreeAim;
                }

                return BasicAttackTargetingMode.SoftLock;

            default:
                return _useMeleeTargetAssist
                    ? BasicAttackTargetingMode.SoftLock
                    : BasicAttackTargetingMode.FreeAim;
        }
    }

    private bool ResolvePreferLockedTarget(BasicAttackMode attackMode, AttackData_SO attackProfile)
    {
        switch (attackMode)
        {
            case BasicAttackMode.RangedStraight:
                return false;

            case BasicAttackMode.RangedHoming:
            case BasicAttackMode.RangedChargeRelease:
                if (attackProfile != null)
                {
                    return attackProfile.preferLockedTarget;
                }

                return _preferLockedTarget;

            default:
                return false;
        }
    }

    public bool ShouldSuppressMoveAnimation()
    {
        if (_currentWeapon == WeaponType.Bow || _charCtrl == null || _charCtrl.Param == null)
        {
            return false;
        }

        bool suppressRepeatMoveAnim =
            _resolvedAttackMode == BasicAttackMode.MeleeRepeat &&
            _suppressMoveAnimDuringMeleeRepeat;
        bool suppressComboMoveAnim =
            _resolvedAttackMode == BasicAttackMode.MeleeCombo &&
            _suppressMoveAnimDuringMeleeCombo;
        if (!suppressRepeatMoveAnim && !suppressComboMoveAnim)
        {
            return false;
        }

        bool canMoveDuringAttack = _autoWeaponCfg ? CanMoveOnAtkByWeapon() : !_lightAtkLockMove;
        if (!canMoveDuringAttack || _charCtrl.Param.Locomotion.sqrMagnitude <= 0.01f)
        {
            return false;
        }

        return IsMeleeBasicAimPresentationActive();
    }

    public bool TryGetMeleeAttackAimDirection(out Vector3 direction)
    {
        direction = Vector3.zero;
        if (!_faceAttackDirection || _currentWeapon == WeaponType.Bow || _charCtrl == null || _charCtrl.Param == null)
        {
            return false;
        }

        if (_resolvedAttackMode == BasicAttackMode.MeleeRepeat)
        {
            AttackInputState repeatInput = _charCtrl.Param.AttackState;
            if (repeatInput.isDown || repeatInput.isHeld)
            {
                _meleeRepeatAimGraceRemain = Mathf.Max(_meleeRepeatAimGraceRemain, _meleeRepeatAimGraceTime);
            }

            bool isRepeatAimActive =
                repeatInput.isDown ||
                repeatInput.isHeld ||
                _meleeRepeatAimGraceRemain > 0f ||
                IsMeleeRepeatPresentationActive();

            return TryResolveAimDirectionWhileActive(isRepeatAimActive, ref _meleeRepeatAimDirection, out direction);
        }

        if (_resolvedAttackMode == BasicAttackMode.MeleeCombo)
        {
            AttackInputState comboInput = _charCtrl.Param.AttackState;
            if (comboInput.isDown)
            {
                _meleeComboAimGraceRemain = Mathf.Max(_meleeComboAimGraceRemain, _meleeComboAimGraceTime);
            }

            bool isComboAimActive =
                comboInput.isDown ||
                _meleeComboAimGraceRemain > 0f ||
                HasActiveSelfAttackAction();

            return TryResolveAimDirectionWhileActive(isComboAimActive, ref _meleeComboAimDirection, out direction);
        }

        return false;
    }

    private bool TryResolveAimDirectionWhileActive(bool isAimActive, ref Vector3 cachedAimDirection, out Vector3 direction)
    {
        direction = Vector3.zero;
        Vector3 aimDirection = CharBasicAttackTargeting.ResolveAimDirection(gameObject, _charCtrl, false);
        if (aimDirection.sqrMagnitude > 0.001f)
        {
            cachedAimDirection = aimDirection.normalized;
        }

        if (!isAimActive || cachedAimDirection.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        direction = cachedAimDirection;
        return true;
    }

    private bool ShouldForceFaceAttackDirection(BasicAttackTargetInfo targetInfo)
    {
        if (!_faceAttackDirection || _charCtrl == null || targetInfo.attackDirection.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        if (_resolvedAttackMode == BasicAttackMode.MeleeRepeat ||
            _resolvedAttackMode == BasicAttackMode.MeleeCombo)
        {
            return false;
        }

        if (ShouldSuppressMoveAnimation())
        {
            return true;
        }

        if (_resolvedAttackMode != BasicAttackMode.MeleeRepeat || !_suppressRepeatFaceWhileMoving)
        {
            return true;
        }

        bool canMoveDuringAttack = _autoWeaponCfg ? CanMoveOnAtkByWeapon() : !_lightAtkLockMove;
        if (!canMoveDuringAttack)
        {
            return true;
        }

        CharParam charParam = _charCtrl.Param;
        if (charParam == null)
        {
            return true;
        }

        return charParam.Locomotion.sqrMagnitude <= 0.01f;
    }

    private float ResolveAttackFaceLockDuration(AttackData_SO attackProfile)
    {
        float lockDuration = _attackFaceLockDuration;
        if (_resolvedAttackMode == BasicAttackMode.MeleeRepeat && ShouldSuppressMoveAnimation())
        {
            lockDuration = Mathf.Max(lockDuration, ResolveAttackCycleDuration(attackProfile, _resolvedAttackMode));
        }

        return lockDuration;
    }

    private bool IsMeleeRepeatPresentationActive()
    {
        return HasActiveSelfAttackAction() ||
               _attackCooldownRemain > 0f ||
               _keepRepeatAttackAnimSpeed ||
               _meleeRepeatHoldRestartBlockRemain > 0f;
    }

    private bool IsMeleeBasicAimPresentationActive()
    {
        if (_resolvedAttackMode == BasicAttackMode.MeleeRepeat)
        {
            return IsMeleeRepeatPresentationActive();
        }

        if (_resolvedAttackMode == BasicAttackMode.MeleeCombo)
        {
            return HasActiveSelfAttackAction() || _meleeComboAimGraceRemain > 0f;
        }

        return false;
    }

    private bool HasActiveSelfAttackAction()
    {
        return _actionCtrl != null &&
               _actionCtrl.CurReq != null &&
               _actionCtrl.CurReq.src == this &&
               _actionCtrl.CurReq.type == CharActionType.Atk &&
               _actionCtrl.State != CharActionState.Idle;
    }

    private void UpdateBlackBoardAttackTarget(BasicAttackTargetInfo targetInfo)
    {
        if (_blackBoard == null || !_blackBoard.Features.useTargeting)
        {
            return;
        }

        _blackBoard.Targeting.currentTarget = targetInfo.targetUnit;
        _blackBoard.Targeting.aimPoint = targetInfo.attackPoint;
        _blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Targeting);
    }

    private bool CanReplaceActiveAttack(bool allowReplaceActiveAttack)
    {
        if (!allowReplaceActiveAttack || _actionCtrl == null || _actionCtrl.CurReq == null)
        {
            return false;
        }

        return _actionCtrl.CurReq.type == CharActionType.Atk && _actionCtrl.CurReq.src == this;
    }

    private float ResolveActionDuration(AttackData_SO attackProfile, BasicAttackMode attackMode)
    {
        float cycleDuration = ResolveAttackCycleDuration(attackProfile, attackMode);
        if (attackMode == BasicAttackMode.MeleeRepeat)
        {
            return cycleDuration * Mathf.Clamp01(_meleeRepeatLockMoveFraction);
        }

        return cycleDuration;
    }

    private float ResolveAttackCycleDuration(AttackData_SO attackProfile, BasicAttackMode attackMode)
    {
        if (attackMode == BasicAttackMode.MeleeRepeat)
        {
            return ResolveAttackInterval(ResolveAttackDuration(attackProfile), attackMode);
        }

        return ResolveAttackDuration(attackProfile);
    }

    private void ApplyAttackAnimSpeed(AttackData_SO attackProfile, BasicAttackMode attackMode)
    {
        float nextSpeed = 1f;
        if (attackMode == BasicAttackMode.MeleeRepeat && _currentWeapon != WeaponType.Bow)
        {
            float baseAnimDuration = ResolveAttackDuration(attackProfile);
            float cycleDuration = ResolveAttackCycleDuration(attackProfile, attackMode);
            if (baseAnimDuration > 0.01f && cycleDuration > 0.01f)
            {
                nextSpeed = Mathf.Clamp(baseAnimDuration / cycleDuration, 0.1f, 4f);
            }
        }

        SetAttackAnimSpeed(nextSpeed);
    }

    private void SetAttackAnimSpeed(float speed)
    {
        float finalSpeed = Mathf.Max(0.01f, speed);
        _activeAttackAnimSpeed = finalSpeed;

        Animator bodyAnim = _animCtrl != null ? _animCtrl.BodyAnim : null;
        bool useBodySpeedParam =
            _useBodyMeleeSpeedParam &&
            _resolvedAttackMode == BasicAttackMode.MeleeRepeat &&
            _currentWeapon != WeaponType.Bow &&
            _animCtrl != null &&
            _animCtrl.SetMeleeRepeatSpeed(finalSpeed);

        if (bodyAnim != null)
        {
            bodyAnim.speed = useBodySpeedParam ? 1f : finalSpeed;
        }

        if (_weaponAnimCtrl != null && _weaponAnimCtrl.Anim != null)
        {
            _weaponAnimCtrl.Anim.speed = finalSpeed;
        }
    }

    private void ResetAttackAnimSpeed()
    {
        _activeAttackAnimSpeed = 1f;

        if (_animCtrl != null)
        {
            _animCtrl.SetMeleeRepeatSpeed(1f);
        }

        Animator bodyAnim = _animCtrl != null ? _animCtrl.BodyAnim : null;
        if (bodyAnim != null)
        {
            bodyAnim.speed = 1f;
        }

        if (_weaponAnimCtrl != null && _weaponAnimCtrl.Anim != null)
        {
            _weaponAnimCtrl.Anim.speed = 1f;
        }
    }

    private void UpdateRepeatAttackAnimSpeedState()
    {
        if (!_keepRepeatAttackAnimSpeed)
        {
            return;
        }

        if (_resolvedAttackMode != BasicAttackMode.MeleeRepeat || _currentWeapon == WeaponType.Bow)
        {
            _keepRepeatAttackAnimSpeed = false;
            ResetAttackAnimSpeed();
            return;
        }

        if (_attackCooldownRemain > 0f)
        {
            return;
        }

        bool hasActiveAttackAction =
            _actionCtrl != null &&
            _actionCtrl.CurReq != null &&
            _actionCtrl.CurReq.src == this &&
            _actionCtrl.CurReq.type == CharActionType.Atk &&
            _actionCtrl.State != CharActionState.Idle;

        if (hasActiveAttackAction)
        {
            return;
        }

        _keepRepeatAttackAnimSpeed = false;
        ResetAttackAnimSpeed();
    }
}
