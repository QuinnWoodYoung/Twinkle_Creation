using UnityEngine;

public static class CharRuntimeResolver
{
    // Runtime state is now blackboard-owned.

    public static float GetMoveSpeed(GameObject obj, float fallbackBaseSpeed = 0f)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return Mathf.Max(0f, fallbackBaseSpeed);
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard == null)
        {
            return Mathf.Max(0f, fallbackBaseSpeed);
        }

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

    public static float GetTurnSpeed(GameObject obj, float fallbackBaseTurnSpeed = 720f)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return Mathf.Max(0f, fallbackBaseTurnSpeed);
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard == null)
        {
            return Mathf.Max(0f, fallbackBaseTurnSpeed);
        }

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
        if (blackBoard == null)
        {
            return false;
        }

        if (blackBoard.Features.useResources && blackBoard.Resources.hasHealth)
        {
            return blackBoard.Resources.hp <= 0f;
        }

        return blackBoard.Action.isDead;
    }

    public static bool CanMove(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return true;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard == null)
        {
            return true;
        }

        bool canMove = blackBoard.Features.useStatus
            ? blackBoard.Status.snapshot.canMove
            : blackBoard.Motion.canMove;
        return canMove && !blackBoard.Action.isControlLocked && !IsDead(unit);
    }

    public static bool CanRotate(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return true;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard == null)
        {
            return true;
        }

        return blackBoard.Features.useStatus
            ? blackBoard.Status.snapshot.canRotate
            : blackBoard.Motion.canRotate;
    }

    public static bool CanCast(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return true;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard == null)
        {
            return true;
        }

        bool canCast = blackBoard.Features.useStatus
            ? blackBoard.Status.snapshot.canCast
            : true;
        return canCast && !blackBoard.Action.isControlLocked && !IsDead(unit);
    }

    public static bool CanAttack(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return true;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard == null)
        {
            return true;
        }

        bool canAttack = blackBoard.Features.useStatus
            ? blackBoard.Status.snapshot.canAtk
            : true;
        return canAttack && !blackBoard.Action.isControlLocked && !IsDead(unit);
    }

    public static bool IsDamageImmune(GameObject obj)
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

        bool statusImmune =
            blackBoard.Features.useStatus &&
            (blackBoard.Status.snapshot.tags & CharStateTag.Invul) != 0;
        bool runtimeImmune =
            blackBoard.Features.useCombat &&
            blackBoard.Combat.damageImmuneCount > 0;
        return statusImmune || runtimeImmune;
    }

    public static void PushControlLock(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard == null)
        {
            return;
        }

        blackBoard.Action.controlLockCount++;
        blackBoard.Action.isControlLocked = blackBoard.Action.controlLockCount > 0;
        blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Action);
    }

    public static void PopControlLock(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard == null)
        {
            return;
        }

        blackBoard.Action.controlLockCount = Mathf.Max(0, blackBoard.Action.controlLockCount - 1);
        blackBoard.Action.isControlLocked = blackBoard.Action.controlLockCount > 0;
        blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Action);
    }

    public static void PushDamageImmune(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard == null || !blackBoard.Features.useCombat)
        {
            return;
        }

        blackBoard.Combat.damageImmuneCount++;
        blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Combat);
    }

    public static void PopDamageImmune(GameObject obj)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(obj);
        if (unit == null)
        {
            return;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard == null || !blackBoard.Features.useCombat)
        {
            return;
        }

        blackBoard.Combat.damageImmuneCount = Mathf.Max(0, blackBoard.Combat.damageImmuneCount - 1);
        blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Combat);
    }
}
