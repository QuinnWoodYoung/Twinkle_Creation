using UnityEngine;

public static class CharResourceResolver
{
    // Resource access prefers blackboard slices so gameplay readers can move
    // away from legacy StateManager one feature at a time.

    public static AttackData_SO GetAttackData(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return null;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        if (stateManager != null && stateManager.attackData != null)
        {
            return stateManager.attackData;
        }

        CharBlackBoardInitializer initializer = unit.GetComponent<CharBlackBoardInitializer>();
        return initializer != null ? initializer.AttackTemplate : null;
    }

    public static bool HasHealth(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return false;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            return blackBoard.Features.useResources && blackBoard.Resources.hasHealth;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        return stateManager != null;
    }

    public static float GetHitPoint(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return 0f;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            return blackBoard.Features.useResources && blackBoard.Resources.hasHealth
                ? blackBoard.Resources.hp
                : 0f;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        return stateManager != null ? stateManager.HitPoint : 0f;
    }

    public static float GetMaxHitPoint(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return 0f;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            return blackBoard.Features.useResources && blackBoard.Resources.hasHealth
                ? blackBoard.Resources.maxHp
                : 0f;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        return stateManager != null ? stateManager.MaxHitPoint : 0f;
    }

    public static float GetBasicAttackDamage(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return 0f;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            if (!blackBoard.Features.useCombat)
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

        StateManager stateManager = unit.GetComponent<StateManager>();
        if (stateManager != null)
        {
            return stateManager.GetBasicAttackDamage();
        }

        return 0f;
    }

    public static float GetAttackSpeed(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return 0f;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            if (!blackBoard.Features.useCombat)
            {
                return 0f;
            }

            float finalAttackSpeed = blackBoard.Combat.attackSpeed;
            if (blackBoard.Combat.attackSpeedMul > 0f)
            {
                finalAttackSpeed *= blackBoard.Combat.attackSpeedMul;
            }

            return Mathf.Max(finalAttackSpeed, 0f);
        }

        AttackData_SO attackData = GetAttackData(unit);
        return attackData != null
            ? Mathf.Max(attackData.attackSpeed, 0f)
            : 0f;
    }

    public static float GetAttackRange(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return 0f;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            return blackBoard.Features.useCombat
                ? Mathf.Max(blackBoard.Combat.attackRange, 0f)
                : 0f;
        }

        AttackData_SO attackData = GetAttackData(unit);
        return attackData != null
            ? Mathf.Max(attackData.attackRange, 0f)
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
        if (blackBoard != null)
        {
            return blackBoard.Features.useCombat
                ? Mathf.Max(blackBoard.Combat.maxAttackRange, 0f)
                : 0f;
        }

        AttackData_SO attackData = GetAttackData(unit);
        return attackData != null
            ? Mathf.Max(attackData.maxAttackRange, 0f)
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
        if (blackBoard != null)
        {
            return blackBoard.Features.useCombat
                ? Mathf.Max(blackBoard.Combat.attackCooldown, 0f)
                : 0f;
        }

        AttackData_SO attackData = GetAttackData(unit);
        return attackData != null
            ? Mathf.Max(attackData.coolDown, 0f)
            : 0f;
    }

    public static bool ApplyDamage(GameObject obj, float damageAmount)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return false;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        if (stateManager != null && stateManager.enabled)
        {
            // Prefer the legacy runtime owner while it still exists, because it
            // also drives hit react, break-on-damage, and legacy HP listeners.
            stateManager.TakeDamage(damageAmount);
            return true;
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

        blackBoard.Resources.hp = Mathf.Max(0f, blackBoard.Resources.hp - finalDamage);
        blackBoard.Action.isDead = blackBoard.Resources.hp <= 0f;
        blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Resources | CharBlackBoardChangeMask.Action);
        return true;
    }

    public static bool ApplyHeal(GameObject obj, float healAmount)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return false;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        if (stateManager != null && stateManager.enabled)
        {
            stateManager.ApplyHealth(Mathf.RoundToInt(healAmount));
            return true;
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
        return true;
    }
}
