using System.Collections.Generic;
using UnityEngine;

public struct SkillPreviewResolvedSpec
{
    public CastContext displayContext;
    public SkillPreviewShape shape;
    public SkillPreviewAnchor anchor;
    public float radius;
    public float length;
    public float width;
    public float angle;
}

public static class SkillPreviewResolver
{
    private struct PreviewInference
    {
        public bool hasDisplayContext;
        public CastContext displayContext;
        public bool hasAreaRadius;
        public float areaRadius;
    }

    public static SkillPreviewResolvedSpec Resolve(SkillData skill, CastContext castContext)
    {
        SkillPreviewResolvedSpec spec = new SkillPreviewResolvedSpec
        {
            displayContext = castContext != null ? castContext.Snapshot() : null,
            angle = skill != null ? skill.previewAngle : 90f,
        };

        if (skill == null)
        {
            spec.shape = SkillPreviewShape.Circle;
            spec.anchor = SkillPreviewAnchor.TargetPoint;
            spec.radius = 1.5f;
            spec.length = 3f;
            spec.width = 1.5f;
            return spec;
        }

        PreviewInference inference = default;
        TraceEffects(skill.effects, castContext != null ? castContext.Snapshot() : null, ref inference);

        if (inference.hasDisplayContext && inference.displayContext != null)
        {
            spec.displayContext = inference.displayContext;
        }

        spec.shape = ResolveShape(skill, inference);
        spec.anchor = ResolveAnchor(skill);
        spec.radius = ResolveRadius(skill, inference);
        spec.length = ResolveLength(skill, spec.displayContext);
        spec.width = ResolveWidth(skill);
        spec.angle = Mathf.Clamp(skill.previewAngle, 1f, 360f);
        return spec;
    }

    private static SkillPreviewShape ResolveShape(SkillData skill, PreviewInference inference)
    {
        if (skill.previewShape != SkillPreviewShape.Auto)
        {
            return skill.previewShape;
        }

        if (inference.hasAreaRadius)
        {
            return SkillPreviewShape.Circle;
        }

        return skill.targetingMode == SkillTargetMode.Direction
            ? SkillPreviewShape.Rectangle
            : SkillPreviewShape.Circle;
    }

    private static SkillPreviewAnchor ResolveAnchor(SkillData skill)
    {
        if (skill.previewAnchor != SkillPreviewAnchor.Auto)
        {
            return skill.previewAnchor;
        }

        return skill.targetingMode == SkillTargetMode.Direction
            ? SkillPreviewAnchor.Caster
            : SkillPreviewAnchor.TargetPoint;
    }

    private static float ResolveRadius(SkillData skill, PreviewInference inference)
    {
        if (skill.previewRadius > 0f)
        {
            return skill.previewRadius;
        }

        if (inference.hasAreaRadius && inference.areaRadius > 0f)
        {
            return inference.areaRadius;
        }

        return 1.5f;
    }

    private static float ResolveLength(SkillData skill, CastContext displayContext)
    {
        if (skill.previewLength > 0f)
        {
            return skill.previewLength;
        }

        if (displayContext != null && displayContext.caster != null)
        {
            Vector3 offset = displayContext.rawTarget.position - displayContext.caster.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > 0.001f)
            {
                return offset.magnitude;
            }
        }

        if (skill.maxCastRange > 0f)
        {
            return skill.maxCastRange;
        }

        return 3f;
    }

    private static float ResolveWidth(SkillData skill)
    {
        if (skill.previewWidth > 0f)
        {
            return skill.previewWidth;
        }

        if (skill.previewRadius > 0f)
        {
            return skill.previewRadius * 2f;
        }

        return 1.5f;
    }

    private static bool TraceEffects(IList<SkillEffect> effects, CastContext context, ref PreviewInference inference)
    {
        if (effects == null || context == null)
        {
            return false;
        }

        CastContext workingContext = context.Snapshot();
        inference.displayContext = workingContext.Snapshot();
        inference.hasDisplayContext = true;

        for (int i = 0; i < effects.Count; i++)
        {
            SkillEffect effect = effects[i];
            if (effect == null)
            {
                continue;
            }

            if (effect is OffsetTargetEffect offsetEffect)
            {
                offsetEffect.Apply(workingContext);
                inference.displayContext = workingContext.Snapshot();
                inference.hasDisplayContext = true;
                continue;
            }

            if (effect is AoeEffect aoeEffect)
            {
                inference.displayContext = workingContext.Snapshot();
                inference.hasDisplayContext = true;
                inference.hasAreaRadius = aoeEffect.radius > 0f;
                inference.areaRadius = Mathf.Max(0f, aoeEffect.radius);
                return true;
            }

            if (effect is DelayEffect delayEffect)
            {
                if (TraceEffects(delayEffect.delayedEffects, workingContext.Snapshot(), ref inference))
                {
                    return true;
                }

                continue;
            }

            if (effect is RepeatEffect repeatEffect)
            {
                if (TraceEffects(repeatEffect.repeatedEffects, workingContext.Snapshot(), ref inference))
                {
                    return true;
                }

                continue;
            }

            if (effect is LaunchProjectileEffect projectileEffect)
            {
                Projectile projectile = projectileEffect.projectilePrefab != null
                    ? projectileEffect.projectilePrefab.GetComponent<Projectile>()
                    : null;
                if (projectile != null && TraceEffects(projectile.onHitEffects, workingContext.Snapshot(), ref inference))
                {
                    return true;
                }
            }
        }

        inference.displayContext = workingContext.Snapshot();
        inference.hasDisplayContext = true;
        return false;
    }
}
