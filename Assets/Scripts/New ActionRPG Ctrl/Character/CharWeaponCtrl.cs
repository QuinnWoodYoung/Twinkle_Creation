using UnityEngine;

[DisallowMultipleComponent]
public class CharWeaponCtrl : MonoBehaviour
{
    // Fired after the logical weapon state is fully synchronized.
    public event System.Action<WeaponType> WeaponChanged;

    private CharCtrl _charCtrl;
    private CharAnimCtrl _animCtrl;
    private WeaponVisualCtrl _weaponVisualCtrl;
    private CharActionCtrl _actionCtrl;
    private CharBlackBoard _blackBoard;
    private WeaponType _lastWeapon = (WeaponType)(-1);

    [SerializeField] private Transform _weaponRoot;
    [SerializeField] private Animator _weaponAnimator;
    [SerializeField] private WeaponAnimCtrl _weaponAnimCtrl;
    [SerializeField] public WeaponType _currentWeapon;

    public WeaponType CurWeapon => _currentWeapon;

    [Header("Bow")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private Transform bulletSpawnPoint;

    [Header("Debug")]
    [SerializeField] private bool _debugWeaponAnim = true;

    [Header("Light Attack")]
    [SerializeField] private string _lightAtkTrig = "Attack";
    [SerializeField] private float _lightAtkDur = 0.35f;
    [SerializeField] private bool _lightAtkLockMove = true;
    [SerializeField] private bool _lightAtkLockRotate;

    [Header("Heavy Attack")]
    [SerializeField] private bool _useHeavyAtk;
    [SerializeField] private string _heavyAtkTrig = "";
    [SerializeField] private float _heavyAtkThreshold = 0.3f;
    [SerializeField] private float _heavyAtkDur = 0.55f;
    [SerializeField] private bool _heavyAtkLockMove = true;
    [SerializeField] private bool _heavyAtkLockRotate = true;

    [Header("Weapon Animation")]
    [SerializeField] private bool _autoWeaponCfg = true;

    protected void Awake()
    {
        _charCtrl = GetComponent<CharCtrl>();
        _animCtrl = GetComponent<CharAnimCtrl>();
        _weaponVisualCtrl = GetComponent<WeaponVisualCtrl>();
        _actionCtrl = GetComponent<CharActionCtrl>();
        _blackBoard = GetComponent<CharBlackBoard>();

        if (_animCtrl == null)
        {
            _animCtrl = gameObject.AddComponent<CharAnimCtrl>();
        }

        CacheWeaponRoot();
        CacheWeaponAnimator();
        CacheWeaponAnimCtrl();
        SyncWeaponCfg();
    }

    private void Start()
    {
        RefreshWeaponAnim();
        SyncWeaponCfg();
        SyncBlackBoardWeapon();
    }

    private void OnEnable()
    {
        if (_actionCtrl == null)
        {
            _actionCtrl = GetComponent<CharActionCtrl>();
        }

        if (_actionCtrl != null)
        {
            _actionCtrl.ActionStart += OnActionStart;
        }
    }

    private void OnDisable()
    {
        if (_actionCtrl != null)
        {
            _actionCtrl.ActionStart -= OnActionStart;
        }
    }

    private void Update()
    {
        if (_lastWeapon != _currentWeapon)
        {
            SyncWeaponCfg();
            NotifyWeaponChanged();
        }

        UpdateAtkInput_New();
    }

    private void UpdateAtkInput_New()
    {
        if (_charCtrl == null || _charCtrl.Param == null)
        {
            return;
        }

        AttackInputState input = _charCtrl.Param.AttackState;
        if (!input.isUp)
        {
            return;
        }

        if (_useHeavyAtk && input.holdDuration >= _heavyAtkThreshold)
        {
            TryHeavyAtk_New();
            return;
        }

        TryLightAtk_New();
    }

    private void OnActionStart(CharActionReq req)
    {
        if (req == null || req.type != CharActionType.Atk || req.src != this)
        {
            return;
        }

        // Presentation waits until the action request is really accepted.
        PlayAtk_New(req.animKey);
    }

    private void TryLightAtk_New()
    {
        bool lockMove = _autoWeaponCfg ? !CanMoveOnAtkByWeapon() : _lightAtkLockMove;
        if (!TryStartAtk_New(_lightAtkTrig, _lightAtkDur, lockMove, _lightAtkLockRotate))
        {
            return;
        }

        if (_actionCtrl == null)
        {
            PlayAtk_New(_lightAtkTrig);
        }
    }

    private void TryHeavyAtk_New()
    {
        string trig = string.IsNullOrEmpty(_heavyAtkTrig) ? _lightAtkTrig : _heavyAtkTrig;
        float dur = _heavyAtkDur > 0f ? _heavyAtkDur : _lightAtkDur;
        bool lockMove = _autoWeaponCfg ? !CanMoveOnAtkByWeapon() : _heavyAtkLockMove;
        if (!TryStartAtk_New(trig, dur, lockMove, _heavyAtkLockRotate))
        {
            return;
        }

        if (_actionCtrl == null)
        {
            PlayAtk_New(trig);
        }
    }

    private bool TryStartAtk_New(string animKey, float dur, bool lockMove, bool lockRotate)
    {
        if (!CanAtkByState_New())
        {
            return false;
        }

        if (_actionCtrl == null)
        {
            return true;
        }

        CharActionReq req = new CharActionReq
        {
            type = CharActionType.Atk,
            state = CharActionState.AtkWindup,
            src = this,
            dur = Mathf.Max(0.01f, dur),
            lockMove = lockMove,
            lockRotate = lockRotate,
            interruptible = true,
            animKey = animKey,
        };

        return _actionCtrl.TryStart(req);
    }

    private bool CanAtkByState_New()
    {
        return CharRuntimeResolver.CanAttack(gameObject);
    }

    private void PlayAtk_New(string trig)
    {
        if (_weaponAnimCtrl == null)
        {
            RefreshWeaponAnim();
        }

        if (_weaponAnimCtrl != null)
        {
            if (_debugWeaponAnim)
            {
                Debug.Log($"[CharWeaponCtrl] PlayAtk trig={trig} root={_weaponRoot?.name} weaponAnim={_weaponAnimCtrl.name} curWeapon={_currentWeapon}", this);
            }

            _weaponAnimCtrl.PlayAtk(trig);
        }
        else if (_debugWeaponAnim)
        {
            Debug.LogWarning($"[CharWeaponCtrl] PlayAtk failed, no WeaponAnimCtrl. root={_weaponRoot?.name} animator={_weaponAnimator?.name} curWeapon={_currentWeapon}", this);
        }

        if (_currentWeapon == WeaponType.Bow)
        {
            Shoot();
        }
    }

    public void SetWeapon(WeaponType weaponType)
    {
        if (_currentWeapon == weaponType)
        {
            return;
        }

        _currentWeapon = weaponType;
        SyncWeaponCfg();
        SyncBlackBoardWeapon();
        NotifyWeaponChanged();
    }

    public void BindWeaponRoot(Transform weaponRoot)
    {
        _weaponRoot = weaponRoot;
        _weaponAnimator = null;
        _weaponAnimCtrl = null;

        CacheWeaponAnimator();
        CacheWeaponAnimCtrl();

        if (_weaponAnimCtrl != null)
        {
            _weaponAnimCtrl.Bind(gameObject, _currentWeapon);
        }

        if (_debugWeaponAnim)
        {
            Debug.Log($"[CharWeaponCtrl] BindWeaponRoot root={_weaponRoot?.name} anim={_weaponAnimator?.name} ctrl={_weaponAnimCtrl?.name} curWeapon={_currentWeapon}", this);
        }

        SyncBlackBoardWeapon();
    }

    public void ClearWeaponRoot()
    {
        if (_weaponAnimCtrl != null)
        {
            _weaponAnimCtrl.Unbind();
        }

        _weaponRoot = null;
        _weaponAnimator = null;
        _weaponAnimCtrl = null;
        SyncBlackBoardWeapon();
    }

    public void RefreshWeaponAnim()
    {
        CacheWeaponRoot();
        if (_weaponRoot == null)
        {
            _weaponAnimator = null;
            _weaponAnimCtrl = null;
            SyncBlackBoardWeapon();
            return;
        }

        Animator bodyAnimator = _animCtrl != null ? _animCtrl.BodyAnim : null;
        Animator[] animators = _weaponRoot.GetComponentsInChildren<Animator>(true);
        _weaponAnimator = null;

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator != null && animator != bodyAnimator)
            {
                _weaponAnimator = animator;
                break;
            }
        }

        _weaponAnimCtrl = null;
        CacheWeaponAnimCtrl();

        if (_weaponAnimCtrl != null)
        {
            _weaponAnimCtrl.Bind(gameObject, _currentWeapon);
        }

        if (_debugWeaponAnim)
        {
            Debug.Log($"[CharWeaponCtrl] RefreshWeaponAnim root={_weaponRoot?.name} anim={_weaponAnimator?.name} ctrl={_weaponAnimCtrl?.name} curWeapon={_currentWeapon}", this);
        }

        SyncBlackBoardWeapon();
    }

    private void SyncWeaponCfg()
    {
        _lastWeapon = _currentWeapon;

        if (_autoWeaponCfg)
        {
            // Attack keys stay simple; weapon difference is mostly movement and layers.
            _lightAtkTrig = Weapon.GetAtkAnimName(_currentWeapon);
            if (!_useHeavyAtk || string.IsNullOrEmpty(_heavyAtkTrig))
            {
                _heavyAtkTrig = _lightAtkTrig;
            }
        }

        if (_weaponRoot == null)
        {
            CacheWeaponRoot();
        }

        if (_weaponAnimator == null)
        {
            CacheWeaponAnimator();
        }

        if (_weaponAnimCtrl == null)
        {
            CacheWeaponAnimCtrl();
        }

        if (_weaponAnimCtrl != null)
        {
            _weaponAnimCtrl.Bind(gameObject, _currentWeapon);
        }

        SyncBlackBoardWeapon();
    }

    private bool CanMoveOnAtkByWeapon()
    {
        // Current simplified rule: axe attack locks movement, most others do not.
        return Weapon.CanMoveAtk(_currentWeapon);
    }

    private void CacheWeaponAnimator()
    {
        if (_weaponRoot != null)
        {
            Animator[] rootAnimators = _weaponRoot.GetComponentsInChildren<Animator>(true);
            Animator bodyAnimator = _animCtrl != null ? _animCtrl.BodyAnim : null;
            for (int i = 0; i < rootAnimators.Length; i++)
            {
                Animator animator = rootAnimators[i];
                if (animator != null && animator != bodyAnimator)
                {
                    _weaponAnimator = animator;
                    return;
                }
            }
        }

        Animator[] animators = GetComponentsInChildren<Animator>(true);
        Animator ownBodyAnimator = _animCtrl != null ? _animCtrl.BodyAnim : null;
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator != null && animator != ownBodyAnimator)
            {
                _weaponAnimator = animator;
                return;
            }
        }
    }

    private void CacheWeaponAnimCtrl()
    {
        if (_weaponAnimCtrl != null)
        {
            return;
        }

        if (_weaponRoot != null)
        {
            _weaponAnimCtrl = _weaponRoot.GetComponent<WeaponAnimCtrl>();
            if (_weaponAnimCtrl == null)
            {
                _weaponAnimCtrl = _weaponRoot.GetComponentInChildren<WeaponAnimCtrl>(true);
            }

            if (_weaponAnimCtrl == null)
            {
                _weaponAnimCtrl = _weaponRoot.gameObject.AddComponent<WeaponAnimCtrl>();
            }

            return;
        }

        if (_weaponAnimator != null)
        {
            _weaponAnimCtrl = _weaponAnimator.GetComponent<WeaponAnimCtrl>();
            if (_weaponAnimCtrl == null)
            {
                _weaponAnimCtrl = _weaponAnimator.GetComponentInParent<WeaponAnimCtrl>();
            }

            return;
        }

        WeaponAnimCtrl[] ctrls = GetComponentsInChildren<WeaponAnimCtrl>(true);
        for (int i = 0; i < ctrls.Length; i++)
        {
            if (ctrls[i] != null)
            {
                _weaponAnimCtrl = ctrls[i];
                return;
            }
        }
    }

    private void CacheWeaponRoot()
    {
        if (_weaponVisualCtrl == null)
        {
            _weaponVisualCtrl = GetComponent<WeaponVisualCtrl>();
        }

        _weaponRoot = CharEquipmentResolver.ResolveWeaponRoot(
            gameObject,
            _blackBoard,
            _weaponVisualCtrl,
            _animCtrl,
            _currentWeapon);
    }

    private void SyncBlackBoardWeapon()
    {
        if (_blackBoard == null || !_blackBoard.Features.useEquipment)
        {
            return;
        }

        // Weapon ownership still lives here. The blackboard only mirrors the result.
        _blackBoard.Equipment.weaponType = _currentWeapon;
        _blackBoard.Equipment.weaponRoot = _weaponRoot;
    }

    private void NotifyWeaponChanged()
    {
        WeaponChanged?.Invoke(_currentWeapon);
    }

    private void Shoot()
    {
        if (bulletPrefab == null || bulletSpawnPoint == null)
        {
            return;
        }

        GameObject newBullet = Instantiate(
            bulletPrefab,
            bulletSpawnPoint.position,
            Quaternion.LookRotation(bulletSpawnPoint.forward));

        Rigidbody rb = newBullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = bulletSpawnPoint.forward * bulletSpeed;
        }

        Bullet bullet = newBullet.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.launcher = gameObject;
        }
    }
}
