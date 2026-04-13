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
    private BasicAttackMode _resolvedAttackMode = BasicAttackMode.MeleeCombo;
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
    [SerializeField] private float _lightAtkDur = 0.35f;
    [SerializeField] private bool _lightAtkLockRotate;

    [Header("Attack Input")]
    [Tooltip("远程默认按住持续普攻。松开后立刻停止后续连射。")]
    [SerializeField] private bool _repeatRangedAttackWhileHeld = true;

    [Header("Melee Assist")]
    [Tooltip("近战在攻击开始前，轻微朝最近目标修正朝向。")]
    [SerializeField] private bool _useMeleeTargetAssist = true;
    [Tooltip("近战目标辅助的搜索角度。")]
    [SerializeField] private float _meleeAssistAngle = 40f;
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
    [Tooltip("用于朝向修正的短暂锁向时长。")]
    [SerializeField] private float _attackFaceLockDuration = 0.08f;

    private float _activeAttackAnimSpeed = 1f;
    private float _meleeComboAimGraceRemain;
    private Vector3 _meleeComboAimDirection = Vector3.forward;

    // ──────────────────── Lifecycle ────────────────────

    protected void Awake()
    {
        _charCtrl = GetComponent<CharCtrl>();
        _animCtrl = GetComponent<CharAnimCtrl>();
        _weaponVisualCtrl = GetComponent<WeaponVisualCtrl>();
        _actionCtrl = GetComponent<CharActionCtrl>();
        _blackBoard = GetComponent<CharBlackBoard>();

        if (_animCtrl == null)
            _animCtrl = gameObject.AddComponent<CharAnimCtrl>();

        CacheWeaponRoot();
        CacheWeaponAnimator();
        CacheWeaponAnimCtrl();
        SyncWeaponCfg();
    }

    private void Start()
    {
        RefreshWeaponAnim();
        SyncWeaponCfg();
    }

    private void OnEnable()
    {
        if (_actionCtrl == null)
            _actionCtrl = GetComponent<CharActionCtrl>();

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
            _attackCooldownRemain -= Time.deltaTime;

        if (_meleeComboAimGraceRemain > 0f)
            _meleeComboAimGraceRemain -= Time.deltaTime;

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

        UpdateAtkInput();
    }

    // ──────────────────── Attack Input ────────────────────

    private void UpdateAtkInput()
    {
        if (_charCtrl == null || _charCtrl.Param == null)
            return;

        AttackInputState input = _charCtrl.Param.AttackState;
        AttackData_SO profile = ResolveAttackProfile();

        switch (_resolvedAttackMode)
        {
            case BasicAttackMode.MeleeCombo:
                if (input.isDown) TryComboAtk(profile);
                return;

            case BasicAttackMode.RangedChargeRelease:
                UpdateChargeReleaseInput(input, profile);
                return;
        }

        // RangedStraight / RangedHoming
        bool repeat = (_resolvedAttackMode == BasicAttackMode.RangedStraight
                    || _resolvedAttackMode == BasicAttackMode.RangedHoming)
                    && _repeatRangedAttackWhileHeld;

        if (repeat)
        {
            if (input.isHeld || input.isDown)
                TryLightAtk(profile, ResolveDefaultAttackAnimKey(profile));
        }
        else if (input.isDown)
        {
            TryLightAtk(profile, ResolveDefaultAttackAnimKey(profile));
        }
    }

    // ──────────────────── Action Callbacks ────────────────────

    private void OnActionStart(CharActionReq req)
    {
        if (req == null || req.type != CharActionType.Atk || req.src != this) return;
        PlayAtk(req.animKey);
    }

    private void OnActionEnd(CharActionReq req)
    {
        if (req == null || req.type != CharActionType.Atk || req.src != this) return;
        SetAttackAnimSpeed(1f);
    }

    private void OnActionInterrupted(CharActionReq req, string reason)
    {
        if (req == null || req.type != CharActionType.Atk || req.src != this) return;
        _meleeComboAimGraceRemain = 0f;
        SetAttackAnimSpeed(1f);
    }

    // ──────────────────── Light Attack ────────────────────

    private bool TryLightAtk(AttackData_SO profile, string animKey = null)
    {
        string finalAnimKey = string.IsNullOrEmpty(animKey) ? _lightAtkTrig : animKey;
        float dur = ResolveAttackDuration(profile);
        bool lockMove = !CanMoveWhileAttacking(profile);

        if (!TryStartAtk(finalAnimKey, dur, lockMove, _lightAtkLockRotate, true, false, dur))
            return false;

        if (_actionCtrl == null)
            PlayAtk(finalAnimKey);

        return true;
    }

    private bool TryStartAtk(
        string animKey, float dur, bool lockMove, bool lockRotate,
        bool applyCooldown = true, bool allowReplaceActiveAttack = false,
        float cooldownFallbackDuration = -1f)
    {
        if (!CharRuntimeResolver.CanAttack(gameObject))
            return false;

        // 只有在动作控制器真的处于非 Idle 状态时，才认为当前被动作占用。
        if (_actionCtrl != null
            && _actionCtrl.CurReq != null
            && _actionCtrl.State != CharActionState.Idle
            && !CanReplaceActiveAttack(allowReplaceActiveAttack))
            return false;

        if (applyCooldown && _attackCooldownRemain > 0f)
            return false;

        if (_actionCtrl == null)
        {
            if (applyCooldown)
                BeginAttackCooldown(cooldownFallbackDuration >= 0f ? cooldownFallbackDuration : dur);
            return true;
        }

        CharActionReq req = new CharActionReq
        {
            type = CharActionType.Atk,
            state = CharActionState.AtkWindup,
            src = this,
            dur = Mathf.Max(0.01f, dur),
            lockMove = lockMove,
            lockRotate = lockRotate,
            interruptible = true,
            animKey = animKey,
        };

        if (!_actionCtrl.TryStart(req))
            return false;

        if (applyCooldown)
            BeginAttackCooldown(cooldownFallbackDuration >= 0f ? cooldownFallbackDuration : req.dur);
        return true;
    }

    // ──────────────────── Combo Attack ────────────────────

    private void TryComboAtk(AttackData_SO profile)
    {
        string comboAnimKey = ResolveComboAnimKey(profile);
        bool lockMove = !CanMoveWhileAttacking(profile);

        if (!TryStartAtk(comboAnimKey, Mathf.Max(0.01f, _lightAtkDur),
                lockMove, _lightAtkLockRotate, false, true))
            return;

        if (_actionCtrl == null)
        {
            SetAttackAnimSpeed(1f);
            PlayAtk(comboAnimKey);
        }

        int comboCount = profile?.comboAnimKeys?.Length ?? 0;
        if (comboCount > 0)
            _comboIndex = (_comboIndex + 1) % comboCount;

        _comboResetRemain = profile != null && profile.comboResetTime > 0f
            ? profile.comboResetTime : 0.6f;
    }

    private string ResolveComboAnimKey(AttackData_SO profile)
    {
        if (profile == null || profile.comboAnimKeys == null || profile.comboAnimKeys.Length == 0)
            return _comboAtkTrig;

        int slot = _comboResetRemain > 0f ? _comboIndex : 0;
        slot = Mathf.Clamp(slot, 0, profile.comboAnimKeys.Length - 1);
        string key = profile.comboAnimKeys[slot];
        return string.IsNullOrEmpty(key) ? _comboAtkTrig : key;
    }

    // ──────────────────── Charge Attack ────────────────────

    private void UpdateChargeReleaseInput(AttackInputState input, AttackData_SO profile)
    {
        if (input.isDown)
            TryBeginChargeAttack();

        if (_isChargingBasicAttack && input.isHeld)
        {
            float maxCharge = profile != null && profile.maxChargeTime > 0f
                ? profile.maxChargeTime : 1.2f;
            _chargeHoldTime = Mathf.Min(_chargeHoldTime + Time.deltaTime, maxCharge);
            return;
        }

        if (_isChargingBasicAttack && input.isUp)
        {
            float minCharge = profile != null && profile.minChargeTime > 0f
                ? profile.minChargeTime : 0.15f;

            if (_chargeHoldTime >= minCharge)
            {
                string releaseKey = profile != null && !string.IsNullOrEmpty(profile.chargeReleaseAnimKey)
                    ? profile.chargeReleaseAnimKey
                    : ResolveDefaultAttackAnimKey(profile);
                TryLightAtk(profile, releaseKey);
            }

            ResetChargeAttack();
        }
    }

    private void TryBeginChargeAttack()
    {
        if (_attackCooldownRemain > 0f)
            return;
        if (_actionCtrl != null && _actionCtrl.CurReq != null && _actionCtrl.State != CharActionState.Idle)
            return;

        _isChargingBasicAttack = true;
        _chargeHoldTime = 0f;
    }

    private void ResetChargeAttack()
    {
        _isChargingBasicAttack = false;
        _chargeHoldTime = 0f;
    }

    // ──────────────────── Play Attack ────────────────────

    private void PlayAtk(string trig)
    {
        AttackData_SO profile = ResolveAttackProfile();
        SetAttackAnimSpeed(1f);

        BasicAttackTargetInfo targetInfo = ResolveBasicAttackTarget(profile, _resolvedAttackMode);
        UpdateBlackBoardAttackTarget(targetInfo);

        if (ShouldForceFaceAttackDirection(targetInfo))
        {
            _charCtrl.ForceBasicAttackFaceDirection(
                targetInfo.attackDirection, _attackFaceLockDuration, true);
        }

        if (_weaponAnimCtrl == null)
        {
            RefreshWeaponAnim();
            SetAttackAnimSpeed(_activeAttackAnimSpeed);
        }

        if (_weaponAnimCtrl != null)
        {
            if (_debugWeaponAnim)
                Debug.Log($"[CharWeaponCtrl] PlayAtk trig={trig} root={_weaponRoot?.name} weaponAnim={_weaponAnimCtrl.name} curWeapon={_currentWeapon}", this);
            _weaponAnimCtrl.PlayAtk(trig);
        }
        else if (_debugWeaponAnim)
        {
            Debug.LogWarning($"[CharWeaponCtrl] PlayAtk failed, no WeaponAnimCtrl. root={_weaponRoot?.name} animator={_weaponAnimator?.name} curWeapon={_currentWeapon}", this);
        }

        PlayAttackCastVfx(profile, targetInfo);

        if (IsRangedAttackMode(_resolvedAttackMode))
        {
            Shoot(targetInfo, profile, _resolvedAttackMode);
            return;
        }
    }

    // ──────────────────── Shooting ────────────────────

    private void Shoot(BasicAttackTargetInfo targetInfo, AttackData_SO profile, BasicAttackMode attackMode)
    {
        if (!Weapon.IsRangedWeapon(_currentWeapon) || !IsRangedAttackMode(attackMode))
            return;

        GameObject prefab = profile != null && profile.projectilePrefab != null
            ? profile.projectilePrefab : bulletPrefab;
        float speed = profile != null && profile.projectileSpeed > 0f
            ? profile.projectileSpeed : bulletSpeed;
        float turnSpeed = profile != null && profile.homingTurnSpeed > 0f
            ? profile.homingTurnSpeed : 18f;
        Transform spawnPoint = ResolveProjectileSpawnPoint(profile);

        if (prefab == null || spawnPoint == null)
            return;

        Vector3 fireDir = targetInfo.attackDirection.sqrMagnitude > 0.001f
            ? targetInfo.attackDirection
            : spawnPoint.forward;

        GameObject newBullet = Instantiate(prefab, spawnPoint.position, Quaternion.LookRotation(fireDir));

        Rigidbody rb = newBullet.GetComponent<Rigidbody>();
        if (rb != null)
            rb.velocity = fireDir * speed;

        Bullet bullet = newBullet.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.launcher = gameObject;
            bullet.ConfigureFromAttackProfile(profile);
            bullet.SetMoveSpeed(speed);
            bullet.SetImpactVfx(profile != null ? profile.attackHitVfx : null);

            if ((attackMode == BasicAttackMode.RangedHoming || attackMode == BasicAttackMode.RangedChargeRelease)
                && targetInfo.targetUnit != null)
            {
                bullet.SetHomingTarget(targetInfo.targetUnit.transform, speed, turnSpeed);
            }
            else
            {
                bullet.ClearHoming();
                bullet.SetStraightFlight(fireDir, speed);
            }
        }
        else if (rb != null)
        {
            rb.velocity = fireDir * speed;
        }
    }

    private Transform ResolveProjectileSpawnPoint(AttackData_SO profile)
    {
        if (bulletSpawnPoint != null)
            return bulletSpawnPoint;

        string spawnPointName = profile != null && !string.IsNullOrEmpty(profile.projectileSpawnPointName)
            ? profile.projectileSpawnPointName
            : _projectileSpawnPointName;

        if (_weaponRoot != null)
        {
            Transform named = FindChildRecursive(_weaponRoot, spawnPointName);
            return named != null ? named : _weaponRoot;
        }

        return transform;
    }

    // ──────────────────── Weapon Management ────────────────────

    public void SetWeapon(WeaponType weaponType)
    {
        if (_currentWeapon == weaponType) return;
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
            _weaponAnimCtrl.Bind(gameObject, _currentWeapon);

        if (_debugWeaponAnim)
            Debug.Log($"[CharWeaponCtrl] BindWeaponRoot root={_weaponRoot?.name} anim={_weaponAnimator?.name} ctrl={_weaponAnimCtrl?.name} curWeapon={_currentWeapon}", this);

        SyncBlackBoardWeapon();
    }

    public void ClearWeaponRoot()
    {
        if (_weaponAnimCtrl != null)
            _weaponAnimCtrl.Unbind();

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

        _weaponAnimator = FindWeaponAnimator(_weaponRoot);
        _weaponAnimCtrl = null;
        CacheWeaponAnimCtrl();

        if (_weaponAnimCtrl != null)
            _weaponAnimCtrl.Bind(gameObject, _currentWeapon);

        if (_debugWeaponAnim)
            Debug.Log($"[CharWeaponCtrl] RefreshWeaponAnim root={_weaponRoot?.name} anim={_weaponAnimator?.name} ctrl={_weaponAnimCtrl?.name} curWeapon={_currentWeapon}", this);

        SyncBlackBoardWeapon();
    }

    // ──────────────────── Sync & Resolve ────────────────────

    private void SyncWeaponCfg()
    {
        _lastWeapon = _currentWeapon;
        _lightAtkTrig = Weapon.GetAtkAnimName(_currentWeapon);

        if (_weaponRoot == null) CacheWeaponRoot();
        if (_weaponAnimator == null) CacheWeaponAnimator();
        if (_weaponAnimCtrl == null) CacheWeaponAnimCtrl();

        if (_weaponAnimCtrl != null)
            _weaponAnimCtrl.Bind(gameObject, _currentWeapon);

        SyncBlackBoardWeapon();
    }

    private void SyncResolvedAttackProfile()
    {
        AttackData_SO profile = ResolveAttackProfile();
        BasicAttackMode newMode = ResolveBasicAttackMode(profile);

        // 只在模式真正切换时才重置旧模式的状态
        if (newMode != _resolvedAttackMode)
        {
            if (_resolvedAttackMode == BasicAttackMode.MeleeCombo)
            {
                _comboIndex = 0;
                _comboResetRemain = 0f;
                _meleeComboAimGraceRemain = 0f;
            }
            if (_resolvedAttackMode == BasicAttackMode.RangedChargeRelease)
                ResetChargeAttack();

            _resolvedAttackMode = newMode;
        }

        string animKey = ResolveDefaultAttackAnimKey(profile);
        if (!string.IsNullOrEmpty(animKey))
            _lightAtkTrig = animKey;
    }

    private AttackData_SO ResolveAttackProfile()
    {
        return CharResourceResolver.GetAttackData(gameObject);
    }

    private BasicAttackMode ResolveBasicAttackMode(AttackData_SO profile)
    {
        bool isRanged = Weapon.IsRangedWeapon(_currentWeapon);
        if (profile == null)
            return isRanged ? BasicAttackMode.RangedHoming : BasicAttackMode.MeleeCombo;

        if (isRanged && !IsRangedAttackMode(profile.basicAttackMode))
            return BasicAttackMode.RangedHoming;

        if (!isRanged && IsRangedAttackMode(profile.basicAttackMode))
            return BasicAttackMode.MeleeCombo;

        return profile.basicAttackMode;
    }

    private string ResolveDefaultAttackAnimKey(AttackData_SO profile)
    {
        if (!Weapon.IsRangedWeapon(_currentWeapon))
            return _comboAtkTrig;

        if (profile != null && !string.IsNullOrEmpty(profile.attackAnimKey))
            return profile.attackAnimKey;

        return Weapon.GetAtkAnimName(_currentWeapon);
    }

    private float ResolveAttackDuration(AttackData_SO profile)
    {
        return profile != null && profile.attackTime > 0f ? profile.attackTime : _lightAtkDur;
    }

    // ──────────────────── Targeting ────────────────────

    private BasicAttackTargetInfo ResolveBasicAttackTarget(AttackData_SO profile, BasicAttackMode attackMode)
    {
        if (_charCtrl == null) return default;

        bool isRanged = IsRangedAttackMode(attackMode);
        float range = isRanged
            ? CharResourceResolver.GetMaxAttackRange(gameObject)
            : CharResourceResolver.GetAttackRange(gameObject);
        if (range <= 0f)
            range = isRanged ? 8f : 2f;

        BasicAttackTargetingMode targetingMode = ResolveTargetingMode(attackMode, profile);
        float assistAngle = isRanged ? _rangedAssistAngle : _meleeAssistAngle;
        bool preferLocked = ResolvePreferLockedTarget(attackMode, profile);
        bool useLockedAim = attackMode != BasicAttackMode.RangedStraight;

        return CharBasicAttackTargeting.Resolve(
            gameObject, _charCtrl, targetingMode, range, assistAngle, preferLocked, useLockedAim);
    }

    private BasicAttackTargetingMode ResolveTargetingMode(BasicAttackMode attackMode, AttackData_SO profile)
    {
        switch (attackMode)
        {
            case BasicAttackMode.RangedStraight:
                return BasicAttackTargetingMode.FreeAim;

            case BasicAttackMode.RangedHoming:
            case BasicAttackMode.RangedChargeRelease:
                return (profile != null && !profile.enableSoftLock)
                    ? BasicAttackTargetingMode.FreeAim
                    : BasicAttackTargetingMode.SoftLock;

            default:
                return _useMeleeTargetAssist
                    ? BasicAttackTargetingMode.SoftLock
                    : BasicAttackTargetingMode.FreeAim;
        }
    }

    private bool ResolvePreferLockedTarget(BasicAttackMode attackMode, AttackData_SO profile)
    {
        switch (attackMode)
        {
            case BasicAttackMode.RangedStraight:
                return false;
            case BasicAttackMode.RangedHoming:
            case BasicAttackMode.RangedChargeRelease:
                return profile != null ? profile.preferLockedTarget : _preferLockedTarget;
            default:
                return false;
        }
    }

    // ──────────────────── Face Direction & Aim ────────────────────

    private bool ShouldForceFaceAttackDirection(BasicAttackTargetInfo targetInfo)
    {
        // 近战 combo 由 TryGetMeleeAttackAimDirection 驱动朝向，这里不重复处理
        return _faceAttackDirection
            && _charCtrl != null
            && targetInfo.attackDirection.sqrMagnitude > 0.001f
            && _resolvedAttackMode != BasicAttackMode.MeleeCombo;
    }

    public bool ShouldSuppressMoveAnimation()
    {
        if (Weapon.IsRangedWeapon(_currentWeapon) || _charCtrl == null || _charCtrl.Param == null)
            return false;

        if (UsesUpperBodyMoveAttackPresentation())
            return false;

        if (_resolvedAttackMode != BasicAttackMode.MeleeCombo || !_suppressMoveAnimDuringMeleeCombo)
            return false;

        AttackData_SO profile = ResolveAttackProfile();
        if (!CanMoveWhileAttacking(profile) || _charCtrl.Param.Locomotion.sqrMagnitude <= 0.01f)
            return false;

        return IsMeleeBasicAimPresentationActive();
    }

    private bool UsesUpperBodyMoveAttackPresentation()
    {
        if (Weapon.IsRangedWeapon(_currentWeapon)) return false;
        AttackData_SO profile = ResolveAttackProfile();
        if (profile == null) return false;
        return CanMoveWhileAttacking(profile) && profile.useUpperBodyMoveAttackPresentation;
    }

    private bool CanMoveWhileAttacking(AttackData_SO profile)
    {
        return profile == null || profile.canMoveWhileAttack;
    }

    public bool TryGetMeleeAttackAimDirection(out Vector3 direction)
    {
        direction = Vector3.zero;
        if (!_faceAttackDirection || Weapon.IsRangedWeapon(_currentWeapon)
            || _charCtrl == null || _charCtrl.Param == null)
            return false;

        if (_resolvedAttackMode != BasicAttackMode.MeleeCombo)
            return false;

        AttackInputState comboInput = _charCtrl.Param.AttackState;
        if (comboInput.isDown)
            _meleeComboAimGraceRemain = Mathf.Max(_meleeComboAimGraceRemain, _meleeComboAimGraceTime);

        bool isActive = comboInput.isDown
            || _meleeComboAimGraceRemain > 0f
            || HasActiveSelfAttackAction();

        Vector3 aimDir = CharBasicAttackTargeting.ResolveAimDirection(gameObject, _charCtrl, false);
        if (aimDir.sqrMagnitude > 0.001f)
            _meleeComboAimDirection = aimDir.normalized;

        if (!isActive || _meleeComboAimDirection.sqrMagnitude <= 0.001f)
            return false;

        direction = _meleeComboAimDirection;
        return true;
    }

    private void PlayAttackCastVfx(AttackData_SO profile, BasicAttackTargetInfo targetInfo)
    {
        if (profile == null || profile.attackCastVfx == null)
        {
            return;
        }

        bool isRangedAttack = IsRangedAttackMode(_resolvedAttackMode);
        Transform mount = ResolveAttackVfxMount(profile, isRangedAttack);
        Vector3 direction = targetInfo.attackDirection.sqrMagnitude > 0.001f
            ? targetInfo.attackDirection
            : transform.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = transform.forward;
        }

        Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        SpawnAttackVfx(
            profile.attackCastVfx,
            mount,
            mount != null ? mount.position : transform.position,
            rotation,
            profile.attackCastVfxOffset,
            profile.attachAttackCastVfxToPoint,
            profile.attackCastVfxLifetime);
    }

    private void SpawnAttackVfx(
        GameObject prefab,
        Transform mount,
        Vector3 worldPosition,
        Quaternion rotation,
        Vector3 offset,
        bool attachToMount,
        float lifetime)
    {
        if (prefab == null)
        {
            return;
        }

        GameObject instance;
        if (attachToMount && mount != null)
        {
            instance = Instantiate(prefab, worldPosition, rotation, mount);
            instance.transform.localPosition += offset;
        }
        else
        {
            instance = Instantiate(prefab, worldPosition + rotation * offset, rotation);
        }

        if (lifetime > 0f)
        {
            Destroy(instance, lifetime);
        }
    }

    private Transform ResolveAttackVfxMount(AttackData_SO profile, bool isRangedAttack)
    {
        if (profile == null)
        {
            return isRangedAttack ? ResolveProjectileSpawnPoint(null) : _weaponRoot;
        }

        string pointName = profile.attackVfxPointName;
        if (!string.IsNullOrEmpty(pointName))
        {
            if (profile.preferOwnerAttackVfxPoint)
            {
                Transform ownerPoint = FindChildRecursive(transform, pointName);
                if (ownerPoint != null)
                {
                    return ownerPoint;
                }
            }

            if (_weaponRoot != null)
            {
                Transform weaponPoint = FindChildRecursive(_weaponRoot, pointName);
                if (weaponPoint != null)
                {
                    return weaponPoint;
                }
            }

            if (!profile.preferOwnerAttackVfxPoint)
            {
                Transform ownerPoint = FindChildRecursive(transform, pointName);
                if (ownerPoint != null)
                {
                    return ownerPoint;
                }
            }
        }

        if (isRangedAttack)
        {
            return ResolveProjectileSpawnPoint(profile);
        }

        return _weaponRoot != null ? _weaponRoot : transform;
    }

    // ──────────────────── Cooldown ────────────────────

    private void BeginAttackCooldown(float fallbackDuration)
    {
        float attackSpeed = CharResourceResolver.GetAttackSpeed(gameObject);
        float explicitCooldown = CharResourceResolver.GetAttackCooldown(gameObject);

        float cooldown;
        if (explicitCooldown > 0f)
            cooldown = explicitCooldown;
        else if (attackSpeed > 0f)
            cooldown = 1f / attackSpeed;
        else
            cooldown = Mathf.Max(0f, fallbackDuration);

        _attackCooldownRemain = Mathf.Max(0f, cooldown);
    }

    // ──────────────────── Anim Speed ────────────────────

    private void SetAttackAnimSpeed(float speed)
    {
        _activeAttackAnimSpeed = Mathf.Max(0.01f, speed);

        Animator bodyAnim = _animCtrl != null ? _animCtrl.BodyAnim : null;
        if (bodyAnim != null)
            bodyAnim.speed = _activeAttackAnimSpeed;

        if (_weaponAnimCtrl != null && _weaponAnimCtrl.Anim != null)
            _weaponAnimCtrl.Anim.speed = _activeAttackAnimSpeed;
    }

    // ──────────────────── Blackboard ────────────────────

    private void SyncBlackBoardWeapon()
    {
        if (_blackBoard == null || !_blackBoard.Features.useEquipment) return;

        _blackBoard.Equipment.weaponType = _currentWeapon;
        _blackBoard.Equipment.weaponRoot = _weaponRoot;
        CharBlackBoardChangeMask mask = CharBlackBoardChangeMask.Equipment;
        if (_blackBoard.Features.useTargeting)
            mask |= CharBlackBoardChangeMask.Targeting;
        _blackBoard.MarkRuntimeChanged(mask);
    }

    private void UpdateBlackBoardAttackTarget(BasicAttackTargetInfo targetInfo)
    {
        if (_blackBoard == null || !_blackBoard.Features.useTargeting) return;
        _blackBoard.Targeting.currentTarget = targetInfo.targetUnit;
        _blackBoard.Targeting.aimPoint = targetInfo.attackPoint;
        _blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Targeting);
    }

    private void NotifyWeaponChanged()
    {
        WeaponChanged?.Invoke(_currentWeapon);
    }

    // ──────────────────── Helpers ────────────────────

    private static bool IsRangedAttackMode(BasicAttackMode mode)
    {
        return mode == BasicAttackMode.RangedStraight
            || mode == BasicAttackMode.RangedHoming
            || mode == BasicAttackMode.RangedChargeRelease;
    }

    private bool HasActiveSelfAttackAction()
    {
        return _actionCtrl != null
            && _actionCtrl.CurReq != null
            && _actionCtrl.CurReq.src == this
            && _actionCtrl.CurReq.type == CharActionType.Atk
            && _actionCtrl.State != CharActionState.Idle;
    }

    private bool CanReplaceActiveAttack(bool allowed)
    {
        return allowed
            && _actionCtrl != null
            && _actionCtrl.CurReq != null
            && _actionCtrl.CurReq.type == CharActionType.Atk
            && _actionCtrl.CurReq.src == this;
    }

    private bool IsMeleeBasicAimPresentationActive()
    {
        return _resolvedAttackMode == BasicAttackMode.MeleeCombo
            && (HasActiveSelfAttackAction() || _meleeComboAimGraceRemain > 0f);
    }

    // ──────────────────── Weapon Caching ────────────────────

    private Animator FindWeaponAnimator(Transform root)
    {
        if (root == null) return null;
        Animator bodyAnim = _animCtrl != null ? _animCtrl.BodyAnim : null;
        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null && animators[i] != bodyAnim)
                return animators[i];
        }
        return null;
    }

    private void CacheWeaponAnimator()
    {
        _weaponAnimator = FindWeaponAnimator(_weaponRoot) ?? FindWeaponAnimator(transform);
    }

    private void CacheWeaponAnimCtrl()
    {
        if (_weaponAnimCtrl != null) return;

        if (_weaponRoot != null)
        {
            _weaponAnimCtrl = _weaponRoot.GetComponentInChildren<WeaponAnimCtrl>(true);
            if (_weaponAnimCtrl == null)
                _weaponAnimCtrl = _weaponRoot.gameObject.AddComponent<WeaponAnimCtrl>();
            return;
        }

        if (_weaponAnimator != null)
        {
            _weaponAnimCtrl = _weaponAnimator.GetComponent<WeaponAnimCtrl>()
                ?? _weaponAnimator.GetComponentInParent<WeaponAnimCtrl>();
            return;
        }

        _weaponAnimCtrl = GetComponentInChildren<WeaponAnimCtrl>(true);
    }

    private void CacheWeaponRoot()
    {
        if (_weaponVisualCtrl == null)
            _weaponVisualCtrl = GetComponent<WeaponVisualCtrl>();

        _weaponRoot = CharEquipmentResolver.ResolveWeaponRoot(
            gameObject, _blackBoard, _weaponVisualCtrl, _animCtrl, _currentWeapon);
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName)) return null;
        if (root.name == childName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null) return found;
        }
        return null;
    }
}
