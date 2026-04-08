using UnityEngine;

[CreateAssetMenu(menuName = "SkillSystem/Effects/Damage")]
public class DamageEffect : SkillEffect
{
    public ContextUnitSelector targetUnit = ContextUnitSelector.CurrentTarget;
    public float damageAmount = 50f;

    public override void Apply(CastContext context)
    {
        GameObject targetObject = CastContextResolver.ResolveUnit(context, targetUnit);
        targetObject = CharRelationResolver.NormalizeUnit(targetObject);
        if (targetObject == null)
        {
            return;
        }

        CharResourceResolver.ApplyDamage(targetObject, damageAmount);
    }
}
