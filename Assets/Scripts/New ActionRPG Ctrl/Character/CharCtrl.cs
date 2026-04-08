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
    
    public float moveSpeed = 3f;
    [SerializeField] private float turnSpeedDegrees = 720f;
    
    // 鏈畬鍠勭殑鍐插埡
    private float DodgeSpeed = 4f;
    
    private CharAimCtrl _aimCtrl;
    // 鏂板锛氱敤浜庡瓨鍌ㄥ綋鍓嶉攣瀹氱洰鏍?
    private Transform _lockedTarget;
    private Vector3 _forcedFaceDirection;
    private float _forcedFaceTimer;
    private bool _movementLocked;
    private bool _skillFacingActive;
    private Vector3 _skillFacingDirection;
    
    private CharacterController _characterController;
    
    
    private CharAnimCtrl _animCtrl;
    private CharStatusCtrl _statusCtrl;
    private CharStatusVfxCtrl _statusVfxCtrl;
    private CharActionCtrl _actionCtrl;
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
                print("Dodge");
                Dodge();
            }
        }

        SyncBlackBoardMotion();
    } 

    private void ApplyMove()
    {
        moveDir = new Vector3(Param.Locomotion.x, 0, Param.Locomotion.y);
        moveDir = Quaternion.Euler(0, -45f, 0) * moveDir;
        ApplyGravity();

        Vector3 frameMove = moveDir * moveSpeed * Time.deltaTime;
        // 褰撳墠绉诲姩鍚屾椂璇诲彇涓ゅ閿侊細
        // 1. 鐘舵€佺郴缁熷揩鐓ч噷鐨勯檺鍒?        // 2. 鍔ㄤ綔绯荤粺閲岀殑杩愯鏃堕攣
        if (!CanMoveByState() || _movementLocked || (_actionCtrl != null && _actionCtrl.IsMoveLocked()))
        {
            frameMove.x = 0f;
            frameMove.z = 0f;
        }

        if (frameMove.sqrMagnitude > 0f)
        {
            _characterController.Move(frameMove);
        }

        if (_forcedFaceTimer > 0f)
        {
            _forcedFaceTimer -= Time.deltaTime;
            RotateTowardsDirection(_forcedFaceDirection);
            return;
        }

        if (_skillFacingActive)
        {
            // Skill-facing keeps rotation inside CharCtrl so actions stay lightweight.
            RotateTowardsDirection(_skillFacingDirection);
            return;
        }

        if (!CanRotateByState() || (_actionCtrl != null && _actionCtrl.IsRotateLocked()))
        {
            return;
        }

        if (Param.isLock == false || !_lockedTarget)
        {
            Vector3 lookDirection = new Vector3(moveDir.x, 0f, moveDir.z);
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }
        else if (_lockedTarget != null)
        {
            Vector3 lookDirection = _lockedTarget.position - transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }
    }

    public void ForceFaceDirection(Vector3 direction, float duration)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        _forcedFaceDirection = direction.normalized;
        _forcedFaceTimer = Mathf.Max(_forcedFaceTimer, duration);
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

    public bool RotateTowardsDirection(Vector3 direction, float toleranceDegrees = 0f)
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
            turnSpeedDegrees * Time.deltaTime);

        return Quaternion.Angle(transform.rotation, targetRotation) <= toleranceDegrees;
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
        moveSpeed *= DodgeSpeed;
        StartCoroutine(EndDodge());
    }

    private IEnumerator EndDodge()
    {
        float DodgeTime = .2f;
        yield return new WaitForSeconds(DodgeTime);
        moveSpeed /= DodgeSpeed;
    }
    // ------------------------- 娴嬭瘯鐢ㄥ姩鐢绘帶鍒?-------------------------
    private void AnimCtrl()
    {
        if (_animCtrl == null)
        {
            return;
        }

        _animCtrl.SetMove(moveDir, CanMoveByState(), Param.isLock, _lockedTarget);
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
        _blackBoard.Motion.moveVector = moveDir;
        _blackBoard.Motion.baseMoveSpeed = moveSpeed;
        _blackBoard.Motion.baseTurnSpeed = turnSpeedDegrees;
        _blackBoard.Motion.isMoving = moveDir.sqrMagnitude > 0.001f;

        if (_blackBoard.Features.useTargeting)
        {
            _blackBoard.Targeting.lockedTarget = _lockedTarget;
        }
    }

    private bool ResolveIsDead()
    {
        return CharRuntimeResolver.IsDead(gameObject);
    }
}

