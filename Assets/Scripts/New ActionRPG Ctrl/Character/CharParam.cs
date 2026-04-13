using System;
using System.Collections.Generic;
using UnityEngine;

public struct AttackInputState
{
    public bool isDown;
    public bool isHeld;
    public bool isUp;
}

[Serializable]
public class CharParam
{
    public Vector2 Locomotion;
    public Vector2 AimTarget;
    public bool isLock;
    public AttackInputState AttackState;
    public bool Dodge;
    public readonly List<bool> SkillInputDown = new List<bool>();
}
