using UnityEngine;

[System.Serializable]
/// <summary>
/// 单条“运行时状态实例”。
/// 
/// 要区分它和 CharStatusDef：
/// - CharStatusDef 是配置资产，描述“这个状态应该是什么”
/// - CharStatusRt 是运行时实例，描述“这个状态现在正挂在谁身上，还剩多久，叠了几层”
/// </summary>
public class CharStatusRt
{
    // 运行时唯一 id，方便事件、调试和后续扩展定位某条状态。
    public int rtId;
    // 这条状态对应的配置资产。
    public CharStatusDef def;
    // 施加者。UniquePerCaster 之类的叠层规则会用到它。
    public GameObject applier;
    // 更宽泛的来源对象，可用于技能、子弹、陷阱等来源追踪。
    public Object src;
    // 剩余持续时间。
    public float remain;
    // 已经持续了多久。
    public float elapsed;
    // 当前层数。
    public int stack;
    // 强度值，用于“更强则替换”等规则。
    public float power;

    public bool IsExpired
    {
        get { return remain <= 0f; }
    }

    public bool MatchDef(CharStatusDef otherDef)
    {
        return def == otherDef;
    }

    public bool MatchCaster(GameObject caster)
    {
        return applier == caster;
    }

    /// <summary>
    /// 用一次状态请求初始化运行时状态实例。
    /// </summary>
    public void Init(int id, CharStatusApplyReq req)
    {
        rtId = id;
        def = req.def;
        applier = req.applier;
        src = req.src;
        remain = req.FinalDur;
        elapsed = 0f;
        stack = Mathf.Clamp(req.stackAdd <= 0 ? 1 : req.stackAdd, 1, def != null ? Mathf.Max(1, def.maxStack) : 1);
        power = req.power;
    }

    /// <summary>
    /// 每帧推进持续时间。
    /// </summary>
    public void Tick(float dt)
    {
        if (remain <= 0f)
        {
            return;
        }

        remain -= dt;
        elapsed += dt;
    }

    /// <summary>
    /// 刷新持续时间，并保留较高强度。
    /// </summary>
    public void Refresh(CharStatusApplyReq req)
    {
        remain = req.FinalDur;
        power = Mathf.Max(power, req.power);
    }

    /// <summary>
    /// 增加层数并刷新持续时间。
    /// </summary>
    public void AddStack(CharStatusApplyReq req)
    {
        if (def == null)
        {
            return;
        }

        int nextAdd = req.stackAdd <= 0 ? 1 : req.stackAdd;
        stack = Mathf.Clamp(stack + nextAdd, 1, Mathf.Max(1, def.maxStack));
        remain = req.FinalDur;
        power = Mathf.Max(power, req.power);
    }
}
