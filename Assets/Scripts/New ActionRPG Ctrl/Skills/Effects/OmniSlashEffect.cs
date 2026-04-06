using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 本体跳斩型效果。
///
/// 这是专门给“无敌斩”这类技能准备的执行器：
/// 1. 锁定施法者控制与移动。
/// 2. 在每一刀时瞬移到目标附近。
/// 3. 每刀重新寻找下一名合法敌人。
/// 4. 技能期间令施法者免疫伤害。
/// 5. 对每一刀执行 effectsPerSlash。
/// </summary>
[CreateAssetMenu(fileName = "Omni Slash Effect", menuName = "SkillSystem/Effects/OmniSlash")]
public class OmniSlashEffect : SkillEffect
{
    [Header("Slash")]
    [Min(1)] public int slashCount = 6;
    [Min(0.01f)] public float slashInterval = 0.2f;
    [Min(0f)] public float searchRadius = 8f;
    public LayerMask targetLayers;
    public SkillTargetTeamRule targetTeamRule = SkillTargetTeamRule.Enemy;
    public bool allowRepeatTargetWhenAlone = true;
    public bool avoidImmediateRepeat = true;

    [Header("Teleport")]
    [Tooltip("停在目标身边的距离，避免和目标重叠。")]
    public float targetStopDistance = 1.2f;
    public float targetHeightOffset = 0f;

    [Header("Protection")]
    public bool grantDamageImmunity = true;
    public bool lockControlDuringSlash = true;
    public bool lockMovementDuringSlash = true;

    [Header("VFX")]
    public GameObject slashHitVfxPrefab;
    [Min(0f)] public float slashHitVfxLifetime = 0.4f;

    [Header("Effects")]
    public List<SkillEffect> effectsPerSlash = new List<SkillEffect>();

    public override void Apply(CastContext context)
    {
        if (context == null || context.caster == null)
        {
            return;
        }

        SkillEffectRuntime runtime = SkillEffectRuntime.Get(context.caster);
        if (runtime == null)
        {
            return;
        }

        runtime.Run(OmniSlashRoutine(context.Snapshot()));
    }

    private IEnumerator OmniSlashRoutine(CastContext context)
    {
        GameObject caster = context.caster;
        if (caster == null)
        {
            yield break;
        }

        CharCtrl charCtrl = caster.GetComponent<CharCtrl>();
        StateManager casterState = caster.GetComponent<StateManager>();
        CharacterController cc = caster.GetComponent<CharacterController>();

        if (lockMovementDuringSlash && charCtrl != null)
        {
            charCtrl.SetMovementLocked(true);
        }

        if (lockControlDuringSlash && casterState != null)
        {
            casterState.PushControlLock();
        }

        if (grantDamageImmunity && casterState != null)
        {
            casterState.PushDamageImmune();
        }

        try
        {
            GameObject previousTarget = null;
            GameObject currentTarget = IsValidSlashTarget(caster, context.rawTarget.unit)
                ? context.rawTarget.unit
                : FindNextTarget(caster, null, context.rawTarget.position);

            for (int i = 0; i < slashCount; i++)
            {
                if (caster == null)
                {
                    yield break;
                }

                if (!IsValidSlashTarget(caster, currentTarget))
                {
                    Vector3 searchCenter = previousTarget != null
                        ? previousTarget.transform.position
                        : caster.transform.position;
                    currentTarget = FindNextTarget(caster, previousTarget, searchCenter);
                }

                if (!IsValidSlashTarget(caster, currentTarget))
                {
                    yield break;
                }

                TeleportNearTarget(caster, currentTarget, cc);
                FaceTarget(caster, currentTarget, charCtrl);
                SpawnSlashHitVfx(currentTarget.transform.position + Vector3.up * targetHeightOffset);
                ExecuteSlashEffects(context, currentTarget, previousTarget);

                previousTarget = currentTarget;

                if (i < slashCount - 1)
                {
                    currentTarget = FindNextTarget(caster, previousTarget, previousTarget.transform.position);
                    yield return new WaitForSeconds(slashInterval);
                }
            }
        }
        finally
        {
            if (grantDamageImmunity && casterState != null)
            {
                casterState.PopDamageImmune();
            }

            if (lockControlDuringSlash && casterState != null)
            {
                casterState.PopControlLock();
            }

            if (lockMovementDuringSlash && charCtrl != null)
            {
                charCtrl.SetMovementLocked(false);
            }
        }
    }

    private GameObject FindNextTarget(GameObject caster, GameObject previousTarget, Vector3 searchCenter)
    {
        Collider[] hits = Physics.OverlapSphere(searchCenter, searchRadius, targetLayers);
        GameObject bestTarget = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            StateManager state = hits[i].GetComponent<StateManager>();
            if (state == null)
            {
                state = hits[i].GetComponentInParent<StateManager>();
            }

            if (state == null)
            {
                continue;
            }

            GameObject candidate = state.gameObject;
            if (!IsValidSlashTarget(caster, candidate))
            {
                continue;
            }

            if (avoidImmediateRepeat && previousTarget != null && candidate == previousTarget)
            {
                continue;
            }

            float dist = Vector3.Distance(searchCenter, candidate.transform.position);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestTarget = candidate;
            }
        }

        if (bestTarget == null && allowRepeatTargetWhenAlone && previousTarget != null && IsValidSlashTarget(caster, previousTarget))
        {
            return previousTarget;
        }

        return bestTarget;
    }

    private bool IsValidSlashTarget(GameObject caster, GameObject candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        return SkillTargetingRules.IsUnitTargetValid(caster, candidate, targetTeamRule);
    }

    private void TeleportNearTarget(GameObject caster, GameObject target, CharacterController cc)
    {
        if (caster == null || target == null)
        {
            return;
        }

        Vector3 targetPosition = target.transform.position + Vector3.up * targetHeightOffset;
        Vector3 dirToCaster = caster.transform.position - targetPosition;
        dirToCaster.y = 0f;

        if (dirToCaster.sqrMagnitude < 0.001f)
        {
            dirToCaster = -target.transform.forward;
            dirToCaster.y = 0f;
        }

        dirToCaster = dirToCaster.sqrMagnitude > 0.001f ? dirToCaster.normalized : Vector3.back;
        Vector3 destination = targetPosition + dirToCaster * targetStopDistance;

        if (cc != null)
        {
            cc.enabled = false;
        }

        caster.transform.position = destination;

        if (cc != null)
        {
            cc.enabled = true;
        }
    }

    private void FaceTarget(GameObject caster, GameObject target, CharCtrl charCtrl)
    {
        if (caster == null || target == null)
        {
            return;
        }

        Vector3 direction = target.transform.position - caster.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        if (charCtrl != null)
        {
            charCtrl.RotateTowardsDirection(direction, 0f);
        }
        else
        {
            caster.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private void SpawnSlashHitVfx(Vector3 position)
    {
        if (slashHitVfxPrefab == null)
        {
            return;
        }

        GameObject instance = Object.Instantiate(slashHitVfxPrefab, position, Quaternion.identity);
        if (slashHitVfxLifetime > 0f)
        {
            Object.Destroy(instance, slashHitVfxLifetime);
        }
    }

    private void ExecuteSlashEffects(CastContext originalContext, GameObject target, GameObject previousTarget)
    {
        Vector3 previousPosition = previousTarget != null
            ? previousTarget.transform.position
            : originalContext.caster.transform.position;
        Vector3 direction = target.transform.position - previousPosition;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.001f ? direction.normalized : originalContext.rawTarget.direction;

        TargetInfo targetInfo = new TargetInfo(target, target.transform.position, direction);
        CastContext slashContext = originalContext.CreateChild(targetInfo, false);
        slashContext.UpdateHitPoint(target.transform.position);

        SkillEffectUtility.ExecuteEffects(effectsPerSlash, slashContext);
    }
}
