using UnityEngine;

/// <summary>
/// 决定 Buff 要施加给谁。
/// </summary>
public enum EBuffTarget
{
    // 给施法者自己上状态。
    Caster,

    // 给当前技能目标上状态。
    Target
}

/// <summary>
/// 给单位施加一个状态效果。
///
/// 这个 Effect 的职责很单纯:
/// 1. 决定最终目标是施法者还是当前目标。
/// 2. 找到目标的 StateManager。
/// 3. 调用 ApplyStatus。
///
/// 状态如何生效、能否叠加、是否可驱散、是否打断施法，
/// 这些更细的规则仍然应该交给状态系统本身处理。
/// </summary>
[CreateAssetMenu(fileName = "New Apply Buff Effect", menuName = "SkillSystem/Effects/ApplyBuff")]
public class ApplyBuffEffect : SkillEffect
{
    [Header("Status")]
    // 施加什么状态类型，例如 Stun / Slow / Silence。
    public EStatusType statusToApply;

    // Buff 最终给谁。
    public EBuffTarget whoToApply = EBuffTarget.Target;

    [Header("Duration")]
    // 状态持续时间，单位秒。
    public float buffDuration = 10f;

    public override void Apply(CastContext context)
    {
        // 先确定这次状态的目标是谁。
        GameObject finalTarget = whoToApply == EBuffTarget.Caster
            ? context.caster
            : context.rawTarget.unit;

        if (finalTarget == null)
        {
            return;
        }

        // 状态入口仍然通过 StateManager。
        StateManager stateManager = finalTarget.GetComponent<StateManager>();
        if (stateManager == null)
        {
            stateManager = finalTarget.GetComponentInParent<StateManager>();
        }

        if (stateManager != null)
        {
            stateManager.ApplyStatus(statusToApply, buffDuration);
            Debug.Log($"Apply {statusToApply} to {finalTarget.name}");
        }
    }
}
