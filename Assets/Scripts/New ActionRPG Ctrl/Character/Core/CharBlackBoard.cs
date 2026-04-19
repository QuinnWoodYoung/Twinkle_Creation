using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
// Feature toggles describe which gameplay modules are enabled for this unit.
// They do not describe transient runtime state.
public sealed class CharFeatureSet
{
    public bool useResources = true;
    public bool useCombat = true;
    public bool useStatus = true;
    public bool useSkills = true;
    public bool useEquipment = true;
    public bool useTargeting = true;
}

[System.Serializable]
public sealed class CharIdentitySlice
{
    // Unique id for this spawned runtime unit instance.
    public string runtimeId;
    // Stable design-time id for unit templates or logical unit types.
    public string unitId;
    // Logical owner/controller id. Network layer can map this to a player/session.
    public string ownerPlayerId;
    // Opaque network id reserved for future multiplayer integration.
    public string netId;
    public bool isPlayerControlled = true;
    public Team team;
    public int teamId = -1;
    public TeamSide teamSide = TeamSide.Neutral;
}

[System.Serializable]
public sealed class CharTransformSlice
{
    public Vector3 position;
    public Vector3 forward = Vector3.forward;
}

[System.Serializable]
public sealed class CharMotionSlice
{
    public Vector2 moveInput;
    public Vector2 aimInput;
    public Vector3 moveVector;
    public Vector3 velocity;
    public float baseMoveSpeed = 3f;
    public float baseTurnSpeed = 720f;
    public bool canMove = true;
    public bool canRotate = true;
    public bool isMoving;
}

[System.Serializable]
public sealed class CharActionSlice
{
    public CharActionState state = CharActionState.None;
    public bool hasAction;
    public int controlLockCount;
    public bool isControlLocked;
    public bool isWaitingFace;
    public bool isCasting;
    public bool isAttacking;
    public bool isDead;
    public bool isInterrupted;
    public string animKey;
    public UnityEngine.Object source;
}

[System.Serializable]
public sealed class CharResourceSlice
{
    public bool hasHealth = true;
    public float hp = 100f;
    public float maxHp = 100f;
    [FormerlySerializedAs("hasMana")] public bool hasEnergy;
    [FormerlySerializedAs("mp")] public float energy;
    [FormerlySerializedAs("maxMp")] public float maxEnergy;
}

[System.Serializable]
public sealed class CharCombatSlice
{
    // Runtime attack profile consumed by basic-attack systems.
    public AttackData_SO attackData;
    // True when attackData is a runtime clone that blackboard should destroy.
    public bool ownsAttackDataInstance;
    public float attackPower = 10f;
    public float criticalAttackPower = 10f;
    [FormerlySerializedAs("attackSpeed")] public float rangedAttackSpeed = 1f;
    public float attackSpeedMul = 1f;
    public float attackRange = 1.5f;
    public float maxAttackRange = 3f;
    public float attackCooldown = 1f;
    public float castSpeed = 1f;
    public float castSpeedMul = 1f;
    public bool isCritical;
    public float damageTakenMul = 1f;
    public int damageImmuneCount;
}

[System.Serializable]
public sealed class CharStatusSlice
{
    // Raw runtime statuses currently applied to the unit.
    public List<CharStatusRt> runtimeStatuses = new List<CharStatusRt>();
    // Folded result of the raw status list. Controllers should read this first.
    public CharStateSnap snapshot = new CharStateSnap();
}

[System.Serializable]
public sealed class CharSkillSlice
{
    public List<SkillData> slots = new List<SkillData>();
    public List<float> cooldowns = new List<float>();
    public bool pendingCast;
    public int pendingSlot = -1;
}

[System.Serializable]
public sealed class CharEquipmentSlice
{
    public WeaponType weaponType = WeaponType.None;
    public Transform weaponRoot;
}

[System.Serializable]
public sealed class CharTargetingSlice
{
    public Transform lockedTarget;
    public GameObject currentTarget;
    public Vector3 aimPoint;
}

[Flags]
public enum CharBlackBoardChangeMask
{
    None = 0,
    Features = 1 << 0,
    Identity = 1 << 1,
    Transform = 1 << 2,
    Motion = 1 << 3,
    Action = 1 << 4,
    Resources = 1 << 5,
    Combat = 1 << 6,
    Status = 1 << 7,
    Skills = 1 << 8,
    Equipment = 1 << 9,
    Targeting = 1 << 10,
    All =
        Features |
        Identity |
        Transform |
        Motion |
        Action |
        Resources |
        Combat |
        Status |
        Skills |
        Equipment |
        Targeting,
}

[Serializable]
public struct CharBlackBoardSyncStamp
{
    public uint revision;
    public CharBlackBoardChangeMask changeMask;
    public string runtimeId;
    public string unitId;
    public string ownerPlayerId;
    public string netId;
}

[DisallowMultipleComponent]
/// <summary>
/// 单个角色的运行时共享数据中心。
/// 它本身几乎不做玩法判断，主要负责存放各系统共享的数据切片，
/// 并在数据变化时广播 RuntimeChanged 事件。
/// 
/// 读这套新角色控制器时，可以把它理解为“角色黑板”：
/// 输入、移动、动作、血蓝、战斗参数、状态、技能、装备、目标信息最终都会汇总到这里。
/// </summary>
public class CharBlackBoard : MonoBehaviour
{
    // CharBlackBoard is the single runtime data source for one character.
    // Systems should prefer reading here instead of caching their own truth.
    private static readonly HashSet<CharBlackBoard> _activeBoards = new HashSet<CharBlackBoard>();

    // Runtime revision is a cheap extension point for future networking, replay,
    // and UI/event systems that need to know when blackboard data changed.
    public event Action<CharBlackBoard, CharBlackBoardChangeMask> RuntimeChanged;

    [Header("Features")]
    // 模块总开关：决定这个单位是否启用资源、战斗、状态、技能等系统。
    [SerializeField] private CharFeatureSet _features = new CharFeatureSet();

    [Header("Always-On Data")]
    // 基础运行数据：角色身份、位置朝向、移动输入、动作态等。
    [SerializeField] private CharIdentitySlice _identity = new CharIdentitySlice();
    [SerializeField] private CharTransformSlice _transformState = new CharTransformSlice();
    [SerializeField] private CharMotionSlice _motion = new CharMotionSlice();
    [SerializeField] private CharActionSlice _action = new CharActionSlice();

    [Header("Optional Data")]
    // 可选模块数据：是否真正参与运行由 Features 中的开关决定。
    [SerializeField] private CharResourceSlice _resources = new CharResourceSlice();
    [SerializeField] private CharCombatSlice _combat = new CharCombatSlice();
    [SerializeField] private CharStatusSlice _status = new CharStatusSlice();
    [SerializeField] private CharSkillSlice _skills = new CharSkillSlice();
    [SerializeField] private CharEquipmentSlice _equipment = new CharEquipmentSlice();
    [SerializeField] private CharTargetingSlice _targeting = new CharTargetingSlice();
    [SerializeField] private uint _runtimeRevision;
    [SerializeField] private CharBlackBoardChangeMask _lastChangeMask;

    public CharFeatureSet Features => _features;
    public CharIdentitySlice Identity => _identity;
    public CharTransformSlice TransformState => _transformState;
    public CharMotionSlice Motion => _motion;
    public CharActionSlice Action => _action;
    public CharResourceSlice Resources => _resources;
    public CharCombatSlice Combat => _combat;
    public CharStatusSlice Status => _status;
    public CharSkillSlice Skills => _skills;
    public CharEquipmentSlice Equipment => _equipment;
    public CharTargetingSlice Targeting => _targeting;
    public static IEnumerable<CharBlackBoard> ActiveBoards => _activeBoards;
    public uint RuntimeRevision => _runtimeRevision;
    public CharBlackBoardChangeMask LastChangeMask => _lastChangeMask;

    private void Reset()
    {
        AutoBindIdentity();
        SyncFromScene();
    }

    private void Awake()
    {
        AutoBindIdentity();
        SyncFromScene();
    }

    private void OnEnable()
    {
        _activeBoards.Add(this);
    }

    private void OnDisable()
    {
        _activeBoards.Remove(this);
    }

    private void OnValidate()
    {
        AutoBindIdentity();
        SyncFromScene();
    }

    /// <summary>
    /// 把场景对象上天然存在的信息回写到黑板。
    /// 这里只同步 transform / team 这类客观数据，不负责玩法推导。
    /// </summary>
    public void SyncFromScene()
    {
        CharBlackBoardChangeMask changed = CharBlackBoardChangeMask.None;

        // Only sync data that already exists on the scene object itself.
        if (_transformState.position != transform.position)
        {
            _transformState.position = transform.position;
            changed |= CharBlackBoardChangeMask.Transform;
        }

        if (_transformState.forward != transform.forward)
        {
            _transformState.forward = transform.forward;
            changed |= CharBlackBoardChangeMask.Transform;
        }

        if (_identity.team != null)
        {
            TeamSide newSide = _identity.team.side;
            int newTeamId = _identity.team.EffectiveTeamId;
            if (_identity.teamSide != newSide || _identity.teamId != newTeamId)
            {
                _identity.teamSide = newSide;
                _identity.teamId = newTeamId;
                changed |= CharBlackBoardChangeMask.Identity;
            }
        }

        if (changed != CharBlackBoardChangeMask.None)
        {
            MarkRuntimeChanged(changed);
        }
    }

    /// <summary>
    /// 清理一轮运行中的临时状态，常用于重生/重置。
    /// 这里不会清掉基础配置，只清空动态数据。
    /// </summary>
    public void ClearRuntimeData()
    {
        // Clear only transient runtime data. This is useful for respawn/reset flows.
        _motion.moveInput = Vector2.zero;
        _motion.aimInput = Vector2.zero;
        _motion.moveVector = Vector3.zero;
        _motion.velocity = Vector3.zero;
        _motion.isMoving = false;
        _motion.canMove = true;
        _motion.canRotate = true;

        _action.state = CharActionState.None;
        _action.hasAction = false;
        _action.controlLockCount = 0;
        _action.isControlLocked = false;
        _action.isWaitingFace = false;
        _action.isCasting = false;
        _action.isAttacking = false;
        _action.isDead = false;
        _action.isInterrupted = false;
        _action.animKey = null;
        _action.source = null;

        if (_features.useStatus)
        {
            _status.runtimeStatuses.Clear();
            _status.snapshot.Reset();
        }

        if (_features.useCombat)
        {
            _combat.attackSpeedMul = 1f;
            _combat.castSpeedMul = 1f;
            _combat.damageImmuneCount = 0;
        }

        if (_features.useSkills)
        {
            _skills.pendingCast = false;
            _skills.pendingSlot = -1;

            for (int i = 0; i < _skills.cooldowns.Count; i++)
            {
                _skills.cooldowns[i] = 0f;
            }
        }

        if (_features.useTargeting)
        {
            _targeting.lockedTarget = null;
            _targeting.currentTarget = null;
            _targeting.aimPoint = Vector3.zero;
        }

        MarkRuntimeChanged(
            CharBlackBoardChangeMask.Motion |
            CharBlackBoardChangeMask.Action |
            CharBlackBoardChangeMask.Status |
            CharBlackBoardChangeMask.Combat |
            CharBlackBoardChangeMask.Skills |
            CharBlackBoardChangeMask.Targeting);
    }

    /// <summary>
    /// 统一的“黑板数据已更新”出口。
    /// revision 主要给未来的网络、UI、回放系统留接口。
    /// </summary>
    public void MarkRuntimeChanged(CharBlackBoardChangeMask changeMask)
    {
        if (changeMask == CharBlackBoardChangeMask.None)
        {
            return;
        }

        _runtimeRevision++;
        _lastChangeMask = changeMask;
        RuntimeChanged?.Invoke(this, changeMask);
    }

    public CharBlackBoardSyncStamp CaptureSyncStamp(CharBlackBoardChangeMask changeMask = CharBlackBoardChangeMask.All)
    {
        return new CharBlackBoardSyncStamp
        {
            revision = _runtimeRevision,
            changeMask = changeMask,
            runtimeId = _identity.runtimeId,
            unitId = _identity.unitId,
            ownerPlayerId = _identity.ownerPlayerId,
            netId = _identity.netId,
        };
    }

    /// <summary>
    /// 在未显式配置时，用当前 GameObject 自动补齐基础身份信息。
    /// </summary>
    private void AutoBindIdentity()
    {
        if (string.IsNullOrEmpty(_identity.runtimeId))
        {
            _identity.runtimeId = gameObject.name + "_" + GetInstanceID();
        }

        if (string.IsNullOrEmpty(_identity.unitId))
        {
            _identity.unitId = gameObject.name;
        }

        if (_identity.team == null)
        {
            _identity.team = GetComponent<Team>();
        }

        if (_identity.team != null)
        {
            _identity.teamSide = _identity.team.side;
            _identity.teamId = _identity.team.EffectiveTeamId;
        }
    }
}
