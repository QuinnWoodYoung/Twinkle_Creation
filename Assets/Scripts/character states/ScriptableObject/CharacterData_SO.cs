using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Data",menuName = "Character Stats/Data")]
public class CharacterData_SO : ScriptableObject
{
    public float HitPoint;
    public float MaxHitPoint;
    public float Energy;
    public float MaxEnergy;
}
