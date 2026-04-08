using UnityEngine;

[System.Serializable]
public class CharStatusRt
{
    public int rtId;
    public CharStatusDef def;
    public GameObject applier;
    public Object src;
    public float remain;
    public float elapsed;
    public int stack;
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

    public void Tick(float dt)
    {
        if (remain <= 0f)
        {
            return;
        }

        remain -= dt;
        elapsed += dt;
    }

    public void Refresh(CharStatusApplyReq req)
    {
        remain = req.FinalDur;
        power = Mathf.Max(power, req.power);
    }

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
