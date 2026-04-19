using UnityEngine;

[System.Serializable]
// CharStateSnap is not the raw status list.
// It is the folded result that other systems can consume directly.
/// <summary>
/// 状态快照。
/// 它是所有运行时状态折叠后的结果，其他系统通常直接读这份快照，而不是遍历原始状态列表。
/// </summary>
public class CharStateSnap
{
    public CharStateTag tags = CharStateTag.None;
    public CharRestrict restricts = CharRestrict.None;
    public CharImmuneType immunes = CharImmuneType.None;
    public CharActionState actionState = CharActionState.None;
    public CharStateTag domCtrlTag = CharStateTag.None;

    public float moveSpdMul = 1f;
    public float moveSpdAdd;
    public float atkSpdMul = 1f;
    public float castSpdMul = 1f;
    public float turnSpdMul = 1f;
    public float dmgTakenMul = 1f;

    public bool canMove = true;
    public bool canAtk = true;
    public bool canCast = true;
    public bool canRotate = true;
    public bool canSelect = true;
    public bool canUnitTarget = true;
    public bool canAtkTarget = true;

    /// <summary>
    /// 重置成“没有任何状态影响”的默认值。
    /// </summary>
    public void Reset()
    {
        tags = CharStateTag.None;
        restricts = CharRestrict.None;
        immunes = CharImmuneType.None;
        domCtrlTag = CharStateTag.None;
        moveSpdMul = 1f;
        moveSpdAdd = 0f;
        atkSpdMul = 1f;
        castSpdMul = 1f;
        turnSpdMul = 1f;
        dmgTakenMul = 1f;
        canMove = true;
        canAtk = true;
        canCast = true;
        canRotate = true;
        canSelect = true;
        canUnitTarget = true;
        canAtkTarget = true;
    }
}
