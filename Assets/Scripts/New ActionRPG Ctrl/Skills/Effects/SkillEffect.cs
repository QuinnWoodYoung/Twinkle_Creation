using UnityEngine;

/// <summary>
/// 所有技能效果的抽象基类。
///
/// 你可以把 SkillEffect 理解为“技能流水线中的一个处理节点”。
/// 每个具体子类只关心自己做什么，例如伤害、治疗、位移、播放特效、延迟执行等。
/// 技能系统真正的组合能力，就来自多个 SkillEffect 的串联。
/// </summary>
public abstract class SkillEffect : ScriptableObject
{
    /// <summary>
    /// 执行当前效果。
    /// </summary>
    /// <param name="context">
    /// 本次施法的运行时上下文。
    /// 里面包含施法者、原始目标、当前命中点、方向等信息。
    /// </param>
    public abstract void Apply(CastContext context);
}
