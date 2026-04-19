using UnityEngine;

public static class TargetingUtil
{
    private const float GamepadAimDeadZone = 0.2f;
    private const float GamepadAimDistanceCurve = 2.0f;
    private const float GamepadMinProbeDistance = 0.75f;

    public static TargetInfo Collect(CharCtrl caster, LayerMask groundLayer)
    {
        TargetInfo info = new TargetInfo();
        Vector3 finalPos;

        if (caster.LockedTarget != null)
        {
            GameObject lockedUnit = CharRelationResolver.NormalizeUnit(caster.LockedTarget.gameObject);
            info.unit = lockedUnit;
            finalPos = lockedUnit != null ? lockedUnit.transform.position : caster.LockedTarget.position;
        }
        else if (ShouldUseGamepadTargeting(caster))
        {
            return CollectFromGamepad(caster, groundLayer);
        }
        else
        {
            Ray ray = Camera.main.ScreenPointToRay(caster.Param.AimTarget);
            GameObject hitUnit = FindFirstUnitOnRay(ray, 1000f);
            if (hitUnit != null)
            {
                info.unit = hitUnit;
                finalPos = hitUnit.transform.position;
            }
            else if (Physics.Raycast(ray, out RaycastHit groundHit, 500f, groundLayer))
            {
                finalPos = groundHit.point;
            }
            else
            {
                Plane groundPlane = new Plane(Vector3.up, caster.transform.position);
                groundPlane.Raycast(ray, out float enter);
                finalPos = ray.GetPoint(enter);
            }
        }

        info.position = finalPos;
        Vector3 dir = finalPos - caster.transform.position;
        dir.y = 0f;
        info.direction = dir.magnitude > 0.1f ? dir.normalized : caster.transform.forward;
        return info;
    }

    private static bool ShouldUseGamepadTargeting(CharCtrl caster)
    {
        return caster != null &&
               caster.Param != null &&
               PlayerInputManager.instance != null &&
               PlayerInputManager.instance.IsUsingGamepadInput;
    }

    private static TargetInfo CollectFromGamepad(CharCtrl caster, LayerMask groundLayer)
    {
        TargetInfo info = new TargetInfo();
        Vector3 origin = caster.transform.position;
        Vector2 stick = ResolveGamepadStick();
        Vector3 direction = ResolveGamepadDirection(caster, stick);
        float probeDistance = ResolveGamepadProbeDistance(caster.gameObject, stick);

        if (TryFindDirectionalUnit(caster.gameObject, origin, direction, probeDistance, out GameObject unit))
        {
            info.unit = unit;
            info.position = unit.transform.position;
        }
        else if (TryResolveGroundPoint(origin, direction, probeDistance, groundLayer, out Vector3 groundPoint))
        {
            info.position = groundPoint;
        }
        else
        {
            info.position = origin + direction * probeDistance;
        }

        Vector3 finalDirection = info.position - origin;
        finalDirection.y = 0f;
        info.direction = finalDirection.sqrMagnitude > 0.01f
            ? finalDirection.normalized
            : direction;
        return info;
    }

    private static Vector2 ResolveGamepadStick()
    {
        PlayerInputManager inputManager = PlayerInputManager.instance;
        return inputManager != null ? inputManager.GamepadAimStick : Vector2.zero;
    }

    private static Vector3 ResolveGamepadDirection(CharCtrl caster, Vector2 stick)
    {
        Vector3 fallbackForward = caster.transform.forward;
        fallbackForward.y = 0f;
        if (fallbackForward.sqrMagnitude < 0.001f)
        {
            fallbackForward = Vector3.forward;
        }

        if (stick.sqrMagnitude < 0.001f)
        {
            return fallbackForward.normalized;
        }

        Camera mainCamera = Camera.main;
        Vector3 cameraForward = fallbackForward;
        Vector3 cameraRight = caster.transform.right;
        cameraRight.y = 0f;

        if (mainCamera != null)
        {
            cameraForward = mainCamera.transform.forward;
            cameraForward.y = 0f;
            if (cameraForward.sqrMagnitude < 0.001f)
            {
                cameraForward = fallbackForward;
            }

            cameraRight = mainCamera.transform.right;
            cameraRight.y = 0f;
            if (cameraRight.sqrMagnitude < 0.001f)
            {
                cameraRight = caster.transform.right;
                cameraRight.y = 0f;
            }
        }

        Vector3 worldDirection =
            cameraRight.normalized * stick.x +
            cameraForward.normalized * stick.y;
        worldDirection.y = 0f;

        return worldDirection.sqrMagnitude > 0.001f
            ? worldDirection.normalized
            : fallbackForward.normalized;
    }

    private static float ResolveGamepadProbeDistance(GameObject caster, Vector2 stick)
    {
        float maxDistance = CharResourceResolver.GetMaxAttackRange(caster);
        maxDistance = maxDistance > 0f ? Mathf.Max(maxDistance, 10f) : 10f;

        float stickMagnitude = stick.magnitude;
        if (stickMagnitude <= GamepadAimDeadZone)
        {
            return GamepadMinProbeDistance;
        }

        float normalizedMagnitude = Mathf.InverseLerp(GamepadAimDeadZone, 1f, Mathf.Clamp01(stickMagnitude));
        float curvedMagnitude = Mathf.Pow(normalizedMagnitude, GamepadAimDistanceCurve);
        return Mathf.Lerp(GamepadMinProbeDistance, maxDistance, curvedMagnitude);
    }

    private static bool TryFindDirectionalUnit(
        GameObject caster,
        Vector3 origin,
        Vector3 direction,
        float maxDistance,
        out GameObject result)
    {
        result = null;
        GameObject casterUnit = CharRelationResolver.NormalizeUnit(caster);
        float bestScore = float.MaxValue;

        foreach (CharBlackBoard board in CharBlackBoard.ActiveBoards)
        {
            if (board == null || board.gameObject == null)
            {
                continue;
            }

            GameObject candidate = CharRelationResolver.NormalizeUnit(board.gameObject);
            if (candidate == null || candidate == casterUnit)
            {
                continue;
            }

            Vector3 toTarget = candidate.transform.position - origin;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance <= 0.01f || distance > maxDistance)
            {
                continue;
            }

            float angle = Vector3.Angle(direction, toTarget / distance);
            if (angle > 22f)
            {
                continue;
            }

            float score = angle * 1000f + distance;
            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            result = candidate;
        }

        return result != null;
    }

    private static bool TryResolveGroundPoint(
        Vector3 origin,
        Vector3 direction,
        float probeDistance,
        LayerMask groundLayer,
        out Vector3 groundPoint)
    {
        groundPoint = origin + direction * probeDistance;

        Vector3 rayOrigin = origin + Vector3.up * 6f;
        if (Physics.Raycast(rayOrigin, direction, out RaycastHit forwardHit, probeDistance, groundLayer))
        {
            groundPoint = forwardHit.point;
            return true;
        }

        Vector3 candidate = origin + direction * probeDistance;
        Vector3 downOrigin = candidate + Vector3.up * 12f;
        if (Physics.Raycast(downOrigin, Vector3.down, out RaycastHit downHit, 30f, groundLayer))
        {
            groundPoint = downHit.point;
            return true;
        }

        Plane groundPlane = new Plane(Vector3.up, origin);
        Ray fallbackRay = new Ray(rayOrigin, direction);
        if (groundPlane.Raycast(fallbackRay, out float enter))
        {
            groundPoint = fallbackRay.GetPoint(enter);
            return true;
        }

        return false;
    }

    private static GameObject FindFirstUnitOnRay(Ray ray, float distance)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, distance);
        if (hits == null || hits.Length == 0)
        {
            return null;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            if (!CharRelationResolver.TryResolveUnit(hits[i].collider.gameObject, out GameObject unit))
            {
                continue;
            }

            return unit;
        }

        return null;
    }
}
