using System;

[Flags]
public enum CharStateTag
{
    None = 0,
    Stun = 1 << 0,
    Silence = 1 << 1,
    Root = 1 << 2,
    Sleep = 1 << 3,
    Disarm = 1 << 4,
    Invis = 1 << 5,
    Reveal = 1 << 6,
    Invul = 1 << 7,
    Untarget = 1 << 8,
    MagicImmune = 1 << 9,
    Casting = 1 << 10,
    Atking = 1 << 11,
    ForcedMove = 1 << 12,
    HitReact = 1 << 13,
    Dead = 1 << 14,
}
