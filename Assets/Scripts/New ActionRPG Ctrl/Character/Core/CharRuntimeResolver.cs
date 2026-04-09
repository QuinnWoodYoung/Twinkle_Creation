using UnityEngine;

public static class CharRuntimeResolver
{
    // This helper centralizes blackboard-first runtime state reads so gameplay
    // code does not need to understand legacy component ownership.

    public static float GetMoveSpeed(GameObject obj, float fallbackBaseSpeed = 0f)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return Mathf.Max(0f, fallbackBaseSpeed);
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            float baseMoveSpeed = blackBoard.Motion.baseMoveSpeed > 0f
                ? blackBoard.Motion.baseMoveSpeed
                : fallbackBaseSpeed;

            if (!blackBoard.Features.useStatus)
            {
                return Mathf.Max(0f, baseMoveSpeed);
            }

            CharStateSnap snap = blackBoard.Status.snapshot;
            float finalMoveSpeed = baseMoveSpeed * Mathf.Max(0f, snap.moveSpdMul) + snap.moveSpdAdd;
            return Mathf.Max(0f, finalMoveSpeed);
        }

        CharStatusCtrl statusCtrl = unit.GetComponent<CharStatusCtrl>();
        if (statusCtrl != null)
        {
            CharStateSnap snap = statusCtrl.Snap;
            float finalMoveSpeed = fallbackBaseSpeed * Mathf.Max(0f, snap.moveSpdMul) + snap.moveSpdAdd;
            return Mathf.Max(0f, finalMoveSpeed);
        }

        return Mathf.Max(0f, fallbackBaseSpeed);
    }

    public static float GetTurnSpeed(GameObject obj, float fallbackBaseTurnSpeed = 720f)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return Mathf.Max(0f, fallbackBaseTurnSpeed);
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            float baseTurnSpeed = blackBoard.Motion.baseTurnSpeed > 0f
                ? blackBoard.Motion.baseTurnSpeed
                : fallbackBaseTurnSpeed;

            if (!blackBoard.Features.useStatus)
            {
                return Mathf.Max(0f, baseTurnSpeed);
            }

            float finalTurnSpeed = baseTurnSpeed * Mathf.Max(0f, blackBoard.Status.snapshot.turnSpdMul);
            return Mathf.Max(0f, finalTurnSpeed);
        }

        CharStatusCtrl statusCtrl = unit.GetComponent<CharStatusCtrl>();
        if (statusCtrl != null)
        {
            float finalTurnSpeed = fallbackBaseTurnSpeed * Mathf.Max(0f, statusCtrl.Snap.turnSpdMul);
            return Mathf.Max(0f, finalTurnSpeed);
        }

        return Mathf.Max(0f, fallbackBaseTurnSpeed);
    }

    public static float GetCastSpeed(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return 1f;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null)
        {
            if (!blackBoard.Features.useCombat)
            {
                return 1f;
            }

            float finalCastSpeed = blackBoard.Combat.castSpeed;
            if (blackBoard.Combat.castSpeedMul > 0f)
            {
                finalCastSpeed *= blackBoard.Combat.castSpeedMul;
            }

            return Mathf.Max(finalCastSpeed, 0.01f);
        }

        return 1f;
    }

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
            blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Action);
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
            blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Action);
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
            blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Combat);
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
            blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Combat);
            return;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        if (stateManager != null)
        {
            stateManager.PopDamageImmune();
        }
    }
}
