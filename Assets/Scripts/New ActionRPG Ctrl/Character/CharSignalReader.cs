using System.Collections.Generic;
using UnityEngine;

public class CharSignalReader : MonoBehaviour
{
    [SerializeField] private bool isPlayerControlled = true;
    [SerializeField] private CharCtrl charCtrl;
    private ICharCtrlSignal currentInputSource;

    public interface ICharCtrlSignal
    {
        Vector2 GetMovementInput();
        Vector2 GetAimInput();
        AttackInputState GetAttackState();
        bool GetLockInput();
        bool GetDodgeInput();
        void UpdateSkillInputs(List<bool> skillInputs);
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

    private void Update()
    {
        Vector2 movementInput = currentInputSource.GetMovementInput();
        Vector2 aimInput = currentInputSource.GetAimInput();

        charCtrl.Param.Locomotion = movementInput;
        charCtrl.Param.AimTarget = aimInput;
        charCtrl.Param.AttackState = currentInputSource.GetAttackState();
        charCtrl.Param.isLock = currentInputSource.GetLockInput();
        charCtrl.Param.Dodge = currentInputSource.GetDodgeInput();
        currentInputSource.UpdateSkillInputs(charCtrl.Param.SkillInputDown);
    }

    public void SetInputSource(ICharCtrlSignal source)
    {
        currentInputSource = source;
    }
}

public class PlayerInputSource : CharSignalReader.ICharCtrlSignal
{
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

    public void UpdateSkillInputs(List<bool> skillInputs)
    {
        var charSkillInputStates = PlayerInputManager.instance.PlayerInputSkillValues;

        while (_skillWasPressedLastFrame.Count < skillInputs.Count)
        {
            _skillWasPressedLastFrame.Add(false);
        }

        for (int i = 0; i < skillInputs.Count; i++)
        {
            if (i < charSkillInputStates.Count)
            {
                bool isPressed = charSkillInputStates[i];
                skillInputs[i] = isPressed && !_skillWasPressedLastFrame[i];
                _skillWasPressedLastFrame[i] = isPressed;
            }
            else
            {
                skillInputs[i] = false;
            }
        }
    }
}

public class AIInputSource : CharSignalReader.ICharCtrlSignal
{
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

    public void UpdateSkillInputs(List<bool> skillInputs)
    {
        for(int i = 0; i < skillInputs.Count; i++)
        {
            skillInputs[i] = false;
        }
    }
}
