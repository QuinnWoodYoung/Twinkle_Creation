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
    public Transform rightHandSlot;
    public Transform leftHandSlot;

    [Header("Statuses")]
    [SerializeField] private readonly HashSet<EStatusType> _activeStatuses = new HashSet<EStatusType>();
    private readonly Dictionary<EStatusType, Coroutine> _statusCoroutines = new Dictionary<EStatusType, Coroutine>();
    private int _controlLockCount;
    private int _damageImmuneCount;

    public bool IsStunned => _activeStatuses.Contains(EStatusType.Stunned);
    public bool IsRooted => _activeStatuses.Contains(EStatusType.Rooted);
    public bool IsSilenced => _activeStatuses.Contains(EStatusType.Silenced);
    public bool IsInvulnerable => _activeStatuses.Contains(EStatusType.Invulnerable) || _damageImmuneCount > 0;
    public bool CanMove => !IsStunned && !IsRooted && _controlLockCount <= 0;
    public bool CanCastSkills => !IsStunned && !IsSilenced && _controlLockCount <= 0;

    private void Awake()
    {
        if (templateData != null)
        {
            characterData = Instantiate(templateData);
        }

        if (attackData != null)
        {
            baseAttackData = Instantiate(attackData);
        }
    }

    private void Update()
    {
        if (HitPoint <= 0f)
        {
            isDead = true;
        }
        else
        {
            UpdateHP?.Invoke(HitPoint, MaxHitPoint);
        }
    }

    public void ApplyStatus(EStatusType status, float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        _activeStatuses.Add(status);

        if (_statusCoroutines.TryGetValue(status, out Coroutine runningCoroutine) && runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
        }

        _statusCoroutines[status] = StartCoroutine(StatusCoroutine(status, duration));
    }

    public void RemoveStatus(EStatusType status)
    {
        if (_statusCoroutines.TryGetValue(status, out Coroutine runningCoroutine) && runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            _statusCoroutines.Remove(status);
        }

        _activeStatuses.Remove(status);
    }

    public void PushControlLock()
    {
        _controlLockCount++;
    }

    public void PopControlLock()
    {
        _controlLockCount = Mathf.Max(0, _controlLockCount - 1);
    }

    public void PushDamageImmune()
    {
        _damageImmuneCount++;
    }

    public void PopDamageImmune()
    {
        _damageImmuneCount = Mathf.Max(0, _damageImmuneCount - 1);
    }

    private IEnumerator StatusCoroutine(EStatusType status, float duration)
    {
        yield return new WaitForSeconds(duration);
        _statusCoroutines.Remove(status);
        _activeStatuses.Remove(status);
    }

    public float MaxHitPoint
    {
        get => characterData.MaxHitPoint;
        set => characterData.MaxHitPoint = value;
    }

    public float HitPoint
    {
        get => characterData.HitPoint;
        set => characterData.HitPoint = value;
    }

    public void TakeDamage(StateManager attacker, StateManager defender)
    {
        if (defender == null || defender.IsInvulnerable)
        {
            return;
        }

        int damage = Mathf.Max(attacker.CurrentDamage(), 0);
        HitPoint = Mathf.Max(HitPoint - damage, 0);
        UpdateHP?.Invoke(HitPoint, MaxHitPoint);
    }

    public void TakeDamage(float damageAmount)
    {
        if (IsInvulnerable)
        {
            return;
        }

        float actualDamage = Mathf.Max(damageAmount, 0f);
        HitPoint = Mathf.Max(HitPoint - actualDamage, 0f);
        UpdateHP?.Invoke(HitPoint, MaxHitPoint);
    }

    private int CurrentDamage()
    {
        float coreDamage = attackData.minDamage;

        if (isCritical)
        {
            coreDamage = attackData.maxDamage;
            Debug.Log("Critical damage: " + coreDamage);
        }

        return (int)coreDamage;
    }

    public void EquipWeapon(ItemData_SO weapon)
    {
        if (weapon.weaponPrefab != null)
        {
            Instantiate(weapon.weaponPrefab, rightHandSlot);
        }

        attackData.ApplyWeaponData(weapon.weaponData);
    }

    public void UnEquipWeapon()
    {
        if (rightHandSlot != null && rightHandSlot.childCount != 0)
        {
            for (int i = 0; i < rightHandSlot.childCount; i++)
            {
                Destroy(rightHandSlot.GetChild(i).gameObject);
            }
        }

        if (baseAttackData != null)
        {
            attackData.ApplyWeaponData(baseAttackData);
        }
    }

    public void ChangeWeapon(ItemData_SO weapon)
    {
        UnEquipWeapon();
        EquipWeapon(weapon);
    }

    public void ApplyHealth(int amount)
    {
        if (HitPoint + amount <= MaxHitPoint)
        {
            HitPoint += amount;
        }
        else
        {
            HitPoint = MaxHitPoint;
        }
    }
}
