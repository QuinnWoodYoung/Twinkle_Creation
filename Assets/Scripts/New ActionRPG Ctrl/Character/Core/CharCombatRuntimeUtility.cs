using UnityEngine;

public static class CharCombatRuntimeUtility
{
    public static AttackData_SO AssignAttackData(
        CharBlackBoard blackBoard,
        AttackData_SO attackSource,
        bool createRuntimeInstance)
    {
        if (blackBoard == null)
        {
            return null;
        }

        ReleaseOwnedAttackData(blackBoard);

        AttackData_SO assignedAttackData = attackSource;
        bool ownsAttackDataInstance = false;
        if (createRuntimeInstance && attackSource != null)
        {
            assignedAttackData = Object.Instantiate(attackSource);
            ownsAttackDataInstance = true;
        }

        blackBoard.Combat.attackData = assignedAttackData;
        blackBoard.Combat.ownsAttackDataInstance = ownsAttackDataInstance;
        return assignedAttackData;
    }

    public static void ApplyAttackStats(CharCombatSlice combat, AttackData_SO attackData)
    {
        if (combat == null)
        {
            return;
        }

        if (attackData == null)
        {
            ClearAttackStats(combat);
            return;
        }

        combat.attackPower = attackData.minDamage;
        combat.criticalAttackPower = attackData.maxDamage;
        combat.rangedAttackSpeed = attackData.rangedAttackSpeed;
        combat.attackRange = attackData.attackRange;
        combat.maxAttackRange = attackData.maxAttackRange;
        combat.attackCooldown = attackData.coolDown;
    }

    public static void ClearAttackData(CharBlackBoard blackBoard)
    {
        if (blackBoard == null)
        {
            return;
        }

        ReleaseOwnedAttackData(blackBoard);
        blackBoard.Combat.attackData = null;
        blackBoard.Combat.ownsAttackDataInstance = false;
        ClearAttackStats(blackBoard.Combat);
    }

    public static void ReleaseOwnedAttackData(CharBlackBoard blackBoard)
    {
        if (blackBoard == null ||
            !blackBoard.Combat.ownsAttackDataInstance ||
            blackBoard.Combat.attackData == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(blackBoard.Combat.attackData);
        }
        else
        {
            Object.DestroyImmediate(blackBoard.Combat.attackData);
        }

        blackBoard.Combat.attackData = null;
        blackBoard.Combat.ownsAttackDataInstance = false;
    }

    public static void ClearAttackStats(CharCombatSlice combat)
    {
        if (combat == null)
        {
            return;
        }

        combat.attackPower = 0f;
        combat.criticalAttackPower = 0f;
        combat.rangedAttackSpeed = 0f;
        combat.attackRange = 0f;
        combat.maxAttackRange = 0f;
        combat.attackCooldown = 0f;
    }
}
