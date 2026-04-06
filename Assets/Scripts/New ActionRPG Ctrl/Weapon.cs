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
    
    // 直接在属性中返回相关信息
    public bool IsRanged => weaponType == WeaponType.Bow;
    public bool IsMelee => weaponType != WeaponType.Bow;
    
    public string AnimationLayerName => weaponType switch
    {
        WeaponType.Sword => "Light Sword",
        WeaponType.Axe => "Sword",
        WeaponType.Bow => "Bow",
        WeaponType.Shield => "ShieldLayer",
        WeaponType.None => "UnarmedLayer",
        _ => "DefaultLayer"
    };
    
    public string AttackAnimationName => weaponType switch
    {
        WeaponType.Sword => "swordAttack",
        WeaponType.Axe => "axeAttack", 
        WeaponType.Bow => "bowAttack",
        WeaponType.Shield => "shieldBash",
        WeaponType.None => "unarmedAttack",
        _ => "defaultAttack"
    };
}