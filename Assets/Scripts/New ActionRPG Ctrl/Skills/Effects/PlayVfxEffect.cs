using UnityEngine;

/// <summary>
/// 播放一个 VFX 预制体。
///
/// 这个 Effect 只负责生成特效对象，不负责伤害、Buff、命中判断。
/// 它通常和 Damage / Aoe / Delay / Projectile 组合使用。
///
/// 支持两种常见模式:
/// 1. 在世界空间某个点生成一次性特效。
/// 2. 挂到某个单位身上，作为持续附着特效。
/// </summary>
[CreateAssetMenu(fileName = "Play VFX Effect", menuName = "SkillSystem/Effects/PlayVFX")]
public class PlayVfxEffect : SkillEffect
{
    [Header("Prefab")]
    // 要播放的特效预制体。
    public GameObject vfxPrefab;

    [Header("Placement")]
    // 不挂接时，世界坐标从哪里取。
    public ContextPointSelector spawnPoint = ContextPointSelector.HitPoint;

    // 生成朝向使用哪个方向。
    public ContextDirectionSelector directionSource = ContextDirectionSelector.CasterForward;

    // 如果 attachToTarget = true，就挂到哪个单位身上。
    public ContextUnitSelector attachTarget = ContextUnitSelector.CurrentTarget;

    // 是否把特效作为子物体挂到目标上。
    public bool attachToTarget;

    // 是否让特效朝向 directionSource。
    public bool rotateWithDirection = true;

    // 偏移量是否跟随 rotation 一起旋转。
    // 勾选时 positionOffset 会被视作“局部偏移”。
    public bool localOffsetUsesDirection = true;

    // 位置偏移。
    public Vector3 positionOffset;

    // 额外欧拉角偏移。
    public Vector3 eulerOffset;

    [Header("Lifetime")]
    // 自动销毁延迟。
    // 小于等于 0 表示不自动销毁，由特效自己决定生命周期。
    public float autoDestroyDelay = 5f;

    public override void Apply(CastContext context)
    {
        if (vfxPrefab == null)
        {
            return;
        }

        // 先根据上下文算出生成朝向。
        Vector3 direction = CastContextResolver.ResolveDirection(context, directionSource);
        Quaternion rotation = rotateWithDirection
            ? Quaternion.LookRotation(direction, Vector3.up)
            : Quaternion.identity;
        rotation *= Quaternion.Euler(eulerOffset);

        GameObject instance;
        if (attachToTarget)
        {
            // 挂接模式: 特效跟随某个单位移动。
            Transform parent = CastContextResolver.ResolveTransform(context, attachTarget);
            if (parent == null)
            {
                return;
            }

            instance = Object.Instantiate(vfxPrefab, parent.position, rotation, parent);

            // 挂接模式下，positionOffset 本质是局部坐标偏移。
            instance.transform.localPosition = localOffsetUsesDirection
                ? Quaternion.identity * positionOffset
                : positionOffset;
        }
        else
        {
            // 世界空间模式: 在某个点直接生成。
            Vector3 spawnPosition = CastContextResolver.ResolvePoint(context, spawnPoint);
            Vector3 offset = localOffsetUsesDirection ? rotation * positionOffset : positionOffset;
            instance = Object.Instantiate(vfxPrefab, spawnPosition + offset, rotation);
        }

        if (autoDestroyDelay > 0f)
        {
            Object.Destroy(instance, autoDestroyDelay);
        }
    }
}
