using UnityEngine;

[CreateAssetMenu(fileName = "Heal Effect", menuName = "SkillSystem/Effects/Heal")]
public class HealEffect : SkillEffect
{
    public ContextUnitSelector targetUnit = ContextUnitSelector.CurrentTarget;
    public int healAmount = 50;

    public override void Apply(CastContext context)
    {
        GameObject targetObject = CastContextResolver.ResolveUnit(context, targetUnit);
        targetObject = CharRelationResolver.NormalizeUnit(targetObject);
        if (targetObject == null)
        {
            return;
        }

        CharResourceResolver.ApplyHeal(targetObject, healAmount);
    }
}
