using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 重复执行一组效果。
///
/// 适合制作:
/// - 持续多跳伤害
/// - 周期治疗
/// - 多段脉冲 AoE
/// </summary>
[CreateAssetMenu(fileName = "Repeat Effect", menuName = "SkillSystem/Effects/Repeat")]
public class RepeatEffect : SkillEffect
{
    // 一共触发几次。
    [Min(1)] public int repeatCount = 3;

    // 每两次触发之间的间隔。
    [Min(0f)] public float interval = 0.5f;

    // 是否在一开始就先执行一次。
    // 为 false 时，先等 interval，再开始第一次触发。
    public bool triggerImmediately = true;

    // 每一跳都要执行的效果列表。
    public List<SkillEffect> repeatedEffects = new List<SkillEffect>();

    public override void Apply(CastContext context)
    {
        if (repeatCount <= 0)
        {
            return;
        }

        SkillEffectRuntime runtime = SkillEffectRuntime.Get(context.caster);
        if (runtime == null)
        {
            return;
        }

        // 重复型效果是异步过程，必须复制一份独立上下文。
        runtime.Run(RepeatRoutine(context.Snapshot()));
    }

    private IEnumerator RepeatRoutine(CastContext snapshot)
    {
        if (!triggerImmediately)
        {
            yield return new WaitForSeconds(interval);
        }

        for (int i = 0; i < repeatCount; i++)
        {
            // 每一轮都再次 Snapshot，
            // 防止 repeatedEffects 内部修改上下文影响后续轮次。
            SkillEffectUtility.ExecuteEffects(repeatedEffects, snapshot.Snapshot());

            if (i < repeatCount - 1)
            {
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
