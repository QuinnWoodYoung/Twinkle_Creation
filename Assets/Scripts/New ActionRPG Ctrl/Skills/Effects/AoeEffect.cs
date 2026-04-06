using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 范围搜索效果。
///
/// 它会在某个圆形区域内找到所有合法单位，然后把 effectsToApply
/// 逐个作用到这些单位身上。
///
/// 所以它本质上不是“伤害效果”，而是一个“范围分发器”。
/// 你可以把 Damage / Buff / VFX 再挂在它内部，做出真正的 AoE 技能。
/// </summary>
[CreateAssetMenu(fileName = "New AoE Finder", menuName = "SkillSystem/Effects/AoeFinder")]
public class AoeEffect : SkillEffect
{
    [Header("Search")]
    // 搜索半径。
    public float radius = 5f;

    // 参与物理搜索的 Layer。
    // 在 MOBA 里通常是 Unit，而不是拿来区分敌我。
    public LayerMask targetLayers;

    // 搜索圆心从哪里取。
    // 最常见的是 HitPoint，也就是技能实际落点。
    public ContextPointSelector searchOrigin = ContextPointSelector.HitPoint;

    // 命中的敌我规则。
    // 这里真正用的是 Team 判断，不是 Layer 判断。
    public SkillTargetTeamRule targetTeamRule = SkillTargetTeamRule.Enemy;

    // 是否允许 AoE 命中施法者自己。
    public bool includeCaster;

    [Header("Effects")]
    // 对每个命中目标都要执行的一组效果。
    public List<SkillEffect> effectsToApply = new List<SkillEffect>();

    public override void Apply(CastContext context)
    {
        // 先确定范围中心。
        Vector3 origin = CastContextResolver.ResolvePoint(context, searchOrigin);

        // 在指定 Layer 内做球形重叠检测。
        Collider[] hits = Physics.OverlapSphere(origin, radius, targetLayers);

        // 一个角色可能挂了多个碰撞体，必须去重。
        HashSet<GameObject> processedTargets = new HashSet<GameObject>();

        foreach (Collider hit in hits)
        {
            // 命中的可能只是角色身上的某个子节点。
            StateManager state = hit.GetComponent<StateManager>();
            if (state == null)
            {
                state = hit.GetComponentInParent<StateManager>();
            }

            if (state == null)
            {
                continue;
            }

            GameObject targetObject = state.gameObject;
            if (!processedTargets.Add(targetObject))
            {
                continue;
            }

            if (targetObject == context.caster && !includeCaster)
            {
                continue;
            }

            // 用 Team 规则决定它是不是这次 AoE 的合法目标。
            if (targetTeamRule != SkillTargetTeamRule.Any &&
                !SkillTargetingRules.IsUnitTargetValid(context.caster, targetObject, targetTeamRule))
            {
                continue;
            }

            // 为当前命中单位构造新的朝向和子上下文。
            // 后续像 VFX、前向偏移、弹道生成就能正确使用这个局部信息。
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
