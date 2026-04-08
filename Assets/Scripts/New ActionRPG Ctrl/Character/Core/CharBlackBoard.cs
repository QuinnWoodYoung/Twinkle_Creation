using System.Collections.Generic;
using UnityEngine;

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
    public Object source;
}

[System.Serializable]
public sealed class CharResourceSlice
{
    public bool hasHealth = true;
    public float hp = 100f;
    public float maxHp = 100f;
    public bool hasMana;
    public float mp;
    public float maxMp;
}

[System.Serializable]
public sealed class CharCombatSlice
{
    public float attackPower = 10f;
    public float criticalAttackPower = 10f;
    public float attackSpeed = 1f;
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

[DisallowMultipleComponent]
public class CharBlackBoard : MonoBehaviour
{
    // CharBlackBoard is the single runtime data source for one character.
    // Systems should prefer reading here instead of caching their own truth.
    private static readonly HashSet<CharBlackBoard> _activeBoards = new HashSet<CharBlackBoard>();

    [Header("Features")]
    [SerializeField] private CharFeatureSet _features = new CharFeatureSet();

    [Header("Always-On Data")]
    [SerializeField] private CharIdentitySlice _identity = new CharIdentitySlice();
    [SerializeField] private CharTransformSlice _transformState = new CharTransformSlice();
    [SerializeField] private CharMotionSlice _motion = new CharMotionSlice();
    [SerializeField] private CharActionSlice _action = new CharActionSlice();

    [Header("Optional Data")]
    [SerializeField] private CharResourceSlice _resources = new CharResourceSlice();
    [SerializeField] private CharCombatSlice _combat = new CharCombatSlice();
    [SerializeField] private CharStatusSlice _status = new CharStatusSlice();
    [SerializeField] private CharSkillSlice _skills = new CharSkillSlice();
    [SerializeField] private CharEquipmentSlice _equipment = new CharEquipmentSlice();
    [SerializeField] private CharTargetingSlice _targeting = new CharTargetingSlice();

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

    public void SyncFromScene()
    {
        // Only sync data that already exists on the scene object itself.
        _transformState.position = transform.position;
        _transformState.forward = transform.forward;
        if (_identity.team != null)
        {
            _identity.teamSide = _identity.team.side;
            _identity.teamId = _identity.team.EffectiveTeamId;
        }
    }

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
    }

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
