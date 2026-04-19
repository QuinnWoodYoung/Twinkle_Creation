using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Sector Aoe", menuName = "SkillSystem/Effects/SectorAoe")]
public class SectorAoeEffect : SkillEffect
{
    [Header("Search")]
    [Min(0f)] public float radius = 5f;
    [Range(1f, 360f)] public float angle = 45f;
    public ContextPointSelector searchOrigin = ContextPointSelector.CasterPosition;
    public ContextDirectionSelector directionSource = ContextDirectionSelector.CurrentTargetDirection;
    public SkillTargetTeamRule targetTeamRule = SkillTargetTeamRule.Enemy;
    public bool includeCaster;

    [Header("Effects")]
    public List<SkillEffect> effectsToApply = new List<SkillEffect>();

    public override void Apply(CastContext context)
    {
        if (context == null || context.caster == null || radius <= 0f)
        {
            return;
        }

        Vector3 origin = CastContextResolver.ResolvePoint(context, searchOrigin);
        Vector3 forward = CastContextResolver.ResolveDirection(context, directionSource);
        Collider[] hits = Physics.OverlapSphere(origin, radius);
        HashSet<GameObject> processedTargets = new HashSet<GameObject>();
        float halfAngle = angle * 0.5f;

        for (int i = 0; i < hits.Length; i++)
        {
            if (!CharRelationResolver.TryResolveUnit(hits[i].gameObject, out GameObject targetObject))
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

            Vector3 toTarget = targetObject.transform.position - origin;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            if (Vector3.Angle(forward, toTarget.normalized) > halfAngle)
            {
                continue;
            }

            Vector3 direction = toTarget.normalized;
            TargetInfo subInfo = new TargetInfo(targetObject, targetObject.transform.position, direction);
            CastContext subContext = context.CreateChild(subInfo, false);
            subContext.UpdateHitPoint(targetObject.transform.position);
            SkillEffectUtility.ExecuteEffects(effectsToApply, subContext);
        }
    }
}
