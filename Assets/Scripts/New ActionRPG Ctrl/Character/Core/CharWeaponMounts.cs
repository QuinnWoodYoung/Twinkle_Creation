using UnityEngine;

[DisallowMultipleComponent]
public class CharWeaponMounts : MonoBehaviour
{
    [SerializeField] private Transform _rightHandSlot;
    [SerializeField] private Transform _leftHandSlot;

    public Transform RightHandSlot => _rightHandSlot;
    public Transform LeftHandSlot => _leftHandSlot;

    private void Reset()
    {
        AutoBindLegacySlots();
    }

    private void Awake()
    {
        AutoBindLegacySlots();
    }

    private void OnValidate()
    {
        AutoBindLegacySlots();
    }

    public Transform GetSlot(WeaponSlotType slotType)
    {
        return slotType == WeaponSlotType.LeftHand ? _leftHandSlot : _rightHandSlot;
    }

    public static CharWeaponMounts Resolve(GameObject owner)
    {
        if (owner == null)
        {
            return null;
        }

        CharWeaponMounts mounts = owner.GetComponent<CharWeaponMounts>();
        if (mounts != null)
        {
            mounts.AutoBindLegacySlots();
            return mounts;
        }

        StateManager legacyState = owner.GetComponent<StateManager>();
        if (legacyState == null || !Application.isPlaying)
        {
            return null;
        }

        mounts = owner.AddComponent<CharWeaponMounts>();
        mounts.AutoBindLegacySlots();
        return mounts;
    }

    private void AutoBindLegacySlots()
    {
        StateManager legacyState = GetComponent<StateManager>();
        if (legacyState == null)
        {
            return;
        }

        if (_rightHandSlot == null)
        {
            _rightHandSlot = legacyState.rightHandSlot;
        }

        if (_leftHandSlot == null)
        {
            _leftHandSlot = legacyState.leftHandSlot;
        }
    }
}
