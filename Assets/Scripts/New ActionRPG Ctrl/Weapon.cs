public enum WeaponType
{
    Sword = 0,
    Axe = 1,
    Bow = 2,
    Shield = 3,
    None = 4,
    Magic = 5,
}

[System.Serializable]
public class Weapon
{
    public WeaponType weaponType;

    public bool IsRanged => IsRangedWeapon(weaponType);
    public bool IsMelee => !IsRangedWeapon(weaponType);
    public string AttackAnimationName => GetAtkAnimName(weaponType);

    public static bool IsRangedWeapon(WeaponType type) => type switch
    {
        WeaponType.Bow => true,
        WeaponType.Magic => true,
        _ => false
    };

    public static bool IsMeleeWeapon(WeaponType type) => !IsRangedWeapon(type);

    public static string GetAtkAnimName(WeaponType type) => "Attack";
}
