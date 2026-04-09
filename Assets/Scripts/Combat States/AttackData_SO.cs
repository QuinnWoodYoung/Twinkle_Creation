using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BasicAttackMode
{
    MeleeRepeat,
    MeleeCombo,
    RangedStraight,
    RangedHoming,
    RangedChargeRelease,
}

[CreateAssetMenu(fileName = "New Attack", menuName = "Attack")]
public class AttackData_SO : ScriptableObject
{
    [Header("Core Stats")]
    public float attackRange;
    public float maxAttackRange;//???
    public float attackSpeed;
    public float coolDown;
    public float attackTime;
    public float minDamage;
    public float maxDamage;

    [Header("Basic Attack Mode")]
    [Tooltip("这份攻击数据的普攻模式。由具体武器决定，而不是由 WeaponType 决定。")]
    public BasicAttackMode basicAttackMode = BasicAttackMode.MeleeRepeat;
    [Tooltip("普通攻击默认使用的动画 key。")]
    public string attackAnimKey = "Attack";

    [Header("Combo")]
    [Tooltip("近战连击的动画 key。为空时回退到 attackAnimKey。")]
    public string[] comboAnimKeys;
    [Tooltip("连击输入窗口的最大间隔时间。超时后从第一段重新开始。")]
    public float comboResetTime = 0.6f;

    [Header("Projectile")]
    [Tooltip("远程武器是否优先使用锁定目标。通常 Homing 会开启，Straight 会关闭。")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float homingTurnSpeed = 18f;
    public bool preferLockedTarget = true;
    [Tooltip("远程普攻没有锁定目标时，是否允许方向附近的软索敌。")]
    public bool enableSoftLock = true;

    [Header("Charge Release")]
    [Tooltip("蓄力弓的最短蓄力时间。")]
    public float minChargeTime = 0.15f;
    [Tooltip("蓄力弓的最长蓄力时间。")]
    public float maxChargeTime = 1.2f;
    [Tooltip("松手释放时默认使用的动画 key。为空时回退到 attackAnimKey。")]
    public string chargeReleaseAnimKey = "Attack";

    public void ApplyWeaponData(AttackData_SO weapon)
    {
        if (weapon == null)
        {
            return;
        }

        attackRange = weapon.attackRange;
        maxAttackRange = weapon.maxAttackRange;
        attackSpeed = weapon.attackSpeed;
        coolDown = weapon.coolDown;
        attackTime = weapon.attackTime;
        minDamage = weapon.minDamage;
        maxDamage = weapon.maxDamage;

        basicAttackMode = weapon.basicAttackMode;
        attackAnimKey = weapon.attackAnimKey;
        comboAnimKeys = weapon.comboAnimKeys != null ? (string[])weapon.comboAnimKeys.Clone() : null;
        comboResetTime = weapon.comboResetTime;
        projectilePrefab = weapon.projectilePrefab;
        projectileSpeed = weapon.projectileSpeed;
        homingTurnSpeed = weapon.homingTurnSpeed;
        preferLockedTarget = weapon.preferLockedTarget;
        enableSoftLock = weapon.enableSoftLock;
        minChargeTime = weapon.minChargeTime;
        maxChargeTime = weapon.maxChargeTime;
        chargeReleaseAnimKey = weapon.chargeReleaseAnimKey;
    }

}
