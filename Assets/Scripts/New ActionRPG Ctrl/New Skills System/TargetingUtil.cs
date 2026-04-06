using UnityEngine;

/// <summary>
/// 目标采集器。
/// 负责把角色当前的输入状态转换成一份 TargetInfo。
/// 它只负责“收集”，不负责目标是否合法。
/// </summary>
public static class TargetingUtil
{
    /// <summary>
    /// 收集一次施法目标。
    /// 优先级：
    /// 1. 锁定目标
    /// 2. 鼠标命中的单位
    /// 3. 鼠标命中的地面
    /// 4. 脚下平面的投影点
    /// </summary>
    public static TargetInfo Collect(CharCtrl caster, LayerMask groundLayer, LayerMask unitLayer)
    {
        TargetInfo info = new TargetInfo();
        Vector3 finalPos;

        // 1. 优先级最高：如果当前有锁定目标，直接取锁定数据，无视鼠标
        if (caster.LockedTarget != null)
        {
            info.unit = caster.LockedTarget.gameObject;
            finalPos = caster.LockedTarget.position;
        }
        else
        {
            // 2. 优先级次之：鼠标射线探测
            Ray ray = Camera.main.ScreenPointToRay(caster.Param.AimTarget);
            
            if (Physics.Raycast(ray, out RaycastHit unitHit, 1000f, unitLayer))
            {
                info.unit = unitHit.collider.gameObject;
                finalPos = unitHit.collider.transform.position;
            }
            else if (Physics.Raycast(ray, out RaycastHit groundHit, 500f, groundLayer))
            {
                finalPos = groundHit.point;
            }
            else
            {
                // 兜底：投影到脚下平面
                Plane groundPlane = new Plane(Vector3.up, caster.transform.position);
                groundPlane.Raycast(ray, out float enter);
                finalPos = ray.GetPoint(enter);
            }
        }

        info.position = finalPos;
        Vector3 dir = (finalPos - caster.transform.position);
        dir.y = 0;
        info.direction = dir.magnitude > 0.1f ? dir.normalized : caster.transform.forward;

        return info;
    }
}
