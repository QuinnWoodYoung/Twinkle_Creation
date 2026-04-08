using UnityEngine;

public enum CharActionType
{
    None,
    Move,
    Atk,
    Cast,
    Channel,
    Dodge,
    ForcedMove,
    HitReact,
    Dead,
}

public enum CharActionState
{
    None,
    Idle,
    Moving,
    AtkWindup,
    AtkRelease,
    AtkRecover,
    CastPoint,
    CastRelease,
    Channeling,
    Dodging,
    ForcedMoving,
    HitReact,
    Dead,
}

[System.Serializable]
public class CharActionReq
{
    // High-level action type used by the runtime gate.
    public CharActionType type = CharActionType.None;
    // Runtime action state. If None, CharActionCtrl resolves it from type.
    public CharActionState state = CharActionState.None;
    // Source object that requested the action.
    public Object src;
    // Simplified action duration used by the current ARPG runtime.
    public float dur;
    // CharCtrl reads these to decide whether movement/rotation are locked.
    public bool lockMove;
    public bool lockRotate;
    // If false, other actions cannot interrupt or replace this one.
    public bool interruptible = true;
    // Animation key consumed by the presentation layer.
    public string animKey;
    // If true, the action must face a direction before it can really start.
    public bool waitFace;
    public Vector3 faceDir;
    public float faceTol = 5f;
}
