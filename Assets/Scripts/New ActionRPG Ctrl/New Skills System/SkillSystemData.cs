using UnityEngine;

/// <summary>
/// 鎶€鑳界郴缁熷簳灞傛暟鎹畾涔夈€?/// 瀹冩弿杩颁竴娆℃柦娉曡繃绋嬩腑鈥滅洰鏍囨槸浠€涔堛€佸綋鍓嶇粨绠楀埌鍝噷銆佸浣曚粠涓婁笅鏂囧彇鏁版嵁鈥濄€?/// </summary>
[System.Serializable]
public struct TargetInfo
{
    public GameObject unit;    // 鐩爣鍗曚綅
    public Vector3 position;   // 鍧愭爣
    public Vector3 direction;  // 鏂瑰悜

    public bool HasUnit => unit != null;

    public TargetInfo(GameObject unit, Vector3 position, Vector3 direction = default)
    {
        this.unit = unit;
        this.position = position;
        this.direction = direction;
    }
}

/// <summary>
/// 鎶€鑳藉厑璁哥殑鏂芥硶鐩爣绫诲瀷銆?/// </summary>
public enum SkillTargetMode
{
    Point,
    Unit,
    UnitOrPoint,
    Direction,
    NoTarget
}

/// <summary>
/// 鎶€鑳藉厑璁稿懡涓殑闃佃惀绫诲瀷銆?/// </summary>
public enum SkillTargetTeamRule
{
    Any,
    Enemy,
    Ally,
    Self
}

/// <summary>
/// 浠?CastContext 涓彇鍝釜鍗曚綅銆?/// </summary>
public enum ContextUnitSelector
{
    CurrentTarget,
    Caster,
    OriginalTarget
}

/// <summary>
/// 浠?CastContext 涓彇鍝釜鍧愭爣銆?/// </summary>
public enum ContextPointSelector
{
    HitPoint,
    CasterPosition,
    CurrentTargetPosition,
    OriginalTargetPosition
}

/// <summary>
/// 浠?CastContext 涓彇鍝釜鏂瑰悜銆?/// </summary>
public enum ContextDirectionSelector
{
    CasterForward,
    CurrentTargetDirection,
    OriginalTargetDirection
}

/// <summary>
/// 涓€娆℃柦娉曞湪杩愯鏃跺叡浜殑涓婁笅鏂囥€?/// originalTarget 璁板綍鏈€鍒濊緭鍏ワ紝rawTarget 璁板綍褰撳墠閾捐矾姝ｅ湪澶勭悊鐨勭洰鏍囷紝hitPoint 璁板綍鐪熷疄鍛戒腑鐐广€?/// </summary>
public class CastContext
{
    public readonly TargetInfo originalTarget;
    public GameObject caster;      // 鏂芥硶鑰?
    public TargetInfo rawTarget;   // 鍘熷鐩爣蹇収锛堟柦娉曢偅涓€鍒荤殑鎸囦护锛?
    public Vector3 hitPoint;       // 瀹炴椂鍛戒腑鐐癸紙鐢ㄤ簬寮归亾鎾炲嚮銆佺壒鏁堢敓鎴愮殑鍧愭爣锛?
    public SkillTargetTeamRule teamRule;

    public CastContext(GameObject caster, TargetInfo rawTarget)
    {
        this.caster = caster;
        this.originalTarget = rawTarget;
        this.rawTarget = rawTarget;
        this.teamRule = SkillTargetTeamRule.Any;
        // 鍒濆鐘舵€佷笅锛屽懡涓偣绛変簬鐩爣鐐?
        this.hitPoint = rawTarget.position;
    }

    private CastContext(GameObject caster, TargetInfo originalTarget, TargetInfo rawTarget, Vector3 hitPoint, SkillTargetTeamRule teamRule)
    {
        this.caster = caster;
        this.originalTarget = originalTarget;
        this.rawTarget = rawTarget;
        this.hitPoint = hitPoint;
        this.teamRule = teamRule;
    }

    // 寮归亾閫昏緫浼氬湪椋炶鐨勬瘡涓€甯ц皟鐢ㄨ繖涓紝鏇存柊鈥滄挒鍑讳綅缃€?
    /// <summary>
    /// 鏇存柊褰撳墠鏁堟灉閾剧殑鍛戒腑鐐广€?    /// </summary>
    public void UpdateHitPoint(Vector3 newPoint)
    {
        hitPoint = newPoint;
    }
    
    // 鍦?CastContext 绫讳腑娣诲姞
    /// <summary>
    /// 鍙慨鏀圭洰鏍囧潗鏍囷紝涓嶄慨鏀圭洰鏍囧崟浣嶅紩鐢ㄣ€?    /// 甯哥敤浜庤秴璺濇埅鏂€?    /// </summary>
    public void OverrideTargetPosition(Vector3 newPos)
    {
        // 閲嶆柊鏋勫缓 TargetInfo锛屼繚鐣欏師鏈夊崟浣嶅紩鐢紝浣嗘洿鏂板潗鏍?
        rawTarget = new TargetInfo(rawTarget.unit, newPos, BuildDirection(newPos, rawTarget.direction));
        hitPoint = newPos;
    }

    /// <summary>
    /// 瀹屾暣閲嶅畾鍚戝綋鍓嶇洰鏍囥€?    /// 甯哥敤浜庡墠鏂瑰亸绉荤垎鐐广€佹姇灏勭墿鍛戒腑閲嶅畾鍚戠瓑鍦烘櫙銆?    /// </summary>
    public void Retarget(GameObject newUnit, Vector3 newPos)
    {
        rawTarget = new TargetInfo(newUnit, newPos, BuildDirection(newPos, rawTarget.direction));
        hitPoint = newPos;
    }

    /// <summary>
    /// 鍒涘缓涓€涓瓙涓婁笅鏂囷紝渚?AOE/杩為攣寮硅烦绛変负姣忎釜鍛戒腑鍗曚綅鍗曠嫭缁撶畻銆?    /// </summary>
    public CastContext CreateChild(TargetInfo newTarget, bool inheritHitPoint = true)
    {
        Vector3 nextHitPoint = inheritHitPoint ? hitPoint : newTarget.position;
        return new CastContext(caster, originalTarget, newTarget, nextHitPoint, teamRule);
    }

    /// <summary>
    /// 鍒涘缓褰撳墠涓婁笅鏂囩殑闈欐€佸揩鐓э紝渚涘欢杩熷拰鍛ㄦ湡鏁堟灉浣跨敤銆?    /// </summary>
    public CastContext Snapshot()
    {
        return new CastContext(caster, originalTarget, rawTarget, hitPoint, teamRule);
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
/// 缁熶竴浠?CastContext 涓В鏋愬崟浣嶃€佸潗鏍囥€佹柟鍚戙€?/// 鍚勪釜 Effect 閮藉簲璇ヤ紭鍏堜娇鐢ㄨ繖浜涘伐鍏峰嚱鏁般€?/// </summary>
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
/// 鐩爣鍚堟硶鎬у垽瀹氳鍒欍€?/// 杩欓噷鍙仛闃佃惀鍜屽瓨娲绘鏌ワ紝涓嶅仛灏勭嚎妫€娴嬨€?/// </summary>
public static class SkillTargetingRules
{
    public static bool IsUnitTargetValid(GameObject caster, GameObject target, SkillTargetTeamRule teamRule)
    {
        return CharRelationResolver.IsSkillTargetValid(caster, target, teamRule);
    }

    public static bool AreEnemies(GameObject caster, GameObject target)
    {
        return CharRelationResolver.IsEnemy(caster, target);
    }
}


