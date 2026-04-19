using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

/// <summary>
/// Character bootstrap entry for blackboard data.
/// 
/// This component exists to make blackboard initialization explicit:
/// 1. prefab-level feature toggles live here
/// 2. identity / team data can be authored without hard-binding to StateManager
/// 3. legacy StateManager can still feed runtime data into the same entry point
/// 
/// Long term, units should be able to spawn with only CharBlackBoard +
/// CharBlackBoardInitializer and skip legacy state modules entirely.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharBlackBoard))]
public class CharBlackBoardInitializer : MonoBehaviour
{
    [System.Serializable]
    private sealed class InitialLegacyStatusEntry
    {
        public EStatusType status;
        [Tooltip("Status duration in seconds. Use a large value for long-lived authored states.")]
        public float duration = 1f;
        public int stackAdd = 1;
        public float power;
    }

    [Header("Bootstrap")]
    // 这份组件是黑板的初始化入口：把检视面板、模板数据、旧系统数据写进黑板。
    [Tooltip("Auto initialize on Awake when this unit does not use StateManager.")]
    [SerializeField] private bool _initializeOnAwake = true;

    [Header("Feature Overrides")]
    [Tooltip("When enabled, initializer writes module toggles into the blackboard.")]
    [SerializeField] private bool _applyFeatureOverrides;
    [SerializeField] private CharFeatureSet _featureOverrides = new CharFeatureSet();

    [Header("Identity")]
    [Tooltip("Stable logical id for this unit type or prefab.")]
    [SerializeField] private string _unitId = "";
    [Tooltip("Owner/controller id. Useful for future multiplayer authority mapping.")]
    [SerializeField] private string _ownerPlayerId = "";
    [Tooltip("Reserved runtime net id. Usually written by the network layer.")]
    [SerializeField] private string _netId = "";
    [SerializeField] private bool _isPlayerControlled = true;
    [Tooltip("Write team id directly into blackboard. Negative means auto/fallback.")]
    [SerializeField] private int _teamId = -1;
    [Tooltip("Fallback team side used when no Team component exists.")]
    [SerializeField] private TeamSide _teamSide = TeamSide.Neutral;

    [Header("Resources")]
    [Tooltip("Disable when this unit should not participate in HP logic.")]
    [SerializeField] private bool _hasHealth = true;
    [Tooltip("Optional resource template for blackboard-only units.")]
    [SerializeField] private CharacterData_SO _resourceTemplate;
    [Tooltip("Negative means use template/current value.")]
    [SerializeField] private float _startHp = -1f;
    [Tooltip("Negative means use template/current value.")]
    [SerializeField] private float _maxHp = -1f;
    [Tooltip("Enable when this unit should consume skill energy.")]
    [SerializeField] private bool _hasEnergy = true;
    [Tooltip("Negative means use template/current value.")]
    [SerializeField] private float _startEnergy = -1f;
    [Tooltip("Negative means use template/current value.")]
    [SerializeField] private float _maxEnergy = -1f;

    [Header("Combat")]
    [Tooltip("Optional combat template for blackboard-only units.")]
    [SerializeField] private AttackData_SO _attackTemplate;
    [Tooltip("Negative means use template/current value.")]
    [SerializeField] private float _attackPower = -1f;
    [Tooltip("Negative means use template/current value.")]
    [SerializeField] private float _criticalAttackPower = -1f;
    [Tooltip("Negative means use template/current value.")]
    [FormerlySerializedAs("_attackSpeed")]
    [SerializeField] private float _rangedAttackSpeed = -1f;
    [Tooltip("Negative means use template/current value.")]
    [SerializeField] private float _attackRange = -1f;
    [Tooltip("Negative means use template/current value.")]
    [SerializeField] private float _maxAttackRange = -1f;
    [Tooltip("Negative means use template/current value.")]
    [SerializeField] private float _attackCooldown = -1f;
    [Tooltip("Negative means keep current value.")]
    [SerializeField] private float _castSpeed = -1f;
    [SerializeField] private bool _isCritical;

    [Header("Initial Statuses")]
    [Tooltip("Optional authored statuses applied after startup for blackboard-driven units.")]
    [SerializeField] private List<InitialLegacyStatusEntry> _initialLegacyStatuses = new List<InitialLegacyStatusEntry>();

    private CharBlackBoard _blackBoard;
    private bool _initialStatusesApplied;

    public AttackData_SO AttackTemplate => _attackTemplate;

    private void Awake()
    {
        StateManager stateManager = GetComponent<StateManager>();
        if (!_initializeOnAwake || (stateManager != null && stateManager.enabled))
        {
            return;
        }

        Initialize();
    }

    private void Start()
    {
        StateManager stateManager = GetComponent<StateManager>();
        if (stateManager != null && stateManager.enabled)
        {
            return;
        }

        ApplyInitialStatuses();
    }

    private void OnValidate()
    {
        CacheBlackBoard();
    }

    /// <summary>
    /// Initialize from authored values only.
    /// Use this for pure blackboard-driven units without StateManager.
    /// </summary>
    public void Initialize()
    {
        Initialize(null);
    }

    /// <summary>
    /// Initialize blackboard from authored values, optionally merging legacy
    /// runtime data exposed by StateManager.
    /// </summary>
    /// <summary>
    /// 黑板初始化总入口。
    /// 顺序上先同步场景，再写入模块开关、身份、资源和战斗基线。
    /// </summary>
    public void Initialize(StateManager stateManager)
    {
        CacheBlackBoard();
        if (_blackBoard == null)
        {
            return;
        }

        _blackBoard.SyncFromScene();

        ApplyFeatures();
        ApplyIdentity(stateManager);
        ApplyResources(stateManager);
        ApplyCombat(stateManager);

        _blackBoard.MarkRuntimeChanged(
            CharBlackBoardChangeMask.Features |
            CharBlackBoardChangeMask.Identity |
            CharBlackBoardChangeMask.Resources |
            CharBlackBoardChangeMask.Action |
            CharBlackBoardChangeMask.Combat);
    }

    public void ApplyCombatBaseline()
    {
        CacheBlackBoard();
        if (_blackBoard == null || !_blackBoard.Features.useCombat)
        {
            return;
        }

        ApplyCombat(null);
        _blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Combat);
    }

    /// <summary>
    /// 应用模块开关覆盖。
    /// </summary>
    private void ApplyFeatures()
    {
        if (!_applyFeatureOverrides)
        {
            return;
        }

        _blackBoard.Features.useResources = _featureOverrides.useResources;
        _blackBoard.Features.useCombat = _featureOverrides.useCombat;
        _blackBoard.Features.useStatus = _featureOverrides.useStatus;
        _blackBoard.Features.useSkills = _featureOverrides.useSkills;
        _blackBoard.Features.useEquipment = _featureOverrides.useEquipment;
        _blackBoard.Features.useTargeting = _featureOverrides.useTargeting;
    }

    /// <summary>
    /// 初始化身份信息。
    /// Team 组件优先级高于手填 teamId / teamSide。
    /// </summary>
    private void ApplyIdentity(StateManager stateManager)
    {
        CharIdentitySlice identity = _blackBoard.Identity;

        if (!string.IsNullOrEmpty(_unitId))
        {
            identity.unitId = _unitId;
        }

        if (!string.IsNullOrEmpty(_ownerPlayerId))
        {
            identity.ownerPlayerId = _ownerPlayerId;
        }

        if (!string.IsNullOrEmpty(_netId))
        {
            identity.netId = _netId;
        }

        identity.isPlayerControlled = _isPlayerControlled;

        if (identity.team == null)
        {
            identity.team = GetComponent<Team>();
        }

        if (identity.team != null)
        {
            identity.teamSide = identity.team.side;
            identity.teamId = identity.team.EffectiveTeamId;
            return;
        }

        if (_teamId >= 0)
        {
            identity.teamId = _teamId;
        }

        if (_teamSide != TeamSide.Neutral || identity.team == null)
        {
            identity.teamSide = _teamSide;
        }
    }

    /// <summary>
    /// 初始化生命/能量面板。
    /// 有 StateManager 时优先复用旧系统当前值，否则退回到模板或面板配置。
    /// </summary>
    private void ApplyResources(StateManager stateManager)
    {
        if (!_blackBoard.Features.useResources)
        {
            return;
        }

        CharacterData_SO resourceSource = ResolveResourceSource(stateManager);
        CharResourceSlice resources = _blackBoard.Resources;
        resources.hasHealth = _hasHealth;

        if (!resources.hasHealth)
        {
            resources.hp = 0f;
            resources.maxHp = 0f;
            _blackBoard.Action.isDead = false;
        }
        else
        {
            float resolvedMaxHp = _maxHp >= 0f
                ? _maxHp
                : resourceSource != null
                    ? resourceSource.MaxHitPoint
                    : resources.maxHp;

            resolvedMaxHp = Mathf.Max(0f, resolvedMaxHp);

            float defaultHp = resolvedMaxHp;
            if (resourceSource != null)
            {
                defaultHp = resourceSource.HitPoint;
            }
            else if (resources.hp > 0f)
            {
                defaultHp = resources.hp;
            }

            float resolvedHp = _startHp >= 0f ? _startHp : defaultHp;
            resources.maxHp = resolvedMaxHp;
            resources.hp = Mathf.Clamp(resolvedHp, 0f, resolvedMaxHp);
            _blackBoard.Action.isDead = resources.hp <= 0f;
        }

        resources.hasEnergy = _hasEnergy;

        float resolvedMaxEnergy = _maxEnergy >= 0f
            ? _maxEnergy
            : resourceSource != null
                ? resourceSource.MaxEnergy
                : resources.maxEnergy;

        resolvedMaxEnergy = Mathf.Max(0f, resolvedMaxEnergy);

        float defaultEnergy = resolvedMaxEnergy;
        if (resourceSource != null)
        {
            defaultEnergy = resourceSource.Energy;
        }
        else if (resources.energy > 0f)
        {
            defaultEnergy = resources.energy;
        }

        float resolvedEnergy = _startEnergy >= 0f ? _startEnergy : defaultEnergy;
        resources.maxEnergy = resources.hasEnergy ? resolvedMaxEnergy : 0f;
        resources.energy = resources.hasEnergy
            ? Mathf.Clamp(resolvedEnergy, 0f, resources.maxEnergy)
            : 0f;
    }

    /// <summary>
    /// 初始化攻击模板和战斗基线数据。
    /// </summary>
    private void ApplyCombat(StateManager stateManager)
    {
        if (!_blackBoard.Features.useCombat)
        {
            return;
        }

        AttackData_SO attackSource = ResolveAttackSource(stateManager);
        CharCombatSlice combat = _blackBoard.Combat;
        AttackData_SO runtimeAttackData = CharCombatRuntimeUtility.AssignAttackData(
            _blackBoard,
            attackSource,
            stateManager == null);

        if (_attackPower >= 0f)
        {
            combat.attackPower = Mathf.Max(0f, _attackPower);
        }
        else if (runtimeAttackData != null)
        {
            combat.attackPower = Mathf.Max(0f, runtimeAttackData.minDamage);
        }

        if (_criticalAttackPower >= 0f)
        {
            combat.criticalAttackPower = Mathf.Max(0f, _criticalAttackPower);
        }
        else if (runtimeAttackData != null)
        {
            combat.criticalAttackPower = Mathf.Max(0f, runtimeAttackData.maxDamage);
        }

        if (_rangedAttackSpeed >= 0f)
        {
            combat.rangedAttackSpeed = Mathf.Max(0f, _rangedAttackSpeed);
        }
        else if (runtimeAttackData != null)
        {
            combat.rangedAttackSpeed = Mathf.Max(0f, runtimeAttackData.rangedAttackSpeed);
        }

        if (_attackRange >= 0f)
        {
            combat.attackRange = Mathf.Max(0f, _attackRange);
        }
        else if (runtimeAttackData != null)
        {
            combat.attackRange = Mathf.Max(0f, runtimeAttackData.attackRange);
        }

        if (_maxAttackRange >= 0f)
        {
            combat.maxAttackRange = Mathf.Max(0f, _maxAttackRange);
        }
        else if (runtimeAttackData != null)
        {
            combat.maxAttackRange = Mathf.Max(0f, runtimeAttackData.maxAttackRange);
        }

        if (_attackCooldown >= 0f)
        {
            combat.attackCooldown = Mathf.Max(0f, _attackCooldown);
        }
        else if (runtimeAttackData != null)
        {
            combat.attackCooldown = Mathf.Max(0f, runtimeAttackData.coolDown);
        }

        if (_castSpeed >= 0f)
        {
            combat.castSpeed = Mathf.Max(0f, _castSpeed);
        }

        combat.isCritical = stateManager != null ? stateManager.isCritical : _isCritical;
    }

    private CharacterData_SO ResolveResourceSource(StateManager stateManager)
    {
        if (stateManager != null)
        {
            if (stateManager.characterData != null)
            {
                return stateManager.characterData;
            }

            if (stateManager.templateData != null)
            {
                return stateManager.templateData;
            }
        }

        return _resourceTemplate;
    }

    private AttackData_SO ResolveAttackSource(StateManager stateManager)
    {
        if (stateManager != null && stateManager.attackData != null)
        {
            return stateManager.attackData;
        }

        return _attackTemplate;
    }

    private void CacheBlackBoard()
    {
        if (_blackBoard == null)
        {
            _blackBoard = GetComponent<CharBlackBoard>();
        }
    }

    /// <summary>
    /// 给纯黑板角色补上开场状态。
    /// 依然走 CharStatusCtrl，是为了统一叠层、互斥和快照逻辑。
    /// </summary>
    private void ApplyInitialStatuses()
    {
        if (_initialStatusesApplied || _initialLegacyStatuses == null || _initialLegacyStatuses.Count == 0)
        {
            return;
        }

        CacheBlackBoard();
        if (_blackBoard == null || !_blackBoard.Features.useStatus)
        {
            _initialStatusesApplied = true;
            return;
        }

        CharStatusCtrl statusCtrl = GetComponent<CharStatusCtrl>();
        if (statusCtrl == null)
        {
            statusCtrl = gameObject.AddComponent<CharStatusCtrl>();
        }

        for (int i = 0; i < _initialLegacyStatuses.Count; i++)
        {
            InitialLegacyStatusEntry entry = _initialLegacyStatuses[i];
            if (entry == null || entry.duration <= 0f)
            {
                continue;
            }

            statusCtrl.ApplyStatus(entry.status, entry.duration, gameObject, this, entry.stackAdd, entry.power);
        }

        _initialStatusesApplied = true;
    }
}
