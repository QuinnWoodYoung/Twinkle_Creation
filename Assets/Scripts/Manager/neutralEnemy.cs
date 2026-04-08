using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyStates { GUARD, PATROL, CHASE, DEAD }

[RequireComponent(typeof(NavMeshAgent))]
public class neutralEnemy : MonoBehaviour, IEndGameObserver
{
    public StateManager sm;     // 仅保留给旧预制体引用，不再作为主数据源
    public GameObject model;
    private EnemyStates enemyStates;

    private NavMeshAgent agent;
    private Vector3 thrustVec; // 冲量预留，保持旧脚本字段兼容
    private Animator anim;

    public float sightRadius;
    public float LookAtTime;
    private float remainLookAtTime;
    private float lastAttackTime;

    public bool isGuard;

    private float speed;
    private GameObject attackTarget;

    public float patrolRange;
    public Vector3 wayPoint;
    public Vector3 guardPos;

    bool isWalk;
    bool isChase;
    bool isFollow;
    bool isDead;

    bool playerDead;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        speed = agent != null ? agent.speed : 0f;
        guardPos = transform.position;

        if (sm == null)
        {
            sm = GetComponent<StateManager>();
        }

        remainLookAtTime = LookAtTime;
    }

    void Start()
    {
        if (isGuard)
        {
            enemyStates = EnemyStates.GUARD;
        }
        else
        {
            enemyStates = EnemyStates.PATROL;
            GetNewWayPoint();
        }

        GameManager.Instance.AddObserver(this);
    }

    void OnDisable()
    {
        if (!GameManager.IsInitialized) return;
        GameManager.Instance.RemoveObserver(this);
    }

    void Update()
    {
        isDead = CharRuntimeResolver.IsDead(gameObject);
        if (!playerDead)
        {
            SwitchStates();
            SwitchAnimation();
            lastAttackTime -= Time.deltaTime;
        }
    }

    void SwitchAnimation()
    {
        if (anim == null)
        {
            return;
        }

        anim.SetBool("isWalk", isWalk);
        anim.SetBool("isChase", isChase);
        anim.SetBool("isFollow", isFollow);
        anim.SetBool("isDead", isDead);
    }

    void SwitchStates()
    {
        bool foundTarget = FoundTarget();

        if (isDead)
        {
            enemyStates = EnemyStates.DEAD;
        }
        else if (foundTarget)
        {
            enemyStates = EnemyStates.CHASE;
        }

        switch (enemyStates)
        {
            case EnemyStates.GUARD:
                isWalk = false;
                isChase = false;
                isFollow = false;
                break;

            case EnemyStates.PATROL:
                isChase = false;
                isFollow = false;
                if (agent == null)
                {
                    break;
                }

                agent.speed = speed * 0.5f;

                if (Vector3.Distance(wayPoint, transform.position) <= agent.stoppingDistance)
                {
                    isWalk = false;
                    if (remainLookAtTime > 0)
                    {
                        remainLookAtTime -= Time.deltaTime;
                    }
                    else
                    {
                        GetNewWayPoint();
                    }
                }
                else
                {
                    isWalk = true;
                    agent.destination = wayPoint;
                }
                break;

            case EnemyStates.CHASE:
                isWalk = false;
                isChase = true;
                if (agent == null)
                {
                    break;
                }

                agent.speed = speed;

                if (!foundTarget)
                {
                    isFollow = false;
                    if (remainLookAtTime > 0)
                    {
                        agent.destination = transform.position;
                        remainLookAtTime -= Time.deltaTime;
                    }
                    else if (isGuard)
                    {
                        enemyStates = EnemyStates.GUARD;
                    }
                    else
                    {
                        enemyStates = EnemyStates.PATROL;
                    }
                }
                else
                {
                    isFollow = true;
                    agent.isStopped = false;
                    agent.destination = attackTarget != null ? attackTarget.transform.position : transform.position;
                }

                if ((TargetInAttackRange() || TargetInSkillRange()) && CharRuntimeResolver.CanAttack(gameObject))
                {
                    isFollow = false;
                    agent.isStopped = true;

                    if (lastAttackTime < 0f)
                    {
                        lastAttackTime = CharResourceResolver.GetAttackCooldown(gameObject);
                        Attack();
                    }
                }
                break;

            case EnemyStates.DEAD:
                isWalk = false;
                isChase = false;
                isFollow = false;
                if (agent != null && agent.enabled)
                {
                    agent.enabled = false;
                }

                Destroy(gameObject, 2f);
                break;
        }
    }

    bool FoundTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, sightRadius);
        GameObject bestTarget = null;
        float bestDistanceSqr = float.MaxValue;
        HashSet<GameObject> seenUnits = new HashSet<GameObject>();

        foreach (Collider target in colliders)
        {
            GameObject targetUnit = CharRelationResolver.NormalizeUnit(target.gameObject);
            if (targetUnit == null || targetUnit == gameObject)
            {
                continue;
            }

            if (!seenUnits.Add(targetUnit))
            {
                continue;
            }

            if (!CharRelationResolver.IsAlive(targetUnit) || !CharRelationResolver.IsEnemy(gameObject, targetUnit))
            {
                continue;
            }

            float distanceSqr = (targetUnit.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            bestTarget = targetUnit;
        }

        attackTarget = bestTarget;
        return attackTarget != null;
    }

    void Attack()
    {
        if (attackTarget == null || anim == null)
        {
            return;
        }

        transform.LookAt(attackTarget.transform);
        if (TargetInAttackRange())
        {
            anim.SetTrigger("Attack");
        }

        if (TargetInSkillRange())
        {
            anim.SetTrigger("Skill");
        }
    }

    bool TargetInAttackRange()
    {
        if (attackTarget == null)
        {
            return false;
        }

        return Vector3.Distance(attackTarget.transform.position, transform.position) <= CharResourceResolver.GetAttackRange(gameObject);
    }

    bool TargetInSkillRange()
    {
        if (attackTarget == null)
        {
            return false;
        }

        return Vector3.Distance(attackTarget.transform.position, transform.position) <= CharResourceResolver.GetMaxAttackRange(gameObject);
    }

    void GetNewWayPoint()
    {
        remainLookAtTime = LookAtTime;
        float randomX = Random.Range(-patrolRange, patrolRange);
        float randomZ = Random.Range(-patrolRange, patrolRange);

        Vector3 randomPoint = new Vector3(guardPos.x + randomX, transform.position.y, guardPos.z + randomZ);

        NavMeshHit hit;
        wayPoint = NavMesh.SamplePosition(randomPoint, out hit, patrolRange, 1) ? hit.position : transform.position;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, sightRadius);
    }

    public void EndNotify()
    {
        playerDead = true;
        isChase = false;
        isWalk = false;
        attackTarget = null;
    }
}
