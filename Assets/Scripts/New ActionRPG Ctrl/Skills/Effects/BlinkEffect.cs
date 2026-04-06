using UnityEngine;

/// <summary>
/// 闪烁 / 瞬移 / 突进类位移效果。
///
/// 如果目标是单位，就会落在目标附近并自动朝向目标。
/// 如果目标只是一个地点，就会直接传送到目标点。
/// </summary>
[CreateAssetMenu(fileName = "Blink Effect", menuName = "SkillSystem/Effects/Blink")]
public class BlinkEffect : SkillEffect
{
    [Header("Landing")]
    [Tooltip("当目标是单位时，与目标保持的最小停靠距离。")]
    public float unitStopDistance = 1.2f;

    public override void Apply(CastContext context)
    {
        GameObject caster = context.caster;

        // 默认情况下直接使用当前上下文里的目标点作为传送落点。
        Vector3 destination = context.rawTarget.position;

        // 如果当前目标是单位，就在单位身边停下，而不是压在目标中心点上。
        if (context.rawTarget.HasUnit)
        {
            Vector3 dirToCaster = (caster.transform.position - destination).normalized;
            dirToCaster.y = 0f;
            destination = destination + dirToCaster * unitStopDistance;

            // 突进结束后立刻朝向目标，常用于后续接平A或下一个技能。
            Vector3 lookAtPos = new Vector3(
                context.rawTarget.position.x,
                caster.transform.position.y,
                context.rawTarget.position.z);
            caster.transform.LookAt(lookAtPos);
        }

        // 如果角色本身由 CharacterController 驱动，
        // 直接改 transform.position 前先禁用它，避免被控制器拉回原位。
        CharacterController cc = caster.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }

        caster.transform.position = destination;

        if (cc != null)
        {
            cc.enabled = true;
        }

        Debug.Log($"[Blink] {caster.name} moved to {destination}");
    }
}
