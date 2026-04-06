using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Attack", menuName = "Attack")]
public class AttackData_SO : ScriptableObject
{
    public float attackRange;
    public float maxAttackRange;//???
    public float attackSpeed;
    public float coolDown;
    public float attackTime;
    public float minDamage;
    public float maxDamage;

    public void ApplyWeaponData(AttackData_SO weapon)
    {
        minDamage = weapon.minDamage;
        maxDamage = weapon.maxDamage;
    }

}
