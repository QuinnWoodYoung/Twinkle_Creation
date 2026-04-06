using System;
using UnityEngine;
// ========== MODIFICATION START | 2026年2月3日 ==========
// 修复：添加 System.Collections.Generic 命名空间引用，以便使用 List<T>。
using System.Collections.Generic;
// ========== MODIFICATION END | 2026年2月3日 ==========

/// <summary>
/// 描述攻击输入的状态。
/// </summary>
public struct AttackInputState
{
    /// <summary>在攻击键按下的那一帧为 true。</summary>
    public bool isDown;
    /// <summary>在攻击键被按住的所有帧为 true。</summary>
    public bool isHeld;
    /// <summary>在攻击键松开的那一帧为 true。</summary>
    public bool isUp;
    /// <summary>按键被按住了多长时间（秒）。</summary>
    public float holdDuration;
}

[Serializable]
public class CharParam
{
    public Vector2 Locomotion;
    public Vector2 AimTarget;
    //玩家和AI都有自己的锁定状态，isLock可以确定是否锁定
    public bool isLock;
    public AttackInputState AttackState;
    public bool Dodge;

    // ========== MODIFICATION START | 2026年2月3日 ==========
    // 新增：用于传递“技能键被按下瞬间”信号的动态列表。
    public readonly List<bool> SkillInputDown = new List<bool>();
    // ========== MODIFICATION END | 2026年2月3日 ==========
    
    // --- 旧版攻击信号，留作参考 ---
    // public bool Attack;
}
