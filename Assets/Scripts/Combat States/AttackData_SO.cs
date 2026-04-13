using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum BasicAttackMode
{
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
    public float maxAttackRange;
    [FormerlySerializedAs("attackSpeed")] public float rangedAttackSpeed;
    public float coolDown;
    public float attackTime;
    public float minDamage;
    public float maxDamage;

    [Header("Basic Attack Mode")]
    [Tooltip("这份攻击数据的普攻模式。由具体武器决定，而不是由 WeaponType 决定。")]
    public BasicAttackMode basicAttackMode = BasicAttackMode.MeleeCombo;
    [Tooltip("普通攻击默认使用的动画 key。")]
    public string attackAnimKey = "Attack";

    [Header("Combo")]
    [Tooltip("近战连击的动画 key。为空时回退到 attackAnimKey。")]
    public string[] comboAnimKeys;
    [Tooltip("连击输入窗口的最大间隔时间。超时后从第一段重新开始。")]
    public float comboResetTime = 0.6f;
    [Tooltip("普攻期间是否允许角色继续移动。")]
    public bool canMoveWhileAttack = true;
    public bool useUpperBodyMoveAttackPresentation;

    [Header("Logic Hit")]
    public bool useLogicHitResolution = true;
    public float logicHitDelay = 0.08f;
    public bool allowLegacyCollisionDamage = false;

    [Header("Melee Logic Hit")]
    public float meleeLogicRange = 2f;
    public float meleeHitRadius = 0.9f;
    [Range(10f, 180f)]
    public float meleeHitAngle = 100f;
    public bool preferResolvedTarget = true;
    public bool allowMultiTargetMelee = false;
    public int maxMeleeTargets = 1;
    [Range(0f, 1f)]
    public float targetAimHeight = 0.55f;

    [Header("Projectile Logic Hit")]
    public bool projectileUseLogicHit = true;
    public float projectileHitRadius = 0.45f;
    public float projectileVerticalTolerance = 1.2f;
    public bool projectileFollowGround = true;
    public float projectileGroundOffset = 0.9f;
    public float projectileGroundProbeHeight = 4f;
    public float projectileGroundProbeDistance = 12f;
    public float projectileImpactDistance = 0.35f;
    public float projectileTargetHeightOffset = 0.1f;

    [Header("Projectile")]
    [Tooltip("远程武器是否优先使用锁定目标。通常 Homing 开启，Straight 关闭。")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float homingTurnSpeed = 18f;
    public bool preferLockedTarget = true;
    [Tooltip("远程普攻没有锁定目标时，是否允许方向附近的软索敌。")]
    public bool enableSoftLock = true;

    [Header("Charge Release")]
    [Tooltip("蓄力攻击的最短蓄力时间。")]
    public float minChargeTime = 0.15f;
    [Tooltip("蓄力攻击的最长蓄力时间。")]
    public float maxChargeTime = 1.2f;
    [Tooltip("松手释放时默认使用的动画 key。为空时回退到 attackAnimKey。")]
    public string chargeReleaseAnimKey = "Attack";

    [Header("Basic Attack VFX")]
    public GameObject attackCastVfx;
    public GameObject attackHitVfx;
    public string attackVfxPointName = "BasicAttack VFX";
    public string projectileSpawnPointName = "";
    public bool preferOwnerAttackVfxPoint = true;
    public bool attachAttackCastVfxToPoint = false;
    public bool attachAttackHitVfxToTarget = false;
    public float attackCastVfxLifetime = 2f;
    public float attackHitVfxLifetime = 2f;
    public Vector3 attackCastVfxOffset;
    public Vector3 attackHitVfxOffset;

    public void ApplyWeaponData(AttackData_SO weapon)
    {
        if (weapon == null)
        {
            return;
        }

        attackRange = weapon.attackRange;
        maxAttackRange = weapon.maxAttackRange;
        rangedAttackSpeed = weapon.rangedAttackSpeed;
        coolDown = weapon.coolDown;
        attackTime = weapon.attackTime;
        minDamage = weapon.minDamage;
        maxDamage = weapon.maxDamage;

        basicAttackMode = weapon.basicAttackMode;
        attackAnimKey = weapon.attackAnimKey;
        comboAnimKeys = weapon.comboAnimKeys != null ? (string[])weapon.comboAnimKeys.Clone() : null;
        comboResetTime = weapon.comboResetTime;
        canMoveWhileAttack = weapon.canMoveWhileAttack;
        useUpperBodyMoveAttackPresentation = weapon.useUpperBodyMoveAttackPresentation;
        projectilePrefab = weapon.projectilePrefab;
        projectileSpeed = weapon.projectileSpeed;
        homingTurnSpeed = weapon.homingTurnSpeed;
        preferLockedTarget = weapon.preferLockedTarget;
        enableSoftLock = weapon.enableSoftLock;
        useLogicHitResolution = weapon.useLogicHitResolution;
        logicHitDelay = weapon.logicHitDelay;
        allowLegacyCollisionDamage = weapon.allowLegacyCollisionDamage;
        meleeLogicRange = weapon.meleeLogicRange;
        meleeHitRadius = weapon.meleeHitRadius;
        meleeHitAngle = weapon.meleeHitAngle;
        preferResolvedTarget = weapon.preferResolvedTarget;
        allowMultiTargetMelee = weapon.allowMultiTargetMelee;
        maxMeleeTargets = weapon.maxMeleeTargets;
        targetAimHeight = weapon.targetAimHeight;
        projectileUseLogicHit = weapon.projectileUseLogicHit;
        projectileHitRadius = weapon.projectileHitRadius;
        projectileVerticalTolerance = weapon.projectileVerticalTolerance;
        projectileFollowGround = weapon.projectileFollowGround;
        projectileGroundOffset = weapon.projectileGroundOffset;
        projectileGroundProbeHeight = weapon.projectileGroundProbeHeight;
        projectileGroundProbeDistance = weapon.projectileGroundProbeDistance;
        projectileImpactDistance = weapon.projectileImpactDistance;
        projectileTargetHeightOffset = weapon.projectileTargetHeightOffset;
        minChargeTime = weapon.minChargeTime;
        maxChargeTime = weapon.maxChargeTime;
        chargeReleaseAnimKey = weapon.chargeReleaseAnimKey;
        attackCastVfx = weapon.attackCastVfx;
        attackHitVfx = weapon.attackHitVfx;
        attackVfxPointName = weapon.attackVfxPointName;
        projectileSpawnPointName = weapon.projectileSpawnPointName;
        preferOwnerAttackVfxPoint = weapon.preferOwnerAttackVfxPoint;
        attachAttackCastVfxToPoint = weapon.attachAttackCastVfxToPoint;
        attachAttackHitVfxToTarget = weapon.attachAttackHitVfxToTarget;
        attackCastVfxLifetime = weapon.attackCastVfxLifetime;
        attackHitVfxLifetime = weapon.attackHitVfxLifetime;
        attackCastVfxOffset = weapon.attackCastVfxOffset;
        attackHitVfxOffset = weapon.attackHitVfxOffset;
    }
}
