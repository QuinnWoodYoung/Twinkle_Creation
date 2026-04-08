using UnityEngine;

public static class CharRuntimeResolver
{
    // This helper centralizes blackboard-first runtime state reads so gameplay
    // code does not need to understand legacy component ownership.

    public static bool IsDead(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return false;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            if (blackBoard.Features.useResources && blackBoard.Resources.hasHealth)
            {
                return blackBoard.Resources.hp <= 0f;
            }

            return blackBoard.Action.isDead;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        return stateManager != null && stateManager.HitPoint <= 0f;
    }

    public static bool CanMove(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return true;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            bool canMove = blackBoard.Features.useStatus
                ? blackBoard.Status.snapshot.canMove
                : blackBoard.Motion.canMove;
            return canMove && !blackBoard.Action.isControlLocked && !IsDead(unit);
        }

        CharStatusCtrl statusCtrl = unit.GetComponent<CharStatusCtrl>();
        if (statusCtrl != null)
        {
            return statusCtrl.Snap.canMove;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        return stateManager == null || stateManager.CanMove;
    }

    public static bool CanRotate(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return true;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            return blackBoard.Features.useStatus
                ? blackBoard.Status.snapshot.canRotate
                : blackBoard.Motion.canRotate;
        }

        CharStatusCtrl statusCtrl = unit.GetComponent<CharStatusCtrl>();
        return statusCtrl == null || statusCtrl.Snap.canRotate;
    }

    public static bool CanCast(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return true;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            bool canCast = blackBoard.Features.useStatus
                ? blackBoard.Status.snapshot.canCast
                : true;
            return canCast && !blackBoard.Action.isControlLocked && !IsDead(unit);
        }

        CharStatusCtrl statusCtrl = unit.GetComponent<CharStatusCtrl>();
        if (statusCtrl != null)
        {
            return statusCtrl.Snap.canCast;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        return stateManager == null || stateManager.CanCastSkills;
    }

    public static bool CanAttack(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return true;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            bool canAttack = blackBoard.Features.useStatus
                ? blackBoard.Status.snapshot.canAtk
                : true;
            return canAttack && !blackBoard.Action.isControlLocked && !IsDead(unit);
        }

        CharStatusCtrl statusCtrl = unit.GetComponent<CharStatusCtrl>();
        if (statusCtrl != null)
        {
            return statusCtrl.Snap.canAtk;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        return stateManager == null || !stateManager.IsStunned;
    }

    public static bool IsDamageImmune(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return false;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            bool statusImmune =
                blackBoard.Features.useStatus &&
                (blackBoard.Status.snapshot.tags & CharStateTag.Invul) != 0;
            bool runtimeImmune =
                blackBoard.Features.useCombat &&
                blackBoard.Combat.damageImmuneCount > 0;
            return statusImmune || runtimeImmune;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        return stateManager != null && stateManager.IsInvulnerable;
    }

    public static void PushControlLock(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            blackBoard.Action.controlLockCount++;
            blackBoard.Action.isControlLocked = blackBoard.Action.controlLockCount > 0;
            return;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        if (stateManager != null)
        {
            stateManager.PushControlLock();
        }
    }

    public static void PopControlLock(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            blackBoard.Action.controlLockCount = Mathf.Max(0, blackBoard.Action.controlLockCount - 1);
            blackBoard.Action.isControlLocked = blackBoard.Action.controlLockCount > 0;
            return;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        if (stateManager != null)
        {
            stateManager.PopControlLock();
        }
    }

    public static void PushDamageImmune(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null && blackBoard.Features.useCombat)
        {
            blackBoard.Combat.damageImmuneCount++;
            return;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        if (stateManager != null)
        {
            stateManager.PushDamageImmune();
        }
    }

    public static void PopDamageImmune(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null && blackBoard.Features.useCombat)
        {
            blackBoard.Combat.damageImmuneCount = Mathf.Max(0, blackBoard.Combat.damageImmuneCount - 1);
            return;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        if (stateManager != null)
        {
            stateManager.PopDamageImmune();
        }
    }
}
