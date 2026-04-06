using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLock : MonoBehaviour
{

    public PlayerInput pi;
    public float range = 20.0f;
    

    public class Enemy
    {
        public int id;
        public Vector3 enemyPosition;
    }
    public List<neutralEnemy> locks; //存储所有敌人
    public List<neutralEnemy> detect;//存储攻击范围内的敌人列表
    public List<GameObject> lock2;
    public List<GameObject> detect2;  

    private Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {

    }
    void Awake()
    {
        pi = GetComponent<PlayerInput>();//获取玩家挂载的PlayerInput模块
    }
    // Update is called once per frame
    void Update()
    {
        detect = new List<neutralEnemy>();//存储攻击范围内的敌人列表
        locks = new List<neutralEnemy>(FindObjectsOfType<neutralEnemy>());//存储场景内所有敌人的列表

        foreach (var enemy in locks)//遍历locks表
        {
            if (Vector3.Distance(gameObject.transform.position, enemy.transform.position) < range)//如果敌人在攻击范围内
            {
                detect.Add(enemy);//将敌人添加至列表
            }
        }
        if (pi.lockon)//如果玩家按下锁定键
        {

            foreach (var enemy in detect)//遍历范围内的所有挂载了NE的单位
            {
                Debug.Log(enemy);
            }
        }
    }
}
