using System.Collections;
using UnityEngine;

// 鐢ㄤ簬鎺у埗瑙掕壊鐘舵€佸拰鍔ㄧ敾鎾斁鐨勬牳蹇冭剼鏈?
public class CharCtrl : MonoBehaviour
{

    // 鍙傛暟瀹瑰櫒锛堢帺瀹惰緭鍏ョ殑淇″彿宸蹭紶鍏ュ叾涓級
    [SerializeField] private CharParam _charParam;
    public CharParam Param => _charParam;
    // ========== MODIFICATION START | 2026骞?鏈?鏃?==========
    public Transform LockedTarget => _lockedTarget;
    // ========== MODIFICATION END | 2026骞?鏈?鏃?==========
    // ------------------------- 鍩虹缁勪欢 -------------------------
    public Vector3 moveDir;

    private float _verticalVelocity;
    private float _dodgeTimer;
    
    public float moveSpeed = 3f;
    [SerializeField] private float turnSpeedDegrees = 720f;
    [SerializeField] private float _basicAttackFaceTurnSpeedDegrees = 2160f;
    [SerializeField] private float _forcedFaceToleranceDegrees = 2f;
    
    // 鏈畬鍠勭殑鍐插埡
    private float DodgeSpeed = 4f;
    [SerializeField] private float _dodgeDuration = 0.2f;
    
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
        OnDeadAnimation();
        
        
        _lockedTarget = _aimCtrl != null ? _aimCtrl.lockedTarget : null;
        if (!isDead)
        {
            ApplyMove();
            AnimCtrl();
            if (_charParam.Dodge)
            {
                Dodge();
            }
        }
        else if (_blackBoard != null)
        {
            _blackBoard.Motion.velocity = Vector3.zero;
            _blackBoard.Motion.isMoving = false;
        }

        if (_dodgeTimer > 0f)
        {
            _dodgeTimer -= Time.deltaTime;
        }

        SyncBlackBoardMotion();
    } 

    private void ApplyMove()
    {
        moveDir = new Vector3(Param.Locomotion.x, 0, Param.Locomotion.y);
        moveDir = Quaternion.Euler(0, -45f, 0) * moveDir;
        ApplyGravity();

        float currentMoveSpeed = ResolveMoveSpeed();
        Vector3 planarMove = new Vector3(moveDir.x, 0f, moveDir.z);
        Vector3 frameMove = planarMove * currentMoveSpeed * Time.deltaTime;
        frameMove.y = moveDir.y * Time.deltaTime;
        // 褰撳墠绉诲姩鍚屾椂璇诲彇涓ゅ閿侊細
        // 1. 鐘舵€佺郴缁熷揩鐓ч噷鐨勯檺鍒?        // 2. 鍔ㄤ綔绯荤粺閲岀殑杩愯鏃堕攣
        if (!CanMoveByState() || _movementLocked || (_actionCtrl != null && _actionCtrl.IsMoveLocked()))
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

        if (Param.isLock == false || !_lockedTarget)
        {
            Vector3 lookDirection = new Vector3(moveDir.x, 0f, moveDir.z);
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                RotateTowardsDirection(lookDirection);
            }
        }
        else if (_lockedTarget != null)
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
    
    private void Dodge()
    {
        _dodgeTimer = Mathf.Max(_dodgeTimer, _dodgeDuration);
    }
    // ------------------------- 娴嬭瘯鐢ㄥ姩鐢绘帶鍒?-------------------------
    private void AnimCtrl()
    {
        if (_animCtrl == null)
        {
            return;
        }

        Vector3 planarMove = new Vector3(moveDir.x, 0f, moveDir.z);
        if (_weaponCtrl != null && _weaponCtrl.ShouldSuppressMoveAnimation())
        {
            planarMove = Vector3.zero;
        }

        _animCtrl.SetMove(planarMove, CanMoveByState(), Param.isLock, _lockedTarget);
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
        // Once the blackboard exists, movement should prefer the folded
        // state stored there instead of recomputing from multiple sources.
        return CharRuntimeResolver.CanMove(gameObject);
    }

    private bool CanRotateByState()
    {
        return CharRuntimeResolver.CanRotate(gameObject);
    }

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
        if (_dodgeTimer > 0f)
        {
            finalSpeed *= Mathf.Max(1f, DodgeSpeed);
        }

        return Mathf.Max(0f, finalSpeed);
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
}

