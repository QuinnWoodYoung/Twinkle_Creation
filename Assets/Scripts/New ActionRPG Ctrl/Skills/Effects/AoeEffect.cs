using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New AoE Finder", menuName = "SkillSystem/Effects/AoeFinder")]
public class AoeEffect : SkillEffect
{
    [Header("Search")]
    public float radius = 5f;
    public ContextPointSelector searchOrigin = ContextPointSelector.HitPoint;
    public SkillTargetTeamRule targetTeamRule = SkillTargetTeamRule.Enemy;
    public bool includeCaster;

    [Header("Effects")]
    public List<SkillEffect> effectsToApply = new List<SkillEffect>();

    public override void Apply(CastContext context)
    {
        Vector3 origin = CastContextResolver.ResolvePoint(context, searchOrigin);
        Collider[] hits = Physics.OverlapSphere(origin, radius);
        HashSet<GameObject> processedTargets = new HashSet<GameObject>();

        foreach (Collider hit in hits)
        {
            if (!CharRelationResolver.TryResolveUnit(hit.gameObject, out GameObject targetObject))
            {
                continue;
            }

            if (!processedTargets.Add(targetObject))
            {
                continue;
            }

            if (targetObject == context.caster && !includeCaster)
            {
                continue;
            }

            if (targetTeamRule != SkillTargetTeamRule.Any &&
                !SkillTargetingRules.IsUnitTargetValid(context.caster, targetObject, targetTeamRule))
            {
                continue;
            }

            Vector3 direction = targetObject.transform.position - origin;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.001f ? direction.normalized : context.rawTarget.direction;

            TargetInfo subInfo = new TargetInfo(targetObject, targetObject.transform.position, direction);
            CastContext subContext = context.CreateChild(subInfo, false);
            subContext.UpdateHitPoint(targetObject.transform.position);

            SkillEffectUtility.ExecuteEffects(effectsToApply, subContext);
        }
    }
}
