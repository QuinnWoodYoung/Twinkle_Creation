using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 连锁闪电效果。
///
/// 它会从一个单位目标开始，依次在附近寻找下一个合法目标，
/// 每一跳都可以:
/// 1. 生成一段闪电链路 VFX。
/// 2. 在命中点播放命中特效。
/// 3. 对当前目标执行 effectsPerHit。
///
/// 这样你就可以直接做出一个可视化比较完整的“闪电链”技能，
/// 而不只是瞬间把多个单位同时扣血。
/// </summary>
[CreateAssetMenu(fileName = "Chain Lightning Effect", menuName = "SkillSystem/Effects/ChainLightning")]
public class ChainLightningEffect : SkillEffect
{
    [Header("Chain")]
    // 最多命中几个单位。
    [Min(1)] public int maxHits = 4;

    // 每一跳搜索下一名目标的半径。
    public float bounceRadius = 6f;

    // 相邻两跳之间的时间间隔。
    // 设为 0 时会瞬间结算完整条闪电链。
    [Min(0f)] public float bounceDelay = 0.08f;

    // 参与搜索的 Layer。
    public LayerMask targetLayers;

    // 连锁目标的敌我规则。
    public SkillTargetTeamRule targetTeamRule = SkillTargetTeamRule.Enemy;

    // 是否让初始目标也吃到一次 effectsPerHit。
    public bool includeInitialTarget = true;

    [Header("Bolt VFX")]
    // 是否在相邻两跳之间生成一条链路闪电。
    public bool spawnBoltVfx = true;

    // 链路闪电的离地高度。
    public float boltHeightOffset = 1.1f;

    // 链路闪电的宽度。
    [Min(0.01f)] public float boltWidth = 0.16f;

    // 链路闪电存在多久。
    [Min(0.01f)] public float boltLifetime = 0.12f;

    // 闪电折线分成多少段。
    [Min(2)] public int boltSegments = 6;

    // 中间段左右抖动幅度，让闪电不是一根死直线。
    [Min(0f)] public float boltJitter = 0.2f;

    // 链路闪电颜色。
    public Color boltColor = new Color(0.45f, 0.95f, 1f, 1f);

    // 可选材质；不填时会自动使用 Sprites/Default。
    public Material boltMaterial;

    [Header("Hit VFX")]
    // 每次命中时在目标位置播放的特效。
    public GameObject hitVfxPrefab;

    // 命中特效的自动销毁时间。
    [Min(0f)] public float hitVfxLifetime = 0.4f;

    [Header("Effects")]
    // 每命中一跳都要执行的效果。
    public List<SkillEffect> effectsPerHit = new List<SkillEffect>();

    private static Material _fallbackBoltMaterial;

    public override void Apply(CastContext context)
    {
        GameObject currentTarget = context.rawTarget.unit;
        if (currentTarget == null)
        {
            return;
        }

        CastContext snapshot = context.Snapshot();
        SkillEffectRuntime runtime = SkillEffectRuntime.Get(context.caster);

        // 只要有运行时容器，就走协程版本。
        // 这样即使 bounceDelay 很小，视觉上也会是一跳一跳地传播。
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
            // 不命中初始目标时，也必须把它加入 visited，
            // 否则第一跳可能仍然会选回初始目标自己。
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

    /// <summary>
    /// 从当前跳点附近寻找距离最近的下一个合法目标。
    /// </summary>
    private GameObject FindNextTarget(GameObject caster, GameObject currentTarget, HashSet<GameObject> visited)
    {
        if (currentTarget == null)
        {
            return null;
        }

        Collider[] hits = Physics.OverlapSphere(currentTarget.transform.position, bounceRadius, targetLayers);
        GameObject bestTarget = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            StateManager state = hits[i].GetComponent<StateManager>();
            if (state == null)
            {
                state = hits[i].GetComponentInParent<StateManager>();
            }

            if (state == null)
            {
                continue;
            }

            GameObject candidate = state.gameObject;
            if (candidate == null || visited.Contains(candidate) || candidate == caster)
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

    /// <summary>
    /// 对当前这一跳执行 VFX 和效果结算。
    /// </summary>
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
