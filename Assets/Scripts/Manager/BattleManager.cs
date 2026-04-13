using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public ActorManager am;
    public StateManager sm;

    private void Awake()
    {
        if (am == null)
        {
            am = GetComponent<ActorManager>();
        }

        if (sm == null)
        {
            sm = GetComponent<StateManager>();
        }
    }

    private void OnTriggerEnter(Collider col)
    {
        GameObject selfUnit = CharRelationResolver.NormalizeUnit(gameObject);
        if (col == null || !CharRelationResolver.IsAlive(selfUnit))
        {
            return;
        }

        if (TryResolveWeaponAttacker(col, out GameObject weaponAttacker))
        {
            AttackData_SO attackData = CharResourceResolver.GetAttackData(weaponAttacker);
            if (ShouldSkipLegacyWeaponCollisionDamage(attackData))
            {
                return;
            }

            TryReceiveAttack(weaponAttacker, col);
            return;
        }

        if (TryResolveProjectileAttacker(col, out GameObject projectileAttacker))
        {
            TryReceiveAttack(projectileAttacker, col);
        }
    }

    private bool TryResolveWeaponAttacker(Collider col, out GameObject attacker)
    {
        attacker = null;
        WeaponInfo weaponInfo = col.GetComponent<WeaponInfo>();
        if (weaponInfo == null)
        {
            weaponInfo = col.GetComponentInParent<WeaponInfo>();
        }

        if (weaponInfo == null || weaponInfo.owner == null)
        {
            return false;
        }

        attacker = weaponInfo.owner;
        return true;
    }

    private bool TryResolveProjectileAttacker(Collider col, out GameObject attacker)
    {
        attacker = null;
        Bullet bullet = col.GetComponent<Bullet>();
        if (bullet == null)
        {
            bullet = col.GetComponentInParent<Bullet>();
        }

        if (bullet == null || bullet.launcher == null)
        {
            return false;
        }

        if (!bullet.UseLegacyCollisionDamage)
        {
            return false;
        }

        if (!bullet.TryConsumeImpact())
        {
            return false;
        }

        attacker = bullet.launcher;
        return true;
    }

    private void TryReceiveAttack(GameObject attacker, Collider sourceCollider)
    {
        GameObject attackerUnit = CharRelationResolver.NormalizeUnit(attacker);
        GameObject defenderUnit = CharRelationResolver.NormalizeUnit(gameObject);
        if (!CharRelationResolver.CanReceiveBasicAttack(attackerUnit, defenderUnit))
        {
            return;
        }

        float damage = CharResourceResolver.GetBasicAttackDamage(attackerUnit);
        if (damage <= 0f)
        {
            return;
        }

        CharResourceResolver.ApplyDamage(defenderUnit, damage);
        AttackData_SO attackData = CharResourceResolver.GetAttackData(attackerUnit);
        Vector3 impactPoint = ResolveImpactPoint(sourceCollider, defenderUnit, attackData);
        CharBasicAttackVfxUtility.PlayHitVfx(attackData, impactPoint, defenderUnit);
    }

    private static Vector3 ResolveImpactPoint(Collider sourceCollider, GameObject defenderUnit, AttackData_SO attackData)
    {
        float aimHeight = attackData != null ? attackData.targetAimHeight : 0.55f;
        Vector3 fallbackPoint = CharBasicAttackHitUtility.ResolveUnitAimPoint(defenderUnit, aimHeight);
        if (sourceCollider == null)
        {
            return fallbackPoint;
        }

        Vector3 impactPoint = sourceCollider.ClosestPoint(fallbackPoint);
        if (float.IsNaN(impactPoint.x) || float.IsInfinity(impactPoint.x)
            || float.IsNaN(impactPoint.y) || float.IsInfinity(impactPoint.y)
            || float.IsNaN(impactPoint.z) || float.IsInfinity(impactPoint.z))
        {
            return fallbackPoint;
        }

        return impactPoint;
    }

    private static bool ShouldSkipLegacyWeaponCollisionDamage(AttackData_SO attackData)
    {
        if (attackData == null || !attackData.useLogicHitResolution || attackData.allowLegacyCollisionDamage)
        {
            return false;
        }

        switch (attackData.basicAttackMode)
        {
            case BasicAttackMode.RangedStraight:
            case BasicAttackMode.RangedHoming:
            case BasicAttackMode.RangedChargeRelease:
                return true;
            default:
                return false;
        }
    }
}
