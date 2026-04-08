using UnityEngine;

public enum EBuffTarget
{
    Caster,
    Target
}

[CreateAssetMenu(fileName = "New Apply Buff Effect", menuName = "SkillSystem/Effects/ApplyBuff")]
public class ApplyBuffEffect : SkillEffect
{
    [Header("Status")]
    public EStatusType statusToApply;
    public EBuffTarget whoToApply = EBuffTarget.Target;

    [Header("Duration")]
    public float buffDuration = 10f;

    public override void Apply(CastContext context)
    {
        GameObject finalTarget = whoToApply == EBuffTarget.Caster
            ? context.caster
            : context.rawTarget.unit;

        finalTarget = CharRelationResolver.NormalizeUnit(finalTarget);
        if (finalTarget == null)
        {
            return;
        }

        CharStatusResolver.ApplyLegacyStatus(finalTarget, statusToApply, buffDuration, context.caster, this);
    }
}
