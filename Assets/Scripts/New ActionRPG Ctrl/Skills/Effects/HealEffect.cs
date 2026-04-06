using UnityEngine;

/// <summary>
/// 基础治疗效果。
/// 它的结构和 DamageEffect 几乎一致，只是最终调用的是 ApplyHealth。
/// </summary>
[CreateAssetMenu(fileName = "Heal Effect", menuName = "SkillSystem/Effects/Heal")]
public class HealEffect : SkillEffect
{
    // 治疗给谁。
    public ContextUnitSelector targetUnit = ContextUnitSelector.CurrentTarget;

    // 固定治疗量。
    public int healAmount = 50;

    public override void Apply(CastContext context)
    {
        GameObject targetObject = CastContextResolver.ResolveUnit(context, targetUnit);
        if (targetObject == null)
        {
            return;
        }

        StateManager state = targetObject.GetComponent<StateManager>();
        if (state == null)
        {
            state = targetObject.GetComponentInParent<StateManager>();
        }

        if (state != null)
        {
            state.ApplyHealth(healAmount);
        }
    }
}
