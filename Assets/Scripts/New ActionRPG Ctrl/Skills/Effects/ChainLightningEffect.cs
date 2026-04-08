using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Chain Lightning Effect", menuName = "SkillSystem/Effects/ChainLightning")]
public class ChainLightningEffect : SkillEffect
{
    [Header("Chain")]
    [Min(1)] public int maxHits = 4;
    public float bounceRadius = 6f;
    [Min(0f)] public float bounceDelay = 0.08f;
    public SkillTargetTeamRule targetTeamRule = SkillTargetTeamRule.Enemy;
    public bool includeInitialTarget = true;

    [Header("Bolt VFX")]
    public bool spawnBoltVfx = true;
    public float boltHeightOffset = 1.1f;
    [Min(0.01f)] public float boltWidth = 0.16f;
    [Min(0.01f)] public float boltLifetime = 0.12f;
    [Min(2)] public int boltSegments = 6;
    [Min(0f)] public float boltJitter = 0.2f;
    public Color boltColor = new Color(0.45f, 0.95f, 1f, 1f);
    public Material boltMaterial;

    [Header("Hit VFX")]
    public GameObject hitVfxPrefab;
    [Min(0f)] public float hitVfxLifetime = 0.4f;

    [Header("Effects")]
    public List<SkillEffect> effectsPerHit = new List<SkillEffect>();

    private static Material _fallbackBoltMaterial;

    public override void Apply(CastContext context)
    {
        GameObject currentTarget = CharRelationResolver.NormalizeUnit(context.rawTarget.unit);
        if (currentTarget == null)
        {
            return;
        }

        context.Retarget(currentTarget, currentTarget.transform.position);
        CastContext snapshot = context.Snapshot();
        SkillEffectRuntime runtime = SkillEffectRuntime.Get(context.caster);
        if (runtime != null)
        {
            runtime.Run(ChainRoutine(snapshot));
            return;
        }

        ExecuteChainImmediate(snapshot);
    }

    private IEnumerator ChainRoutine(CastContext context)
    {
        GameObject currentTarget = context.rawTarget.unit;
        HashSet<GameObject> visited = new HashSet<GameObject>();
        int hitsApplied = 0;
        Vector3 sourcePosition = GetInitialSourcePosition(context);

        if (includeInitialTarget)
        {
            ApplyToTarget(context, currentTarget, currentTarget.transform.position, sourcePosition);
            visited.Add(currentTarget);
            hitsApplied++;
            sourcePosition = currentTarget.transform.position;

            if (hitsApplied < maxHits && bounceDelay > 0f)
            {
                yield return new WaitForSeconds(bounceDelay);
            }
        }
        else
        {
            visited.Add(currentTarget);
            sourcePosition = currentTarget.transform.position;
        }

        while (hitsApplied < maxHits && currentTarget != null)
        {
            GameObject nextTarget = FindNextTarget(context.caster, currentTarget, visited);
            if (nextTarget == null)
            {
                yield break;
            }

            visited.Add(nextTarget);
            ApplyToTarget(context, nextTarget, nextTarget.transform.position, sourcePosition);

            currentTarget = nextTarget;
            sourcePosition = nextTarget.transform.position;
            hitsApplied++;

            if (hitsApplied < maxHits && bounceDelay > 0f)
            {
                yield return new WaitForSeconds(bounceDelay);
            }
        }
    }

    private void ExecuteChainImmediate(CastContext context)
    {
        GameObject currentTarget = context.rawTarget.unit;
        HashSet<GameObject> visited = new HashSet<GameObject>();
        int hitsApplied = 0;
        Vector3 sourcePosition = GetInitialSourcePosition(context);

        if (includeInitialTarget)
        {
            ApplyToTarget(context, currentTarget, currentTarget.transform.position, sourcePosition);
            visited.Add(currentTarget);
            hitsApplied++;
            sourcePosition = currentTarget.transform.position;
        }
        else
        {
            visited.Add(currentTarget);
            sourcePosition = currentTarget.transform.position;
        }

        while (hitsApplied < maxHits && currentTarget != null)
        {
            GameObject nextTarget = FindNextTarget(context.caster, currentTarget, visited);
            if (nextTarget == null)
            {
                break;
            }

            visited.Add(nextTarget);
            ApplyToTarget(context, nextTarget, nextTarget.transform.position, sourcePosition);
            currentTarget = nextTarget;
            sourcePosition = nextTarget.transform.position;
            hitsApplied++;
        }
    }

    private GameObject FindNextTarget(GameObject caster, GameObject currentTarget, HashSet<GameObject> visited)
    {
        if (currentTarget == null)
        {
            return null;
        }

        Collider[] hits = Physics.OverlapSphere(currentTarget.transform.position, bounceRadius);
        GameObject bestTarget = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            if (!CharRelationResolver.TryResolveUnit(hits[i].gameObject, out GameObject candidate) ||
                visited.Contains(candidate) ||
                candidate == caster)
            {
                continue;
            }

            if (!SkillTargetingRules.IsUnitTargetValid(caster, candidate, targetTeamRule))
            {
                continue;
            }

            float dist = Vector3.Distance(currentTarget.transform.position, candidate.transform.position);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestTarget = candidate;
            }
        }

        return bestTarget;
    }

    private void ApplyToTarget(CastContext context, GameObject target, Vector3 targetPosition, Vector3 sourcePosition)
    {
        if (target == null)
        {
            return;
        }

        if (spawnBoltVfx)
        {
            SpawnBoltVfx(sourcePosition, targetPosition);
        }

        SpawnHitVfx(targetPosition);

        Vector3 direction = targetPosition - sourcePosition;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.001f ? direction.normalized : context.rawTarget.direction;

        TargetInfo targetInfo = new TargetInfo(target, targetPosition, direction);
        CastContext childContext = context.CreateChild(targetInfo, false);
        childContext.UpdateHitPoint(targetPosition);
        SkillEffectUtility.ExecuteEffects(effectsPerHit, childContext);
    }

    private Vector3 GetInitialSourcePosition(CastContext context)
    {
        if (context != null && context.caster != null)
        {
            return context.caster.transform.position;
        }

        return context != null ? context.rawTarget.position : Vector3.zero;
    }

    private void SpawnHitVfx(Vector3 targetPosition)
    {
        if (hitVfxPrefab == null)
        {
            return;
        }

        GameObject instance = Object.Instantiate(
            hitVfxPrefab,
            targetPosition + Vector3.up * boltHeightOffset,
            Quaternion.identity);

        if (hitVfxLifetime > 0f)
        {
            Object.Destroy(instance, hitVfxLifetime);
        }
    }

    private void SpawnBoltVfx(Vector3 startPosition, Vector3 endPosition)
    {
        Vector3 start = startPosition + Vector3.up * boltHeightOffset;
        Vector3 end = endPosition + Vector3.up * boltHeightOffset;

        GameObject boltObject = new GameObject("ChainLightningBolt");
        LineRenderer line = boltObject.AddComponent<LineRenderer>();

        line.useWorldSpace = true;
        line.positionCount = Mathf.Max(2, boltSegments);
        line.startWidth = boltWidth;
        line.endWidth = boltWidth * 0.85f;
        line.startColor = boltColor;
        line.endColor = boltColor;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.textureMode = LineTextureMode.Stretch;
        line.material = ResolveBoltMaterial();

        Vector3 pathDirection = end - start;
        Vector3 side = Vector3.Cross(pathDirection.normalized, Vector3.up);
        if (side.sqrMagnitude < 0.001f)
        {
            side = Vector3.right;
        }
        side.Normalize();

        for (int i = 0; i < line.positionCount; i++)
        {
            float t = line.positionCount == 1 ? 1f : (float)i / (line.positionCount - 1);
            Vector3 point = Vector3.Lerp(start, end, t);

            if (i > 0 && i < line.positionCount - 1 && boltJitter > 0f)
            {
                point += side * Random.Range(-boltJitter, boltJitter);
                point += Vector3.up * Random.Range(-boltJitter * 0.2f, boltJitter * 0.2f);
            }

            line.SetPosition(i, point);
        }

        Object.Destroy(boltObject, boltLifetime);
    }

    private Material ResolveBoltMaterial()
    {
        if (boltMaterial != null)
        {
            return boltMaterial;
        }

        if (_fallbackBoltMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _fallbackBoltMaterial = new Material(shader);
            }
        }

        return _fallbackBoltMaterial;
    }
}
