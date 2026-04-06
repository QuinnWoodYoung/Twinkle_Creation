using UnityEngine;

/// <summary>
/// 弹道是否启用追踪。
/// </summary>
public enum EProjectileHomingMode
{
    // 永远不追踪，只沿初始方向前进。
    Never,

    // 始终追踪当前单位目标。
    Always,

    // 如果有单位目标则追踪，否则走直线。
    Smart
}

/// <summary>
/// 发射弹道预制体。
///
/// 这个 Effect 负责:
/// 1. 按施法者当前位置和偏移生成 Projectile。
/// 2. 把 CastContext 交给 Projectile 保存。
/// 3. 决定它是追踪飞行还是直线飞行。
///
/// 命中时真正执行什么，由 Projectile.onHitEffects 决定。
/// </summary>
[CreateAssetMenu(fileName = "New Launch Projectile", menuName = "SkillSystem/Effects/LaunchProjectile")]
public class LaunchProjectileEffect : SkillEffect
{
    [Header("Prefab")]
    // 必须挂有 Projectile 脚本的预制体。
    public GameObject projectilePrefab;

    // 弹道生成点，相对施法者本地坐标。
    public Vector3 spawnOffset = new Vector3(0, 1.2f, 1f);

    [Header("Launch")]
    // 追踪模式。
    public EProjectileHomingMode homingMode = EProjectileHomingMode.Smart;

    // 如果目标离得太近，则跳过飞行过程，直接结算命中。
    public float minLaunchDistance = 1.5f;

    public override void Apply(CastContext context)
    {
        if (projectilePrefab == null)
        {
            return;
        }

        GameObject caster = context.caster;
        GameObject targetUnit = context.rawTarget.unit;

        // 目标非常近时，飞行过程通常没有意义，直接触发命中效果更稳定。
        if (targetUnit != null && Vector3.Distance(caster.transform.position, targetUnit.transform.position) < minLaunchDistance)
        {
            InstantHit(context);
            return;
        }

        Vector3 spawnPos = caster.transform.TransformPoint(spawnOffset);
        GameObject projectileObject = Instantiate(projectilePrefab, spawnPos, caster.transform.rotation);
        Projectile projectile = projectileObject.GetComponent<Projectile>();

        if (projectile == null)
        {
            return;
        }

        // 弹道命中后还要继续知道“是谁放的技能、原始目标是谁”，
        // 所以这里必须把上下文传进去。
        projectile.Initialize(context);

        bool shouldHome = homingMode == EProjectileHomingMode.Always ||
                          (homingMode == EProjectileHomingMode.Smart && targetUnit != null);

        projectile.isHoming = shouldHome;
        projectile.target = shouldHome ? targetUnit.transform : null;

        // 非追踪弹用当前上下文方向飞出去。
        if (!shouldHome)
        {
            projectileObject.transform.forward = context.rawTarget.direction;
        }
    }

    /// <summary>
    /// 近距离直接判定命中，不生成真正的飞行物。
    /// </summary>
    private void InstantHit(CastContext context)
    {
        Projectile projectile = projectilePrefab.GetComponent<Projectile>();
        if (projectile == null || projectile.onHitEffects == null)
        {
            return;
        }

        // 仍然构造一个“命中时上下文”，这样 onHitEffects 的写法可以统一。
        CastContext impactContext = context.CreateChild(context.rawTarget, false);
        impactContext.UpdateHitPoint(context.rawTarget.position);

        SkillEffectUtility.ExecuteEffects(projectile.onHitEffects, impactContext);
    }
}
