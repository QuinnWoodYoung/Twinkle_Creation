using UnityEngine;

[CreateAssetMenu(fileName = "New Multi Shot Projectile", menuName = "SkillSystem/Effects/MultiShotProjectile")]
public class MultiShotProjectileEffect : SkillEffect
{
    [Header("Prefab")]
    public GameObject projectilePrefab;
    public Vector3 spawnOffset = new Vector3(0f, 1.2f, 1f);

    [Header("Spread")]
    [Min(1)] public int projectileCount = 3;
    [Min(0f)] public float totalSpreadAngle = 36f;

    [Header("Launch")]
    public EProjectileHomingMode homingMode = EProjectileHomingMode.Never;

    public override void Apply(CastContext context)
    {
        if (context == null || context.caster == null || projectilePrefab == null)
        {
            return;
        }

        GameObject targetUnit = CharRelationResolver.NormalizeUnit(context.rawTarget.unit);
        int count = Mathf.Max(1, projectileCount);
        float step = count > 1 ? totalSpreadAngle / (count - 1) : 0f;
        float startAngle = -totalSpreadAngle * 0.5f;

        Vector3 baseDirection = context.rawTarget.direction;
        baseDirection.y = 0f;
        if (baseDirection.sqrMagnitude <= 0.001f)
        {
            baseDirection = context.caster.transform.forward;
            baseDirection.y = 0f;
        }

        if (baseDirection.sqrMagnitude <= 0.001f)
        {
            baseDirection = Vector3.forward;
        }

        baseDirection.Normalize();

        for (int i = 0; i < count; i++)
        {
            float yaw = startAngle + (step * i);
            Vector3 direction = Quaternion.AngleAxis(yaw, Vector3.up) * baseDirection;
            SpawnProjectile(context, targetUnit, direction.normalized);
        }
    }

    private void SpawnProjectile(CastContext sourceContext, GameObject targetUnit, Vector3 direction)
    {
        GameObject caster = sourceContext.caster;
        Vector3 spawnPos = caster.transform.TransformPoint(spawnOffset);
        GameObject projectileObject = Object.Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
        Projectile projectile = projectileObject.GetComponent<Projectile>();

        if (projectile == null)
        {
            Object.Destroy(projectileObject);
            return;
        }

        bool shouldHome = homingMode == EProjectileHomingMode.Always ||
                          (homingMode == EProjectileHomingMode.Smart && targetUnit != null);

        Vector3 targetPosition = caster.transform.position + direction * 8f;
        TargetInfo targetInfo = shouldHome && targetUnit != null
            ? new TargetInfo(targetUnit, targetUnit.transform.position, direction)
            : new TargetInfo(null, targetPosition, direction);

        CastContext projectileContext = sourceContext.CreateChild(targetInfo, false);
        projectile.Initialize(projectileContext);
        projectile.isHoming = shouldHome;
        projectile.target = shouldHome && targetUnit != null ? targetUnit.transform : null;

        projectileObject.transform.forward = direction;
    }
}
