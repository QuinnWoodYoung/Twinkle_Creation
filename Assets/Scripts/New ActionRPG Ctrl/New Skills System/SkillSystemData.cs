using UnityEngine;

/// <summary>
/// 技能系统底层数据定义。
/// 它描述一次施法过程中“目标是什么、当前结算到哪里、如何从上下文取数据”。
/// </summary>
[System.Serializable]
public struct TargetInfo
{
    public GameObject unit;    // 目标单位
    public Vector3 position;   // 坐标
    public Vector3 direction;  // 方向

    public bool HasUnit => unit != null;

    public TargetInfo(GameObject unit, Vector3 position, Vector3 direction = default)
    {
        this.unit = unit;
        this.position = position;
        this.direction = direction;
    }
}

/// <summary>
/// 技能允许的施法目标类型。
/// </summary>
public enum SkillTargetMode
{
    Point,
    Unit,
    UnitOrPoint,
    Direction,
    NoTarget
}

/// <summary>
/// 技能允许命中的阵营类型。
/// </summary>
public enum SkillTargetTeamRule
{
    Any,
    Enemy,
    Ally,
    Self
}

/// <summary>
/// 从 CastContext 中取哪个单位。
/// </summary>
public enum ContextUnitSelector
{
    CurrentTarget,
    Caster,
    OriginalTarget
}

/// <summary>
/// 从 CastContext 中取哪个坐标。
/// </summary>
public enum ContextPointSelector
{
    HitPoint,
    CasterPosition,
    CurrentTargetPosition,
    OriginalTargetPosition
}

/// <summary>
/// 从 CastContext 中取哪个方向。
/// </summary>
public enum ContextDirectionSelector
{
    CasterForward,
    CurrentTargetDirection,
    OriginalTargetDirection
}

/// <summary>
/// 一次施法在运行时共享的上下文。
/// originalTarget 记录最初输入，rawTarget 记录当前链路正在处理的目标，hitPoint 记录真实命中点。
/// </summary>
public class CastContext
{
    public readonly TargetInfo originalTarget;
    public GameObject caster;      // 施法者
    public TargetInfo rawTarget;   // 原始目标快照（施法那一刻的指令）
    public Vector3 hitPoint;       // 实时命中点（用于弹道撞击、特效生成的坐标）

    public CastContext(GameObject caster, TargetInfo rawTarget)
    {
        this.caster = caster;
        this.originalTarget = rawTarget;
        this.rawTarget = rawTarget;
        // 初始状态下，命中点等于目标点
        this.hitPoint = rawTarget.position;
    }

    private CastContext(GameObject caster, TargetInfo originalTarget, TargetInfo rawTarget, Vector3 hitPoint)
    {
        this.caster = caster;
        this.originalTarget = originalTarget;
        this.rawTarget = rawTarget;
        this.hitPoint = hitPoint;
    }

    // 弹道逻辑会在飞行的每一帧调用这个，更新“撞击位置”
    /// <summary>
    /// 更新当前效果链的命中点。
    /// </summary>
    public void UpdateHitPoint(Vector3 newPoint)
    {
        hitPoint = newPoint;
    }
    
    // 在 CastContext 类中添加
    /// <summary>
    /// 只修改目标坐标，不修改目标单位引用。
    /// 常用于超距截断。
    /// </summary>
    public void OverrideTargetPosition(Vector3 newPos)
    {
        // 重新构建 TargetInfo，保留原有单位引用，但更新坐标
        rawTarget = new TargetInfo(rawTarget.unit, newPos, BuildDirection(newPos, rawTarget.direction));
        hitPoint = newPos;
    }

    /// <summary>
    /// 完整重定向当前目标。
    /// 常用于前方偏移爆点、投射物命中重定向等场景。
    /// </summary>
    public void Retarget(GameObject newUnit, Vector3 newPos)
    {
        rawTarget = new TargetInfo(newUnit, newPos, BuildDirection(newPos, rawTarget.direction));
        hitPoint = newPos;
    }

    /// <summary>
    /// 创建一个子上下文，供 AOE/连锁弹跳等为每个命中单位单独结算。
    /// </summary>
    public CastContext CreateChild(TargetInfo newTarget, bool inheritHitPoint = true)
    {
        Vector3 nextHitPoint = inheritHitPoint ? hitPoint : newTarget.position;
        return new CastContext(caster, originalTarget, newTarget, nextHitPoint);
    }

    /// <summary>
    /// 创建当前上下文的静态快照，供延迟和周期效果使用。
    /// </summary>
    public CastContext Snapshot()
    {
        return new CastContext(caster, originalTarget, rawTarget, hitPoint);
    }

    private Vector3 BuildDirection(Vector3 newPos, Vector3 fallback)
    {
        if (caster == null)
        {
            return fallback;
        }

        Vector3 dir = newPos - caster.transform.position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.001f ? dir.normalized : fallback;
    }
}

/// <summary>
/// 统一从 CastContext 中解析单位、坐标、方向。
/// 各个 Effect 都应该优先使用这些工具函数。
/// </summary>
public static class CastContextResolver
{
    public static GameObject ResolveUnit(CastContext context, ContextUnitSelector selector)
    {
        if (context == null)
        {
            return null;
        }

        switch (selector)
        {
            case ContextUnitSelector.Caster:
                return context.caster;
            case ContextUnitSelector.OriginalTarget:
                return context.originalTarget.unit;
            default:
                return context.rawTarget.unit;
        }
    }

    public static Vector3 ResolvePoint(CastContext context, ContextPointSelector selector)
    {
        if (context == null)
        {
            return Vector3.zero;
        }

        switch (selector)
        {
            case ContextPointSelector.CasterPosition:
                return context.caster != null ? context.caster.transform.position : Vector3.zero;
            case ContextPointSelector.CurrentTargetPosition:
                return context.rawTarget.position;
            case ContextPointSelector.OriginalTargetPosition:
                return context.originalTarget.position;
            default:
                return context.hitPoint;
        }
    }

    public static Vector3 ResolveDirection(CastContext context, ContextDirectionSelector selector)
    {
        if (context == null)
        {
            return Vector3.forward;
        }

        Vector3 direction;
        switch (selector)
        {
            case ContextDirectionSelector.CurrentTargetDirection:
                direction = context.rawTarget.direction;
                break;
            case ContextDirectionSelector.OriginalTargetDirection:
                direction = context.originalTarget.direction;
                break;
            default:
                direction = context.caster != null ? context.caster.transform.forward : Vector3.forward;
                break;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            return direction.normalized;
        }

        if (context.caster != null)
        {
            Vector3 casterForward = context.caster.transform.forward;
            casterForward.y = 0f;
            if (casterForward.sqrMagnitude > 0.001f)
            {
                return casterForward.normalized;
            }
        }

        return Vector3.forward;
    }

    public static Transform ResolveTransform(CastContext context, ContextUnitSelector selector)
    {
        GameObject unit = ResolveUnit(context, selector);
        return unit != null ? unit.transform : null;
    }
}

/// <summary>
/// 目标合法性判定规则。
/// 这里只做阵营和存活检查，不做射线检测。
/// </summary>
public static class SkillTargetingRules
{
    public static bool IsUnitTargetValid(GameObject caster, GameObject target, SkillTargetTeamRule teamRule)
    {
        if (target == null)
        {
            return false;
        }

        StateManager targetState = target.GetComponentInParent<StateManager>();
        if (targetState != null && targetState.isDead)
        {
            return false;
        }

        switch (teamRule)
        {
            case SkillTargetTeamRule.Any:
                return true;
            case SkillTargetTeamRule.Self:
                return caster != null && target == caster;
            case SkillTargetTeamRule.Enemy:
                return AreEnemies(caster, target);
            case SkillTargetTeamRule.Ally:
                return caster != null && target != caster && !AreEnemies(caster, target);
            default:
                return true;
        }
    }

    public static bool AreEnemies(GameObject caster, GameObject target)
    {
        if (caster == null || target == null || caster == target)
        {
            return false;
        }

        Team casterTeam = caster.GetComponentInParent<Team>();
        Team targetTeam = target.GetComponentInParent<Team>();

        if (casterTeam == null)
        {
            return true;
        }

        return casterTeam.IsEnemy(targetTeam);
    }
}
