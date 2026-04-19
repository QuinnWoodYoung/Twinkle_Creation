using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager instance;

    [Header("Input Mode")]
    [SerializeField] private bool useGamepadInput;

    [Header("Input Values")]
    [SerializeField] public Vector2 playerInputMovementValue;
    [SerializeField] public Vector2 playerInputAimValue;
    [SerializeField] public bool playerInputAttackValue;
    [SerializeField] public bool playerInputLockValue;
    [SerializeField] public bool playerInputDodgeValue;
    [SerializeField] public bool playerInputSkillModifierValue;

    public readonly List<bool> PlayerInputSkillValues = new List<bool> { false, false, false, false };

    private PlayerInputMap playerInputMap;
    private InputAction _skillModifierAction;
    private Vector2 _gamepadAimStick;

    public bool IsUsingGamepadInput => useGamepadInput;
    public Vector2 GamepadAimStick => useGamepadInput ? _gamepadAimStick : Vector2.zero;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
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
            _skillModifierAction = playerInputMap.asset.FindAction("GamePlay/SkillModifier", false);

            playerInputMap.GamePlay.Movement.performed += OnMovementPerformed;
            playerInputMap.GamePlay.Movement.canceled += OnMovementCanceled;
            playerInputMap.GamePlay.Aim.performed += OnAimPerformed;
            playerInputMap.GamePlay.Aim.canceled += OnAimCanceled;
            playerInputMap.GamePlay.Attack.performed += OnAttackPerformed;
            playerInputMap.GamePlay.Attack.canceled += OnAttackCanceled;
            playerInputMap.GamePlay.Lock.performed += OnLockPerformed;
            playerInputMap.GamePlay.Dodge.performed += OnDodgePerformed;
            playerInputMap.GamePlay.Dodge.canceled += OnDodgeCanceled;

            if (playerInputMap.GamePlay.Skill1 != null)
            {
                playerInputMap.GamePlay.Skill1.performed += ctx => OnSkillPerformed(ctx, 0);
                playerInputMap.GamePlay.Skill1.canceled += ctx => OnSkillCanceled(ctx, 0);
            }

            if (playerInputMap.GamePlay.Skill2 != null)
            {
                playerInputMap.GamePlay.Skill2.performed += ctx => OnSkillPerformed(ctx, 1);
                playerInputMap.GamePlay.Skill2.canceled += ctx => OnSkillCanceled(ctx, 1);
            }

            if (playerInputMap.GamePlay.Skill3 != null)
            {
                playerInputMap.GamePlay.Skill3.performed += ctx => OnSkillPerformed(ctx, 2);
                playerInputMap.GamePlay.Skill3.canceled += ctx => OnSkillCanceled(ctx, 2);
            }

            if (playerInputMap.GamePlay.Skill4 != null)
            {
                playerInputMap.GamePlay.Skill4.performed += ctx => OnSkillPerformed(ctx, 3);
                playerInputMap.GamePlay.Skill4.canceled += ctx => OnSkillCanceled(ctx, 3);
            }
        }

        ApplySelectedDevices();

        if (_skillModifierAction != null)
        {
            _skillModifierAction.performed += OnSkillModifierPerformed;
            _skillModifierAction.canceled += OnSkillModifierCanceled;
        }

        playerInputMap.Enable();
        ResetRuntimeState();
    }

    private void Update()
    {
    }

    private void OnDisable()
    {
        if (playerInputMap != null)
        {
            playerInputMap.Disable();
        }

        if (_skillModifierAction != null)
        {
            _skillModifierAction.performed -= OnSkillModifierPerformed;
            _skillModifierAction.canceled -= OnSkillModifierCanceled;
        }
    }

    private void ResetRuntimeState()
    {
        playerInputMovementValue = Vector2.zero;
        playerInputAttackValue = false;
        playerInputLockValue = false;
        playerInputDodgeValue = false;
        playerInputSkillModifierValue = false;
        _gamepadAimStick = Vector2.zero;

        for (int i = 0; i < PlayerInputSkillValues.Count; i++)
        {
            PlayerInputSkillValues[i] = false;
        }

        playerInputAimValue = Vector2.zero;
    }

    private void ApplySelectedDevices()
    {
        if (playerInputMap == null)
        {
            return;
        }

        List<InputDevice> selectedDevices = new List<InputDevice>();

        if (useGamepadInput)
        {
            foreach (Gamepad gamepad in Gamepad.all)
            {
                if (gamepad != null)
                {
                    selectedDevices.Add(gamepad);
                }
            }
        }
        else
        {
            if (Keyboard.current != null)
            {
                selectedDevices.Add(Keyboard.current);
            }

            if (Mouse.current != null)
            {
                selectedDevices.Add(Mouse.current);
            }
        }

        playerInputMap.devices = selectedDevices.Count > 0
            ? new ReadOnlyArray<InputDevice>(selectedDevices.ToArray())
            : default;
    }

    private void OnMovementPerformed(InputAction.CallbackContext context)
    {
        if (!ShouldProcessContext(context))
        {
            return;
        }

        playerInputMovementValue = context.ReadValue<Vector2>();
    }

    private void OnMovementCanceled(InputAction.CallbackContext context)
    {
        if (!ShouldProcessContext(context))
        {
            return;
        }

        playerInputMovementValue = Vector2.zero;
    }

    private void OnAimPerformed(InputAction.CallbackContext context)
    {
        if (!ShouldProcessContext(context))
        {
            return;
        }

        Vector2 aimValue = context.ReadValue<Vector2>();
        if (IsGamepadContext(context))
        {
            _gamepadAimStick = aimValue;
            playerInputAimValue = aimValue;
            return;
        }

        playerInputAimValue = aimValue;
    }

    private void OnAimCanceled(InputAction.CallbackContext context)
    {
        if (!ShouldProcessContext(context))
        {
            return;
        }

        if (IsGamepadContext(context))
        {
            _gamepadAimStick = Vector2.zero;
            playerInputAimValue = Vector2.zero;
            return;
        }

        playerInputAimValue = Vector2.zero;
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (!ShouldProcessContext(context))
        {
            return;
        }

        playerInputAttackValue = true;
    }

    private void OnAttackCanceled(InputAction.CallbackContext context)
    {
        if (!ShouldProcessContext(context))
        {
            return;
        }

        playerInputAttackValue = false;
    }

    private void OnLockPerformed(InputAction.CallbackContext context)
    {
        if (!ShouldProcessContext(context))
        {
            return;
        }

        playerInputLockValue = !playerInputLockValue;
    }

    private void OnDodgePerformed(InputAction.CallbackContext context)
    {
        if (!ShouldProcessContext(context))
        {
            return;
        }

        playerInputDodgeValue = true;
    }

    private void OnDodgeCanceled(InputAction.CallbackContext context)
    {
        if (!ShouldProcessContext(context))
        {
            return;
        }

        playerInputDodgeValue = false;
    }

    private void OnSkillPerformed(InputAction.CallbackContext context, int skillIndex)
    {
        if (!ShouldProcessContext(context) || !IsValidSkillIndex(skillIndex))
        {
            return;
        }

        PlayerInputSkillValues[skillIndex] = true;
    }

    private void OnSkillCanceled(InputAction.CallbackContext context, int skillIndex)
    {
        if (!ShouldProcessContext(context) || !IsValidSkillIndex(skillIndex))
        {
            return;
        }

        PlayerInputSkillValues[skillIndex] = false;
    }

    private void OnSkillModifierPerformed(InputAction.CallbackContext context)
    {
        if (!ShouldProcessContext(context))
        {
            return;
        }

        playerInputSkillModifierValue = context.ReadValueAsButton();
    }

    private void OnSkillModifierCanceled(InputAction.CallbackContext context)
    {
        if (!ShouldProcessContext(context))
        {
            return;
        }

        playerInputSkillModifierValue = false;
    }

    private bool ShouldProcessContext(InputAction.CallbackContext context)
    {
        if (context.control == null || context.control.device == null)
        {
            return false;
        }

        return useGamepadInput ? IsGamepadContext(context) : IsMouseKeyboardContext(context);
    }

    private static bool IsGamepadContext(InputAction.CallbackContext context)
    {
        return context.control != null && context.control.device is Gamepad;
    }

    private static bool IsMouseKeyboardContext(InputAction.CallbackContext context)
    {
        return context.control != null &&
               (context.control.device is Mouse || context.control.device is Keyboard);
    }

    private bool IsValidSkillIndex(int skillIndex)
    {
        return skillIndex >= 0 && skillIndex < PlayerInputSkillValues.Count;
    }
}
