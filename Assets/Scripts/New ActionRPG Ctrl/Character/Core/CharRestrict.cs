using System;

[Flags]
/// <summary>
/// 状态快照里的“行为限制位”。
/// 某条状态写入的限制最终会汇总到 CharStateSnap.restricts。
/// </summary>
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
