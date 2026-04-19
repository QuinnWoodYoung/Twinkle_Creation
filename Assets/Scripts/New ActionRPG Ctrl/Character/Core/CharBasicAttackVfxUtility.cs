using UnityEngine;

/// <summary>
/// 普攻命中特效工具。
/// 负责把命中 VFX 生在目标身上或命中点上。
/// </summary>
public static class CharBasicAttackVfxUtility
{
    public static void PlayHitVfx(AttackData_SO profile, Vector3 impactPoint, GameObject targetUnit)
    {
        if (profile == null)
        {
            return;
        }

        PlayHitVfx(
            profile.attackHitVfx,
            profile.attachAttackHitVfxToTarget,
            profile.attackHitVfxOffset,
            profile.attackHitVfxLifetime,
            impactPoint,
            targetUnit,
            profile.targetAimHeight);
    }

    public static void PlayHitVfx(
        GameObject vfxPrefab,
        bool attachToTarget,
        Vector3 offset,
        float lifetime,
        Vector3 impactPoint,
        GameObject targetUnit,
        float targetAimHeight = 0.55f)
    {
        if (vfxPrefab == null)
        {
            return;
        }

        GameObject resolvedTarget = CharRelationResolver.NormalizeUnit(targetUnit);
        Transform parent = attachToTarget && resolvedTarget != null ? resolvedTarget.transform : null;
        Vector3 spawnPoint = impactPoint;

        if (resolvedTarget != null)
        {
            spawnPoint = CharBasicAttackHitUtility.ResolveUnitAimPoint(resolvedTarget, targetAimHeight);
        }

        GameObject instance;
        if (parent != null)
        {
            instance = Object.Instantiate(vfxPrefab, spawnPoint, Quaternion.identity, parent);
            instance.transform.localPosition += offset;
        }
        else
        {
            instance = Object.Instantiate(vfxPrefab, spawnPoint + offset, Quaternion.identity);
        }

        if (lifetime > 0f)
        {
            Object.Destroy(instance, lifetime);
        }
    }
}
