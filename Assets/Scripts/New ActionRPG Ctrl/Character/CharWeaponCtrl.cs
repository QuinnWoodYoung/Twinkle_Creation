using UnityEngine;

public class CharWeaponCtrl : MonoBehaviour
{
    private CharCtrl _charCtrl;
    private Animator _animator;
    [SerializeField]private Animator _weaponAnimator;
    
    [SerializeField] public WeaponType _currentWeapon;
    
    //——————
    private bool _attackTrigger;
    private bool _lastAttackTrigger;
    //——————

    private float _LerpTarget;  //平滑攻击动画层动画
    private float _currentWeight;
    private string LayerName = "Sword";

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private Transform bulletSpawnPoint;
    
    
    protected void Awake()
    {
        _charCtrl = GetComponent<CharCtrl>();
        _animator = GetComponentInChildren<Animator>();
        //_weaponAnimator = GetComponentInChildren<Animator>();
    }
    

    private void Update()
    {
        AttackInputState input = _charCtrl.Param.AttackState;

        // 我们在按键松开的这一帧检查输入。
        if (input.isUp)
        {
            // 用于区分短按和长按的阈值。你可以调整这个值。
            const float heavyAttackThreshold = 0.3f;

            if (input.holdDuration < heavyAttackThreshold)
            {
                // 这是一个“短按”（Tap）。
                Debug.Log("轻攻击触发! (按键时长: " + input.holdDuration + ")");
                _animator.SetTrigger("Attack"); // 假设 "Attack" 是轻攻击的触发器。
                _weaponAnimator.SetTrigger("Attack");
                if (_currentWeapon == WeaponType.Bow)
                {
                    Shoot();
                }
                //
            }
            else
            {
                // 这是一个“长按”（Hold）。
                Debug.Log("重攻击触发! (按键时长: " + input.holdDuration + ")");
                // 你可以在这里触发你的重攻击动画，例如：
                // _animator.SetTrigger("HeavyAttack");
            }
        }

        /*
        // 你也可以使用其他状态来实现不同机制，例如：
        if (input.isDown)
        {
            // 瞬间按下时就触发的逻辑，比如举盾。
        }
        if (input.isHeld)
        {
            // 按住期间持续触发的逻辑，比如给激光充能。
        }
        */
        
        /* --- 旧版 Update 逻辑，留作参考 ---
        if (_charCtrl.Param.Attack)
        {
            Debug.Log("Triggering Attack!");
            _animator.SetTrigger("Attack");
            _charCtrl.Param.Attack = false;  
        }
        */
    }

    private void Shoot()
    {
        GameObject newBullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.LookRotation(bulletSpawnPoint.forward));

        
        newBullet.GetComponent<Rigidbody>().velocity = bulletSpawnPoint.forward * bulletSpeed;
        newBullet.GetComponent<Bullet>().launcher = gameObject;
        
        //Destroy(newBullet, 5f);
    }

    
//     public void OnAttack1hEnter()
//     {
//         _LerpTarget = 1.0f;
//     
//     }
//     
//     public void OnAttack1hAUpdate()
//     {
//         _currentWeight = _animator.GetLayerWeight(_animator.GetLayerIndex(LayerName));
//         _currentWeight = Mathf.Lerp(_currentWeight, _LerpTarget, 0.1f);
//         _animator.SetLayerWeight(_animator.GetLayerIndex(LayerName),_currentWeight);
//     }
//     public void OnAttackIdle()
//     {
//         _LerpTarget = 0.0f;
//         // if (_charCtrl.Param.Attack)// && !_previousAttackState
//         // {
//         //     _animator.SetTrigger("Attack");
//         // }
//     }
//     
//     public void OnAttackIdleUpdate()
//     {
//         _currentWeight = _animator.GetLayerWeight(_animator.GetLayerIndex(LayerName));
//         _currentWeight = Mathf.Lerp(_currentWeight, _LerpTarget, 0.1f);
//         _animator.SetLayerWeight(_animator.GetLayerIndex(LayerName),_currentWeight);
//     }
}
