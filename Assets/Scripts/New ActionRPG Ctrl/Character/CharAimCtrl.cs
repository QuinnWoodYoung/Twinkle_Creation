using System.Collections.Generic;
using UnityEngine;

// Controls lock-on aiming. The target can come from player lock-on or AI logic.
public class CharAimCtrl : MonoBehaviour
{
    private CharCtrl _charCtrl;
    private CharBlackBoard _blackBoard;
    private Camera _mainCamera;

    [Header("Indicator Settings")]
    [Tooltip("True if this aim controller belongs to the local player character.")]
    public bool isPlayerControlled = false;
    [Tooltip("Optional target indicator prefab.")]
    public TargetIndicator indicatorPrefab;
    private TargetIndicator _currentIndicator;

    [Header("Lock-On Settings")]
    [Tooltip("Maximum screen-space distance from the mouse cursor when selecting a lock-on target.")]
    public float maxLockOnRadius = 200f;

    public Transform lockedTarget;

    protected void Awake()
    {
        _charCtrl = GetComponent<CharCtrl>();
        _blackBoard = GetComponent<CharBlackBoard>();
        _mainCamera = Camera.main;

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

            float distance = Vector2.Distance(screenPos, Input.mousePosition);
            if (distance >= minDistance || distance > maxLockOnRadius)
            {
                continue;
            }

            minDistance = distance;
            bestTarget = candidateUnit.transform;
        }

        lockedTarget = bestTarget;
    }

    protected void OnDestroy()
    {
        if (isPlayerControlled && _currentIndicator != null)
        {
            Destroy(_currentIndicator.gameObject);
        }
    }
}
