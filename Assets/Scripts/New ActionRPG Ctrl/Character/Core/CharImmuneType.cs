using System;

[Flags]
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
