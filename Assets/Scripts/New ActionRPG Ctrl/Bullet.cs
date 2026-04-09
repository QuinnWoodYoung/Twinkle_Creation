using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float _moveSpeed = 10f;
    [SerializeField] private float _turnSpeed = 18f;
    [SerializeField] private bool _useHoming;
    [SerializeField] private float _targetHeightOffset = 0.1f;
    [SerializeField] private float _impactDistance = 0.2f;

    private Rigidbody _rigidbody;
    private Collider _targetCollider;
    private CharacterController _targetCharacterController;
    private bool _impactConsumed;

    public GameObject launcher;

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
        if (!_useHoming || _target == null)
        {
            return;
        }

        Vector3 targetPoint = ResolveTargetPoint();
        Vector3 toTarget = targetPoint - transform.position;
        if (toTarget.sqrMagnitude <= 0.001f)
        {
            TryImpactTarget(_target.gameObject);
            return;
        }

        float distanceToTarget = toTarget.magnitude;
        float moveStep = _moveSpeed * Time.fixedDeltaTime;
        if (distanceToTarget <= ResolveImpactDistance() || moveStep >= distanceToTarget)
        {
            TryImpactTarget(_target.gameObject);
            return;
        }

        Vector3 desiredDirection = toTarget.normalized;
        Vector3 currentDirection = _rigidbody != null && _rigidbody.velocity.sqrMagnitude > 0.001f
            ? _rigidbody.velocity.normalized
            : transform.forward;

        Vector3 nextDirection = Vector3.RotateTowards(
            currentDirection,
            desiredDirection,
            _turnSpeed * Time.fixedDeltaTime,
            0f);

        if (_rigidbody != null)
        {
            _rigidbody.velocity = nextDirection * _moveSpeed;
        }

        transform.rotation = Quaternion.LookRotation(nextDirection);
    }

    public void SetMoveSpeed(float moveSpeed)
    {
        _moveSpeed = Mathf.Max(0f, moveSpeed);
    }

    public void SetHomingTarget(Transform target, float moveSpeed, float turnSpeed = 18f)
    {
        _target = target;
        _moveSpeed = Mathf.Max(0f, moveSpeed);
        _turnSpeed = Mathf.Max(0f, turnSpeed);
        _useHoming = _target != null;
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

    private void TryImpactTarget(GameObject hitUnit)
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

        Destroy(gameObject);
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
