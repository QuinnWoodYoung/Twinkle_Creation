using System;

[Flags]
/// <summary>
/// 状态/战斗系统里使用的免疫类型位掩码。
/// 用来表示“这个单位对哪类控制或伤害免疫”。
/// </summary>
public enum CharImmuneType
{
    None = 0,
    Debuff = 1 << 0,
    Slow = 1 << 1,
    Root = 1 << 2,
    Silence = 1 << 3,
    ForcedMove = 1 << 4,
    ProjectileTrack = 1 << 5,
    MagicDmg = 1 << 6,
    PhysDmg = 1 << 7,
}
