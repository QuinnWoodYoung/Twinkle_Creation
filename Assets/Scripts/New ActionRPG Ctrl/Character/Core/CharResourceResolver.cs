using UnityEngine;

public static class CharResourceResolver
{
    // Resource access is now blackboard-owned. Legacy StateManager may still
    // observe changes, but it is no longer the source of truth.

    public static AttackData_SO GetAttackData(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return null;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard == null || !blackBoard.Features.useCombat)
        {
            return null;
        }

        return blackBoard.Combat.attackData;
    }

    public static bool HasHealth(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return false;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        return blackBoard != null &&
            blackBoard.Features.useResources &&
            blackBoard.Resources.hasHealth;
    }

    public static float GetHitPoint(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return 0f;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        return blackBoard != null &&
            blackBoard.Features.useResources &&
            blackBoard.Resources.hasHealth
                ? blackBoard.Resources.hp
                : 0f;
    }

    public static float GetMaxHitPoint(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return 0f;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        return blackBoard != null &&
            blackBoard.Features.useResources &&
            blackBoard.Resources.hasHealth
                ? blackBoard.Resources.maxHp
                : 0f;
    }

    public static bool HasEnergy(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return false;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        return blackBoard != null &&
            blackBoard.Features.useResources &&
            blackBoard.Resources.hasEnergy;
    }

    public static float GetEnergy(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return 0f;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        return blackBoard != null &&
            blackBoard.Features.useResources &&
            blackBoard.Resources.hasEnergy
                ? blackBoard.Resources.energy
                : 0f;
    }

    public static float GetMaxEnergy(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return 0f;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        return blackBoard != null &&
            blackBoard.Features.useResources &&
            blackBoard.Resources.hasEnergy
                ? blackBoard.Resources.maxEnergy
                : 0f;
    }

    public static bool HasEnoughEnergy(GameObject obj, float energyCost)
    {
        if (energyCost <= 0f)
        {
            return true;
        }

        if (!HasEnergy(obj))
        {
            return false;
        }

        return GetEnergy(obj) >= energyCost;
    }

    public static bool TrySpendEnergy(GameObject obj, float energyCost)
    {
        if (energyCost <= 0f)
        {
            return true;
        }

        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return false;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard == null ||
            !blackBoard.Features.useResources ||
            !blackBoard.Resources.hasEnergy ||
            blackBoard.Resources.energy < energyCost)
        {
            return false;
        }

        blackBoard.Resources.energy = Mathf.Max(0f, blackBoard.Resources.energy - Mathf.Max(0f, energyCost));
        blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Resources);
        return true;
    }

    public static float GetBasicAttackDamage(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return 0f;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard == null || !blackBoard.Features.useCombat)
        {
            return 0f;
        }

        float finalDamage = blackBoard.Combat.attackPower;
        if (blackBoard.Combat.isCritical && blackBoard.Combat.criticalAttackPower > 0f)
        {
            finalDamage = blackBoard.Combat.criticalAttackPower;
        }

        return Mathf.Max(finalDamage, 0f);
    }

    public static float GetRangedAttackSpeed(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return 0f;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard == null || !blackBoard.Features.useCombat)
        {
            return 0f;
        }

        float finalAttackSpeed = blackBoard.Combat.rangedAttackSpeed;
        if (blackBoard.Combat.attackSpeedMul > 0f)
        {
            finalAttackSpeed *= blackBoard.Combat.attackSpeedMul;
        }

        return Mathf.Max(finalAttackSpeed, 0f);
    }

    public static float GetAttackSpeed(GameObject obj)
    {
        return GetRangedAttackSpeed(obj);
    }

    public static float GetAttackRange(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return 0f;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        return blackBoard != null && blackBoard.Features.useCombat
            ? Mathf.Max(blackBoard.Combat.attackRange, 0f)
            : 0f;
    }

    public static float GetMaxAttackRange(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return 0f;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        return blackBoard != null && blackBoard.Features.useCombat
            ? Mathf.Max(blackBoard.Combat.maxAttackRange, 0f)
            : 0f;
    }

    public static float GetAttackCooldown(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return 0f;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        return blackBoard != null && blackBoard.Features.useCombat
            ? Mathf.Max(blackBoard.Combat.attackCooldown, 0f)
            : 0f;
    }

    public static bool ApplyDamage(GameObject obj, float damageAmount)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return false;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard == null)
        {
            return false;
        }

        if (!blackBoard.Features.useResources || !blackBoard.Resources.hasHealth)
        {
            return false;
        }

        if (CharRuntimeResolver.IsDamageImmune(unit))
        {
            return false;
        }

        float finalDamage = Mathf.Max(0f, damageAmount);
        if (finalDamage <= 0f)
        {
            return false;
        }

        finalDamage *= ResolveDamageTakenMultiplier(blackBoard);
        if (finalDamage <= 0f)
        {
            return false;
        }

        blackBoard.Resources.hp = Mathf.Max(0f, blackBoard.Resources.hp - finalDamage);
        blackBoard.Action.isDead = blackBoard.Resources.hp <= 0f;
        blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Resources | CharBlackBoardChangeMask.Action);

        StateManager stateManager = unit.GetComponent<StateManager>();
        if (stateManager != null && stateManager.enabled)
        {
            stateManager.NotifyBlackboardDamageApplied(finalDamage);
        }

        return true;
    }

    public static bool ApplyHeal(GameObject obj, float healAmount)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return false;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard == null)
        {
            return false;
        }

        if (!blackBoard.Features.useResources || !blackBoard.Resources.hasHealth)
        {
            return false;
        }

        float finalHeal = Mathf.Max(0f, healAmount);
        if (finalHeal <= 0f)
        {
            return false;
        }

        blackBoard.Resources.hp = Mathf.Min(blackBoard.Resources.hp + finalHeal, blackBoard.Resources.maxHp);
        blackBoard.Action.isDead = blackBoard.Resources.hp <= 0f;
        blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Resources | CharBlackBoardChangeMask.Action);

        StateManager stateManager = unit.GetComponent<StateManager>();
        if (stateManager != null && stateManager.enabled)
        {
            stateManager.NotifyBlackboardHealthChanged();
        }

        return true;
    }

    private static float ResolveDamageTakenMultiplier(CharBlackBoard blackBoard)
    {
        if (blackBoard == null)
        {
            return 1f;
        }

        if (blackBoard.Features.useCombat && blackBoard.Combat.damageTakenMul > 0f)
        {
            return blackBoard.Combat.damageTakenMul;
        }

        if (blackBoard.Features.useStatus && blackBoard.Status.snapshot != null)
        {
            return Mathf.Max(0f, blackBoard.Status.snapshot.dmgTakenMul);
        }

        return 1f;
    }
}
