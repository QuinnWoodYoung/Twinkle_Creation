using System.Collections;
using UnityEngine;

// 用于控制角色状态和动画播放的核心脚本
public class CharCtrl : MonoBehaviour
{

    // 参数容器（玩家输入的信号已传入其中）
    [SerializeField] private CharParam _charParam;
    public CharParam Param => _charParam;
    // ========== MODIFICATION START | 2026年2月4日 ==========
    public Transform LockedTarget => _lockedTarget;
    // ========== MODIFICATION END | 2026年2月4日 ==========
    // ------------------------- 基础组件 -------------------------
    public Vector3 moveDir;

    private Vector3 LookDir;
 
    private float _verticalVelocity;
    
    public float moveSpeed = 3f;
    [SerializeField] private float turnSpeedDegrees = 720f;
    
    // 未完善的冲刺
    private float DodgeSpeed = 4f;
    
    private CharAimCtrl _aimCtrl;
    // 新增：用于存储当前锁定目标
    private Transform _lockedTarget;
    private Vector3 _forcedFaceDirection;
    private float _forcedFaceTimer;
    private bool _movementLocked;
    private bool _skillFacingActive;
    private Vector3 _skillFacingDirection;
    
    private CharacterController _characterController;
    
    
    // ------------------------- 测试用动画控制 -------------------------
    private Animator _animator;

    // 测试用动画控制     
    
    // ------------------------- 测试用状态控制 -------------------------
    private StateManager _stateManager;
    private bool isDead;
    // 测试用状态控制    
    
    
    void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        //测试用动画控制相关脚本
        _animator = GetComponentInChildren<Animator>();
        _aimCtrl = GetComponent<CharAimCtrl>();
        //测试用动画控制相关脚本
        
        //测试用状态控制相关脚本
        _stateManager = GetComponent<StateManager>();
        
        //测试用状态控制相关脚本
    }

    protected void Update()
    {
        isDead = _stateManager != null && _stateManager.HitPoint <= 0f;
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

        
    } 

    private void ApplyMove()
    {
        // ========== MODIFICATION START | 2026年2月4日 ==========
        // 如果角色处于眩晕状态，则阻止其移动。
        if (_stateManager != null && !_stateManager.CanMove)
        {
            // 可以在这里额外处理一些眩晕时的逻辑，例如打断当前动画
            // _animator.SetFloat("xVelocity", 0);
            // _animator.SetFloat("zVelocity", 0);
            return;
        }
        // ========== MODIFICATION END | 2026年2月4日 ==========

        moveDir = new Vector3(Param.Locomotion.x, 0, Param.Locomotion.y);
        moveDir = Quaternion.Euler(0, -45f, 0) * moveDir;
        ApplyGravity();
        
        Vector3 frameMove = moveDir * moveSpeed * Time.deltaTime;
        if (_movementLocked)
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
            RotateTowardsDirection(_skillFacingDirection);
            return;
        }
        
        if (Param.isLock == false || !_lockedTarget)
        {
            // 创建一个只包含水平方向的向量用于计算朝向，避免角色低头。
            Vector3 lookDirection = new Vector3(moveDir.x, 0f, moveDir.z);

            // 只有在有实际的水平移动时，才进行转向。
            // (sqrMagnitude 比 magnitude 效率更高)
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                // 计算目标朝向
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

                // 使用 Slerp 进行平滑的球形插值转向
                // 10f 是旋转速度，数值越大转得越快，你可以根据手感调整
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime *
                    10f);
            }
        }
        else
        {
            // [锁定状态]：如果存在锁定目标，则朝向目标
            if (_lockedTarget != null)
            {
                // 计算从角色到目标的向量
                Vector3 lookDirection = _lockedTarget.position - transform.position;
                lookDirection.y = 0; // 确保只在水平面上旋转

                if (lookDirection.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,Time.deltaTime * 10f);
                }
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
    // ------------------------- 测试用动画控制 -------------------------
    private void AnimCtrl()
    {
        if (_animator == null)
        {
            return;
        }

        if (!Param.isLock && _lockedTarget)
        {
            float zVelocity = Vector3.Dot(moveDir.normalized,transform.forward);
            _animator.SetFloat("zVelocity", zVelocity, 0.1f, Time.deltaTime);
        }
        else
        {
            float xVelocity = Vector3.Dot(moveDir.normalized,transform.right) ;
            float zVelocity = Vector3.Dot(moveDir.normalized,transform.forward);
        
            _animator.SetFloat("xVelocity", xVelocity, 0.1f, Time.deltaTime);
            _animator.SetFloat("zVelocity", zVelocity, 0.1f, Time.deltaTime);
        }
    }
    // ------------------------- 测试用动画控制 -------------------------
    
    // ------------------------- 测试用状态控制 -------------------------
    
    //控制角色死亡
    public void OnDeadAnimation()
    {
        if (_animator != null)
        {
            _animator.SetBool("dead", isDead);
        }
    }
    // ------------------------- 测试用状态控制 -------------------------
}
