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
            TryReceiveAttack(weaponAttacker);
            return;
        }

        if (TryResolveProjectileAttacker(col, out GameObject projectileAttacker))
        {
            TryReceiveAttack(projectileAttacker);
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

        if (!bullet.TryConsumeImpact())
        {
            return false;
        }

        attacker = bullet.launcher;
        return true;
    }

    private void TryReceiveAttack(GameObject attacker)
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
    }
}
