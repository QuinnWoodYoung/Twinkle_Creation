using System.Collections.Generic;
using UnityEngine;

// Controls lock-on aiming. The target can come from player lock-on or AI logic.
public class CharAimCtrl : MonoBehaviour
{
    private CharCtrl _charCtrl;
    private CharBlackBoard _blackBoard;
    private Camera _mainCamera;
    private SkillPreviewController _skillPreviewCtrl;

    [Header("Indicator Settings")]
    [Tooltip("True if this aim controller belongs to the local player character.")]
    public bool isPlayerControlled = false;
    [Tooltip("Optional target indicator prefab.")]
    public TargetIndicator indicatorPrefab;
    private TargetIndicator _currentIndicator;

    [Header("Lock-On Settings")]
    [Tooltip("Maximum screen-space distance from the current aim cursor when selecting a lock-on target.")]
    public float maxLockOnRadius = 200f;
    [Tooltip("Gamepad lock switch only triggers when the right stick reaches this magnitude.")]
    public float gamepadLockSwitchThreshold = 0.65f;
    [Tooltip("After a switch, the right stick must relax below this value before another switch can happen.")]
    public float gamepadLockSwitchResetThreshold = 0.35f;
    [Tooltip("Candidates farther than this from the player are ignored during right-stick lock switching.")]
    public float gamepadLockSwitchRange = 18f;

    public Transform lockedTarget;
    private bool _gamepadLockSwitchReady = true;

    protected void Awake()
    {
        _charCtrl = GetComponent<CharCtrl>();
        _blackBoard = GetComponent<CharBlackBoard>();
        _mainCamera = Camera.main;
        _skillPreviewCtrl = GetComponent<SkillPreviewController>();

        if (isPlayerControlled && indicatorPrefab != null)
        {
            _currentIndicator = Instantiate(indicatorPrefab);
            _currentIndicator.ClearTarget();
        }
    }

    protected void Update()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_skillPreviewCtrl == null)
        {
            _skillPreviewCtrl = GetComponent<SkillPreviewController>();
        }

        if (_charCtrl != null && _charCtrl.Param.isLock)
        {
            HandleLockOn();
        }
        else
        {
            lockedTarget = null;
        }

        if (isPlayerControlled && _currentIndicator != null)
        {
            _currentIndicator.SetTarget(lockedTarget);
        }
    }

    private void HandleLockOn()
    {
        if (_mainCamera == null)
        {
            lockedTarget = null;
            return;
        }

        if (lockedTarget != null && IsValidLockTarget(lockedTarget))
        {
            if (TryHandleGamepadLockSwitch())
            {
                return;
            }

            return;
        }

        if (TryHandleGamepadLockSwitch())
        {
            return;
        }

        lockedTarget = FindBestCursorLockTarget();
    }

    private Transform FindBestCursorLockTarget()
    {
        GameObject selfUnit = _blackBoard != null ? _blackBoard.gameObject : gameObject;
        float minDistance = float.MaxValue;
        Transform bestTarget = null;
        HashSet<GameObject> visited = new HashSet<GameObject>();

        foreach (CharBlackBoard board in CharBlackBoard.ActiveBoards)
        {
            if (board == null)
            {
                continue;
            }

            GameObject candidateUnit = board.gameObject;
            if (candidateUnit == null || !visited.Add(candidateUnit))
            {
                continue;
            }

            if (!CharRelationResolver.CanReceiveBasicAttack(selfUnit, candidateUnit))
            {
                continue;
            }

            Vector3 screenPos = _mainCamera.WorldToScreenPoint(candidateUnit.transform.position);
            if (screenPos.z <= 0f)
            {
                continue;
            }

            Vector2 aimCursor;
            PlayerInputManager inputManager = PlayerInputManager.instance;
            if (inputManager != null && inputManager.IsUsingGamepadInput)
            {
                aimCursor = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            }
            else
            {
                aimCursor = _charCtrl != null && _charCtrl.Param != null
                    ? _charCtrl.Param.AimTarget
                    : (Vector2)Input.mousePosition;
            }
            float distance = Vector2.Distance(screenPos, aimCursor);
            if (distance >= minDistance || distance > maxLockOnRadius)
            {
                continue;
            }

            minDistance = distance;
            bestTarget = candidateUnit.transform;
        }

        return bestTarget;
    }

    private bool TryHandleGamepadLockSwitch()
    {
        PlayerInputManager inputManager = PlayerInputManager.instance;
        if (inputManager == null || !inputManager.IsUsingGamepadInput)
        {
            _gamepadLockSwitchReady = true;
            return false;
        }

        if (_skillPreviewCtrl != null && _skillPreviewCtrl.IsPreviewing)
        {
            return lockedTarget != null;
        }

        Vector2 stick = inputManager.GamepadAimStick;
        float magnitude = stick.magnitude;
        float switchThreshold = Mathf.Max(0f, gamepadLockSwitchThreshold);
        float resetThreshold = Mathf.Max(0f, gamepadLockSwitchResetThreshold);

        if (magnitude <= resetThreshold)
        {
            _gamepadLockSwitchReady = true;
            return false;
        }

        if (!_gamepadLockSwitchReady || magnitude < switchThreshold)
        {
            return lockedTarget != null;
        }

        _gamepadLockSwitchReady = false;
        Transform nextTarget = ResolveGamepadLockTarget(stick);
        if (nextTarget != null)
        {
            lockedTarget = nextTarget;
            return true;
        }

        return lockedTarget != null;
    }

    private Transform ResolveGamepadLockTarget(Vector2 stickInput)
    {
        if (_mainCamera == null)
        {
            return lockedTarget;
        }

        GameObject selfUnit = _blackBoard != null ? _blackBoard.gameObject : gameObject;
        Vector3 desiredDirection = ResolveWorldDirectionFromStick(stickInput);
        if (desiredDirection.sqrMagnitude <= 0.001f)
        {
            return lockedTarget;
        }

        float bestScore = float.MaxValue;
        Transform bestTarget = null;
        float maxRange = Mathf.Max(0.1f, gamepadLockSwitchRange);

        foreach (CharBlackBoard board in CharBlackBoard.ActiveBoards)
        {
            if (board == null || board.transform == lockedTarget)
            {
                continue;
            }

            GameObject candidateUnit = board.gameObject;
            if (!CharRelationResolver.CanReceiveBasicAttack(selfUnit, candidateUnit))
            {
                continue;
            }

            Vector3 toTarget = candidateUnit.transform.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance <= 0.01f || distance > maxRange)
            {
                continue;
            }

            Vector3 direction = toTarget / distance;
            float angle = Vector3.Angle(desiredDirection, direction);
            if (angle > 70f)
            {
                continue;
            }

            float score = angle * 1000f + distance;
            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestTarget = candidateUnit.transform;
        }

        return bestTarget != null ? bestTarget : lockedTarget;
    }

    private bool IsValidLockTarget(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        GameObject selfUnit = _blackBoard != null ? _blackBoard.gameObject : gameObject;
        return CharRelationResolver.CanReceiveBasicAttack(selfUnit, target.gameObject);
    }

    private Vector3 ResolveWorldDirectionFromStick(Vector2 stickInput)
    {
        if (_mainCamera == null || stickInput.sqrMagnitude <= 0.001f)
        {
            return Vector3.zero;
        }

        Vector3 forward = _mainCamera.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = transform.forward;
        }

        Vector3 right = _mainCamera.transform.right;
        right.y = 0f;
        if (right.sqrMagnitude <= 0.001f)
        {
            right = transform.right;
        }

        Vector3 worldDirection =
            right.normalized * stickInput.x +
            forward.normalized * stickInput.y;
        worldDirection.y = 0f;
        return worldDirection.sqrMagnitude > 0.001f ? worldDirection.normalized : Vector3.zero;
    }

    protected void OnDestroy()
    {
        if (isPlayerControlled && _currentIndicator != null)
        {
            Destroy(_currentIndicator.gameObject);
        }
    }
}
