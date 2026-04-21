using System.Collections;
using UnityEngine;

// 鐢ㄤ簬鎺у埗瑙掕壊鐘舵€佸拰鍔ㄧ敾鎾斁鐨勬牳蹇冭剼鏈?
/// <summary>
/// 角色本体控制器。
/// 直接负责位移、重力、朝向、闪避和死亡下的控制开关，
/// 并把这些运行结果同步给动画系统与黑板。
/// </summary>
public class CharCtrl : MonoBehaviour
{
    // 鍙傛暟瀹瑰櫒锛堢帺瀹惰緭鍏ョ殑淇″彿宸蹭紶鍏ュ叾涓級
    [SerializeField] private CharParam _charParam;
    public CharParam Param => _charParam;
    // ========== MODIFICATION START | 2026骞?鏈?鏃?==========
    public Transform LockedTarget => _lockedTarget;
    public bool IsLocking => _aimCtrl != null && _aimCtrl.IsLockModeActive;
    // ========== MODIFICATION END | 2026骞?鏈?鏃?==========
    // ------------------------- 鍩虹缁勪欢 -------------------------
    public Vector3 moveDir;

    private float _verticalVelocity;
    private float _dodgeRemain;
    private Vector3 _dodgeDirection = Vector3.forward;
    
    public float moveSpeed = 3f;
    [SerializeField] private float turnSpeedDegrees = 720f;
    [SerializeField] private float _basicAttackFaceTurnSpeedDegrees = 2160f;
    [SerializeField] private float _forcedFaceToleranceDegrees = 2f;
    
    [SerializeField] private float _dodgeDuration = 0.2f;
    [SerializeField] private float _dodgeDistance = 2.6f;
    [SerializeField] private float _dodgeInputDeadzone = 0.15f;
    [SerializeField] private float _forwardDodgeAngleThreshold = 45f;
    [SerializeField] private GameObject _dodgeVfxPrefab;
    [SerializeField] private Transform _dodgeVfxMount;
    [SerializeField] private Vector3 _dodgeVfxOffset;
    [SerializeField] private bool _attachDodgeVfxToMount = true;
    [SerializeField] private float _dodgeVfxLifetime = 1.5f;
    
    private CharAimCtrl _aimCtrl;
    // 鏂板锛氱敤浜庡瓨鍌ㄥ綋鍓嶉攣瀹氱洰鏍?
    private Transform _lockedTarget;
    private Vector3 _forcedFaceDirection;
    private float _forcedFaceTimer;
    private bool _forcedFaceUntilAligned;
    private float _forcedFaceTurnSpeedOverride = -1f;
    private bool _movementLocked;
    private bool _skillFacingActive;
    private Vector3 _skillFacingDirection;
    
    private CharacterController _characterController;
    
    
    private CharAnimCtrl _animCtrl;
    private CharStatusCtrl _statusCtrl;
    private CharStatusVfxCtrl _statusVfxCtrl;
    private CharActionCtrl _actionCtrl;
    private CharWeaponCtrl _weaponCtrl;
    private CharBlackBoard _blackBoard;
    private bool isDead;
    // 娴嬭瘯鐢ㄧ姸鎬佹帶鍒?   
    
    
    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animCtrl = GetComponent<CharAnimCtrl>();
        if (_animCtrl == null)
        {
            _animCtrl = gameObject.AddComponent<CharAnimCtrl>();
        }
        _aimCtrl = GetComponent<CharAimCtrl>();
        _weaponCtrl = GetComponent<CharWeaponCtrl>();
        //娴嬭瘯鐢ㄥ姩鐢绘帶鍒剁浉鍏宠剼鏈?        
        //娴嬭瘯鐢ㄧ姸鎬佹帶鍒剁浉鍏宠剼鏈?
        _statusCtrl = GetComponent<CharStatusCtrl>();
        _statusVfxCtrl = GetComponent<CharStatusVfxCtrl>();
        _blackBoard = GetComponent<CharBlackBoard>();
        if (_statusCtrl != null && _statusVfxCtrl == null)
        {
            _statusVfxCtrl = gameObject.AddComponent<CharStatusVfxCtrl>();
        }
        _actionCtrl = GetComponent<CharActionCtrl>();
        
        //娴嬭瘯鐢ㄧ姸鎬佹帶鍒剁浉鍏宠剼鏈?
    }

    protected void Update()
    {
        isDead = ResolveIsDead();
        SyncDeathState();
        OnDeadAnimation();
        
        
        _lockedTarget = _aimCtrl != null ? _aimCtrl.lockedTarget : null;
        if (_charParam != null)
        {
            _charParam.isLock = IsLocking;
        }
        if (ShouldAbortActiveDodge())
        {
            ClearDodgeState();
        }
        if (!isDead)
        {
            if (_charParam != null && _charParam.Dodge)
            {
                TryStartDodge();
            }

            ApplyMove();
            AnimCtrl();
        }
        else if (_blackBoard != null)
        {
            ClearDodgeState();
            moveDir = Vector3.zero;
            _blackBoard.Motion.velocity = Vector3.zero;
            _blackBoard.Motion.isMoving = false;
        }
        else
        {
            ClearDodgeState();
            moveDir = Vector3.zero;
        }

        if (_dodgeRemain > 0f)
        {
            _dodgeRemain -= Time.deltaTime;
            if (_dodgeRemain <= 0f)
            {
                ClearDodgeState();
            }
        }

        SyncBlackBoardMotion();
    } 

    private void SyncDeathState()
    {
        if (_characterController == null)
        {
            return;
        }

        bool shouldEnableController = !isDead;
        if (_characterController.enabled == shouldEnableController)
        {
            return;
        }

        _characterController.enabled = shouldEnableController;
    }

    /// <summary>
    /// 角色移动主流程。
    /// 顺序上依次处理：输入转位移、重力、状态限制、角色位移、强制转向、技能转向、普通朝向。
    /// </summary>
    private void ApplyMove()
    {
        Vector3 planarDirection = IsDodgingActive()
            ? _dodgeDirection
            : ResolveMoveDirectionFromInput(Param.Locomotion);
        moveDir = planarDirection;
        ApplyGravity();

        float currentMoveSpeed = IsDodgingActive()
            ? ResolveDodgeSpeed()
            : ResolveMoveSpeed();
        Vector3 planarMove = new Vector3(planarDirection.x, 0f, planarDirection.z);
        Vector3 frameMove = planarMove * currentMoveSpeed * Time.deltaTime;
        frameMove.y = moveDir.y * Time.deltaTime;
        // 褰撳墠绉诲姩鍚屾椂璇诲彇涓ゅ閿侊細
        // 1. 鐘舵€佺郴缁熷揩鐓ч噷鐨勯檺鍒?        // 2. 鍔ㄤ綔绯荤粺閲岀殑杩愯鏃堕攣
        if (!IsDodgingActive() &&
            (!CanMoveByState() || _movementLocked || (_actionCtrl != null && _actionCtrl.IsMoveLocked())))
        {
            frameMove.x = 0f;
            frameMove.z = 0f;
        }

        Vector3 appliedMove = frameMove;
        _characterController.Move(frameMove);

        if (_forcedFaceTimer > 0f || _forcedFaceUntilAligned)
        {
            if (_forcedFaceTimer > 0f)
            {
                _forcedFaceTimer -= Time.deltaTime;
            }

            bool aligned = RotateTowardsDirection(
                _forcedFaceDirection,
                _forcedFaceToleranceDegrees,
                ResolveForcedFaceTurnSpeed());
            if (aligned && _forcedFaceTimer <= 0f)
            {
                _forcedFaceUntilAligned = false;
                _forcedFaceTurnSpeedOverride = -1f;
            }

            SyncVelocity(appliedMove);
            return;
        }

        if (IsDodgingActive())
        {
            SyncVelocity(appliedMove);
            return;
        }

        if (_skillFacingActive)
        {
            // Skill-facing keeps rotation inside CharCtrl so actions stay lightweight.
            RotateTowardsDirection(_skillFacingDirection);
            SyncVelocity(appliedMove);
            return;
        }

        if (!CanRotateByState() || (_actionCtrl != null && _actionCtrl.IsRotateLocked()))
        {
            SyncVelocity(appliedMove);
            return;
        }

        if (_weaponCtrl != null && _weaponCtrl.TryGetMeleeAttackAimDirection(out Vector3 meleeAimDirection))
        {
            RotateBasicAttackTowardsDirection(meleeAimDirection, _forcedFaceToleranceDegrees);
            SyncVelocity(appliedMove);
            return;
        }

        bool isLocking = IsLocking;
        if (!isLocking &&
            _weaponCtrl != null &&
            _weaponCtrl.ShouldKeepDirectionalAimFacingWhileMoving() &&
            TryGetDirectionalAimDirection(out Vector3 directionalAimDirection))
        {
            RotateTowardsDirection(directionalAimDirection);
            SyncVelocity(appliedMove);
            return;
        }

        // 非八向角色始终面朝移动方向；八向角色锁定时面朝敌人
        bool faceLockTarget = isLocking && _lockedTarget != null && Has8DirLocomotion();

        if (!faceLockTarget)
        {
            Vector3 lookDirection = new Vector3(moveDir.x, 0f, moveDir.z);
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                RotateTowardsDirection(lookDirection);
            }
        }
        else
        {
            Vector3 lookDirection = _lockedTarget.position - transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.01f)
            {
                RotateTowardsDirection(lookDirection);
            }
        }

        SyncVelocity(appliedMove);
    }

    /// <summary>
    /// 临时强制角色朝向某个方向，常给攻击/技能使用。
    /// </summary>
    public void ForceFaceDirection(Vector3 direction, float duration, bool keepUntilAligned = false)
    {
        ApplyForcedFaceRequest(direction, duration, keepUntilAligned, -1f);
    }

    public void ForceBasicAttackFaceDirection(Vector3 direction, float duration, bool keepUntilAligned = true)
    {
        ApplyForcedFaceRequest(
            direction,
            duration,
            keepUntilAligned,
            Mathf.Max(_basicAttackFaceTurnSpeedDegrees, turnSpeedDegrees));
    }

    private void ApplyForcedFaceRequest(
        Vector3 direction,
        float duration,
        bool keepUntilAligned,
        float turnSpeedOverride)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        _forcedFaceDirection = direction.normalized;
        _forcedFaceTimer = Mathf.Max(_forcedFaceTimer, duration);
        _forcedFaceUntilAligned = _forcedFaceUntilAligned || keepUntilAligned;
        _forcedFaceTurnSpeedOverride = turnSpeedOverride;
    }

    public void SetMovementLocked(bool locked)
    {
        _movementLocked = locked;
    }

    /// <summary>
    /// 进入技能转向阶段。
    /// CharActionCtrl 在 waitFace 流程里会调用它。
    /// </summary>
    public void BeginSkillFacing(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        // 杩欐槸涓€涓交閲忚繍琛屾椂鍏ュ彛锛屽綋鍓嶇敤浜庢妧鑳借浆鍚戠瓑寰咃紝
        // Lightweight runtime facing request used by cast-facing and future forced facing.
        _skillFacingDirection = direction.normalized;
        _skillFacingActive = true;
    }

    public void EndSkillFacing()
    {
        _skillFacingActive = false;
    }

    public bool IsFacingDirection(Vector3 direction, float toleranceDegrees)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return true;
        }

        return Vector3.Angle(transform.forward, direction.normalized) <= toleranceDegrees;
    }

    /// <summary>
    /// 朝指定方向平滑转向，并返回当前是否已经转到容差范围内。
    /// </summary>
    public bool RotateTowardsDirection(Vector3 direction, float toleranceDegrees = 0f, float turnSpeedOverride = -1f)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return true;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            ResolveTurnSpeed(turnSpeedOverride) * Time.deltaTime);

        return Quaternion.Angle(transform.rotation, targetRotation) <= toleranceDegrees;
    }

    public bool RotateBasicAttackTowardsDirection(Vector3 direction, float toleranceDegrees = 0f)
    {
        return RotateTowardsDirection(
            direction,
            toleranceDegrees,
            Mathf.Max(_basicAttackFaceTurnSpeedDegrees, turnSpeedDegrees));
    }

    public bool TryGetDirectionalAimDirection(out Vector3 direction)
    {
        direction = Vector3.zero;
        return _aimCtrl != null && _aimCtrl.TryGetDirectionalAimDirection(out direction);
    }

    public bool TryGetAttackFacingDirection(out Vector3 direction)
    {
        direction = Vector3.zero;
        if (_charParam == null || _charParam.AttackFacingInput.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        direction = ResolveMoveDirectionFromInput(_charParam.AttackFacingInput);
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = Vector3.zero;
            return false;
        }

        direction.Normalize();
        return true;
    }

    private static Vector3 ResolveMoveDirectionFromInput(Vector2 input)
    {
        Vector3 direction = new Vector3(input.x, 0f, input.y);
        return Quaternion.Euler(0f, -45f, 0f) * direction;
    }

    private void ApplyGravity()
    {
        if (_characterController.isGrounded == false)
        {
            _verticalVelocity = _verticalVelocity - 9.81f * Time.deltaTime;
            moveDir.y = _verticalVelocity;
        }
        else
            _verticalVelocity = -0.5f;
    }
    
    private bool TryStartDodge()
    {
        if (_charParam == null || _characterController == null || !_characterController.enabled)
        {
            return false;
        }

        if (isDead || IsDodgingActive() || _dodgeDuration <= 0f || !CanMoveByState())
        {
            return false;
        }

        Vector3 dodgeDirection = ResolveDodgeDirection();
        if (dodgeDirection.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        bool playForwardDodgeAnim = IsForwardDodge(dodgeDirection);

        if (_weaponCtrl != null)
        {
            _weaponCtrl.PrepareForDodgeStart();
        }

        if (_actionCtrl != null)
        {
            CharActionReq req = new CharActionReq
            {
                type = CharActionType.Dodge,
                state = CharActionState.Dodging,
                src = this,
                dur = Mathf.Max(0.01f, _dodgeDuration),
                lockMove = true,
                lockRotate = true,
                interruptible = true,
                animKey = playForwardDodgeAnim ? "Dodge" : string.Empty,
            };

            if (!_actionCtrl.TryStart(req))
            {
                return false;
            }
        }
        else if (playForwardDodgeAnim && _animCtrl != null)
        {
            _animCtrl.PlayDodge();
        }

        _dodgeDirection = dodgeDirection;
        _dodgeRemain = Mathf.Max(0.01f, _dodgeDuration);

        if (playForwardDodgeAnim)
        {
            ForceFaceDirection(dodgeDirection, _dodgeDuration, true);
        }

        PlayDodgeVfx(dodgeDirection);
        return true;
    }
    // ------------------------- 娴嬭瘯鐢ㄥ姩鐢绘帶鍒?-------------------------
    /// <summary>
    /// 把移动结果写给身体动画控制器。
    /// </summary>
    private void AnimCtrl()
    {
        if (_animCtrl == null)
        {
            return;
        }

        Vector3 planarMove = IsDodgingActive()
            ? Vector3.zero
            : new Vector3(moveDir.x, 0f, moveDir.z);
        if (!IsDodgingActive() && _weaponCtrl != null && _weaponCtrl.ShouldSuppressMoveAnimation())
        {
            planarMove = Vector3.zero;
        }

        _animCtrl.SetMove(
            planarMove,
            CanMoveByState(),
            IsLocking,
            _lockedTarget,
            ShouldUseDirectionalStrafeLocomotion());
    }
    // ------------------------- 娴嬭瘯鐢ㄥ姩鐢绘帶鍒?-------------------------
    
    // ------------------------- 娴嬭瘯鐢ㄧ姸鎬佹帶鍒?-------------------------
    
    //鎺у埗瑙掕壊姝讳骸
    public void OnDeadAnimation()
    {
        if (_animCtrl != null)
        {
            _animCtrl.SetDead(isDead);
        }
    }
    private bool CanMoveByState()
    {
        return CharRuntimeResolver.CanMove(gameObject);
    }

    private bool CanRotateByState()
    {
        return CharRuntimeResolver.CanRotate(gameObject);
    }

    private bool Has8DirLocomotion()
    {
        return _animCtrl != null && _animCtrl.Has8DirLocomotion;
    }

    private bool ShouldUseDirectionalStrafeLocomotion()
    {
        return !IsLocking &&
               _weaponCtrl != null &&
               _weaponCtrl.ShouldUseDirectionalStrafeLocomotion() &&
               TryGetDirectionalAimDirection(out _);
    }

    /// <summary>
    /// 将当前帧的移动、输入和锁定目标信息同步到黑板。
    /// </summary>
    private void SyncBlackBoardMotion()
    {
        if (_blackBoard == null || _charParam == null)
        {
            return;
        }

        _blackBoard.SyncFromScene();
        _blackBoard.Motion.moveInput = _charParam.Locomotion;
        _blackBoard.Motion.aimInput = _charParam.AimTarget;
        _blackBoard.Motion.moveVector = new Vector3(moveDir.x, 0f, moveDir.z);
        _blackBoard.Motion.baseMoveSpeed = moveSpeed;
        _blackBoard.Motion.baseTurnSpeed = turnSpeedDegrees;
        _blackBoard.Motion.isMoving = _blackBoard.Motion.moveVector.sqrMagnitude > 0.001f;

        if (_blackBoard.Features.useTargeting)
        {
            _blackBoard.Targeting.lockedTarget = _lockedTarget;
        }

        CharBlackBoardChangeMask changeMask = CharBlackBoardChangeMask.Motion;
        if (_blackBoard.Features.useTargeting)
        {
            changeMask |= CharBlackBoardChangeMask.Targeting;
        }

        _blackBoard.MarkRuntimeChanged(changeMask);
    }

    private bool ResolveIsDead()
    {
        return CharRuntimeResolver.IsDead(gameObject);
    }

    private float ResolveMoveSpeed()
    {
        float finalSpeed = CharRuntimeResolver.GetMoveSpeed(gameObject, moveSpeed);
        return Mathf.Max(0f, finalSpeed);
    }

    private float ResolveDodgeSpeed()
    {
        return _dodgeDuration > 0.001f
            ? Mathf.Max(0f, _dodgeDistance) / _dodgeDuration
            : 0f;
    }

    private float ResolveTurnSpeed(float turnSpeedOverride = -1f)
    {
        if (turnSpeedOverride > 0f)
        {
            return turnSpeedOverride;
        }

        return CharRuntimeResolver.GetTurnSpeed(gameObject, turnSpeedDegrees);
    }

    private float ResolveForcedFaceTurnSpeed()
    {
        return ResolveTurnSpeed(_forcedFaceTurnSpeedOverride);
    }

    private void SyncVelocity(Vector3 frameMove)
    {
        if (_blackBoard == null)
        {
            return;
        }

        Vector3 planarFrameMove = new Vector3(frameMove.x, 0f, frameMove.z);
        _blackBoard.Motion.velocity = Time.deltaTime > 0f
            ? planarFrameMove / Time.deltaTime
            : Vector3.zero;
    }

    private bool IsDodgingActive()
    {
        return _dodgeRemain > 0f;
    }

    private Vector3 ResolveDodgeDirection()
    {
        Vector3 inputDirection = ResolveMoveDirectionFromInput(_charParam.Locomotion);
        inputDirection.y = 0f;
        if (inputDirection.sqrMagnitude > _dodgeInputDeadzone * _dodgeInputDeadzone)
        {
            return inputDirection.normalized;
        }

        Vector3 facingDirection = transform.forward;
        facingDirection.y = 0f;
        if (facingDirection.sqrMagnitude <= 0.001f)
        {
            return Vector3.forward;
        }

        return facingDirection.normalized;
    }

    private bool IsForwardDodge(Vector3 dodgeDirection)
    {
        Vector3 facingDirection = transform.forward;
        facingDirection.y = 0f;
        if (facingDirection.sqrMagnitude <= 0.001f || dodgeDirection.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        return Vector3.Angle(facingDirection.normalized, dodgeDirection.normalized)
               <= Mathf.Clamp(_forwardDodgeAngleThreshold, 0f, 180f);
    }

    private void PlayDodgeVfx(Vector3 dodgeDirection)
    {
        if (_dodgeVfxPrefab == null || dodgeDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Transform mount = ResolveDodgeVfxMount();
        Quaternion rotation = Quaternion.LookRotation(dodgeDirection.normalized, Vector3.up);
        GameObject instance;

        if (_attachDodgeVfxToMount && mount != null)
        {
            instance = Instantiate(_dodgeVfxPrefab, mount.position, rotation, mount);
            instance.transform.localPosition += _dodgeVfxOffset;
        }
        else
        {
            Vector3 basePosition = mount != null ? mount.position : transform.position;
            instance = Instantiate(_dodgeVfxPrefab, basePosition + rotation * _dodgeVfxOffset, rotation);
        }

        if (_dodgeVfxLifetime > 0f)
        {
            Destroy(instance, _dodgeVfxLifetime);
        }
    }

    private Transform ResolveDodgeVfxMount()
    {
        if (_dodgeVfxMount != null)
        {
            return _dodgeVfxMount;
        }

        if (_animCtrl != null && _animCtrl.BodyAnim != null)
        {
            return _animCtrl.BodyAnim.transform;
        }

        return transform;
    }

    private bool HasActiveSelfDodgeAction()
    {
        return _actionCtrl != null
               && _actionCtrl.CurReq != null
               && _actionCtrl.CurReq.src == this
               && _actionCtrl.CurReq.type == CharActionType.Dodge
               && _actionCtrl.State != CharActionState.Idle;
    }

    private bool ShouldAbortActiveDodge()
    {
        return IsDodgingActive() && _actionCtrl != null && !HasActiveSelfDodgeAction();
    }

    private void ClearDodgeState()
    {
        _dodgeRemain = 0f;
        _dodgeDirection = Vector3.zero;
    }

}
