using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能效果工具函数集合。
/// 这里放的是多个 Effect 都会复用的公共逻辑。
/// </summary>
public static class SkillEffectUtility
{
    /// <summary>
    /// 依次执行一个效果列表。
    /// 这是技能链串联的基础工具。
    /// </summary>
    public static void ExecuteEffects(IList<SkillEffect> effects, CastContext context)
    {
        if (effects == null || context == null)
        {
            return;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            SkillEffect effect = effects[i];
            if (effect != null)
            {
                effect.Apply(context);
            }
        }
    }

    /// <summary>
    /// 把施法者的 Team 复制给召唤物。
    /// 这样召唤物能自动沿用敌我判断规则。
    /// </summary>
    public static void CopyCasterTeam(GameObject caster, GameObject spawnedUnit)
    {
        if (caster == null || spawnedUnit == null)
        {
            return;
        }

        Team casterTeam = caster.GetComponentInParent<Team>();
        Team summonTeam = spawnedUnit.GetComponentInParent<Team>();

        if (casterTeam != null && summonTeam != null)
        {
            summonTeam.side = casterTeam.side;
            summonTeam.useExplicitTeamId = casterTeam.useExplicitTeamId;
            summonTeam.teamId = casterTeam.teamId;
        }

        CharBlackBoard blackBoard = spawnedUnit.GetComponentInParent<CharBlackBoard>();
        if (blackBoard != null)
        {
            blackBoard.SyncFromScene();
        }
    }
}
