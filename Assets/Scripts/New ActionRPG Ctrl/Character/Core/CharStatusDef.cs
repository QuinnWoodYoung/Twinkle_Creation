using UnityEngine;

public enum CharStatusPolarity
{
    Neutral,
    Buff,
    Debuff,
}

public enum CharStatusStackMode
{
    Multi,
    RefreshDur,
    AddStackRefresh,
    ReplaceIfStronger,
    UniquePerCaster,
}

public enum CharDispelType
{
    None,
    Basic,
    Strong,
}

public enum CharStatusVfxMount
{
    Root,
    Body,
    Head,
    Feet,
}

public enum CharStatusVfxRefreshMode
{
    Keep,
    ReplayEnter,
    RestartLoop,
    RestartAll,
}

[System.Serializable]
public struct CharStatMod
{
    public float moveSpdMul;
    public float moveSpdAdd;
    public float atkSpdMul;
    public float castSpdMul;
    public float turnSpdMul;
    public float dmgTakenMul;

    public static CharStatMod Default
    {
        get
        {
            return new CharStatMod
            {
                moveSpdMul = 1f,
                atkSpdMul = 1f,
                castSpdMul = 1f,
                turnSpdMul = 1f,
                dmgTakenMul = 1f,
            };
        }
    }
}

[CreateAssetMenu(fileName = "CharStatusDef", menuName = "Char/Status Def")]
public class CharStatusDef : ScriptableObject
{
    public string statusId;
    public CharStatusPolarity polarity = CharStatusPolarity.Debuff;
    [Min(0f)] public float baseDur = 1f;
    [Min(1)] public int maxStack = 1;
    public CharStatusStackMode stackMode = CharStatusStackMode.RefreshDur;
    public CharDispelType dispelType = CharDispelType.Basic;
    public string exclGroup;
    public int priority;
    public bool breakOnDmg;

    [Header("State")]
    public CharStateTag tags = CharStateTag.None;
    public CharRestrict restricts = CharRestrict.None;
    public CharImmuneType immunes = CharImmuneType.None;

    [Header("Stat Mod")]
    public CharStatMod mod = new CharStatMod
    {
        moveSpdMul = 1f,
        atkSpdMul = 1f,
        castSpdMul = 1f,
        turnSpdMul = 1f,
        dmgTakenMul = 1f,
    };

    [Header("Status VFX")]
    [Tooltip("启用后，这个状态在进入/持续/结束时会自动播放对应特效。")]
    public bool useStatusVfx;
    [Tooltip("状态刚被施加时播放的一次性特效。")]
    public GameObject statusVfxOnAdd;
    [Tooltip("进入特效自动销毁时间。小于等于 0 表示不额外强制销毁。")]
    [Min(0f)] public float statusVfxOnAddLife = 2f;
    [Tooltip("状态持续期间存在的循环特效。")]
    public GameObject statusVfxLoop;
    [Tooltip("状态结束时播放的一次性特效。")]
    public GameObject statusVfxOnRemove;
    [Tooltip("结束特效自动销毁时间。小于等于 0 表示不额外强制销毁。")]
    [Min(0f)] public float statusVfxOnRemoveLife = 2f;
    [Tooltip("状态特效默认挂载到角色哪个部位。")]
    public CharStatusVfxMount statusVfxMount = CharStatusVfxMount.Body;
    [Tooltip("为 true 时，循环特效会跟随挂点移动。")]
    public bool statusVfxFollow = true;
    [Tooltip("状态刷新时，特效如何处理。")]
    public CharStatusVfxRefreshMode statusVfxRefresh = CharStatusVfxRefreshMode.Keep;
    [Tooltip("状态特效的局部位置偏移。")]
    public Vector3 statusVfxOffset;
    [Tooltip("状态特效的局部旋转偏移。")]
    public Vector3 statusVfxEuler;
}
