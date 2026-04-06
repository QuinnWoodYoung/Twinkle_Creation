using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionButton : MonoBehaviour
{
    public KeyCode actionKey;

    private SlotHolder currentSlotHolder;

    void Awake()
    {
        currentSlotHolder = GetComponent<SlotHolder>();
    }

    void Update()
    {
        if (Input.GetKeyDown(actionKey) && currentSlotHolder.itemUI.GetItem())
        {
            currentSlotHolder.UseItem();
        }
    }
}
