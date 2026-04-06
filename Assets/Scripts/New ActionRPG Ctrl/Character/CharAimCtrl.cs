using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//该脚本控制角色对某一个方向的锁定，无论是锁定敌人还是方向
public class CharAimCtrl : MonoBehaviour
{
    private CharCtrl _charCtrl;
    // 新增：用于判断是否是玩家控制的CharAimCtrl实例
    [Header("Indicator Settings")]
    [Tooltip("设置为true，如果此控制器属于玩家角色。")]
    public bool isPlayerControlled = false;
    [Tooltip("目标指示器Prefab。")]
    public TargetIndicator indicatorPrefab;
    private TargetIndicator _currentIndicator; // 当前激活的指示器实例
    // 用于存储当前锁定目标
    public Transform lockedTarget;
    //鼠标锁定的最大像素半径，可以在 Inspector 面板中调整
    [Header("Lock-On Settings")]
    public float maxLockOnRadius = 200f;


    protected void Awake()
    {
        _charCtrl = GetComponent<CharCtrl>();
        // 如果是玩家控制，则实例化指示器
        if (isPlayerControlled && indicatorPrefab != null)
        {
            _currentIndicator = Instantiate(indicatorPrefab);
            // 初始状态隐藏，等待SetTarget调用
            _currentIndicator.ClearTarget(); 
        }
    }

    protected void Update()
    {
        // 在锁定状态下，执行索敌逻辑
        if (_charCtrl.Param.isLock)
        {
            HandleLockOn();
        }
        else
        {
            // 如果从锁定状态切换回非锁定状态，清空锁定目标
            lockedTarget = null;
        }

        // ========== MODIFICATION START | 2026年2月6日 ==========
        // 如果是玩家控制，并且指示器已实例化，则更新指示器
        if (isPlayerControlled && _currentIndicator != null)
        {
            // SetTarget方法即使lockedTarget为null也能正确处理隐藏
            _currentIndicator.SetTarget(lockedTarget); 
        }
        // ========== MODIFICATION END | 2026年2月6日 ==========
    }
    private void HandleLockOn() //这个脚本可以让角色朝向自己锁定的目标点，而不需要考虑锁定的方式究竟是玩家的锁定操作或者AI角色的计算
    {
        // 如果没有敌人标签，提前返回避免报错
        // 请确保你已经在项目中创建了 "Enemy" 标签并分配给了敌人
        if (GameObject.FindGameObjectsWithTag("Enemies").Length == 0)
        {
            lockedTarget = null;
            return;
        }

        float minDistance = float.MaxValue;
        Transform bestTarget = null;

        // 获取所有带 "Enemy" 标签的敌人
        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemies"))
        {
            // 将敌人的世界坐标转换为屏幕坐标
            Vector3 screenPos = Camera.main.WorldToScreenPoint(enemy.transform.position);

            // 只考虑在摄像机视野内的敌人
            if (screenPos.z > 0)
            {
                // 计算敌人屏幕位置与鼠标位置的距离
                float distance = Vector2.Distance(screenPos, Input.mousePosition);

                if (distance < minDistance && distance < maxLockOnRadius)
                {
                    minDistance = distance;
                    bestTarget = enemy.transform;
                }
            }

            // 如果找到了最佳目标
            if (bestTarget != null)
            {
                if (lockedTarget != bestTarget)
                {
                    lockedTarget = bestTarget;
                    //Debug.Log("Locked on to: " + lockedTarget.name);
                }
            }
            else
            {
                // 没有任何有效目标，清空锁定
                lockedTarget = null;
            }

        }
    }

    // ========== MODIFICATION START | 2026年2月6日 ==========
    protected void OnDestroy()
    {
        if (isPlayerControlled && _currentIndicator != null)
        {
            Destroy(_currentIndicator.gameObject);
        }
    }
    // ========== MODIFICATION END | 2026年2月6日 ==========
}
