using UnityEngine;

/// <summary>
/// 状态系统对外的轻量入口。
/// 目前它主要承担“兼容旧接口”的职责，把旧的 EStatusType 施加请求转发给 CharStatusCtrl。
/// </summary>
public static class CharStatusResolver
{
    // Status application is now handled by CharStatusCtrl on blackboard units.

    /// <summary>
    /// 兼容旧状态接口的施加入口。
    /// 最终仍会归到目标角色身上的 CharStatusCtrl 去处理。
    /// </summary>
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
        return statusCtrl != null &&
            statusCtrl.ApplyStatus(status, duration, applier, source, stackAdd, power);
    }
}
