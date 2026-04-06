using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public ActorManager am;//����ActorManager
    public StateManager sm;



    // void OnTriggerEnter(Collider col)
    // {
    //     if(col.tag == "weapon" && !sm.isDead)
    //     {
    //         var tar = col.transform.parent.gameObject;
    //         GameObject target = tar.transform.parent.gameObject;
    //         var targetState = target.GetComponent<StateManager>();
    //         sm.TakeDamage(targetState, sm);
    //     }
    // }
    // 这是修改后的 OnTriggerEnter
    void OnTriggerEnter(Collider col) {
    // 检查碰撞体是否是武器
        if (col.CompareTag("weapon") && !sm.isDead)
        { 
            Debug.Log("开始受击");
            // 从武器上获取 WeaponInfo 组件
            WeaponInfo weaponInfo = col.GetComponent<WeaponInfo>();
            // 如果找到了 WeaponInfo 并且它有关联的主人
            if (weaponInfo != null && weaponInfo.owner != null)
            {
                // 直接从"名片"上获取攻击者的 StateManager
                StateManager targetState = weaponInfo.owner.GetComponent<StateManager>();
                if (targetState != null)
                {
                    // 执行受伤
                    sm.TakeDamage(targetState, sm);
                }
            }
        }
        else if(col.CompareTag("Projectile"))
        {
            Debug.Log("我膝盖中了一箭");
            Bullet bullet = col.GetComponent<Bullet>();
            
            if (bullet != null && bullet.launcher != null)
            {
                // 直接从"名片"上获取攻击者的 StateManager
                StateManager targetState = bullet.launcher.GetComponent<StateManager>();
                if (targetState != null)
                {
                    // 执行受伤
                    sm.TakeDamage(targetState, sm);
                }
            }
  
        }
        else if (col.CompareTag("weapon"))
        {
            Debug.Log("武器被触发");
        }
    }
}
