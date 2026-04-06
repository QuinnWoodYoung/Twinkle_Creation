using UnityEngine;

/// <summary>
/// 最基础的伤害效果。
/// 它会从 CastContext 中解析出一个目标单位，然后把 damageAmount 交给目标的 StateManager。
///
/// 这个 Effect 自己不关心敌我，也不关心命中方式。
/// 敌我过滤通常应该在更前面的选目标、AoE 搜索、弹道命中阶段完成。
/// </summary>
[CreateAssetMenu(menuName = "SkillSystem/Effects/Damage")]
public class DamageEffect : SkillEffect
{
    // 伤害打到谁。
    // 常见情况是 CurrentTarget，也就是当前命中的那个单位。
    public ContextUnitSelector targetUnit = ContextUnitSelector.CurrentTarget;

    // 固定伤害值。
    // 后续如果你要做法强加成、技能等级成长、护甲减伤，可以从这里继续扩展。
    public float damageAmount = 50f;

    public override void Apply(CastContext context)
    {
        // 先根据上下文找出真正要受伤的单位。
        GameObject targetObject = CastContextResolver.ResolveUnit(context, targetUnit);
        if (targetObject == null)
        {
            return;
        }

        // 命中的可能是子碰撞体，所以先查自己，再查父级。
        StateManager state = targetObject.GetComponent<StateManager>();
        if (state == null)
        {
            state = targetObject.GetComponentInParent<StateManager>();
        }

        // 真正的伤害结算入口。
        if (state != null)
        {
            state.TakeDamage(damageAmount);
        }
    }
}
