using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用投射物运行时。
/// LaunchProjectileEffect 负责发射，Projectile 负责飞行和命中结算。
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Basic")]
    public float speed = 15f;
    public float lifetime = 5f;
    public GameObject hitVfx;

    /// <summary>
    /// 命中后要执行的效果列表。
    /// </summary>
    [Header("On Hit")]
    public List<SkillEffect> onHitEffects = new List<SkillEffect>();

    [Header("Homing")]
    public bool isHoming = false;
    public Transform target;
    public float targetHeightOffset = 1.0f;

    [Header("Ground Follow")]
    public bool followGround = false;
    public float groundOffset = 0.1f;
    public float groundProbeHeight = 3f;
    public float groundProbeDistance = 8f;

    /// <summary>
    /// 施法时继承下来的上下文。
    /// 命中时会基于它生成新的 impactContext。
    /// </summary>
    private CastContext _context;
    private float _timer;
    private bool _isDisjointed;

    /// <summary>
    /// 在投射物创建时写入施法上下文。
    /// </summary>
    public void Initialize(CastContext context)
    {
        _context = context;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (isHoming && target != null && !_isDisjointed)
        {
            Vector3 targetPoint = target.position + Vector3.up * targetHeightOffset;
            Vector3 diff = targetPoint - transform.position;
            Vector3 directionToTarget = diff.normalized;
            float distanceToTarget = diff.magnitude;
            float moveStep = speed * Time.deltaTime;

            if (moveStep >= distanceToTarget || distanceToTarget < 0.2f)
            {
                transform.position = targetPoint;
                OnHit(target.gameObject);
                return;
            }

            MoveProjectile(transform.position + directionToTarget * moveStep, directionToTarget);
        }
        else
        {
            Vector3 direction = transform.forward;
            MoveProjectile(transform.position + direction * speed * Time.deltaTime, direction);
        }
    }

    /// <summary>
    /// 命中时创建新的命中上下文并执行 onHitEffects。
    /// </summary>
    private void OnHit(GameObject hitObj)
    {
        if (_context == null)
        {
            Destroy(gameObject);
            return;
        }

        TargetInfo impactTarget = _context.rawTarget;
        if (hitObj != null)
        {
            Vector3 direction = hitObj.transform.position - _context.caster.transform.position;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.001f ? direction.normalized : _context.rawTarget.direction;
            impactTarget = new TargetInfo(hitObj, hitObj.transform.position, direction);
        }

        CastContext impactContext = _context.CreateChild(impactTarget, false);
        impactContext.UpdateHitPoint(transform.position);

        SkillEffectUtility.ExecuteEffects(onHitEffects, impactContext);

        if (hitVfx != null)
        {
            Instantiate(hitVfx, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    public void Disjoint()
    {
        _isDisjointed = true;
        isHoming = false;
        target = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isHoming && target != null)
        {
            return;
        }

        if (_context == null)
        {
            return;
        }

        if (CharRelationResolver.TryResolveUnit(other.gameObject, out GameObject hitUnit) && hitUnit != _context.caster)
        {
            if (!CharRelationResolver.IsSkillTargetValid(_context.caster, hitUnit, _context.teamRule))
            {
                return;
            }

            OnHit(hitUnit);
        }
    }

    private void MoveProjectile(Vector3 nextPosition, Vector3 direction)
    {
        if (followGround)
        {
            nextPosition.y = ResolveGroundHeight(nextPosition, nextPosition.y) + groundOffset;
        }

        transform.position = nextPosition;

        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized);
        }
    }

    private float ResolveGroundHeight(Vector3 samplePosition, float fallbackY)
    {
        Vector3 rayOrigin = samplePosition + Vector3.up * Mathf.Max(groundProbeHeight, 0.1f);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, Mathf.Max(groundProbeDistance, 0.1f), ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }

        return fallbackY - groundOffset;
    }
}
