using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 输入桥接层。
/// 角色逻辑不直接依赖“玩家输入”或“AI 输入”，而是统一从这里拿标准化后的控制信号，
/// 再写入 CharCtrl.Param。
/// </summary>
public class CharSignalReader : MonoBehaviour
{
    [SerializeField] private bool isPlayerControlled = true;
    [SerializeField] private CharCtrl charCtrl;
    private ICharCtrlSignal currentInputSource;
    public bool IsPlayerControlled => isPlayerControlled;

    public interface ICharCtrlSignal
    {
        Vector2 GetMovementInput();
        Vector2 GetAimInput();
        AttackInputState GetAttackState();
        bool GetLockInput();
        bool GetDodgeInput();
        void UpdateSkillInputs(List<bool> skillInputs, List<ButtonInputState> skillInputStates);
    }

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

    /// <summary>
    /// 每帧把输入源结果写入 CharCtrl.Param，供移动/攻击/技能控制器消费。
    /// </summary>
    private void Update()
    {
        Vector2 movementInput = currentInputSource.GetMovementInput();
        Vector2 aimInput = currentInputSource.GetAimInput();

        charCtrl.Param.Locomotion = movementInput;
        charCtrl.Param.AimTarget = aimInput;
        charCtrl.Param.AttackState = currentInputSource.GetAttackState();
        charCtrl.Param.isLock = currentInputSource.GetLockInput();
        charCtrl.Param.Dodge = currentInputSource.GetDodgeInput();
        currentInputSource.UpdateSkillInputs(charCtrl.Param.SkillInputDown, charCtrl.Param.SkillInputStates);
    }

    public void SetInputSource(ICharCtrlSignal source)
    {
        currentInputSource = source;
    }
}

public class PlayerInputSource : CharSignalReader.ICharCtrlSignal
{
    // 把持续按键转换成 down / held / up 三种更适合战斗逻辑的输入状态。
    private bool _wasAttackPressedLastFrame;
    private bool _wasDodgePressedLastFrame;
    private readonly List<bool> _skillWasPressedLastFrame = new List<bool>();

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
        bool isPressed = PlayerInputManager.instance.playerInputDodgeValue;
        bool triggered = isPressed && !_wasDodgePressedLastFrame;
        _wasDodgePressedLastFrame = isPressed;
        return triggered;
    }

    public bool GetLockInput()
    {
        return PlayerInputManager.instance.playerInputLockValue;
    }

    public void UpdateSkillInputs(List<bool> skillInputs, List<ButtonInputState> skillInputStates)
    {
        var charSkillInputStates = PlayerInputManager.instance.PlayerInputSkillValues;

        while (_skillWasPressedLastFrame.Count < skillInputs.Count)
        {
            _skillWasPressedLastFrame.Add(false);
        }

        while (skillInputStates.Count < skillInputs.Count)
        {
            skillInputStates.Add(default);
        }

        for (int i = 0; i < skillInputs.Count; i++)
        {
            bool isPressed = i < charSkillInputStates.Count && charSkillInputStates[i];
            ButtonInputState state = new ButtonInputState
            {
                isDown = isPressed && !_skillWasPressedLastFrame[i],
                isHeld = isPressed,
                isUp = !isPressed && _skillWasPressedLastFrame[i],
            };

            skillInputStates[i] = state;
            skillInputs[i] = state.isDown;
            _skillWasPressedLastFrame[i] = isPressed;
        }
    }
}

public class AIInputSource : CharSignalReader.ICharCtrlSignal
{
    // 当前还是空实现，但接口已与玩家输入对齐，后续 AI 可直接复用整套角色控制器。
    public Vector2 GetMovementInput()
    {
        return Vector2.zero;
    }
    public Vector2 GetAimInput()
    {
        return Vector2.zero;
    }
    public AttackInputState GetAttackState()
    {
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

    public void UpdateSkillInputs(List<bool> skillInputs, List<ButtonInputState> skillInputStates)
    {
        while (skillInputStates.Count < skillInputs.Count)
        {
            skillInputStates.Add(default);
        }

        for (int i = 0; i < skillInputs.Count; i++)
        {
            skillInputs[i] = false;
            skillInputStates[i] = default;
        }
    }
}
