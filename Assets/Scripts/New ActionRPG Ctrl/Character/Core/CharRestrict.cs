using System;

[Flags]
public enum CharRestrict
{
    None = 0,
    Move = 1 << 0,
    Rotate = 1 << 1,
    Atk = 1 << 2,
    CastSkill = 1 << 3,
    CastItem = 1 << 4,
    Channel = 1 << 5,
    BeSelect = 1 << 6,
    BeUnitTarget = 1 << 7,
    BeAtkTarget = 1 << 8,
}
