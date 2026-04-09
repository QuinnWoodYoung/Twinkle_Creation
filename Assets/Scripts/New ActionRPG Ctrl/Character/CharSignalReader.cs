using System.Collections.Generic;
using UnityEngine;

public class CharSignalReader : MonoBehaviour
{
    [SerializeField] private bool isPlayerControlled = true;
    [SerializeField] private CharCtrl charCtrl;
    private ICharCtrlSignal currentInputSource;

    /* ========== MODIFICATION START | 2026年2月3日 ==========
    // 将旧接口注释掉，以新的接口代替
    // public interface ICharCtrlSignal
    // {
    //     Vector2 GetMovementInput();
    //     Vector2 GetAimInput();
    //     AttackInputState GetAttackState();
    //     bool GetLockInput();
    //     
    //     // --- 旧版攻击信号接口，留作参考 ---
    //     // bool GetAttackInput();
    //     bool GetDodgeInput();
    // }
    ========== MODIFICATION END | 2026年2月3日 ========== */

    // ========== MODIFICATION START | 2026年2月3日 ==========
    // 新增：ICharCtrlSignal 接口，添加了 UpdateSkillInputs 方法
    public interface ICharCtrlSignal
    {
        Vector2 GetMovementInput();
        Vector2 GetAimInput();
        AttackInputState GetAttackState();
        bool GetLockInput();
        bool GetDodgeInput();
        
        // 新增方法：用于更新技能输入状态
        void UpdateSkillInputs(List<bool> skillInputs);
    }
    // ========== MODIFICATION END | 2026年2月3日 ==========

    private void Awake()
    {
        charCtrl = GetComponent<CharCtrl>();
    }
    private void Start()
    {
        SetInputSource(isPlayerControlled ? 
            (ICharCtrlSignal)new PlayerInputSource() : 
            new AIInputSource());
    }

    private void Update()
    {
        Vector2 movementInput = currentInputSource.GetMovementInput();
        Vector2 aimInput = currentInputSource.GetAimInput();
        
        charCtrl.Param.Locomotion = movementInput;
        charCtrl.Param.AimTarget = aimInput;
        charCtrl.Param.AttackState = currentInputSource.GetAttackState();
        charCtrl.Param.isLock = currentInputSource.GetLockInput();
        charCtrl.Param.Dodge = currentInputSource.GetDodgeInput();
        
        // ========== MODIFICATION START | 2026年2月3日 ==========
        // 新增：调用新方法，将角色的技能输入列表传递给输入源进行填充。
        currentInputSource.UpdateSkillInputs(charCtrl.Param.SkillInputDown);
        // ========== MODIFICATION END | 2026年2月3日 ==========
        
        /* --- 旧版 Update 逻辑，留作参考 ---
        // charCtrl.Param.Attack = currentInputSource.GetAttackInput();
        if (currentInputSource.GetAttackInput())
        {
            charCtrl.Param.Attack = true;
        }
        */
    }

    public void SetInputSource(ICharCtrlSignal source)
    {
        currentInputSource = source;
    }
    
}

public class PlayerInputSource : CharSignalReader.ICharCtrlSignal
{
    private bool _wasAttackPressedLastFrame;
    
    // --- 旧版 GetAttackInput() 使用的变量，留作参考 ---
    private bool _attackTrigger;
    private bool _newAttack;
    private bool _lastAttackTrigger;

    // ========== MODIFICATION START | 2026年2月3日 ==========
    // 新增：用于跟踪每个技能按键在上一帧状态的列表，以计算“按下瞬间”
    private readonly List<bool> _skillWasPressedLastFrame = new List<bool>();
    // ========== MODIFICATION END | 2026年2月3日 ==========
    
    public Vector2 GetMovementInput()
    {
        return PlayerInputManager.instance.playerInputMovementValue;
    }

    public Vector2 GetAimInput()
    {
        return PlayerInputManager.instance.playerInputAimValue;
    }
    
    public AttackInputState GetAttackState()
    {
        var state = new AttackInputState();
        bool isPressed = PlayerInputManager.instance.playerInputAttackValue;

        state.isDown = isPressed && !_wasAttackPressedLastFrame;
        state.isHeld = isPressed;
        state.isUp = !isPressed && _wasAttackPressedLastFrame;

        _wasAttackPressedLastFrame = isPressed;
        return state;
    }
    
    public bool GetDodgeInput()
    {
        _newAttack = PlayerInputManager.instance.playerInputDodgeValue;
        bool attackTriggeredThisFrame = _newAttack && !_lastAttackTrigger;
        _lastAttackTrigger = _newAttack;
        return attackTriggeredThisFrame;
    }
    
    public bool GetLockInput()
    {
        return PlayerInputManager.instance.playerInputLockValue;
    }
    
    // ========== MODIFICATION START | 2026年2月3日 ==========
    // 新增：实现 UpdateSkillInputs 方法
    public void UpdateSkillInputs(List<bool> skillInputs)
    {
        // 从 PlayerInputManager 获取原始的按键持续状态
        var charSkillInputStates = PlayerInputManager.instance.PlayerInputSkillValues;
            
        // 动态调整我们的辅助列表大小，确保它和角色拥有的技能数量一致
        while (_skillWasPressedLastFrame.Count < skillInputs.Count)
        {
            _skillWasPressedLastFrame.Add(false);
        }

        // 遍历角色拥有的每一个技能槽位
        for (int i = 0; i < skillInputs.Count; i++)
        {
            // 检查物理按键输入系统是否提供了这个槽位的输入
            if (i < charSkillInputStates.Count)
            {
                bool isPressed = charSkillInputStates[i];
                // 关键逻辑：只有当“本帧按下”且“上帧未按下”时，才判定为“Down”事件
                skillInputs[i] = isPressed && !_skillWasPressedLastFrame[i];
                // 更新上一帧的状态，为下一帧的计算做准备
                _skillWasPressedLastFrame[i] = isPressed;
            }
            else
            {
                // 如果角色技能数 > 物理按键数，则超出部分的技能永远不会被触发
                skillInputs[i] = false;
            }
        }
    }
    // ========== MODIFICATION END | 2026年2月3日 ==========
}

public class AIInputSource : CharSignalReader.ICharCtrlSignal
{
    public Vector2 GetMovementInput()
    {
        var AItest = new Vector2(0, 0);
        // AI移动逻辑将在这里实现
       //return Vector2.zero;
       return AItest;
    }
    public Vector2 GetAimInput()
    {
        return Vector2.zero;
    }
    public AttackInputState GetAttackState()
    {
        // AI可以直接构建它想要的攻击状态。
        // 目前，它什么也不做。
        return new AttackInputState();
    }

    public bool GetLockInput()
    {
        return false;
    }
    
    public bool GetDodgeInput()
    {
        return false;
    }

    // ========== MODIFICATION START | 2026年2月3日 ==========
    // 新增：实现 UpdateSkillInputs 方法 (AI部分)
    public void UpdateSkillInputs(List<bool> skillInputs)
    {
        // 在这里实现AI的技能决策逻辑。
        // 例如，如果AI决定在这一帧使用它的第一个技能 (索引0)，可以这样写：
        // if (someAICondition) { skillInputs[0] = true; }
        // 为确保单次触发，AI逻辑需要自己管理状态，或在这里清空所有输入
        for(int i = 0; i < skillInputs.Count; i++)
        {
            skillInputs[i] = false; // 默认AI不使用任何技能
        }
    }
    // ========== MODIFICATION END | 2026年2月3日 ==========
}
