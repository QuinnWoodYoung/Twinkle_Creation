using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum SkillCastAnimType
{
    None,
    Cast,
    Shoot,
    Buff,
    Dash,
}

public enum SkillCastFlowType
{
    Instant,
    CastPointRelease,
    Channel,
}

/// <summary>
/// 技能配置配方。
/// 一个 SkillData 负责定义施法规则，并按顺序执行 effect 链。
/// </summary>
[CreateAssetMenu(fileName = "New Skill", menuName = "SkillSystem/SkillData")]
public class SkillData : ScriptableObject
{
    public string skillName;
    public string skillDescribe;
    public float cooldown;
    [Min(0f)] public float energyCost;

    [Header("Cast Anim")]
    [Tooltip("技能施法动画类别。相同类别的技能共用同一套角色动画。")]
    public SkillCastAnimType castAnimType = SkillCastAnimType.Cast;
    [Tooltip("施法动作持续时长。0 表示只触发动画，不额外占用动作时长。")]
    [Min(0f)] public float castAnimDur = 0f;
    [Tooltip("施法动作期间是否锁移动。")]
    public bool lockMoveOnCastAnim = false;
    [Tooltip("施法动作期间是否锁转向。")]
    public bool lockRotateOnCastAnim = false;

    [Header("Cast Flow")]
    [Tooltip("Instant: 立即生效。CastPointRelease: 前摇结束后生效。Channel: 前摇结束后进入引导。")]
    public SkillCastFlowType castFlowType = SkillCastFlowType.Instant;
    [Tooltip("引导总时长。<= 0 表示进入引导后立刻结束。")]
    [Min(0f)] public float channelDuration = 0f;
    [Tooltip("引导期间 tick 间隔。<= 0 表示不执行周期效果。")]
    [Min(0f)] public float channelTickInterval = 0f;
    [Tooltip("进入引导后是否立即执行一次 tick。")]
    public bool triggerChannelTickImmediately = true;
    [Tooltip("引导期间是否锁移动。")]
    public bool lockMoveDuringChannel = true;
    [Tooltip("引导期间是否锁转向。")]
    public bool lockRotateDuringChannel = false;

    /// <summary>
    /// 技能施法规则：
    /// 目标类型、敌我关系、施法距离等都在这里定义。
    /// </summary>
    [Header("Cast Rules")]
    public float maxCastRange = 10f;
    public SkillTargetMode targetingMode = SkillTargetMode.Point;
    public SkillTargetTeamRule targetTeamRule = SkillTargetTeamRule.Any;

    [FormerlySerializedAs("requiresTargetUnit")]
    [HideInInspector] [SerializeField] private bool legacyRequiresTargetUnit;

    [Header("Range Handling")]
    [Tooltip("True: clamp target position to max cast range. False: fail the cast.")]
    public bool clampToMaxRange = false;

    [Header("Facing")]
    [Tooltip("Rotate the caster toward the cast direction before executing effects.")]
    public bool faceSkillDirectionOnCast = true;
    [Min(0f)] public float faceDirectionLockDuration = 0.12f;
    [Tooltip("Wait until the caster has turned to the cast direction before releasing the skill.")]
    public bool requireFacingBeforeCast = true;
    [Min(0.1f)] public float castFacingAngleTolerance = 5f;
    public bool lockMovementDuringFacing = true;
    [HideInInspector] public bool cancelFacingCastOnMoveInput = false;

    [Header("Aim Preview")]
    [Tooltip("False: quick cast like before. True: normal cast preview, then left-click confirm and right-click cancel.")]
    public bool useAimPreview = false;
    public SkillPreviewShape previewShape = SkillPreviewShape.Auto;
    public SkillPreviewAnchor previewAnchor = SkillPreviewAnchor.Auto;
    [Min(0f)] public float previewRadius = 0f;
    [Min(0f)] public float previewLength = 0f;
    [Min(0f)] public float previewWidth = 0f;
    [Range(1f, 360f)] public float previewAngle = 90f;
    public bool showCastRangeIndicator = true;
    public Color previewValidColor = new Color(0.2f, 1f, 0.45f, 0.95f);
    public Color previewInvalidColor = new Color(1f, 0.25f, 0.25f, 0.95f);

    /// <summary>
    /// 技能效果链。
    /// 顺序非常重要。
    /// </summary>
    public List<SkillEffect> effects = new List<SkillEffect>();
    [Tooltip("仅用于引导技能：引导中的周期效果。")]
    public List<SkillEffect> channelTickEffects = new List<SkillEffect>();
    [Tooltip("仅用于引导技能：引导结束或被打断时执行。")]
    public List<SkillEffect> channelEndEffects = new List<SkillEffect>();

    private void OnValidate()
    {
        if (legacyRequiresTargetUnit && targetingMode == SkillTargetMode.Point)
        {
            targetingMode = SkillTargetMode.Unit;
        }
    }

    /// <summary>
    /// 激活技能。
    /// 返回 true 表示施法成功，外部控制器可以让它进入冷却。
    /// </summary>
    public bool Activate(CastContext context)
    {
        if (!CanActivate(context))
        {
            return false;
        }

        // Effects downstream should inherit the skill's team rule.
        context.teamRule = targetTeamRule;
        FaceCaster(context);

        SkillEffectUtility.ExecuteEffects(effects, context);

        return true;
    }

    public bool IsChannelSkill()
    {
        return castFlowType == SkillCastFlowType.Channel;
    }

    public bool UsesCastPointRelease()
    {
        return castFlowType == SkillCastFlowType.CastPointRelease || castFlowType == SkillCastFlowType.Channel;
    }

    public string GetCastAnimKey()
    {
        switch (castAnimType)
        {
            case SkillCastAnimType.Cast:
                return "Cast";
            case SkillCastAnimType.Shoot:
                return "Shoot";
            case SkillCastAnimType.Buff:
                return "Buff";
            case SkillCastAnimType.Dash:
                return "Dash";
            default:
                return "";
        }
    }

    public bool CanActivate(CastContext context)
    {
        if (context == null || context.caster == null)
        {
            return false;
        }

        NormalizeContext(context);

        if (!IsTargetShapeValid(context))
        {
            return false;
        }

        if (!IsRangeValid(context))
        {
            return false;
        }

        if (!IsUnitTargetValid(context))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 规范化上下文，让不同类型的技能落到统一数据格式上。
    /// </summary>
    private void NormalizeContext(CastContext context)
    {
        switch (GetResolvedTargetingMode())
        {
            case SkillTargetMode.NoTarget:
                context.Retarget(null, context.caster.transform.position);
                break;
            case SkillTargetMode.Direction:
                if (context.rawTarget.direction.sqrMagnitude < 0.001f)
                {
                    context.rawTarget = new TargetInfo(
                        context.rawTarget.unit,
                        context.rawTarget.position,
                        context.caster.transform.forward);
                }
                break;
        }
    }

    /// <summary>
    /// 检查目标类型是否合法。
    /// </summary>
    private bool IsTargetShapeValid(CastContext context)
    {
        switch (GetResolvedTargetingMode())
        {
            case SkillTargetMode.Unit:
                return context.rawTarget.HasUnit;
            case SkillTargetMode.NoTarget:
            case SkillTargetMode.Point:
            case SkillTargetMode.UnitOrPoint:
            case SkillTargetMode.Direction:
            default:
                return true;
        }
    }

    /// <summary>
    /// 检查施法距离是否合法。
    /// </summary>
    private bool IsRangeValid(CastContext context)
    {
        if (maxCastRange <= 0f)
        {
            return true;
        }

        Vector3 casterPosition = context.caster.transform.position;
        float dist = Vector3.Distance(casterPosition, context.rawTarget.position);

        if (dist <= maxCastRange)
        {
            return true;
        }

        if (!clampToMaxRange)
        {
            return false;
        }

        Vector3 dir = context.rawTarget.position - casterPosition;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
        {
            return false;
        }

        context.OverrideTargetPosition(casterPosition + dir.normalized * maxCastRange);
        return true;
    }

    /// <summary>
    /// 检查单位目标是否符合敌我规则。
    /// </summary>
    private bool IsUnitTargetValid(CastContext context)
    {
        SkillTargetMode resolvedTargetingMode = GetResolvedTargetingMode();

        if (!context.rawTarget.HasUnit)
        {
            return resolvedTargetingMode != SkillTargetMode.Unit;
        }

        if (resolvedTargetingMode != SkillTargetMode.Unit && resolvedTargetingMode != SkillTargetMode.UnitOrPoint)
        {
            return true;
        }

        return SkillTargetingRules.IsUnitTargetValid(context.caster, context.rawTarget.unit, targetTeamRule);
    }

    private void FaceCaster(CastContext context)
    {
        if (!faceSkillDirectionOnCast || context == null || context.caster == null)
        {
            return;
        }

        Vector3 direction = context.rawTarget.direction;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = context.rawTarget.position - context.caster.transform.position;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        CharCtrl charCtrl = context.caster.GetComponent<CharCtrl>();
        if (charCtrl != null)
        {
            charCtrl.ForceFaceDirection(direction.normalized, faceDirectionLockDuration);
            return;
        }

        context.caster.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private SkillTargetMode GetResolvedTargetingMode()
    {
        if (legacyRequiresTargetUnit && targetingMode == SkillTargetMode.Point)
        {
            return SkillTargetMode.Unit;
        }

        return targetingMode;
    }
}
