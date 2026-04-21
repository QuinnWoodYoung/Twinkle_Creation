using System;
using System.Collections.Generic;
using UnityEngine;

public struct AttackInputState
{
    public bool isDown;
    public bool isHeld;
    public bool isUp;
}

public struct ButtonInputState
{
    public bool isDown;
    public bool isHeld;
    public bool isUp;
}

[Serializable]
/// <summary>
/// CharCtrl 每帧消费的输入快照。
/// CharSignalReader 负责写，CharCtrl / CharWeaponCtrl / CharSkillCtrl 负责读。
/// </summary>
public class CharParam
{
    public Vector2 Locomotion;
    public Vector2 AimTarget;
    public Vector2 AimDirection;
    public Vector2 AttackFacingInput;
    public bool isLock;
    public AttackInputState AttackState;
    public ButtonInputState LockState;
    public bool Dodge;
    public readonly List<bool> SkillInputDown = new List<bool>();
    public readonly List<ButtonInputState> SkillInputStates = new List<ButtonInputState>();
}
