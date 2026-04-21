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
        Vector2 GetAimDirectionInput();
        Vector2 GetAttackFacingInput();
        AttackInputState GetAttackState();
        ButtonInputState GetLockState();
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
        if (charCtrl == null || charCtrl.Param == null || currentInputSource == null)
        {
            return;
        }

        Vector2 movementInput = currentInputSource.GetMovementInput();
        Vector2 aimInput = currentInputSource.GetAimInput();
        Vector2 aimDirectionInput = currentInputSource.GetAimDirectionInput();
        Vector2 attackFacingInput = currentInputSource.GetAttackFacingInput();

        charCtrl.Param.Locomotion = movementInput;
        charCtrl.Param.AimTarget = aimInput;
        charCtrl.Param.AimDirection = aimDirectionInput;
        charCtrl.Param.AttackFacingInput = attackFacingInput;
        charCtrl.Param.AttackState = currentInputSource.GetAttackState();
        charCtrl.Param.LockState = currentInputSource.GetLockState();
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
    // Convert held buttons into down / held / up states for combat logic.
    private bool _wasAttackPressedLastFrame;
    private bool _wasDodgePressedLastFrame;
    private bool _wasLockPressedLastFrame;
    private readonly List<bool> _skillWasPressedLastFrame = new List<bool>();

    public Vector2 GetMovementInput()
    {
        PlayerInputManager inputManager = PlayerInputManager.instance;
        return inputManager != null ? inputManager.playerInputMovementValue : Vector2.zero;
    }

    public Vector2 GetAimInput()
    {
        PlayerInputManager inputManager = PlayerInputManager.instance;
        return inputManager != null ? inputManager.playerInputAimValue : Vector2.zero;
    }

    public Vector2 GetAimDirectionInput()
    {
        PlayerInputManager inputManager = PlayerInputManager.instance;
        return inputManager != null ? inputManager.GamepadAimStick : Vector2.zero;
    }

    public Vector2 GetAttackFacingInput()
    {
        PlayerInputManager inputManager = PlayerInputManager.instance;
        if (inputManager == null || !inputManager.IsUsingGamepadInput)
        {
            return Vector2.zero;
        }

        return inputManager.playerInputMovementValue;
    }

    public AttackInputState GetAttackState()
    {
        PlayerInputManager inputManager = PlayerInputManager.instance;
        var state = new AttackInputState();
        bool isPressed = inputManager != null && inputManager.playerInputAttackValue;

        state.isDown = isPressed && !_wasAttackPressedLastFrame;
        state.isHeld = isPressed;
        state.isUp = !isPressed && _wasAttackPressedLastFrame;

        _wasAttackPressedLastFrame = isPressed;
        return state;
    }

    public bool GetDodgeInput()
    {
        PlayerInputManager inputManager = PlayerInputManager.instance;
        bool isPressed = inputManager != null && inputManager.playerInputDodgeValue;
        bool triggered = isPressed && !_wasDodgePressedLastFrame;
        _wasDodgePressedLastFrame = isPressed;
        return triggered;
    }

    public ButtonInputState GetLockState()
    {
        PlayerInputManager inputManager = PlayerInputManager.instance;
        var state = new ButtonInputState();
        bool isPressed = inputManager != null && inputManager.playerInputLockValue;

        state.isDown = isPressed && !_wasLockPressedLastFrame;
        state.isHeld = isPressed;
        state.isUp = !isPressed && _wasLockPressedLastFrame;

        _wasLockPressedLastFrame = isPressed;
        return state;
    }

    public void UpdateSkillInputs(List<bool> skillInputs, List<ButtonInputState> skillInputStates)
    {
        PlayerInputManager inputManager = PlayerInputManager.instance;
        List<bool> charSkillInputStates = inputManager != null
            ? inputManager.PlayerInputSkillValues
            : new List<bool>();

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
    // AI can later provide the same normalized control data contract.
    public Vector2 GetMovementInput()
    {
        return Vector2.zero;
    }

    /*
    // 当前还是空实现，但接口已与玩家输入对齐，后续 AI 可直接复用整套角色控制器。
    public Vector2 GetMovementInput()
    {
        return Vector2.zero;
    }

    */
    public Vector2 GetAimInput()
    {
        return Vector2.zero;
    }
    public Vector2 GetAimDirectionInput()
    {
        return Vector2.zero;
    }
    public Vector2 GetAttackFacingInput()
    {
        return Vector2.zero;
    }
    public AttackInputState GetAttackState()
    {
        return new AttackInputState();
    }

    public ButtonInputState GetLockState()
    {
        return default;
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
