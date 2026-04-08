using UnityEngine;

/// <summary>
/// 阵营枚举。
/// 这里才是 MOBA 技能系统判断敌我关系的核心，不是 Layer。
/// </summary>
public enum TeamSide
{
    Radiant,
    Dire,
    Neutral
}

/// <summary>
/// 挂在单位身上的阵营信息组件。
///
/// 技能系统做敌我判断时，应该优先看 Team。
/// Layer 只负责“这是不是技能系统应该检测的对象”，
/// 不应该负责“它是敌人还是队友”。
/// </summary>
public class Team : MonoBehaviour
{
    // 当前单位属于哪个阵营。
    public TeamSide side;

    [Tooltip("启用后，使用下面的 teamId 作为精确阵营编号。")]
    public bool useExplicitTeamId;
    [Tooltip("可选的显式阵营编号。小于 0 时，默认使用 side 的枚举值。")]
    public int teamId = -1;

    public int EffectiveTeamId => useExplicitTeamId && teamId >= 0 ? teamId : (int)side;

    /// <summary>
    /// 判断另一个 Team 是否应视为敌人。
    /// 同 side 永远不是敌人；若未来存在多个中立阵营，请使用 teamId 区分。
    /// </summary>
    public bool IsEnemy(Team other)
    {
        if (other == null)
        {
            return true;
        }

        if (side == other.side)
        {
            return false;
        }

        if (EffectiveTeamId >= 0 && other.EffectiveTeamId >= 0)
        {
            return EffectiveTeamId != other.EffectiveTeamId;
        }

        return side == TeamSide.Neutral || other.side == TeamSide.Neutral || side != other.side;
    }
}
