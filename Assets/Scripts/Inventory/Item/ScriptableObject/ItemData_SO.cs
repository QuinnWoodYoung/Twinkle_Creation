using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponSlotType { RightHand, LeftHand }; // 新增的枚举

public enum ItemType { useable, weapon };
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class ItemData_SO : ScriptableObject
{
    public ItemType itemType;
    public string itemName;
    public Sprite itemIcon;
    public int itemCount;
    [TextArea]
    public string description = "";
    public bool Stackable;//�Ƿ�ɶѵ�

    [Header("武器")]
    [Tooltip("装备该武器时，生成到角色手部插槽的模型预制体。")]
    public GameObject weaponPrefab;
    [Tooltip("装备时复制到 StateManager.attackData 的攻击数据。")]
    public AttackData_SO weaponData;
    [Tooltip("逻辑武器类型。CharWeaponCtrl 会根据它切动画层，并决定攻击时能否移动。")]
    public WeaponType weaponType = WeaponType.None;
    [Tooltip("这把武器的具体普攻行为在 weaponData 里配置。比如连击、索敌、直线、蓄力释放。")]
    public bool useWeaponAttackProfile = true;
    [Tooltip("这个武器模型应挂到哪只手。")]
    public WeaponSlotType weaponSlotType; // 新增的字段

    public UsableItemData_SO itemData;
}
