using UnityEngine;

[DisallowMultipleComponent]
public class WeaponAnimCtrl : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("武器 prefab 自己的 Animator。留空时自动在 prefab 根节点或子物体里查找。")]
    [SerializeField] private Animator _animator;

    [Header("调试")]
    [Tooltip("当前这把武器归属于谁。运行时由外层控制器绑定。")]
    [SerializeField] private GameObject _owner;
    [Tooltip("当前武器控制器正在服务的武器类型。")]
    [SerializeField] private WeaponType _weaponType = WeaponType.None;
    [Tooltip("当前武器是否处于已装备状态。")]
    [SerializeField] private bool _equipped;

    [Header("动作 Trigger")]
    [Tooltip("装备时触发的动画。留空则不触发。")]
    [SerializeField] private string _equipTrig = "";
    [Tooltip("卸下时触发的动画。留空则不触发。")]
    [SerializeField] private string _unequipTrig = "";
    [Tooltip("默认攻击 Trigger。外层没传值时就用它。")]
    [SerializeField] private string _atkTrig = "Attack";
    [Tooltip("找不到同名 Trigger 参数时，是否回退为直接播放同名状态。")]
    [SerializeField] private bool _useStateFallback = true;
    [Tooltip("直接切状态时使用的动画层。通常保持 0。")]
    [SerializeField] private int _playLayer;
    [Tooltip("直接切状态时的过渡时间。0 表示立即切。")]
    [SerializeField] private float _crossFadeDur = 0.05f;
    [Tooltip("是否把装备状态同步到 Animator Bool。")]
    [SerializeField] private bool _useEquippedBool = true;
    [Tooltip("装备状态对应的 Animator Bool 名。")]
    [SerializeField] private string _equippedBool = "equipped";
    [Header("调试")]
    [Tooltip("开启后，会在 Console 输出武器动画触发信息。")]
    [SerializeField] private bool _debugLog = true;

    public WeaponType CurWeapon => _weaponType;
    public Animator Anim => _animator;

    private void Awake()
    {
        CacheAnimator();
    }

    private void OnValidate()
    {
        CacheAnimator();
    }

    public void Bind(GameObject owner, WeaponType weaponType)
    {
        bool sameBinding = _equipped && _owner == owner && _weaponType == weaponType;
        _owner = owner;
        _weaponType = weaponType;
        _equipped = true;
        CacheAnimator();
        ApplyEquippedState();

        if (!sameBinding)
        {
            TriggerSafe(_equipTrig);
        }

        if (_debugLog)
        {
            Debug.Log($"[WeaponAnimCtrl] Bind owner={_owner?.name} weapon={_weaponType} obj={name}", this);
        }
    }

    public void Unbind()
    {
        if (!_equipped && _owner == null && _weaponType == WeaponType.None)
        {
            return;
        }

        CacheAnimator();
        TriggerSafe(_unequipTrig);
        _equipped = false;
        ApplyEquippedState();
        _owner = null;
        _weaponType = WeaponType.None;

        if (_debugLog)
        {
            Debug.Log($"[WeaponAnimCtrl] Unbind obj={name}", this);
        }
    }

    public void PlayAtk(string trig = null)
    {
        CacheAnimator();

        string finalTrig = string.IsNullOrEmpty(trig) ? _atkTrig : trig;
        if (_debugLog)
        {
            Debug.Log($"[WeaponAnimCtrl] PlayAtk trig={finalTrig} owner={_owner?.name} weapon={_weaponType} obj={name} animator={_animator?.name}", this);
        }
        TriggerSafe(finalTrig);
    }

    private void ApplyEquippedState()
    {
        if (!_useEquippedBool || !HasAnimatorCtrl(_animator))
        {
            return;
        }

        if (!string.IsNullOrEmpty(_equippedBool) && HasParam(_animator, _equippedBool, AnimatorControllerParameterType.Bool))
        {
            _animator.SetBool(_equippedBool, _equipped);
        }
    }

    private void TriggerSafe(string trig)
    {
        if (string.IsNullOrEmpty(trig))
        {
            return;
        }

        if (!HasAnimatorCtrl(_animator))
        {
            return;
        }

        if (HasParam(_animator, trig, AnimatorControllerParameterType.Trigger))
        {
            _animator.SetTrigger(trig);
            if (_debugLog)
            {
                Debug.Log($"[WeaponAnimCtrl] SetTrigger {trig} on {_animator.name}", this);
            }
            return;
        }

        if (_useStateFallback && HasState(_animator, trig, _playLayer))
        {
            if (_crossFadeDur > 0f)
            {
                _animator.CrossFadeInFixedTime(trig, _crossFadeDur, _playLayer);
            }
            else
            {
                _animator.Play(trig, _playLayer, 0f);
            }

            if (_debugLog)
            {
                Debug.Log($"[WeaponAnimCtrl] PlayState {trig} on {_animator.name}", this);
            }
            return;
        }

        if (_debugLog)
        {
            Debug.LogWarning($"[WeaponAnimCtrl] No trigger/state named {trig} on animator {_animator?.name}", this);
        }
    }

    private void CacheAnimator()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>(true);
        }
    }

    private bool HasAnimatorCtrl(Animator animator)
    {
        return animator != null && animator.runtimeAnimatorController != null;
    }

    private bool HasParam(Animator animator, string paramName, AnimatorControllerParameterType paramType)
    {
        if (!HasAnimatorCtrl(animator) || string.IsNullOrEmpty(paramName))
        {
            return false;
        }

        for (int i = 0; i < animator.parameterCount; i++)
        {
            AnimatorControllerParameter param = animator.parameters[i];
            if (param.type == paramType && param.name == paramName)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasState(Animator animator, string stateName, int layer)
    {
        if (!HasAnimatorCtrl(animator) || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        return animator.HasState(layer, Animator.StringToHash(stateName));
    }
}
