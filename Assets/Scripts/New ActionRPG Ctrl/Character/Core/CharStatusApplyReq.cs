using UnityEngine;

[System.Serializable]
/// <summary>
/// 一次“施加状态”的请求描述。
/// 外部系统不直接创建 CharStatusRt，而是先构造这份请求交给 CharStatusCtrl。
/// </summary>
public struct CharStatusApplyReq
{
    // 要施加的状态定义。
    public CharStatusDef def;
    // 谁施加的。
    public GameObject applier;
    // 来源对象，可用于记录技能/子弹/脚本来源。
    public Object src;
    // 显式指定持续时间。<= 0 时回退到 def.baseDur。
    public float dur;
    // 这次要增加几层。
    public int stackAdd;
    // 这次施加的强度。
    public float power;
    // 是否无视免疫直接上状态。
    public bool ignoreImmune;
    // 额外免疫掩码检查。
    public CharImmuneType immuneMask;

    /// <summary>
    /// 解析这次请求真正使用的持续时间。
    /// </summary>
    public float FinalDur
    {
        get
        {
            if (def == null)
            {
                return 0f;
            }

            return dur > 0f ? dur : def.baseDur;
        }
    }
}

public enum CharStatusApplyResType
{
    // 被免疫、互斥组、强度不足等规则拒绝。
    Reject,
    // 新增一条运行时状态。
    Add,
    // 刷新已有状态。
    Refresh,
    // 用更强的新状态替换旧状态。
    Replace,
}

[System.Serializable]
/// <summary>
/// 状态施加结果。
/// 用于告诉调用方：这次到底是加上了、刷新了、替换了，还是被拒绝了。
/// </summary>
public struct CharStatusApplyRes
{
    public CharStatusApplyResType type;
    public CharStatusRt rt;
    public string reason;

    public bool ok
    {
        get { return type != CharStatusApplyResType.Reject; }
    }

    /// <summary>
    /// 统一构造结果对象。
    /// </summary>
    public static CharStatusApplyRes Make(CharStatusApplyResType type, CharStatusRt rt, string reason = "")
    {
        return new CharStatusApplyRes
        {
            type = type,
            rt = rt,
            reason = reason,
        };
    }
}
