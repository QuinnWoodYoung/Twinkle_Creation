using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyStates { GUARD,PATROL,CHASE,DEAD }



[RequireComponent(typeof(NavMeshAgent))]
public class neutralEnemy : MonoBehaviour,IEndGameObserver
{
    public StateManager sm;     //调用StateManager
    public GameObject model;
    private EnemyStates enemyStates;
    
    
    private NavMeshAgent agent;
    
    private Vector3 thrustVec; //冲量,便于改变碰撞体位置
    
    
    private Animator anim;
    protected StateManager StateManager;

    public float sightRadius;

    public float LookAtTime;
    private float remainLookAtTime;
    private float lastAttackTime;

    public bool isGuard;


    private float speed;

    private GameObject attackTarget; //攻击目标

    public float patrolRange;//范围巡逻
    public Vector3 wayPoint;
    public Vector3 guardPos;

    //配合动画的布尔值
    bool isWalk;
    bool isChase;
    bool isFollow;
    bool isDead;

    bool playerDead;

    void Awake()
    {

        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        speed = agent.speed;
        guardPos = transform.position;
        StateManager = GetComponent<StateManager>();

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
    /* OnEnable()
    {
        GameManager.Instance.RigisterEnemy(sm);
        SaveManager.Instance.SavePlayerData();
    }*/

    void OnDisable()
    {
        if (!GameManager.IsInitialized) return;
        GameManager.Instance.RemoveObserver(this);
    }
    void Update()
    {
        if(sm.HitPoint == 0)
        {
            isDead = true;
        }
        if (!playerDead)
        {
            SwitchStates();
            SwitchAnimation();
            lastAttackTime -= Time.deltaTime;
        }
    }
    void FixedUpdate()
    {

    }
    void SwitchAnimation()
    {
        anim.SetBool("isWalk",isWalk);
        anim.SetBool("isChase", isChase);
        anim.SetBool("isFollow", isFollow);
        anim.SetBool("isDead",isDead);

    }
    void SwitchStates()
    {
        if (isDead)
        {
            enemyStates = EnemyStates.DEAD;
        }
        else if (FoundPlayer())
        {
            enemyStates = EnemyStates.CHASE;
        }
        switch (enemyStates)
        {
            case EnemyStates.GUARD:
                break;
            case EnemyStates.PATROL:
                isChase = false;
                agent.speed = speed * 0.5f;

                if(Vector3.Distance(wayPoint,transform.position) <= agent.stoppingDistance)
                {
                    isWalk = false;
                    if(remainLookAtTime > 0)//到随机巡逻点后停止一会
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

                agent.speed = speed;

                if (!FoundPlayer())
                {
                    isFollow = false;//拉脱敌人
                    if (remainLookAtTime > 0)
                    {
                        agent.destination = transform.position;
                        remainLookAtTime -= Time.deltaTime;
                    }
                    else if(isGuard)
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
                    agent.destination = attackTarget.transform.position;
                }

                if (TargetInAttackRange() || TargetInSkillRange())
                {
                    isFollow = false;
                    agent.isStopped = true;

                    if (lastAttackTime < 0)
                    {
                        lastAttackTime = StateManager.attackData.coolDown;
                        Attack();
                    }

                }
                break;
            case EnemyStates.DEAD:
                agent.enabled = false;

                Destroy(gameObject,2f);
                break;
        }
    }

    bool FoundPlayer()
    {
        var colliders = Physics.OverlapSphere(transform.position, sightRadius);

        foreach (var target in colliders)
        {
            if (target.CompareTag("Player"))
            {
                attackTarget = target.gameObject;
                return true;
            }
        }
        attackTarget = null;
        return false;
    }
    void Attack()
    {
        transform.LookAt(attackTarget.transform);
        if (TargetInAttackRange())
        {
            //近身攻击动画
            anim.SetTrigger("Attack");
        }
        if (TargetInSkillRange())
        {
            //技能攻击动画
            anim.SetTrigger("Skill");
        }
    }
    bool TargetInAttackRange()
    {
        if (attackTarget != null)
            return Vector3.Distance(attackTarget.transform.position, transform.position) <= StateManager.attackData.attackRange;
        else
            return false;
    }

    bool TargetInSkillRange()
    {
        if (attackTarget != null)
            return Vector3.Distance(attackTarget.transform.position, transform.position) <= StateManager.attackData.maxAttackRange;
        else
            return false;
    }

    void GetNewWayPoint()
    {
        remainLookAtTime = LookAtTime;
        float randomX = Random.Range(-patrolRange,patrolRange);
        float randomZ = Random.Range(-patrolRange, patrolRange);

        Vector3 randomPoint = new Vector3(guardPos.x + randomX, transform.position.y, guardPos.z+ randomZ);

        NavMeshHit hit;
        wayPoint = NavMesh.SamplePosition(randomPoint, out hit, patrolRange, 1) ? hit.position : transform.position;
    }


    void OnDrawGizmosSelected()//观察范围
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
