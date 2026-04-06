/// <summary>
/// 定义所有角色可能拥有的状态效果。
/// 使用 [System.Flags] 属性可以让多个状态以位掩码的形式组合在一起，
/// 但为了简单起见，我们暂时不使用它，除非将来有明确需求。
/// </summary>
public enum EStatusType
{
    // --- 控制类效果 ---
    
    /// <summary>
    /// 眩晕：无法移动、攻击或施法。
    /// </summary>
    Stunned,

    /// <summary>
    /// 禁锢：无法移动，但可以攻击和施法。
    /// </summary>
    Rooted,

    /// <summary>
    /// 沉默：可以移动和攻击，但无法施法。
    /// </summary>
    Silenced,

    // --- 属性类效果 ---
    
    /// <summary>
    /// 速度提升。
    /// </summary>
    Hasted,
    
    /// <summary>
    /// 速度降低。
    /// </summary>
    Slowed,

    // --- 持续伤害/治疗效果 ---
    
    /// <summary>
    /// 中毒：每秒持续受到伤害。
    /// </summary>
    Poisoned,

    /// <summary>
    /// 再生：每秒持续恢复生命。
    /// </summary>
    Regenerating,

    /// <summary>
    /// 无敌：不会受到伤害。
    /// </summary>
    Invulnerable,
    
    // 可以在这里添加更多状态，例如...
    // Invulnerable (无敌)
    // Ethereal (虚无)
    // Frozen (冰冻)
}
