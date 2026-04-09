using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CharTestAutoEquip : MonoBehaviour
{
    [Header("Test Auto Equip")]
    [Tooltip("Weapon ItemData equipped on Start. Assign an ItemData_SO weapon asset here.")]
    [SerializeField] private ItemData_SO _weaponItem;
    [Tooltip("Auto equip on Start.")]
    [SerializeField] private bool _equipOnStart = true;
    [Tooltip("Optional delay before auto equip.")]
    [SerializeField] private float _equipDelay = 0f;

    private bool _equipped;

    private IEnumerator Start()
    {
        if (!_equipOnStart)
        {
            yield break;
        }

        if (_equipDelay > 0f)
        {
            yield return new WaitForSeconds(_equipDelay);
        }

        EquipNow();
    }

    [ContextMenu("Equip Now")]
    public void EquipNow()
    {
        if (_weaponItem == null)
        {
            Debug.LogWarning($"{name}: CharTestAutoEquip has no weaponItem assigned.", this);
            return;
        }

        CharEquipmentRuntime.ChangeWeapon(gameObject, _weaponItem);
        _equipped = true;
    }

    [ContextMenu("UnEquip Now")]
    public void UnEquipNow()
    {
        CharEquipmentRuntime.UnEquipWeapon(gameObject);
        _equipped = false;
    }

    public ItemData_SO WeaponItem => _weaponItem;
    public bool Equipped => _equipped;
}
