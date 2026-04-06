using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickUp : MonoBehaviour
{

    public ItemData_SO itemData;
    private bool isPlayerNearby = false;


    private void OnTriggerEnter(Collider other)
    {
        // 检测是否与玩家相交
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 检测是否与玩家分离
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
        }
    }

    private void Update()
    {
        if (isPlayerNearby && FindObjectOfType<PlayerInput>().pickupKeyPressed)
        {
            // 在此处执行拾取物体的操作
            InventoryManager.Instance.inventoryData.AddItem(itemData, itemData.itemCount);
            InventoryManager.Instance.inventoryUI.RefreshUI();
            Destroy(gameObject); 
            Debug.Log("拾取物体");
        }
    }
}
