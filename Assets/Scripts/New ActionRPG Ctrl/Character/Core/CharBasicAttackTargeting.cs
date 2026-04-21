using UnityEngine;

public enum BasicAttackTargetingMode
{
    FreeAim,
    SoftLock,
    LockedTarget,
}

public struct BasicAttackTargetInfo
{
    public GameObject targetUnit;
    public Vector3 attackPoint;
    public Vector3 attackDirection;

    public bool HasTarget => targetUnit != null;
}

/// <summary>
/// 普攻目标解析工具。
/// 目标选择始终先尊重玩家当前意图，再在这个方向附近做有限辅助瞄准。
/// </summary>
public static class CharBasicAttackTargeting
{
    // Resolve a practical attack target for both Hades-like free attacks and
    // MOBA-like lock-on attacks. The system always starts from player intent
    // first, then only applies limited assistance around that intent.
    /// <summary>
    /// 为一次普攻解析最终目标、攻击点和攻击方向。
    /// </summary>
    public static BasicAttackTargetInfo Resolve(
        GameObject attacker,
        CharCtrl charCtrl,
        BasicAttackTargetingMode targetingMode,
        float range,
        float assistAngle,
        bool preferLockedTarget,
        bool useLockedAim = true,
        bool useDirectionalAimInput = false,
        bool useAttackFacingInput = false)
    {
        BasicAttackTargetInfo info = new BasicAttackTargetInfo();
        GameObject attackerUnit = CharRelationResolver.NormalizeUnit(attacker);
        if (attackerUnit == null)
        {
            return info;
        }

        Vector3 origin = attackerUnit.transform.position;
        Vector3 aimDirection = ResolveAimDirection(
            attackerUnit,
            charCtrl,
            useLockedAim,
            useDirectionalAimInput,
            useAttackFacingInput);
        Transform lockedTarget = charCtrl != null ? charCtrl.LockedTarget : null;

        if (preferLockedTarget && TryGetLockedEnemy(attackerUnit, lockedTarget, out GameObject lockedUnit))
        {
            return BuildTargetInfo(origin, lockedUnit);
        }

        switch (targetingMode)
        {
            case BasicAttackTargetingMode.LockedTarget:
                if (TryGetLockedEnemy(attackerUnit, lockedTarget, out GameObject requiredLockTarget))
                {
                    return BuildTargetInfo(origin, requiredLockTarget);
                }

                break;

            case BasicAttackTargetingMode.SoftLock:
                GameObject bestTarget = FindBestTarget(attackerUnit, origin, aimDirection, range, assistAngle);
                if (bestTarget != null)
                {
                    return BuildTargetInfo(origin, bestTarget);
                }

                break;
        }

        info.attackDirection = aimDirection;
        info.attackPoint = origin + aimDirection * Mathf.Max(range, 1f);
        return info;
    }

    /// <summary>
    /// 解析玩家当前想打向哪里。
    /// 优先锁定目标，其次读 AimInput，最后退回角色 forward。
    /// </summary>
    public static Vector3 ResolveAimDirection(
        GameObject attacker,
        CharCtrl charCtrl,
        bool useLockedAim = true,
        bool useDirectionalAimInput = false,
        bool useAttackFacingInput = false)
    {
        GameObject attackerUnit = CharRelationResolver.NormalizeUnit(attacker);
        if (attackerUnit == null)
        {
            return Vector3.forward;
        }

        if (useLockedAim && charCtrl != null && charCtrl.LockedTarget != null)
        {
            Vector3 lockedDir = charCtrl.LockedTarget.position - attackerUnit.transform.position;
            lockedDir.y = 0f;
            if (lockedDir.sqrMagnitude > 0.001f)
            {
                return lockedDir.normalized;
            }
        }

        if (useAttackFacingInput &&
            charCtrl != null &&
            charCtrl.TryGetAttackFacingDirection(out Vector3 attackFacingDirection))
        {
            return attackFacingDirection;
        }

        if (useDirectionalAimInput &&
            charCtrl != null &&
            charCtrl.TryGetDirectionalAimDirection(out Vector3 directionalAim))
        {
            return directionalAim;
        }

        if (charCtrl != null && charCtrl.Param != null)
        {
            Vector2 aimInput = charCtrl.Param.AimTarget;
            if (TryResolveAimDirectionFromInput(attackerUnit.transform, aimInput, out Vector3 inputDirection))
            {
                return inputDirection;
            }
        }

        return attackerUnit.transform.forward;
    }

    private static bool TryResolveAimDirectionFromInput(Transform attackerTransform, Vector2 aimInput, out Vector3 direction)
    {
        direction = Vector3.zero;
        if (aimInput.sqrMagnitude < 0.01f)
        {
            return false;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null &&
            aimInput.x >= 0f &&
            aimInput.y >= 0f &&
            aimInput.x <= Screen.width &&
            aimInput.y <= Screen.height)
        {
            Ray ray = mainCamera.ScreenPointToRay(aimInput);
            Plane groundPlane = new Plane(Vector3.up, attackerTransform.position);
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 screenDirection = hitPoint - attackerTransform.position;
                screenDirection.y = 0f;
                if (screenDirection.sqrMagnitude > 0.001f)
                {
                    direction = screenDirection.normalized;
                    return true;
                }
            }
        }

        Vector3 fallbackForward = attackerTransform.forward;
        Vector3 fallbackRight = attackerTransform.right;
        if (mainCamera != null)
        {
            fallbackForward = mainCamera.transform.forward;
            fallbackForward.y = 0f;
            if (fallbackForward.sqrMagnitude < 0.001f)
            {
                fallbackForward = attackerTransform.forward;
            }

            fallbackRight = mainCamera.transform.right;
            fallbackRight.y = 0f;
            if (fallbackRight.sqrMagnitude < 0.001f)
            {
                fallbackRight = attackerTransform.right;
            }
        }

        Vector3 rawDirection =
            fallbackRight.normalized * aimInput.x +
            fallbackForward.normalized * aimInput.y;

        rawDirection.y = 0f;
        if (rawDirection.sqrMagnitude < 0.001f)
        {
            return false;
        }

        direction = rawDirection.normalized;
        return true;
    }

    private static bool TryGetLockedEnemy(GameObject attackerUnit, Transform lockedTarget, out GameObject lockedUnit)
    {
        lockedUnit = null;
        if (lockedTarget == null)
        {
            return false;
        }

        lockedUnit = CharRelationResolver.NormalizeUnit(lockedTarget.gameObject);
        return CharRelationResolver.CanReceiveBasicAttack(attackerUnit, lockedUnit);
    }

    private static GameObject FindBestTarget(
        GameObject attackerUnit,
        Vector3 origin,
        Vector3 aimDirection,
        float range,
        float assistAngle)
    {
        float maxRange = Mathf.Max(range, 1f);
        float maxAngle = Mathf.Max(assistAngle, 0f);
        float bestScore = float.MaxValue;
        GameObject bestTarget = null;

        foreach (CharBlackBoard board in CharBlackBoard.ActiveBoards)
        {
            if (board == null)
            {
                continue;
            }

            GameObject candidate = board.gameObject;
            if (!CharRelationResolver.CanReceiveBasicAttack(attackerUnit, candidate))
            {
                continue;
            }

            Vector3 toTarget = candidate.transform.position - origin;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance <= 0f || distance > maxRange)
            {
                continue;
            }

            float angle = Vector3.Angle(aimDirection, toTarget.normalized);
            if (angle > maxAngle)
            {
                continue;
            }

            // Favor angle first, then distance. This keeps controller aiming
            // precise enough for MOBA-like targeting instead of always hitting
            // the absolute nearest unit.
            float score = angle * 1000f + distance;
            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestTarget = candidate;
        }

        return bestTarget;
    }

    private static BasicAttackTargetInfo BuildTargetInfo(Vector3 origin, GameObject targetUnit)
    {
        Vector3 targetPoint = ResolveTargetAimPoint(targetUnit);
        BasicAttackTargetInfo info = new BasicAttackTargetInfo
        {
            targetUnit = targetUnit,
            attackPoint = targetPoint,
        };

        Vector3 direction = info.attackPoint - origin;
        direction.y = 0f;
        info.attackDirection = direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : targetUnit.transform.forward;
        return info;
    }

    private static Vector3 ResolveTargetAimPoint(GameObject targetUnit)
    {
        if (targetUnit == null)
        {
            return Vector3.zero;
        }

        Collider targetCollider = targetUnit.GetComponentInChildren<Collider>();
        if (targetCollider != null)
        {
            return targetCollider.bounds.center;
        }

        CharacterController characterController = targetUnit.GetComponentInChildren<CharacterController>();
        if (characterController != null)
        {
            return characterController.bounds.center;
        }

        return targetUnit.transform.position + Vector3.up;
    }
}
