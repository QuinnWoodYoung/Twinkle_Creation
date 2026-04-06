using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 延迟执行一组效果。
///
/// 这个 Effect 常用来做:
/// - 施法前摇后的爆发
/// - 地面预警一段时间后爆炸
/// - 多段技能里的“第二拍”
/// </summary>
[CreateAssetMenu(fileName = "Delay Effect", menuName = "SkillSystem/Effects/Delay")]
public class DelayEffect : SkillEffect
{
    // 延迟多久后触发，单位秒。
    [Min(0f)] public float delay = 0.25f;

    // 延迟结束后要执行的效果列表。
    public List<SkillEffect> delayedEffects = new List<SkillEffect>();

    public override void Apply(CastContext context)
    {
        // 0 延迟直接立即执行，避免无意义协程。
        if (delay <= 0f)
        {
            SkillEffectUtility.ExecuteEffects(delayedEffects, context);
            return;
        }

        // 延迟、重复这类异步效果都依赖运行时协程容器。
        SkillEffectRuntime runtime = SkillEffectRuntime.Get(context.caster);
        if (runtime == null)
        {
            return;
        }

        // 使用 Snapshot 复制上下文，避免延迟期间外部上下文被后续流程改掉。
        runtime.Run(DelayRoutine(context.Snapshot()));
    }

    private IEnumerator DelayRoutine(CastContext snapshot)
    {
        yield return new WaitForSeconds(delay);
        SkillEffectUtility.ExecuteEffects(delayedEffects, snapshot);
    }
}
