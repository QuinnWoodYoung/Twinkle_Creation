using UnityEngine;

/// <summary>
/// 目标偏移效果。
///
/// 它不会直接产生伤害或 VFX，而是修改当前 CastContext 的目标信息，
/// 让后续 Effect 改为在一个新的位置继续执行。
///
/// 这是很多 Dota 类技能里非常关键的“中间处理块”。
/// </summary>
[CreateAssetMenu(fileName = "Offset Target Effect", menuName = "SkillSystem/Effects/OffsetTarget")]
public class OffsetTargetEffect : SkillEffect
{
    [Header("Origin")]
    // 偏移从哪里开始算。
    public ContextPointSelector originPoint = ContextPointSelector.CasterPosition;

    // “前方”取哪个方向。
    public ContextDirectionSelector directionSource = ContextDirectionSelector.CasterForward;

    [Header("Offset")]
    // 沿前方推进的距离。
    public float forwardOffset;

    // 额外的本地空间偏移。
    public Vector3 localOffset;

    [Header("Target")]
    // 是否清空原本锁定的单位目标。
    // 打勾后，后续 Effect 将更像是在处理一个地点目标。
    public bool clearUnitTarget = true;

    public override void Apply(CastContext context)
    {
        Vector3 origin = CastContextResolver.ResolvePoint(context, originPoint);
        Vector3 forward = CastContextResolver.ResolveDirection(context, directionSource);
        Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);

        // 把前向推进和局部偏移合成一个最终世界坐标。
        Vector3 worldOffset = (forward * forwardOffset) + (rotation * localOffset);
        Vector3 targetPosition = origin + worldOffset;

        // 是否保留单位锁定，取决于 clearUnitTarget。
        GameObject targetUnit = clearUnitTarget ? null : context.rawTarget.unit;
        context.Retarget(targetUnit, targetPosition);
    }
}
