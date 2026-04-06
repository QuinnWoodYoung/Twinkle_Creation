using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderFollowManager : MonoBehaviour
{
    public float yOffset = 0.5f;
    public Transform animationObject; // 动画对象

    private void Start()
    {

    }
    private void FixedUpdate()
    {
        // 获取动画对象的位置
        Vector3 pos = animationObject.position;

        // 将碰撞体的位置和旋转与动画对象的位置和旋转匹配
        transform.position = new Vector3(pos.x, pos.y + yOffset, pos.z);
    }
}
