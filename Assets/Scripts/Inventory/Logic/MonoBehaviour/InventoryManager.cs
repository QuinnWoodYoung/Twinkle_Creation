using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : Singleton<InventoryManager>
{
    public class DragData
    {
        public SlotHolder originalHolder;
        public RectTransform originalParent;
    }
    [Header("Inventory Data")]

    public InventoryData_SO inventoryTemplate;

    public InventoryData_SO actionTemplate;

    public InventoryData_SO equipmentTemplate;

    public InventoryData_SO inventoryData;

    public InventoryData_SO actionData;

    public InventoryData_SO equipmentData;

    [Header("Containers")]
    public ContrainerUI inventoryUI;
    public ContrainerUI actionUI;
    public ContrainerUI equipmentUI;

    [Header("DragCanvas")]
    public Canvas dragCanvas;
    public DragData currentDrag;

    [Header("UI Panel")]
    public GameObject bagPanel;

    bool isOpen = false;
    protected override void Awake()
    {
        base.Awake();
        if(inventoryTemplate != null)
        {
            inventoryData = Instantiate(inventoryTemplate);
        }
        if (actionTemplate != null)
        {
            actionData = Instantiate(actionTemplate);
        }
        if (equipmentTemplate != null)
        {
            equipmentData = Instantiate(equipmentTemplate);
        }
    }
    void Start()
    {
        LoadData();
        inventoryUI.RefreshUI();
        actionUI.RefreshUI();
        equipmentUI.RefreshUI();
    }

    void Update()
    {
        if (FindObjectOfType<PlayerInput>().OpenBag)
        {
            isOpen = !isOpen;
            bagPanel.SetActive(isOpen);
        }
        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            FindObjectOfType<PlayerInput>().AttackEnable = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            FindObjectOfType<PlayerInput>().AttackEnable = true;
        }
    }

    public void SaveData()
    {
        SaveManager.Instance.Save(inventoryData, inventoryData.name);
        SaveManager.Instance.Save(actionData, actionData.name);
        SaveManager.Instance.Save(equipmentData, equipmentData.name);
    }
    public void LoadData()
    {
        SaveManager.Instance.Load(inventoryData, inventoryData.name);
        SaveManager.Instance.Load(actionData, actionData.name);
        SaveManager.Instance.Load(equipmentData, equipmentData.name);
    }
    public bool CheckInInventoryUI(Vector3 position)
    {
        for(int i = 0;i < inventoryUI.slotHolders.Length; i++)
        {
            RectTransform t = inventoryUI.slotHolders[i].transform as RectTransform;
            if (RectTransformUtility.RectangleContainsScreenPoint(t, position))
            {
                return true;
            }
        }
        return false;
    }
    public bool CheckInActionUI(Vector3 position)
    {
        for (int i = 0; i < actionUI.slotHolders.Length; i++)
        {
            RectTransform t = actionUI.slotHolders[i].transform as RectTransform;
            if (RectTransformUtility.RectangleContainsScreenPoint(t, position))
            {
                return true;
            }
        }
        return false;
    }
    public bool CheckInEquipmentUI(Vector3 position)
    {
        for (int i = 0; i < equipmentUI.slotHolders.Length; i++)
        {
            RectTransform t = equipmentUI.slotHolders[i].transform as RectTransform;

            if (RectTransformUtility.RectangleContainsScreenPoint(t, position))
            {
                return true;
            }
        }
        return false;
    }
}
