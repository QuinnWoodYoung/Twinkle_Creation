using UnityEngine;

public static class CharEquipmentRuntime
{
    // This helper closes the last few gameplay call-sites that still reached
    // into StateManager directly for weapon changes.

    public static void ChangeWeapon(GameObject owner, ItemData_SO weapon)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(owner);
        if (unit == null)
        {
            return;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        if (stateManager != null && stateManager.enabled)
        {
            stateManager.ChangeWeapon(weapon);
            return;
        }

        ApplyBlackBoardOnlyWeapon(unit, weapon);
    }

    public static void UnEquipWeapon(GameObject owner)
    {
        GameObject unit = CharRelationResolver.NormalizeUnit(owner);
        if (unit == null)
        {
            return;
        }

        StateManager stateManager = unit.GetComponent<StateManager>();
        if (stateManager != null && stateManager.enabled)
        {
            stateManager.UnEquipWeapon();
            return;
        }

        ApplyBlackBoardOnlyWeapon(unit, null);
    }

    private static void ApplyBlackBoardOnlyWeapon(GameObject unit, ItemData_SO weapon)
    {
        CharWeaponMounts weaponMounts = CharWeaponMounts.Resolve(unit);
        CharWeaponCtrl weaponCtrl = unit.GetComponent<CharWeaponCtrl>();
        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        CharBlackBoardInitializer initializer = unit.GetComponent<CharBlackBoardInitializer>();
        WeaponType weaponType = weapon != null ? weapon.weaponType : WeaponType.None;
        Transform weaponRoot = MountWeaponPrefab(unit, weaponMounts, weapon);

        if (weaponCtrl != null)
        {
            weaponCtrl.SetWeapon(weaponType);
            if (weaponRoot != null)
            {
                weaponCtrl.BindWeaponRoot(weaponRoot);
            }
            else if (weapon == null)
            {
                weaponCtrl.ClearWeaponRoot();
            }
        }

        if (blackBoard == null)
        {
            return;
        }

        if (blackBoard.Features.useEquipment)
        {
            blackBoard.Equipment.weaponType = weaponType;
            if (weaponCtrl == null || weaponRoot != null || weapon == null)
            {
                blackBoard.Equipment.weaponRoot = weaponRoot;
            }
        }

        bool changedCombat = false;
        if (blackBoard.Features.useCombat)
        {
            if (weapon != null)
            {
                ApplyCombatProfile(blackBoard, weapon.weaponData);
                changedCombat = true;
            }
            else if (initializer != null)
            {
                initializer.ApplyCombatBaseline();
                changedCombat = true;
            }
            else
            {
                ClearCombatProfile(blackBoard);
                changedCombat = true;
            }
        }

        CharBlackBoardChangeMask changeMask = CharBlackBoardChangeMask.None;
        if (blackBoard.Features.useEquipment)
        {
            changeMask |= CharBlackBoardChangeMask.Equipment;
        }

        if (changedCombat)
        {
            changeMask |= CharBlackBoardChangeMask.Combat;
        }

        if (changeMask != CharBlackBoardChangeMask.None)
        {
            blackBoard.MarkRuntimeChanged(changeMask);
        }
    }

    private static Transform MountWeaponPrefab(GameObject unit, CharWeaponMounts weaponMounts, ItemData_SO weapon)
    {
        if (weaponMounts == null)
        {
            return null;
        }

        ClearWeaponSlot(weaponMounts.RightHandSlot);
        ClearWeaponSlot(weaponMounts.LeftHandSlot);

        if (weapon == null || weapon.weaponPrefab == null)
        {
            return null;
        }

        Transform slot = weaponMounts.GetSlot(weapon.weaponSlotType);
        if (slot == null)
        {
            return null;
        }

        GameObject weaponInstance = Object.Instantiate(weapon.weaponPrefab, slot);
        BindWeaponOwnership(unit, weaponInstance);
        return weaponInstance.transform;
    }

    private static void ApplyCombatProfile(CharBlackBoard blackBoard, AttackData_SO attackSource)
    {
        AttackData_SO runtimeAttackData = CharCombatRuntimeUtility.AssignAttackData(
            blackBoard,
            attackSource,
            true);
        CharCombatRuntimeUtility.ApplyAttackStats(blackBoard.Combat, runtimeAttackData);
    }

    private static void ClearCombatProfile(CharBlackBoard blackBoard)
    {
        CharCombatRuntimeUtility.ClearAttackData(blackBoard);
    }

    private static void ClearWeaponSlot(Transform slot)
    {
        if (slot == null || slot.childCount == 0)
        {
            return;
        }

        for (int i = slot.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(slot.GetChild(i).gameObject);
        }
    }

    private static void BindWeaponOwnership(GameObject owner, GameObject weaponInstance)
    {
        if (owner == null || weaponInstance == null)
        {
            return;
        }

        WeaponInfo[] weaponInfos = weaponInstance.GetComponentsInChildren<WeaponInfo>(true);
        if (weaponInfos == null || weaponInfos.Length == 0)
        {
            WeaponInfo rootInfo = weaponInstance.GetComponent<WeaponInfo>();
            if (rootInfo == null)
            {
                rootInfo = weaponInstance.AddComponent<WeaponInfo>();
            }

            rootInfo.owner = owner;
            return;
        }

        for (int i = 0; i < weaponInfos.Length; i++)
        {
            if (weaponInfos[i] != null)
            {
                weaponInfos[i].owner = owner;
            }
        }
    }
}
