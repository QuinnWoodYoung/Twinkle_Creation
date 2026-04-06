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

    /// <summary>
    /// 判断另一个 Team 是否应视为敌人。
    /// 当前实现里 Neutral 被视为敌对目标。
    /// </summary>
    public bool IsEnemy(Team other)
    {
        if (other == null || other.side == TeamSide.Neutral)
        {
            return true;
        }

        return side != other.side;
    }
}
