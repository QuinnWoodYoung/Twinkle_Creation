using UnityEngine;

public static class CharStatusResolver
{
    // Status access prefers CharStatusCtrl so effects stay independent from the
    // legacy state component. StateManager remains as a compatibility fallback.

    public static bool ApplyLegacyStatus(
        GameObject target,
        EStatusType status,
        float duration,
        GameObject applier = null,
        Object source = null,
        int stackAdd = 1,
        float power = 0f)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(target);
        if (unit == null || duration <= 0f)
        {
            return false;
        }

        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        if (blackBoard != null && !blackBoard.Features.useStatus)
        {
            return false;
        }

        CharStatusCtrl statusCtrl = unit.GetComponent<CharStatusCtrl>();
        if (statusCtrl != null)
        {
            return statusCtrl.ApplyStatus(status, duration, applier, source, stackAdd, power);
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        if (stateManager != null)
        {
            stateManager.ApplyStatus(status, duration);
            return true;
        }

        return false;
    }
}
