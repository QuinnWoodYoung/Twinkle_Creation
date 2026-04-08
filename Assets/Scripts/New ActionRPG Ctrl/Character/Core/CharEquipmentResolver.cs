using UnityEngine;

public static class CharEquipmentResolver
{
    // Equipment lookup prefers blackboard and current visual systems. Legacy
    // state slots remain as a compatibility fallback in one place.

    public static Transform ResolveWeaponRoot(
        GameObject owner,
        CharBlackBoard blackBoard,
        WeaponVisualCtrl weaponVisualCtrl,
        CharAnimCtrl animCtrl,
        WeaponType weaponType)
    {
        if (owner == null)
        {
            return null;
        }

        if (blackBoard != null && blackBoard.Features.useEquipment && blackBoard.Equipment.weaponRoot != null)
        {
            return blackBoard.Equipment.weaponRoot;
        }

        if (weaponVisualCtrl != null)
        {
            Transform visualRoot = weaponVisualCtrl.GetWeaponRoot(weaponType);
            if (visualRoot != null)
            {
                return visualRoot;
            }
        }

        StateManager stateManager = owner.GetComponent<StateManager>();
        if (stateManager != null)
        {
            Transform slotRoot = GetActiveChild(stateManager.rightHandSlot);
            if (slotRoot != null) return slotRoot;

            slotRoot = GetActiveChild(stateManager.leftHandSlot);
            if (slotRoot != null) return slotRoot;

            slotRoot = GetAnyChild(stateManager.rightHandSlot);
            if (slotRoot != null) return slotRoot;

            slotRoot = GetAnyChild(stateManager.leftHandSlot);
            if (slotRoot != null) return slotRoot;
        }

        WeaponAnimCtrl[] weaponCtrls = owner.GetComponentsInChildren<WeaponAnimCtrl>(true);
        for (int i = 0; i < weaponCtrls.Length; i++)
        {
            WeaponAnimCtrl ctrl = weaponCtrls[i];
            if (ctrl != null && ctrl.gameObject.activeInHierarchy)
            {
                return ctrl.transform;
            }
        }

        Animator bodyAnimator = animCtrl != null ? animCtrl.BodyAnim : null;
        Animator[] animators = owner.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator != null && animator != bodyAnimator && animator.gameObject.activeInHierarchy)
            {
                return animator.transform;
            }
        }

        return null;
    }

    private static Transform GetActiveChild(Transform slot)
    {
        if (slot == null || slot.childCount == 0)
        {
            return null;
        }

        for (int i = 0; i < slot.childCount; i++)
        {
            Transform child = slot.GetChild(i);
            if (child != null && child.gameObject.activeInHierarchy)
            {
                return child;
            }
        }

        return null;
    }

    private static Transform GetAnyChild(Transform slot)
    {
        if (slot == null || slot.childCount == 0)
        {
            return null;
        }

        return slot.GetChild(0);
    }
}
