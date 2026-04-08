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

        if (col.CompareTag("weapon"))
        {
            TryReceiveWeaponHit(col);
            return;
        }

        if (col.CompareTag("Projectile"))
        {
            TryReceiveProjectileHit(col);
        }
    }

    private void TryReceiveWeaponHit(Collider col)
    {
        WeaponInfo weaponInfo = col.GetComponent<WeaponInfo>();
        if (weaponInfo == null)
        {
            weaponInfo = col.GetComponentInParent<WeaponInfo>();
        }

        TryReceiveAttack(weaponInfo != null ? weaponInfo.owner : null);
    }

    private void TryReceiveProjectileHit(Collider col)
    {
        Bullet bullet = col.GetComponent<Bullet>();
        if (bullet == null)
        {
            bullet = col.GetComponentInParent<Bullet>();
        }

        TryReceiveAttack(bullet != null ? bullet.launcher : null);
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
