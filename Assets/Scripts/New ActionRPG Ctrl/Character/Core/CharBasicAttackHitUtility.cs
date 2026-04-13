using UnityEngine;

public static class CharBasicAttackHitUtility
{
    public static bool TryGetUnitBounds(GameObject unit, out Bounds bounds)
    {
        bounds = default;
        GameObject targetUnit = CharRelationResolver.NormalizeUnit(unit);
        if (targetUnit == null)
        {
            return false;
        }

        Collider targetCollider = targetUnit.GetComponentInChildren<Collider>();
        if (targetCollider != null)
        {
            bounds = targetCollider.bounds;
            return true;
        }

        CharacterController characterController = targetUnit.GetComponentInChildren<CharacterController>();
        if (characterController != null)
        {
            bounds = characterController.bounds;
            return true;
        }

        bounds = new Bounds(targetUnit.transform.position + Vector3.up, Vector3.one);
        return true;
    }

    public static Vector3 ResolveUnitAimPoint(GameObject unit, float normalizedHeight = 0.55f)
    {
        GameObject targetUnit = CharRelationResolver.NormalizeUnit(unit);
        if (targetUnit == null)
        {
            return Vector3.zero;
        }

        normalizedHeight = Mathf.Clamp01(normalizedHeight);
        if (TryGetUnitBounds(targetUnit, out Bounds bounds))
        {
            Vector3 point = bounds.center;
            point.y = Mathf.Lerp(bounds.min.y, bounds.max.y, normalizedHeight);
            return point;
        }

        return targetUnit.transform.position + Vector3.up;
    }

    public static float ResolveUnitRadius(GameObject unit)
    {
        if (!TryGetUnitBounds(unit, out Bounds bounds))
        {
            return 0.5f;
        }

        Vector3 extents = bounds.extents;
        return Mathf.Max(0.2f, Mathf.Max(extents.x, extents.z));
    }

    public static float ResolveUnitHeight(GameObject unit)
    {
        if (!TryGetUnitBounds(unit, out Bounds bounds))
        {
            return 2f;
        }

        return Mathf.Max(0.5f, bounds.size.y);
    }

    public static float DistancePointToSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        return Vector3.Distance(point, ClosestPointOnSegment(point, start, end));
    }

    public static float DistancePointToSegmentXZ(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector2 point2 = new Vector2(point.x, point.z);
        Vector2 start2 = new Vector2(start.x, start.z);
        Vector2 end2 = new Vector2(end.x, end.z);
        return Vector2.Distance(point2, ClosestPointOnSegment(point2, start2, end2));
    }

    public static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float sqrMagnitude = segment.sqrMagnitude;
        if (sqrMagnitude <= 0.0001f)
        {
            return start;
        }

        float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / sqrMagnitude);
        return start + segment * t;
    }

    private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float sqrMagnitude = segment.sqrMagnitude;
        if (sqrMagnitude <= 0.0001f)
        {
            return start;
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / sqrMagnitude);
        return start + segment * t;
    }
}
