using UnityEngine;

public enum CharRelation
{
    None,
    Self,
    Ally,
    Enemy,
    Neutral,
}

public static class CharRelationResolver
{
    public static bool TryResolveUnit(GameObject obj, out GameObject unit)
    {
        unit = null;
        if (obj == null)
        {
            return false;
        }

        CharBlackBoard blackBoard = obj.GetComponentInParent<CharBlackBoard>();
        if (blackBoard != null)
        {
            unit = blackBoard.gameObject;
            return true;
        }

        Team team = obj.GetComponentInParent<Team>();
        if (team != null)
        {
            unit = team.gameObject;
            return true;
        }

        return false;
    }

    public static CharRelation GetRelation(GameObject observer, GameObject target)
    {
        GameObject observerUnit = NormalizeUnit(observer);
        GameObject targetUnit = NormalizeUnit(target);
        if (observerUnit == null || targetUnit == null)
        {
            return CharRelation.None;
        }

        if (observerUnit == targetUnit)
        {
            return CharRelation.Self;
        }

        bool hasObserverTeam = TryGetTeamInfo(observerUnit, out int observerTeamId, out TeamSide observerSide);
        bool hasTargetTeam = TryGetTeamInfo(targetUnit, out int targetTeamId, out TeamSide targetSide);
        if (!hasObserverTeam || !hasTargetTeam)
        {
            return CharRelation.None;
        }

        if (observerTeamId >= 0 && targetTeamId >= 0)
        {
            return observerTeamId == targetTeamId ? CharRelation.Ally : CharRelation.Enemy;
        }

        // Same side should never be treated as hostile, including Neutral camps.
        // If future gameplay needs multiple neutral camps, assign explicit teamId.
        if (observerSide == targetSide)
        {
            return CharRelation.Ally;
        }

        if (observerSide == TeamSide.Neutral || targetSide == TeamSide.Neutral)
        {
            return CharRelation.Neutral;
        }

        return CharRelation.Enemy;
    }

    public static bool IsEnemy(GameObject observer, GameObject target)
    {
        CharRelation relation = GetRelation(observer, target);
        return relation == CharRelation.Enemy || relation == CharRelation.Neutral;
    }

    public static bool IsAlly(GameObject observer, GameObject target)
    {
        return GetRelation(observer, target) == CharRelation.Ally;
    }

    public static bool IsAlive(GameObject unit)
    {
        GameObject targetUnit = NormalizeUnit(unit);
        if (targetUnit == null)
        {
            return false;
        }

        CharBlackBoard blackBoard = targetUnit.GetComponent<CharBlackBoard>();
        if (blackBoard == null)
        {
            return false;
        }

        if (blackBoard.Features.useResources && blackBoard.Resources.hasHealth)
        {
            return blackBoard.Resources.hp > 0f;
        }

        return !blackBoard.Action.isDead;
    }

    public static bool CanReceiveBasicAttack(GameObject attacker, GameObject target)
    {
        GameObject attackerUnit = NormalizeUnit(attacker);
        GameObject targetUnit = NormalizeUnit(target);
        if (attackerUnit == null || targetUnit == null)
        {
            return false;
        }

        return IsAlive(attackerUnit) && IsAlive(targetUnit) && IsEnemy(attackerUnit, targetUnit);
    }

    public static bool IsSkillTargetValid(GameObject caster, GameObject target, SkillTargetTeamRule teamRule)
    {
        GameObject casterUnit = NormalizeUnit(caster);
        GameObject targetUnit = NormalizeUnit(target);
        if (targetUnit == null || !IsAlive(targetUnit))
        {
            return false;
        }

        switch (teamRule)
        {
            case SkillTargetTeamRule.Any:
                return true;

            case SkillTargetTeamRule.Self:
                return casterUnit != null && casterUnit == targetUnit;

            case SkillTargetTeamRule.Enemy:
                return CanReceiveBasicAttack(casterUnit, targetUnit);

            case SkillTargetTeamRule.Ally:
                return casterUnit != null && IsAlly(casterUnit, targetUnit);

            default:
                return true;
        }
    }

    public static GameObject NormalizeUnit(GameObject obj)
    {
        TryResolveUnit(obj, out GameObject unit);
        return unit;
    }

    private static bool TryGetTeamInfo(GameObject unit, out int teamId, out TeamSide teamSide)
    {
        Team team = unit.GetComponent<Team>();
        if (team != null)
        {
            teamId = team.EffectiveTeamId;
            teamSide = team.side;
            return true;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            if (blackBoard.Identity.team != null)
            {
                teamId = blackBoard.Identity.team.EffectiveTeamId;
                teamSide = blackBoard.Identity.team.side;
                return true;
            }

            teamId = blackBoard.Identity.teamId;
            teamSide = blackBoard.Identity.teamSide;
            return teamId >= 0 || blackBoard.Identity.team != null || teamSide != TeamSide.Neutral;
        }

        teamId = -1;
        teamSide = TeamSide.Neutral;
        return false;
    }
}
