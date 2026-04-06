using UnityEngine;
// ========== MODIFICATION START | 2026年2月3日 ==========
// 修复：添加 System.Collections.Generic 命名空间引用，以便使用 List<T>。
using System.Collections.Generic;
// ========== MODIFICATION END | 2026年2月3日 ==========

public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager instance;
    PlayerInputMap playerInputMap;

    // --- Input Actions ---                                                                           │
    //public event System.Action OnAttackInput; 
    [SerializeField] public Vector2 playerInputMovementValue;
    [SerializeField] public Vector2 playerInputAimValue;
    [SerializeField] public bool playerInputAttackValue;
    [SerializeField] public bool playerInputLockValue;
    [SerializeField] public bool playerInputDodgeValue;

    // ========== MODIFICATION START | 2026年2月3日 ==========
    // 新增：用于存储技能按键持续状态的列表 (按下为true, 抬起为false)。
    // 假设最多有4个技能输入槽位，因此初始化列表包含4个布尔值。
    public readonly List<bool> PlayerInputSkillValues = new List<bool> { false, false, false, false };
    // ========== MODIFICATION END | 2026年2月3日 ==========
    
    

    
    private void Awake()
    {
        if(instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (playerInputMap == null)
        {
            playerInputMap = new PlayerInputMap();
            
            playerInputMap.GamePlay.Movement.performed += i => playerInputMovementValue = i.ReadValue<Vector2>();
            playerInputMap.GamePlay.Movement.canceled += i => playerInputMovementValue = Vector2.zero;
            
            playerInputMap.GamePlay.Aim.performed += i => playerInputAimValue = i.ReadValue<Vector2>();
            playerInputMap.GamePlay.Aim.canceled += i => playerInputAimValue = Vector2.zero;
            
            playerInputMap.GamePlay.Attack.performed += i => playerInputAttackValue = true;
            playerInputMap.GamePlay.Attack.canceled += i =>playerInputAttackValue = false;
            //playerInputMap.GamePlay.Attack.performed += _ => OnAttackInput?.Invoke();  
            // playerInputMap.GamePlay.Lock.performed += i => playerInputLockValue = true;
            // playerInputMap.GamePlay.Lock.canceled += i => playerInputLockValue = false;
            
            playerInputMap.GamePlay.Lock.performed += i => playerInputLockValue = !playerInputLockValue;
            
            playerInputMap.GamePlay.Dodge.performed += i => playerInputDodgeValue = true;
            playerInputMap.GamePlay.Dodge.canceled += i => playerInputDodgeValue = false;

            // ========== MODIFICATION START | 2026年2月3日 ==========
            // 新增：为技能按键注册回调函数。
            // 注意：您需要在您的 PlayerInputMap 编辑器中定义名为 "Skill1", "Skill2", "Skill3", "Skill4" 的 Action。
            // 添加了安全检查，如果某个Action在InputMap中不存在，则不会尝试注册，避免报错。
            if (playerInputMap.GamePlay.Skill1 != null)
            {
                playerInputMap.GamePlay.Skill1.performed += i => PlayerInputSkillValues[0] = true;
                playerInputMap.GamePlay.Skill1.canceled += i => PlayerInputSkillValues[0] = false;
            }
            if (playerInputMap.GamePlay.Skill2 != null)
            {
                playerInputMap.GamePlay.Skill2.performed += i => PlayerInputSkillValues[1] = true;
                playerInputMap.GamePlay.Skill2.canceled += i => PlayerInputSkillValues[1] = false;
            }
            if (playerInputMap.GamePlay.Skill3 != null)
            {
                playerInputMap.GamePlay.Skill3.performed += i => PlayerInputSkillValues[2] = true;
                playerInputMap.GamePlay.Skill3.canceled += i => PlayerInputSkillValues[2] = false;
            }
            if (playerInputMap.GamePlay.Skill4 != null)
            {
                playerInputMap.GamePlay.Skill4.performed += i => PlayerInputSkillValues[3] = true;
                playerInputMap.GamePlay.Skill4.canceled += i => PlayerInputSkillValues[3] = false;
            }
            // ========== MODIFICATION END | 2026年2月3日 ==========

        }


        
        playerInputMap.Enable();
    }
    
    private void OnDisable()
    {
        if (playerInputMap != null)
        {
            playerInputMap.Disable();
        }
    }
}
