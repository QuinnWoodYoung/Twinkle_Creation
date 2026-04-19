using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CharMeleeSlashVfxCtrl : MonoBehaviour
{
    [System.Serializable]
    private enum Axis
    {
        X = 0,
        Y = 1,
        Z = 2,
    }

    [System.Serializable]
    private sealed class SlashSpawnPoint
    {
        public string name = "Spawn Point";
        [Tooltip("Which slash VFX type to spawn from the weapon attack data, for example Fan or Thrust.")]
        public string vfxTypeId = "Fan";
        public Transform mount;
        public bool parentToMount = true;
        public Vector3 localPositionOffset;
        public Vector3 localEulerOffset;
        [Tooltip("Rotate this spawned VFX by 180 degrees around the selected axis.")]
        public bool rotate180;
        public Axis rotateAxis = Axis.Y;
    }

    [System.Serializable]
    private sealed class SlashStagePoint
    {
        public string name = "Attack 1";
        public SlashSpawnPoint[] spawnPoints;
    }

    [System.Serializable]
    private sealed class WeaponSlashLayout
    {
        public WeaponType weaponType = WeaponType.None;
        public SlashStagePoint[] stages;
    }

    [SerializeField] private CharWeaponCtrl _weaponCtrl;
    [SerializeField] private CharAnimCtrl _animCtrl;
    [SerializeField] private CharActionCtrl _actionCtrl;
    [SerializeField] private WeaponSlashLayout[] _weaponLayouts;
    [SerializeField] private bool _debugLog;

    private WeaponSlashLayout _activeLayout;
    private readonly List<GameObject> _activeSlashInstances = new List<GameObject>();

    private void Awake()
    {
        CacheRefs();
        EnsureAnimEventRelay();
        RefreshActiveLayout();
    }

    private void OnEnable()
    {
        CacheRefs();
        EnsureAnimEventRelay();

        if (_weaponCtrl != null)
        {
            _weaponCtrl.WeaponChanged += OnWeaponChanged;
        }

        if (_actionCtrl != null)
        {
            _actionCtrl.ActionEnd += OnActionEnd;
            _actionCtrl.ActionIntd += OnActionInterrupted;
        }

        RefreshActiveLayout();
    }

    private void OnDisable()
    {
        if (_weaponCtrl != null)
        {
            _weaponCtrl.WeaponChanged -= OnWeaponChanged;
        }

        if (_actionCtrl != null)
        {
            _actionCtrl.ActionEnd -= OnActionEnd;
            _actionCtrl.ActionIntd -= OnActionInterrupted;
        }

        HideMeleeSlash();
    }

    public void ShowMeleeSlash()
    {
        ShowMeleeSlashInternal(0);
    }

    public void ShowMeleeSlashStage(int stageNumber)
    {
        ShowMeleeSlashInternal(stageNumber);
    }

    private void ShowMeleeSlashInternal(int stageNumber)
    {
        if (_weaponCtrl == null || Weapon.IsRangedWeapon(_weaponCtrl.CurWeapon))
        {
            return;
        }

        AttackData_SO attackData = CharResourceResolver.GetAttackData(gameObject);
        if (attackData == null)
        {
            return;
        }

        if (!RefreshActiveLayout())
        {
            return;
        }

        SlashStagePoint stagePoint = ResolveStagePoint(stageNumber);
        if (stagePoint == null)
        {
            return;
        }

        SlashSpawnPoint[] spawnPoints = stagePoint.spawnPoints;
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        HideMeleeSlash();

        int spawnedCount = 0;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            SlashSpawnPoint spawnPoint = spawnPoints[i];
            if (spawnPoint == null)
            {
                continue;
            }

            GameObject slashPrefab = attackData.ResolveMeleeSlashVfx(spawnPoint.vfxTypeId);
            if (slashPrefab == null)
            {
                continue;
            }

            SpawnSlashVfx(slashPrefab, spawnPoint);
            spawnedCount++;
        }

        if (_debugLog)
        {
            Debug.Log($"[CharMeleeSlashVfxCtrl] Show weapon={_weaponCtrl.CurWeapon} requestedStage={stageNumber} spawned={spawnedCount}", this);
        }
    }

    public void HideMeleeSlash()
    {
        for (int i = 0; i < _activeSlashInstances.Count; i++)
        {
            if (_activeSlashInstances[i] != null)
            {
                Destroy(_activeSlashInstances[i]);
            }
        }

        _activeSlashInstances.Clear();
    }

    public void RefreshMeleeSlashVfx()
    {
        RefreshActiveLayout();
    }

    private void OnWeaponChanged(WeaponType weaponType)
    {
        HideMeleeSlash();
        RefreshActiveLayout();
    }

    private void OnActionEnd(CharActionReq req)
    {
        if (req != null && req.type == CharActionType.Atk)
        {
            HideMeleeSlash();
        }
    }

    private void OnActionInterrupted(CharActionReq req, string reason)
    {
        if (req != null && req.type == CharActionType.Atk)
        {
            HideMeleeSlash();
        }
    }

    private SlashStagePoint ResolveStagePoint(int stageNumber)
    {
        if (_activeLayout == null || _activeLayout.stages == null || _activeLayout.stages.Length == 0)
        {
            return null;
        }

        int slotIndex = stageNumber > 0
            ? stageNumber - 1
            : (_weaponCtrl != null ? _weaponCtrl.ActiveMeleeComboStageIndex : 0);

        if (slotIndex < 0)
        {
            slotIndex = 0;
        }

        slotIndex = Mathf.Clamp(slotIndex, 0, _activeLayout.stages.Length - 1);
        return _activeLayout.stages[slotIndex];
    }

    private bool RefreshActiveLayout()
    {
        WeaponSlashLayout nextLayout = null;
        WeaponType currentWeapon = _weaponCtrl != null ? _weaponCtrl.CurWeapon : WeaponType.None;

        if (_weaponLayouts != null)
        {
            for (int i = 0; i < _weaponLayouts.Length; i++)
            {
                WeaponSlashLayout candidate = _weaponLayouts[i];
                if (candidate != null && candidate.weaponType == currentWeapon)
                {
                    nextLayout = candidate;
                    break;
                }
            }
        }

        _activeLayout = nextLayout;
        return _activeLayout != null;
    }

    private void CacheRefs()
    {
        if (_weaponCtrl == null)
        {
            _weaponCtrl = GetComponent<CharWeaponCtrl>();
        }

        if (_animCtrl == null)
        {
            _animCtrl = GetComponent<CharAnimCtrl>();
        }

        if (_actionCtrl == null)
        {
            _actionCtrl = GetComponent<CharActionCtrl>();
        }
    }

    private void EnsureAnimEventRelay()
    {
        Animator bodyAnim = _animCtrl != null ? _animCtrl.BodyAnim : null;
        if (bodyAnim == null)
        {
            bodyAnim = GetComponentInChildren<Animator>(true);
            if (bodyAnim == null)
            {
                return;
            }
        }

        CharAnimEventRelay relay = bodyAnim.GetComponent<CharAnimEventRelay>();
        if (relay == null)
        {
            relay = bodyAnim.gameObject.AddComponent<CharAnimEventRelay>();
        }

        relay.Bind(this);
    }

    private void SpawnSlashVfx(GameObject prefab, SlashSpawnPoint spawnPoint)
    {
        if (prefab == null || spawnPoint == null)
        {
            return;
        }

        Transform mount = spawnPoint.mount != null ? spawnPoint.mount : transform;
        Quaternion localRotation = Quaternion.Euler(spawnPoint.localEulerOffset) * ResolveRotate180(spawnPoint.rotate180, spawnPoint.rotateAxis);
        Quaternion worldRotation = mount.rotation * localRotation;
        GameObject instance;

        if (spawnPoint.parentToMount)
        {
            instance = Instantiate(prefab, mount);
            instance.transform.localPosition = spawnPoint.localPositionOffset;
            instance.transform.localRotation = localRotation;
        }
        else
        {
            Vector3 worldPosition = mount.TransformPoint(spawnPoint.localPositionOffset);
            instance = Instantiate(prefab, worldPosition, worldRotation);
        }

        _activeSlashInstances.Add(instance);
    }

    private static Quaternion ResolveRotate180(bool rotate180, Axis axis)
    {
        if (!rotate180)
        {
            return Quaternion.identity;
        }

        Vector3 euler;
        switch (axis)
        {
            case Axis.X:
                euler = new Vector3(180f, 0f, 0f);
                break;

            case Axis.Y:
                euler = new Vector3(0f, 180f, 0f);
                break;

            default:
                euler = new Vector3(0f, 0f, 180f);
                break;
        }

        return Quaternion.Euler(euler);
    }

}
