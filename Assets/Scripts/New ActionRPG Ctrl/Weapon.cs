public enum WeaponType
{
    Sword,    // 近战武器
    Axe,      // 近战武器  
    Bow,      // 远程武器
    Shield,   // 近战武器
    None      // 无武器
}

[System.Serializable]
public class Weapon
{
    public WeaponType weaponType;
    // CharWeaponCtrl 可能会启用的动画层。Animator 里缺少某层时会自动跳过。
    public static readonly string[] AnimLayers = { "Light Sword", "Sword", "Bow", "ShieldLayer", "UnarmedLayer" };
    
    // 直接在属性中返回相关信息
    public bool IsRanged => weaponType == WeaponType.Bow;
    public bool IsMelee => weaponType != WeaponType.Bow;
    
    public string AnimationLayerName => GetAnimLayerName(weaponType);
    
    public string AttackAnimationName => GetAtkAnimName(weaponType);

    public bool CanMoveOnAtk => CanMoveAtk(weaponType);

    // 动画层仍然按武器类型区分。
    public static string GetAnimLayerName(WeaponType type) => type switch
    {
        WeaponType.Sword => "Light Sword",
        WeaponType.Axe => "Sword",
        WeaponType.Bow => "Bow",
        WeaponType.Shield => "ShieldLayer",
        WeaponType.None => "UnarmedLayer",
        _ => "UnarmedLayer"
    };

    // 现阶段普通攻击统一只用一个 Trigger，避免开发中期维护多个攻击参数。
    public static string GetAtkAnimName(WeaponType type) => "Attack";

    // 普攻时是否允许移动的规则。当前只有斧头攻击锁移动。
    public static bool CanMoveAtk(WeaponType type) => type switch
    {
        WeaponType.Axe => false,
        _ => true
    };
}
