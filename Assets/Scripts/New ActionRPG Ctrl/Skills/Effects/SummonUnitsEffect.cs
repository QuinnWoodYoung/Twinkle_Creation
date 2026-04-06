using UnityEngine;

/// <summary>
/// 召唤多个单位。
///
/// 它会在指定中心点附近随机生成 summonCount 个单位，
/// 并可选择继承施法者阵营，以及设置召唤物生命周期。
/// </summary>
[CreateAssetMenu(fileName = "Summon Units Effect", menuName = "SkillSystem/Effects/SummonUnits")]
public class SummonUnitsEffect : SkillEffect
{
    [Header("Summon")]
    // 要生成的召唤物预制体。
    public GameObject summonPrefab;

    // 一次生成几个。
    [Min(1)] public int summonCount = 1;

    // 围绕中心点随机散开的半径。
    public float spawnRadius = 2f;

    // 召唤中心从哪里取。
    public ContextPointSelector spawnOrigin = ContextPointSelector.HitPoint;

    // 召唤物初始朝向。
    public ContextDirectionSelector facingDirection = ContextDirectionSelector.CasterForward;

    // 是否把施法者的 Team 复制给召唤物。
    public bool inheritCasterTeam = true;

    // 生命周期，单位秒。
    // 小于等于 0 表示永久存在。
    public float lifetime;

    public override void Apply(CastContext context)
    {
        if (summonPrefab == null)
        {
            return;
        }

        Vector3 center = CastContextResolver.ResolvePoint(context, spawnOrigin);
        Quaternion facing = Quaternion.LookRotation(
            CastContextResolver.ResolveDirection(context, facingDirection),
            Vector3.up);

        for (int i = 0; i < summonCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = center + new Vector3(randomCircle.x, 0f, randomCircle.y);
            GameObject summoned = Object.Instantiate(summonPrefab, spawnPosition, facing);

            // MOBA 里召唤物通常应该和主人属于同一阵营。
            if (inheritCasterTeam)
            {
                SkillEffectUtility.CopyCasterTeam(context.caster, summoned);
            }

            // 如果设置了生命周期，就到时自动清理。
            if (lifetime > 0f)
            {
                Object.Destroy(summoned, lifetime);
            }
        }
    }
}
