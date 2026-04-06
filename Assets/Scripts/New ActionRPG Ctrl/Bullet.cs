using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public GameObject launcher;//弹丸发射者

    private void Start()
    {
        // 子弹一出生，就给自己定个闹钟：5秒后如果没有撞到任何东西，就自动销毁
        Destroy(gameObject, 5f);
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("投掷物撞到了减速带");
        Destroy(gameObject, 0.1f);
    }
}
