using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateManager : MonoBehaviour
{
    public event Action<float, float> UpdateHP;

    public ActorManager am;
    public CharacterData_SO templateData;
    public CharacterData_SO characterData;
    public AttackData_SO attackData;
    private AttackData_SO baseAttackData;
    public bool isCritical;
    public bool isDead;

    [Header("Weapon")]
    [Tooltip("Right-hand weapon mount.")]
    public Transform rightHandSlot;
    [Tooltip("Left-hand weapon mount.")]
    public Transform leftHandSlot;

    [Header("Statuses")]
    [SerializeField] private readonly HashSet<EStatusType> _activeStatuses = new HashSet<EStatusType>();
    private readonly Dictionary<EStatusType, Coroutine> _statusCoroutines = new Dictionary<EStatusType, Coroutine>();
    private int _controlLockCount;
    private int _damageImmuneCount;
    private CharStatusCtrl _charStatusCtrl;
    private CharActionCtrl _charActionCtrl;
    private CharWeaponCtrl _charWeaponCtrl;
    private CharBlackBoard _blackBoard;
    private CharBlackBoardInitializer _blackBoardInitializer;

    [Header("Hit React")]
    [SerializeField] private bool _breakOnDmg = true;
    [SerializeField] private bool _useHitReact = true;
    [SerializeField] private float _hitReactDur = 0.2f;
    [SerializeField] private bool _hitReactLockMove = true;
    [SerializeField] private bool _hitReactLockRotate;
    [SerializeField] private string _hitReactAnimKey = "";

    public bool IsStunned => HasTag(CharStateTag.Stun) || _activeStatuses.Contains(EStatusType.Stunned);
    public bool IsRooted => HasTag(CharStateTag.Root) || _activeStatuses.Contains(EStatusType.Rooted);
    public bool IsSilenced => HasTag(CharStateTag.Silence) || _activeStatuses.Contains(EStatusType.Silenced);
    public bool IsInvulnerable
    {
        get
        {
            if (_blackBoard != null && _blackBoard.Features.useCombat)
            {
                return HasTag(CharStateTag.Invul) || _blackBoard.Combat.damageImmuneCount > 0;
            }

            return HasTag(CharStateTag.Invul) ||
                   _activeStatuses.Contains(EStatusType.Invulnerable) ||
                   _damageImmuneCount > 0;
        }
    }

    public bool CanMove
    {
        get
        {
            if (_blackBoard != null)
            {
                return CanMoveByState() && !_blackBoard.Action.isControlLocked;
            }

            return CanMoveByState() && _controlLockCount <= 0;
        }
    }

    public bool CanCastSkills
    {
        get
        {
            if (_blackBoard != null)
            {
                return CanCastByState() && !_blackBoard.Action.isControlLocked;
            }

            return CanCastByState() && _controlLockCount <= 0;
        }
    }

    private void Awake()
    {
        // Disabled StateManager should not continue bootstrapping legacy runtime
        // data, otherwise it still competes with blackboard-only initialization.
        if (!enabled)
        {
            return;
        }

        _charStatusCtrl = GetComponent<CharStatusCtrl>();
        _charActionCtrl = GetComponent<CharActionCtrl>();
        _charWeaponCtrl = GetComponent<CharWeaponCtrl>();
        _blackBoard = GetComponent<CharBlackBoard>();
        _blackBoardInitializer = GetComponent<CharBlackBoardInitializer>();

        if (templateData != null)
        {
            characterData = Instantiate(templateData);
        }

        EnsureAttackDataRuntime();

        BootstrapBlackBoardRuntime();
        SyncBlackBoardRuntime();
    }

    private void Update()
    {
        isDead = ResolveIsDead();
        if (!isDead && HasHealthData())
        {
            UpdateHP?.Invoke(HitPoint, MaxHitPoint);
        }

        SyncBlackBoardRuntime();
    }

    public void ApplyStatus(EStatusType status, float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        if (_blackBoard != null && !_blackBoard.Features.useStatus)
        {
            return;
        }

        CharStatusCtrl statusCtrl = GetCharStatusCtrl();
        if (statusCtrl != null && statusCtrl.ApplyStatus(status, duration, gameObject, this))
        {
            return;
        }

        _activeStatuses.Add(status);

        if (_statusCoroutines.TryGetValue(status, out Coroutine runningCoroutine) && runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
        }

        _statusCoroutines[status] = StartCoroutine(StatusCoroutine(status, duration));
        SyncBlackBoardRuntime();
    }

    public void RemoveStatus(EStatusType status)
    {
        if (_blackBoard != null && !_blackBoard.Features.useStatus)
        {
            return;
        }

        if (_statusCoroutines.TryGetValue(status, out Coroutine runningCoroutine) && runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            _statusCoroutines.Remove(status);
        }

        _activeStatuses.Remove(status);
        SyncBlackBoardRuntime();
    }

    public void PushControlLock()
    {
        if (_blackBoard != null)
        {
            _blackBoard.Action.controlLockCount++;
            _blackBoard.Action.isControlLocked = _blackBoard.Action.controlLockCount > 0;
            _controlLockCount = _blackBoard.Action.controlLockCount;
            return;
        }

        _controlLockCount++;
        SyncBlackBoardRuntime();
    }

    public void PopControlLock()
    {
        if (_blackBoard != null)
        {
            _blackBoard.Action.controlLockCount = Mathf.Max(0, _blackBoard.Action.controlLockCount - 1);
            _blackBoard.Action.isControlLocked = _blackBoard.Action.controlLockCount > 0;
            _controlLockCount = _blackBoard.Action.controlLockCount;
            return;
        }

        _controlLockCount = Mathf.Max(0, _controlLockCount - 1);
        SyncBlackBoardRuntime();
    }

    public void PushDamageImmune()
    {
        if (_blackBoard != null && _blackBoard.Features.useCombat)
        {
            _blackBoard.Combat.damageImmuneCount++;
            _damageImmuneCount = _blackBoard.Combat.damageImmuneCount;
            return;
        }

        _damageImmuneCount++;
        SyncBlackBoardRuntime();
    }

    public void PopDamageImmune()
    {
        if (_blackBoard != null && _blackBoard.Features.useCombat)
        {
            _blackBoard.Combat.damageImmuneCount = Mathf.Max(0, _blackBoard.Combat.damageImmuneCount - 1);
            _damageImmuneCount = _blackBoard.Combat.damageImmuneCount;
            return;
        }

        _damageImmuneCount = Mathf.Max(0, _damageImmuneCount - 1);
        SyncBlackBoardRuntime();
    }

    private IEnumerator StatusCoroutine(EStatusType status, float duration)
    {
        yield return new WaitForSeconds(duration);
        _statusCoroutines.Remove(status);
        _activeStatuses.Remove(status);
        SyncBlackBoardRuntime();
    }

    public float MaxHitPoint
    {
        get
        {
            if (_blackBoard != null)
            {
                return _blackBoard.Features.useResources && _blackBoard.Resources.hasHealth
                    ? _blackBoard.Resources.maxHp
                    : 0f;
            }

            return characterData != null ? characterData.MaxHitPoint : 0f;
        }
        set
        {
            float finalValue = Mathf.Max(0f, value);
            if (_blackBoard != null)
            {
                if (!_blackBoard.Features.useResources)
                {
                    return;
                }

                _blackBoard.Resources.hasHealth = true;
                _blackBoard.Resources.maxHp = finalValue;
                if (_blackBoard.Resources.hp > finalValue)
                {
                    _blackBoard.Resources.hp = finalValue;
                }
            }

            if (_blackBoard == null && characterData != null)
            {
                characterData.MaxHitPoint = finalValue;
                if (characterData.HitPoint > finalValue)
                {
                    characterData.HitPoint = finalValue;
                }
            }
        }
    }

    public float HitPoint
    {
        get
        {
            if (_blackBoard != null)
            {
                return _blackBoard.Features.useResources && _blackBoard.Resources.hasHealth
                    ? _blackBoard.Resources.hp
                    : 0f;
            }

            return characterData != null ? characterData.HitPoint : 0f;
        }
        set
        {
            float maxHp = MaxHitPoint;
            float finalValue = maxHp > 0f ? Mathf.Clamp(value, 0f, maxHp) : Mathf.Max(0f, value);

            if (_blackBoard != null)
            {
                if (!_blackBoard.Features.useResources)
                {
                    return;
                }

                _blackBoard.Resources.hasHealth = true;
                _blackBoard.Resources.hp = finalValue;
                _blackBoard.Action.isDead = finalValue <= 0f;
            }

            if (_blackBoard == null && characterData != null)
            {
                characterData.HitPoint = finalValue;
            }

            isDead = finalValue <= 0f;
        }
    }

    public void TakeDamage(StateManager attacker, StateManager defender)
    {
        if (defender == null || defender.IsInvulnerable)
        {
            return;
        }

        float damage = attacker != null ? attacker.GetBasicAttackDamage() : 0f;
        ApplyDamage(damage);
    }

    public void TakeDamage(float damageAmount)
    {
        if (IsInvulnerable)
        {
            return;
        }

        ApplyDamage(damageAmount);
    }

    public float GetBasicAttackDamage()
    {
        float coreDamage = ResolveBaseAttackDamage();
        if (isCritical)
        {
            float criticalDamage = ResolveCriticalAttackDamage();
            if (criticalDamage > 0f)
            {
                coreDamage = criticalDamage;
            }

            Debug.Log("Critical damage: " + coreDamage);
        }

        return Mathf.Max(coreDamage, 0f);
    }

    public void EquipWeapon(ItemData_SO weapon)
    {
        if (weapon == null)
        {
            return;
        }

        GameObject weaponInstance = null;
        Transform slot = GetWeaponSlot(weapon.weaponSlotType);
        if (weapon.weaponPrefab != null && slot != null)
        {
            ClearWeaponSlot(slot);
            weaponInstance = Instantiate(weapon.weaponPrefab, slot);
            BindWeaponOwnership(weaponInstance);
        }

        EnsureAttackDataRuntime(weapon != null ? weapon.weaponData : null);

        if (attackData != null && weapon.weaponData != null)
        {
            attackData.ApplyWeaponData(weapon.weaponData);
        }

        CharWeaponCtrl weaponCtrl = GetCharWeaponCtrl();
        weaponCtrl?.SetWeapon(weapon.weaponType);
        if (weaponInstance != null)
        {
            weaponCtrl?.BindWeaponRoot(weaponInstance.transform);
        }

        SyncBlackBoardRuntime();
    }

    public void UnEquipWeapon()
    {
        GetCharWeaponCtrl()?.ClearWeaponRoot();
        ClearWeaponSlot(rightHandSlot);
        ClearWeaponSlot(leftHandSlot);

        EnsureAttackDataRuntime();

        if (baseAttackData != null && attackData != null)
        {
            attackData.ApplyWeaponData(baseAttackData);
        }

        GetCharWeaponCtrl()?.SetWeapon(WeaponType.None);
        SyncBlackBoardRuntime();
    }

    public void ChangeWeapon(ItemData_SO weapon)
    {
        UnEquipWeapon();
        EquipWeapon(weapon);
    }

    public void ApplyHealth(int amount)
    {
        if (!HasHealthData())
        {
            return;
        }

        HitPoint = Mathf.Min(HitPoint + amount, MaxHitPoint);
        SyncBlackBoardRuntime();
    }

    private bool HasTag(CharStateTag tag)
    {
        if (_blackBoard != null && _blackBoard.Features.useStatus)
        {
            return (_blackBoard.Status.snapshot.tags & tag) == tag;
        }

        CharStatusCtrl statusCtrl = GetCharStatusCtrl();
        return statusCtrl != null && statusCtrl.HasTag(tag);
    }

    private void ApplyDamage(float damageAmount)
    {
        if (!HasHealthData())
        {
            return;
        }

        float actualDamage = Mathf.Max(damageAmount, 0f) * GetDamageTakenMultiplier();
        if (actualDamage <= 0f)
        {
            return;
        }

        CharStatusCtrl statusCtrl = GetCharStatusCtrl();
        if (_breakOnDmg && statusCtrl != null)
        {
            statusCtrl.NotifyBreakByDmg();
        }

        HitPoint = Mathf.Max(HitPoint - actualDamage, 0f);
        UpdateHP?.Invoke(HitPoint, MaxHitPoint);
        SyncBlackBoardRuntime();

        if (HitPoint > 0f)
        {
            TryStartHitReact();
        }
    }

    private void TryStartHitReact()
    {
        if (!_useHitReact || _charActionCtrl == null || _hitReactDur <= 0f)
        {
            return;
        }

        // Keep hit-react lightweight: one request plus an optional animation key.
        CharActionReq req = new CharActionReq
        {
            type = CharActionType.HitReact,
            state = CharActionState.HitReact,
            src = this,
            dur = _hitReactDur,
            lockMove = _hitReactLockMove,
            lockRotate = _hitReactLockRotate,
            interruptible = true,
            animKey = _hitReactAnimKey,
        };

        _charActionCtrl.TryStart(req);
    }

    private bool CanMoveByState()
    {
        if (_blackBoard != null && _blackBoard.Features.useStatus)
        {
            return _blackBoard.Status.snapshot.canMove;
        }

        CharStatusCtrl statusCtrl = GetCharStatusCtrl();
        if (statusCtrl != null)
        {
            return statusCtrl.Snap.canMove;
        }

        return !_activeStatuses.Contains(EStatusType.Stunned) && !_activeStatuses.Contains(EStatusType.Rooted);
    }

    private bool CanCastByState()
    {
        if (_blackBoard != null && _blackBoard.Features.useStatus)
        {
            return _blackBoard.Status.snapshot.canCast;
        }

        CharStatusCtrl statusCtrl = GetCharStatusCtrl();
        if (statusCtrl != null)
        {
            return statusCtrl.Snap.canCast;
        }

        return !_activeStatuses.Contains(EStatusType.Stunned) && !_activeStatuses.Contains(EStatusType.Silenced);
    }

    private CharStatusCtrl GetCharStatusCtrl()
    {
        if (_charStatusCtrl == null)
        {
            _charStatusCtrl = GetComponent<CharStatusCtrl>();
            if (_charStatusCtrl == null)
            {
                _charStatusCtrl = GetComponentInParent<CharStatusCtrl>();
            }
        }

        return _charStatusCtrl;
    }

    private CharWeaponCtrl GetCharWeaponCtrl()
    {
        if (_charWeaponCtrl == null)
        {
            _charWeaponCtrl = GetComponent<CharWeaponCtrl>();
        }

        return _charWeaponCtrl;
    }

    private Transform GetWeaponSlot(WeaponSlotType slotType)
    {
        return slotType == WeaponSlotType.LeftHand ? leftHandSlot : rightHandSlot;
    }

    private void ClearWeaponSlot(Transform slot)
    {
        if (slot == null || slot.childCount == 0)
        {
            return;
        }

        for (int i = slot.childCount - 1; i >= 0; i--)
        {
            Destroy(slot.GetChild(i).gameObject);
        }
    }

    private void BindWeaponOwnership(GameObject weaponInstance)
    {
        if (weaponInstance == null)
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

            rootInfo.owner = gameObject;
            return;
        }

        for (int i = 0; i < weaponInfos.Length; i++)
        {
            if (weaponInfos[i] != null)
            {
                weaponInfos[i].owner = gameObject;
            }
        }
    }

    private void SyncBlackBoardRuntime()
    {
        if (_blackBoard == null)
        {
            return;
        }

        // Resources now prefer blackboard authority. Legacy scriptable data is kept
        // in sync so old UI/save code can keep reading the same fields.
        _blackBoard.SyncFromScene();
        _controlLockCount = _blackBoard.Action.controlLockCount;
        if (_blackBoard.Features.useCombat)
        {
            _damageImmuneCount = _blackBoard.Combat.damageImmuneCount;
        }

        if (_blackBoard.Features.useResources && HasHealthData())
        {
            _blackBoard.Resources.hasHealth = true;
            _blackBoard.Resources.hp = HitPoint;
            _blackBoard.Resources.maxHp = MaxHitPoint;

            if (characterData != null)
            {
                characterData.HitPoint = _blackBoard.Resources.hp;
                characterData.MaxHitPoint = _blackBoard.Resources.maxHp;
            }
        }

        if (_blackBoard.Features.useCombat && attackData != null)
        {
            _blackBoard.Combat.attackPower = attackData.minDamage;
            _blackBoard.Combat.criticalAttackPower = attackData.maxDamage;
            _blackBoard.Combat.attackSpeed = attackData.attackSpeed;
            _blackBoard.Combat.attackRange = attackData.attackRange;
            _blackBoard.Combat.maxAttackRange = attackData.maxAttackRange;
            _blackBoard.Combat.attackCooldown = attackData.coolDown;
            _blackBoard.Combat.isCritical = isCritical;
        }

        _blackBoard.Action.controlLockCount = _controlLockCount;
        _blackBoard.Action.isControlLocked = _controlLockCount > 0;
        _blackBoard.Action.isDead = ResolveIsDead();
        _blackBoard.Motion.canMove = CanMove;
        if (_blackBoard.Features.useCombat)
        {
            _blackBoard.Combat.damageImmuneCount = _damageImmuneCount;
        }
        if (!_blackBoard.Features.useStatus)
        {
            _blackBoard.Motion.canRotate = true;
        }

        CharBlackBoardChangeMask changeMask =
            CharBlackBoardChangeMask.Transform |
            CharBlackBoardChangeMask.Motion |
            CharBlackBoardChangeMask.Action;

        if (_blackBoard.Features.useResources)
        {
            changeMask |= CharBlackBoardChangeMask.Resources;
        }

        if (_blackBoard.Features.useCombat)
        {
            changeMask |= CharBlackBoardChangeMask.Combat;
        }

        _blackBoard.MarkRuntimeChanged(changeMask);
    }

    private bool ResolveIsDead()
    {
        if (HasHealthData())
        {
            return HitPoint <= 0f;
        }

        if (_blackBoard != null && _blackBoard.Features.useResources && _blackBoard.Resources.hasHealth)
        {
            return _blackBoard.Resources.hp <= 0f;
        }

        return isDead;
    }

    private float GetDamageTakenMultiplier()
    {
        if (_blackBoard != null && _blackBoard.Features.useCombat)
        {
            return Mathf.Max(0f, _blackBoard.Combat.damageTakenMul);
        }

        CharStatusCtrl statusCtrl = GetCharStatusCtrl();
        if (statusCtrl != null)
        {
            return Mathf.Max(0f, statusCtrl.Snap.dmgTakenMul);
        }

        return 1f;
    }

    private void BootstrapBlackBoardRuntime()
    {
        if (_blackBoard == null)
        {
            return;
        }

        if (_blackBoardInitializer != null)
        {
            _blackBoardInitializer.Initialize(this);
            return;
        }

        _blackBoard.SyncFromScene();

        if (_blackBoard.Features.useResources && characterData != null)
        {
            _blackBoard.Resources.hasHealth = true;
            _blackBoard.Resources.maxHp = Mathf.Max(0f, characterData.MaxHitPoint);
            _blackBoard.Resources.hp = Mathf.Clamp(characterData.HitPoint, 0f, _blackBoard.Resources.maxHp);
        }

        if (_blackBoard.Features.useCombat && attackData != null)
        {
            _blackBoard.Combat.attackPower = attackData.minDamage;
            _blackBoard.Combat.criticalAttackPower = attackData.maxDamage;
            _blackBoard.Combat.attackSpeed = attackData.attackSpeed;
            _blackBoard.Combat.attackRange = attackData.attackRange;
            _blackBoard.Combat.maxAttackRange = attackData.maxAttackRange;
            _blackBoard.Combat.attackCooldown = attackData.coolDown;
            _blackBoard.Combat.isCritical = isCritical;
        }
    }

    private void EnsureAttackDataRuntime(AttackData_SO fallbackAttackData = null)
    {
        if (attackData == null)
        {
            AttackData_SO source = ResolveAttackDataSource(fallbackAttackData);
            if (source != null)
            {
                attackData = Instantiate(source);
            }
        }

        if (baseAttackData == null)
        {
            AttackData_SO baseSource = ResolveAttackDataSource(null);
            if (baseSource != null)
            {
                baseAttackData = Instantiate(baseSource);
            }
            else if (attackData != null)
            {
                baseAttackData = Instantiate(attackData);
            }
        }
    }

    private AttackData_SO ResolveAttackDataSource(AttackData_SO fallbackAttackData)
    {
        if (attackData != null)
        {
            return attackData;
        }

        if (baseAttackData != null)
        {
            return baseAttackData;
        }

        if (_blackBoardInitializer == null)
        {
            _blackBoardInitializer = GetComponent<CharBlackBoardInitializer>();
        }

        if (_blackBoardInitializer != null && _blackBoardInitializer.AttackTemplate != null)
        {
            return _blackBoardInitializer.AttackTemplate;
        }

        return fallbackAttackData;
    }

    private bool HasHealthData()
    {
        if (_blackBoard != null)
        {
            return _blackBoard.Features.useResources && _blackBoard.Resources.hasHealth;
        }

        return characterData != null;
    }

    private float ResolveBaseAttackDamage()
    {
        if (_blackBoard != null)
        {
            return _blackBoard.Features.useCombat ? _blackBoard.Combat.attackPower : 0f;
        }

        return attackData != null ? attackData.minDamage : 0f;
    }

    private float ResolveCriticalAttackDamage()
    {
        if (_blackBoard != null)
        {
            if (!_blackBoard.Features.useCombat)
            {
                return 0f;
            }

            if (_blackBoard.Combat.criticalAttackPower > 0f)
            {
                return _blackBoard.Combat.criticalAttackPower;
            }
        }

        if (attackData != null)
        {
            return attackData.maxDamage;
        }

        return ResolveBaseAttackDamage();
    }
}
