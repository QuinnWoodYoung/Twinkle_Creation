using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public enum SlotType{BAG,WEAPON,ARMOR,ACTION}
public class SlotHolder : MonoBehaviour,IPointerClickHandler
{
    public SlotType slotType;
    public ItemUI itemUI;

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.clickCount % 2 == 0)
        {
            UseItem();
        }
    }

    public void UseItem()
    {
        if(itemUI.GetItem() != null)
            if(itemUI.GetItem().itemType == ItemType.useable && itemUI.Bag.items[itemUI.Index].amount > 0)
            {
                GameObject playerUnit = GameManager.Instance != null ? GameManager.Instance.PlayerUnit : null;
                if (playerUnit != null)
                {
                    CharResourceResolver.ApplyHeal(playerUnit, itemUI.GetItem().itemData.healthPoint);
                }

                itemUI.Bag.items[itemUI.Index].amount -= 1;
            }

        UpdateItem();
    }

    public void UpdateItem()
    {
        switch (slotType)
        {
            case SlotType.BAG:
                itemUI.Bag = InventoryManager.Instance.inventoryData;
                break;
            case SlotType.WEAPON:
            {
                itemUI.Bag = InventoryManager.Instance.equipmentData;
                GameObject playerUnit = GameManager.Instance != null ? GameManager.Instance.PlayerUnit : null;
                if (itemUI.Bag.items[itemUI.Index].itemData != null)
                {
                    CharEquipmentRuntime.ChangeWeapon(playerUnit, itemUI.Bag.items[itemUI.Index].itemData);
                }
                else
                {
                    CharEquipmentRuntime.UnEquipWeapon(playerUnit);
                }
                break;
            }
            case SlotType.ARMOR:
                break;
            case SlotType.ACTION:
                itemUI.Bag = InventoryManager.Instance.actionData;
                break;
        }

        var item = itemUI.Bag.items[itemUI.Index];
        itemUI.SetUpItemUI(item.itemData,item.amount);
    }
}
