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

    [Header("weapon")]
    public GameObject weaponPrefab;
    public AttackData_SO weaponData;
    public WeaponSlotType weaponSlotType; // 新增的字段

    public UsableItemData_SO itemData;
}
