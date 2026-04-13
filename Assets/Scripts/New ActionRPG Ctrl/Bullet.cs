using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float _moveSpeed = 10f;
    [SerializeField] private float _turnSpeed = 18f;
    [SerializeField] private bool _useHoming;
    [SerializeField] private float _targetHeightOffset = 0.1f;
    [SerializeField] private float _impactDistance = 0.2f;
    [SerializeField] private float _logicHitRadius = 0.45f;
    [SerializeField] private float _verticalHitTolerance = 1.2f;
    [SerializeField] private bool _followGround = true;
    [SerializeField] private float _groundOffset = 0.9f;
    [SerializeField] private float _groundProbeHeight = 4f;
    [SerializeField] private float _groundProbeDistance = 12f;
    [SerializeField] private bool _useLogicHit = true;
    [SerializeField] private bool _useLegacyCollisionDamage;
    [SerializeField] private float _targetAimHeight = 0.55f;
    [SerializeField] private GameObject _impactVfx;
    [SerializeField] private bool _attachImpactVfxToTarget;
    [SerializeField] private Vector3 _impactVfxOffset;

    private Rigidbody _rigidbody;
    private Collider _targetCollider;
    private CharacterController _targetCharacterController;
    private bool _impactConsumed;
    private Vector3 _travelDirection = Vector3.forward;
    private bool _hasStraightFlight;

    public GameObject launcher;
    public bool UseLegacyCollisionDamage => _useLegacyCollisionDamage;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        Destroy(gameObject, 5f);
    }

    private void FixedUpdate()
    {
        if (_useHoming && _target != null)
        {
            UpdateHomingFlight();
            return;
        }

        if (_hasStraightFlight || (_rigidbody != null && _rigidbody.velocity.sqrMagnitude > 0.001f))
        {
            UpdateStraightFlight();
        }
    }

    public void SetMoveSpeed(float moveSpeed)
    {
        _moveSpeed = Mathf.Max(0f, moveSpeed);
    }

    public void ConfigureFromAttackProfile(AttackData_SO profile)
    {
        if (profile == null)
        {
            return;
        }

        _useLogicHit = profile.useLogicHitResolution && profile.projectileUseLogicHit;
        _useLegacyCollisionDamage = profile.allowLegacyCollisionDamage;
        _logicHitRadius = Mathf.Max(0.05f, profile.projectileHitRadius);
        _verticalHitTolerance = Mathf.Max(0.1f, profile.projectileVerticalTolerance);
        _followGround = profile.projectileFollowGround;
        _groundOffset = profile.projectileGroundOffset;
        _groundProbeHeight = Mathf.Max(0.5f, profile.projectileGroundProbeHeight);
        _groundProbeDistance = Mathf.Max(0.5f, profile.projectileGroundProbeDistance);
        _impactDistance = Mathf.Max(0.05f, profile.projectileImpactDistance);
        _targetHeightOffset = profile.projectileTargetHeightOffset;
        _targetAimHeight = profile.targetAimHeight;
        _impactVfx = profile.attackHitVfx;
        _attachImpactVfxToTarget = profile.attachAttackHitVfxToTarget;
        _impactVfxOffset = profile.attackHitVfxOffset;
    }

    public void SetImpactVfx(GameObject impactVfx)
    {
        _impactVfx = impactVfx;
    }

    public void SetStraightFlight(Vector3 direction, float moveSpeed)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = transform.forward;
            direction.y = 0f;
        }

        _travelDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
        _moveSpeed = Mathf.Max(0f, moveSpeed);
        _hasStraightFlight = true;
        _useHoming = false;
        _target = null;
        _targetCollider = null;
        _targetCharacterController = null;

        if (_rigidbody != null)
        {
            _rigidbody.velocity = Vector3.zero;
        }
    }

    public void SetHomingTarget(Transform target, float moveSpeed, float turnSpeed = 18f)
    {
        _target = target;
        _moveSpeed = Mathf.Max(0f, moveSpeed);
        _turnSpeed = Mathf.Max(0f, turnSpeed);
        _useHoming = _target != null;
        _hasStraightFlight = !_useHoming;
        CacheTargetComponents();
    }

    public void ClearHoming()
    {
        _target = null;
        _useHoming = false;
        _targetCollider = null;
        _targetCharacterController = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
        {
            return;
        }

        GameObject launcherUnit = CharRelationResolver.NormalizeUnit(launcher);
        GameObject otherUnit = CharRelationResolver.NormalizeUnit(other.gameObject);
        if (_useLogicHit)
        {
            // Logic-hit projectiles should not be consumed by arbitrary world
            // colliders. They only resolve damage through their own logic path.
            return;
        }

        if (otherUnit != null)
        {
            if (launcherUnit != null && otherUnit == launcherUnit)
            {
                return;
            }

            // Basic attack projectiles should only stop on hostile units.
            if (!CharRelationResolver.CanReceiveBasicAttack(launcherUnit, otherUnit))
            {
                return;
            }
        }
        else if (other.isTrigger)
        {
            // Ignore helper trigger volumes so arrows are not consumed by
            // unrelated gameplay or VFX triggers.
            return;
        }

        TryImpactTarget(otherUnit);
    }

    private void UpdateStraightFlight()
    {
        if (!_hasStraightFlight)
        {
            _travelDirection = _rigidbody.velocity.normalized;
            _hasStraightFlight = _travelDirection.sqrMagnitude > 0.001f;
            if (!_hasStraightFlight)
            {
                return;
            }
        }

        Vector3 start = transform.position;
        Vector3 next = start + _travelDirection * (_moveSpeed * Time.fixedDeltaTime);
        if (_followGround)
        {
            next.y = ResolveGroundHeight(next, start.y) + _groundOffset;
        }

        MoveBullet(next, _travelDirection);

        if (_useLogicHit)
        {
            TryImpactAlongSegment(start, next);
        }
    }

    private void UpdateHomingFlight()
    {
        Vector3 targetPoint = ResolveTargetPoint();
        Vector3 toTarget = targetPoint - transform.position;
        if (toTarget.sqrMagnitude <= 0.001f)
        {
            TryImpactTarget(_target.gameObject, transform.position);
            return;
        }

        float distanceToTarget = toTarget.magnitude;
        float moveStep = _moveSpeed * Time.fixedDeltaTime;
        if (distanceToTarget <= ResolveImpactDistance() || moveStep >= distanceToTarget)
        {
            TryImpactTarget(_target.gameObject, targetPoint);
            return;
        }

        Vector3 desiredDirection = toTarget.normalized;
        Vector3 currentDirection = _travelDirection.sqrMagnitude > 0.001f
            ? _travelDirection
            : (_rigidbody != null && _rigidbody.velocity.sqrMagnitude > 0.001f
                ? _rigidbody.velocity.normalized
                : transform.forward);

        Vector3 nextDirection = Vector3.RotateTowards(
            currentDirection,
            desiredDirection,
            _turnSpeed * Time.fixedDeltaTime,
            0f);

        _travelDirection = nextDirection.sqrMagnitude > 0.001f
            ? nextDirection.normalized
            : desiredDirection;

        Vector3 next = transform.position + _travelDirection * moveStep;
        MoveBullet(next, _travelDirection);
    }

    private void MoveBullet(Vector3 position, Vector3 direction)
    {
        if (_rigidbody != null && !_rigidbody.isKinematic)
        {
            _rigidbody.MovePosition(position);
            _rigidbody.velocity = Vector3.zero;
        }
        else
        {
            transform.position = position;
        }

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void CacheTargetComponents()
    {
        _targetCollider = null;
        _targetCharacterController = null;

        if (_target == null)
        {
            return;
        }

        _targetCollider = _target.GetComponentInChildren<Collider>();
        _targetCharacterController = _target.GetComponentInChildren<CharacterController>();
    }

    private Vector3 ResolveTargetPoint()
    {
        if (_target == null)
        {
            return transform.position;
        }

        if (_targetCollider != null)
        {
            return _targetCollider.bounds.center + Vector3.up * _targetHeightOffset;
        }

        if (_targetCharacterController != null)
        {
            return _targetCharacterController.bounds.center + Vector3.up * _targetHeightOffset;
        }

        return _target.position + Vector3.up * (1f + _targetHeightOffset);
    }

    private float ResolveGroundHeight(Vector3 samplePosition, float fallbackY)
    {
        Vector3 rayOrigin = samplePosition + Vector3.up * _groundProbeHeight;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, _groundProbeDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }

        return fallbackY - _groundOffset;
    }

    private float ResolveImpactDistance()
    {
        float resolvedDistance = Mathf.Max(0.01f, _impactDistance);
        if (_targetCollider != null)
        {
            resolvedDistance = Mathf.Max(
                resolvedDistance,
                _targetCollider.bounds.extents.magnitude * 0.35f);
        }
        else if (_targetCharacterController != null)
        {
            resolvedDistance = Mathf.Max(
                resolvedDistance,
                _targetCharacterController.bounds.extents.magnitude * 0.35f);
        }

        return resolvedDistance;
    }

    private void TryImpactAlongSegment(Vector3 start, Vector3 end)
    {
        GameObject launcherUnit = CharRelationResolver.NormalizeUnit(launcher);
        if (launcherUnit == null)
        {
            return;
        }

        GameObject bestTarget = null;
        Vector3 bestImpactPoint = end;
        float bestScore = float.MaxValue;

        foreach (CharBlackBoard board in CharBlackBoard.ActiveBoards)
        {
            if (board == null)
            {
                continue;
            }

            GameObject candidate = board.gameObject;
            if (!CharRelationResolver.CanReceiveBasicAttack(launcherUnit, candidate))
            {
                continue;
            }

            Vector3 candidatePoint = CharBasicAttackHitUtility.ResolveUnitAimPoint(candidate, _targetAimHeight);
            float lateralDistance = CharBasicAttackHitUtility.DistancePointToSegmentXZ(candidatePoint, start, end);
            float candidateRadius = CharBasicAttackHitUtility.ResolveUnitRadius(candidate);
            if (lateralDistance > _logicHitRadius + candidateRadius)
            {
                continue;
            }

            Vector3 closestPoint = CharBasicAttackHitUtility.ClosestPointOnSegment(candidatePoint, start, end);
            if (CharBasicAttackHitUtility.TryGetUnitBounds(candidate, out Bounds bounds))
            {
                float minY = bounds.min.y - _verticalHitTolerance;
                float maxY = bounds.max.y + _verticalHitTolerance;
                if (closestPoint.y < minY || closestPoint.y > maxY)
                {
                    continue;
                }
            }

            float score = lateralDistance + Vector3.Distance(start, closestPoint) * 0.01f;
            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestTarget = candidate;
            bestImpactPoint = closestPoint;
        }

        if (bestTarget != null)
        {
            TryImpactTarget(bestTarget, bestImpactPoint);
        }
    }

    private void TryImpactTarget(GameObject hitUnit)
    {
        TryImpactTarget(hitUnit, transform.position);
    }

    private void TryImpactTarget(GameObject hitUnit, Vector3 impactPoint)
    {
        if (!TryConsumeImpact())
        {
            Destroy(gameObject);
            return;
        }

        GameObject launcherUnit = CharRelationResolver.NormalizeUnit(launcher);
        GameObject targetUnit = CharRelationResolver.NormalizeUnit(hitUnit);
        if (launcherUnit == null || targetUnit == null)
        {
            Destroy(gameObject);
            return;
        }

        if (CharRelationResolver.CanReceiveBasicAttack(launcherUnit, targetUnit))
        {
            float damage = CharResourceResolver.GetBasicAttackDamage(launcherUnit);
            if (damage > 0f)
            {
                CharResourceResolver.ApplyDamage(targetUnit, damage);
            }
        }

        PlayImpactVfx(impactPoint, targetUnit);
        Destroy(gameObject);
    }

    private void PlayImpactVfx(Vector3 impactPoint, GameObject targetUnit)
    {
        if (_impactVfx == null)
        {
            return;
        }

        Transform parent = null;
        Vector3 spawnPoint = impactPoint;
        if (targetUnit != null)
        {
            parent = _attachImpactVfxToTarget ? targetUnit.transform : null;
            spawnPoint = CharBasicAttackHitUtility.ResolveUnitAimPoint(targetUnit, _targetAimHeight);
        }

        if (parent != null)
        {
            GameObject instance = Instantiate(_impactVfx, spawnPoint, Quaternion.identity, parent);
            instance.transform.localPosition += _impactVfxOffset;
            return;
        }

        Instantiate(_impactVfx, spawnPoint + _impactVfxOffset, Quaternion.identity);
    }

    public bool TryConsumeImpact()
    {
        if (_impactConsumed)
        {
            return false;
        }

        _impactConsumed = true;
        return true;
    }
}
