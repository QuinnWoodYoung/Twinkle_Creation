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
        if (stateManager != null)
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
        if (stateManager != null)
        {
            stateManager.UnEquipWeapon();
            return;
        }

        ApplyBlackBoardOnlyWeapon(unit, null);
    }

    private static void ApplyBlackBoardOnlyWeapon(GameObject unit, ItemData_SO weapon)
    {
        CharWeaponCtrl weaponCtrl = unit.GetComponent<CharWeaponCtrl>();
        CharBlackBoard blackBoard = unit.GetComponent<CharBlackBoard>();
        WeaponType weaponType = weapon != null ? weapon.weaponType : WeaponType.None;

        if (weaponCtrl != null)
        {
            weaponCtrl.SetWeapon(weaponType);
            if (weapon == null)
            {
                weaponCtrl.ClearWeaponRoot();
            }
        }

        if (blackBoard == null || !blackBoard.Features.useEquipment)
        {
            return;
        }

        blackBoard.Equipment.weaponType = weaponType;
        if (weapon == null)
        {
            blackBoard.Equipment.weaponRoot = null;
        }
    }
}
