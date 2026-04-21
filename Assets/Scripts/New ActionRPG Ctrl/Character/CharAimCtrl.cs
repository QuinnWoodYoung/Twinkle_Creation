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
    private bool _directionalLockSwitchReady = true;
    private bool _isLockModeActive;
    private bool _hasDirectionalAimDirection;
    private Vector3 _directionalAimDirection = Vector3.forward;

    public bool IsLockModeActive => _isLockModeActive;

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

        UpdateDirectionalAimDirection();
        UpdateLockModeState();

        if (_charCtrl != null && _charCtrl.Param != null)
        {
            _charCtrl.Param.isLock = _isLockModeActive;
        }

        if (_isLockModeActive)
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

    public bool TryGetDirectionalAimDirection(out Vector3 direction)
    {
        direction = Vector3.zero;

        if (TryResolveCurrentDirectionalAimDirection(out Vector3 currentDirection))
        {
            direction = currentDirection;
            return true;
        }

        if (!_hasDirectionalAimDirection)
        {
            return false;
        }

        direction = _directionalAimDirection;
        return direction.sqrMagnitude > 0.001f;
    }

    private void UpdateDirectionalAimDirection()
    {
        if (TryResolveCurrentDirectionalAimDirection(out Vector3 direction))
        {
            _directionalAimDirection = direction;
            _hasDirectionalAimDirection = true;
        }
        else if (!_hasDirectionalAimDirection)
        {
            Vector3 fallbackForward = transform.forward;
            fallbackForward.y = 0f;
            if (fallbackForward.sqrMagnitude > 0.001f)
            {
                _directionalAimDirection = fallbackForward.normalized;
            }
        }
    }

    private void UpdateLockModeState()
    {
        if (_charCtrl == null || _charCtrl.Param == null)
        {
            _isLockModeActive = false;
            lockedTarget = null;
            return;
        }

        if (_charCtrl.Param.LockState.isDown)
        {
            _isLockModeActive = !_isLockModeActive;
            if (!_isLockModeActive)
            {
                lockedTarget = null;
            }
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
            if (TryHandleDirectionalLockSwitch())
            {
                return;
            }

            return;
        }

        if (TryHandleDirectionalLockSwitch())
        {
            return;
        }

        lockedTarget = FindBestLockTarget();
    }

    private Transform FindBestLockTarget()
    {
        if (TryGetDirectionalAimDirection(out Vector3 directionalAim))
        {
            return FindBestDirectionalLockTarget(directionalAim, false);
        }

        return FindBestCursorLockTarget();
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

            Vector2 aimCursor = _charCtrl != null && _charCtrl.Param != null
                ? _charCtrl.Param.AimTarget
                : (Vector2)Input.mousePosition;
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

    private bool TryHandleDirectionalLockSwitch()
    {
        if (!TryGetDirectionalAimInput(out Vector2 directionalInput))
        {
            _directionalLockSwitchReady = true;
            return false;
        }

        if (_skillPreviewCtrl != null && _skillPreviewCtrl.IsPreviewing)
        {
            return lockedTarget != null;
        }

        float magnitude = directionalInput.magnitude;
        float switchThreshold = Mathf.Max(0f, gamepadLockSwitchThreshold);
        float resetThreshold = Mathf.Max(0f, gamepadLockSwitchResetThreshold);

        if (magnitude <= resetThreshold)
        {
            _directionalLockSwitchReady = true;
            return false;
        }

        if (!_directionalLockSwitchReady || magnitude < switchThreshold)
        {
            return lockedTarget != null;
        }

        if (!TryResolveWorldDirectionFromInput(directionalInput, out Vector3 desiredDirection))
        {
            return lockedTarget != null;
        }

        _directionalLockSwitchReady = false;
        Transform nextTarget = FindBestDirectionalLockTarget(desiredDirection, true);
        if (nextTarget != null)
        {
            lockedTarget = nextTarget;
            return true;
        }

        return lockedTarget != null;
    }

    private Transform FindBestDirectionalLockTarget(Vector3 desiredDirection, bool fallbackToCurrent)
    {
        if (desiredDirection.sqrMagnitude <= 0.001f)
        {
            return fallbackToCurrent ? lockedTarget : null;
        }

        GameObject selfUnit = _blackBoard != null ? _blackBoard.gameObject : gameObject;
        float bestScore = float.MaxValue;
        Transform bestTarget = null;
        float maxRange = ResolveDirectionalLockRange(selfUnit);

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

        return bestTarget != null ? bestTarget : (fallbackToCurrent ? lockedTarget : null);
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

    private bool TryResolveCurrentDirectionalAimDirection(out Vector3 direction)
    {
        direction = Vector3.zero;
        return TryGetDirectionalAimInput(out Vector2 directionalInput) &&
               TryResolveWorldDirectionFromInput(directionalInput, out direction);
    }

    private bool TryGetDirectionalAimInput(out Vector2 directionalInput)
    {
        directionalInput = Vector2.zero;
        if (_charCtrl == null || _charCtrl.Param == null)
        {
            return false;
        }

        directionalInput = _charCtrl.Param.AimDirection;
        return directionalInput.sqrMagnitude > 0.0001f;
    }

    private float ResolveDirectionalLockRange(GameObject selfUnit)
    {
        float lockRange = Mathf.Max(0.1f, gamepadLockSwitchRange);
        float attackRange = CharResourceResolver.GetMaxAttackRange(selfUnit);
        if (attackRange > 0f)
        {
            lockRange = Mathf.Max(lockRange, attackRange);
        }

        return lockRange;
    }

    private bool TryResolveWorldDirectionFromInput(Vector2 stickInput, out Vector3 worldDirection)
    {
        worldDirection = Vector3.zero;
        if (_mainCamera == null || stickInput.sqrMagnitude <= 0.001f)
        {
            return false;
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

        worldDirection =
            right.normalized * stickInput.x +
            forward.normalized * stickInput.y;
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        worldDirection = worldDirection.normalized;
        return true;
    }

    protected void OnDestroy()
    {
        if (isPlayerControlled && _currentIndicator != null)
        {
            Destroy(_currentIndicator.gameObject);
        }
    }
}
