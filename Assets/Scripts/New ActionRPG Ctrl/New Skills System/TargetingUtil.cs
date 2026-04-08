using UnityEngine;

public static class TargetingUtil
{
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
