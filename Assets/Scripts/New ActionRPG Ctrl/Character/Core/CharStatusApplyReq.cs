using UnityEngine;

[System.Serializable]
public struct CharStatusApplyReq
{
    public CharStatusDef def;
    public GameObject applier;
    public Object src;
    public float dur;
    public int stackAdd;
    public float power;
    public bool ignoreImmune;
    public CharImmuneType immuneMask;

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
    Reject,
    Add,
    Refresh,
    Replace,
}

[System.Serializable]
public struct CharStatusApplyRes
{
    public CharStatusApplyResType type;
    public CharStatusRt rt;
    public string reason;

    public bool ok
    {
        get { return type != CharStatusApplyResType.Reject; }
    }

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
