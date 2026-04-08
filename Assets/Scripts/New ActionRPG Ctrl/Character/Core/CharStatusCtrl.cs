using System;
using System.Collections.Generic;
using UnityEngine;

public class CharStatusCtrl : MonoBehaviour
{
    [Serializable]
    private struct LegacyStatusMap
    {
        public EStatusType oldStatus;
        public CharStatusDef def;
        public CharImmuneType immuneMask;
    }

    public event Action<CharStatusRt> StatusAdd;
    public event Action<CharStatusRt> StatusUpd;
    public event Action<CharStatusRt> StatusRemove;
    public event Action<CharStateSnap> SnapUpd;

    [Header("Legacy Map")]
    [SerializeField] private List<LegacyStatusMap> _legacyMap = new List<LegacyStatusMap>();
    // Raw runtime status entries. After blackboard binding this points to
    // blackboard.Status.runtimeStatuses.
    [SerializeField] private List<CharStatusRt> _list = new List<CharStatusRt>();
    // Folded status result. After blackboard binding this points to
    // blackboard.Status.snapshot.
    [SerializeField] private CharStateSnap _snap = new CharStateSnap();

    private int _nextRtId = 1;
    private CharBlackBoard _blackBoard;

    public IReadOnlyList<CharStatusRt> List
    {
        get { return _list; }
    }

    public CharStateSnap Snap
    {
        get { return _snap; }
    }

    private void Awake()
    {
        BindBlackBoard();
        RebuildSnap();
    }

    private void Update()
    {
        bool dirty = false;

        for (int i = _list.Count - 1; i >= 0; i--)
        {
            CharStatusRt rt = _list[i];
            if (rt == null)
            {
                _list.RemoveAt(i);
                dirty = true;
                continue;
            }

            rt.Tick(Time.deltaTime);
            if (rt.IsExpired)
            {
                _list.RemoveAt(i);
                StatusRemove?.Invoke(rt);
                dirty = true;
            }
        }

        if (dirty)
        {
            RebuildSnap();
        }
    }

    public CharStatusApplyRes TryApply(CharStatusApplyReq req)
    {
        if (req.def == null)
        {
            return CharStatusApplyRes.Make(CharStatusApplyResType.Reject, null, "def null");
        }

        if (!req.ignoreImmune && IsBlockedByImmune(req))
        {
            return CharStatusApplyRes.Make(CharStatusApplyResType.Reject, null, "immune block");
        }

        if (!ResolveExcl(req))
        {
            return CharStatusApplyRes.Make(CharStatusApplyResType.Reject, null, "higher excl active");
        }

        CharStatusRt match = FindMatch(req);
        if (match == null || req.def.stackMode == CharStatusStackMode.Multi)
        {
            CharStatusRt addRt = new CharStatusRt();
            addRt.Init(_nextRtId++, req);
            _list.Add(addRt);
            StatusAdd?.Invoke(addRt);
            RebuildSnap();
            return CharStatusApplyRes.Make(CharStatusApplyResType.Add, addRt);
        }

        switch (req.def.stackMode)
        {
            case CharStatusStackMode.RefreshDur:
            case CharStatusStackMode.UniquePerCaster:
                match.Refresh(req);
                StatusUpd?.Invoke(match);
                RebuildSnap();
                return CharStatusApplyRes.Make(CharStatusApplyResType.Refresh, match);

            case CharStatusStackMode.AddStackRefresh:
                match.AddStack(req);
                StatusUpd?.Invoke(match);
                RebuildSnap();
                return CharStatusApplyRes.Make(CharStatusApplyResType.Refresh, match);

            case CharStatusStackMode.ReplaceIfStronger:
                if (req.power < match.power)
                {
                    return CharStatusApplyRes.Make(CharStatusApplyResType.Reject, match, "weaker");
                }

                match.Init(match.rtId, req);
                StatusUpd?.Invoke(match);
                RebuildSnap();
                return CharStatusApplyRes.Make(CharStatusApplyResType.Replace, match);
        }

        return CharStatusApplyRes.Make(CharStatusApplyResType.Reject, match, "unknown stack mode");
    }

    public bool ApplyStatus(EStatusType oldStatus, float dur, GameObject applier = null, UnityEngine.Object src = null, int stackAdd = 1, float power = 0f)
    {
        CharStatusApplyReq req;
        if (!TryBuildLegacyReq(oldStatus, dur, applier, src, stackAdd, power, out req))
        {
            return false;
        }

        return TryApply(req).ok;
    }

    public int Dispel(CharDispelType dispelType, bool removeBuff, bool removeDebuff)
    {
        int removeCount = 0;

        for (int i = _list.Count - 1; i >= 0; i--)
        {
            CharStatusRt rt = _list[i];
            if (rt == null || rt.def == null)
            {
                continue;
            }

            if (rt.def.dispelType == CharDispelType.None)
            {
                continue;
            }

            if (dispelType < rt.def.dispelType)
            {
                continue;
            }

            if (rt.def.polarity == CharStatusPolarity.Buff && !removeBuff)
            {
                continue;
            }

            if (rt.def.polarity == CharStatusPolarity.Debuff && !removeDebuff)
            {
                continue;
            }

            _list.RemoveAt(i);
            StatusRemove?.Invoke(rt);
            removeCount++;
        }

        if (removeCount > 0)
        {
            RebuildSnap();
        }

        return removeCount;
    }

    public bool HasTag(CharStateTag tag)
    {
        return (_snap.tags & tag) == tag;
    }

    public bool HasRestrict(CharRestrict restrict)
    {
        return (_snap.restricts & restrict) == restrict;
    }

    public bool HasImmune(CharImmuneType immune)
    {
        return (_snap.immunes & immune) == immune;
    }

    public void SetActionState(CharActionState actionState)
    {
        if (_snap.actionState == actionState)
        {
            return;
        }

        _snap.actionState = actionState;
        RebuildSnap();
    }

    public void NotifyBreakByDmg()
    {
        bool dirty = false;

        for (int i = _list.Count - 1; i >= 0; i--)
        {
            CharStatusRt rt = _list[i];
            if (rt == null || rt.def == null || !rt.def.breakOnDmg)
            {
                continue;
            }

            _list.RemoveAt(i);
            StatusRemove?.Invoke(rt);
            dirty = true;
        }

        if (dirty)
        {
            RebuildSnap();
        }
    }

    private bool IsBlockedByImmune(CharStatusApplyReq req)
    {
        CharImmuneType checkMask = req.immuneMask;
        if (req.def.polarity == CharStatusPolarity.Debuff)
        {
            checkMask |= CharImmuneType.Debuff;
        }

        return checkMask != CharImmuneType.None && (_snap.immunes & checkMask) != 0;
    }

    private bool TryBuildLegacyReq(EStatusType oldStatus, float dur, GameObject applier, UnityEngine.Object src, int stackAdd, float power, out CharStatusApplyReq req)
    {
        for (int i = 0; i < _legacyMap.Count; i++)
        {
            LegacyStatusMap map = _legacyMap[i];
            if (map.oldStatus != oldStatus || map.def == null)
            {
                continue;
            }

            req = new CharStatusApplyReq
            {
                def = map.def,
                applier = applier,
                src = src,
                dur = dur,
                stackAdd = stackAdd,
                power = power,
                immuneMask = map.immuneMask,
            };
            return true;
        }

        req = default;
        return false;
    }

    private bool ResolveExcl(CharStatusApplyReq req)
    {
        if (string.IsNullOrEmpty(req.def.exclGroup))
        {
            return true;
        }

        for (int i = _list.Count - 1; i >= 0; i--)
        {
            CharStatusRt rt = _list[i];
            if (rt == null || rt.def == null || rt.def.exclGroup != req.def.exclGroup)
            {
                continue;
            }

            if (rt.def.priority > req.def.priority)
            {
                return false;
            }

            _list.RemoveAt(i);
            StatusRemove?.Invoke(rt);
        }

        return true;
    }

    private CharStatusRt FindMatch(CharStatusApplyReq req)
    {
        for (int i = 0; i < _list.Count; i++)
        {
            CharStatusRt rt = _list[i];
            if (rt == null || !rt.MatchDef(req.def))
            {
                continue;
            }

            if (req.def.stackMode == CharStatusStackMode.UniquePerCaster && !rt.MatchCaster(req.applier))
            {
                continue;
            }

            return rt;
        }

        return null;
    }

    private void RebuildSnap()
    {
        CharActionState curActionState = _snap.actionState;
        _snap.Reset();
        _snap.actionState = curActionState;

        int domPriority = int.MinValue;

        for (int i = 0; i < _list.Count; i++)
        {
            CharStatusRt rt = _list[i];
            if (rt == null || rt.def == null)
            {
                continue;
            }

            _snap.tags |= rt.def.tags;
            _snap.restricts |= rt.def.restricts;
            _snap.immunes |= rt.def.immunes;

            CharStatMod mod = rt.def.mod;
            _snap.moveSpdMul *= mod.moveSpdMul <= 0f ? 1f : mod.moveSpdMul;
            _snap.moveSpdAdd += mod.moveSpdAdd;
            _snap.atkSpdMul *= mod.atkSpdMul <= 0f ? 1f : mod.atkSpdMul;
            _snap.castSpdMul *= mod.castSpdMul <= 0f ? 1f : mod.castSpdMul;
            _snap.turnSpdMul *= mod.turnSpdMul <= 0f ? 1f : mod.turnSpdMul;
            _snap.dmgTakenMul *= mod.dmgTakenMul <= 0f ? 1f : mod.dmgTakenMul;

            if (rt.def.priority >= domPriority)
            {
                CharStateTag domTag = PickDomCtrlTag(rt.def.tags);
                if (domTag != CharStateTag.None)
                {
                    domPriority = rt.def.priority;
                    _snap.domCtrlTag = domTag;
                }
            }
        }

        _snap.canMove = (_snap.restricts & CharRestrict.Move) == 0;
        _snap.canAtk = (_snap.restricts & CharRestrict.Atk) == 0;
        _snap.canCast = (_snap.restricts & CharRestrict.CastSkill) == 0;
        _snap.canRotate = (_snap.restricts & CharRestrict.Rotate) == 0;
        _snap.canSelect = (_snap.restricts & CharRestrict.BeSelect) == 0;
        _snap.canUnitTarget = (_snap.restricts & CharRestrict.BeUnitTarget) == 0;
        _snap.canAtkTarget = (_snap.restricts & CharRestrict.BeAtkTarget) == 0;

        AppendActionTags();
        SyncBlackBoardSnapshot();

        SnapUpd?.Invoke(_snap);
    }

    private void AppendActionTags()
    {
        switch (_snap.actionState)
        {
            case CharActionState.AtkWindup:
            case CharActionState.AtkRelease:
            case CharActionState.AtkRecover:
                _snap.tags |= CharStateTag.Atking;
                break;

            case CharActionState.CastPoint:
            case CharActionState.CastRelease:
            case CharActionState.Channeling:
                _snap.tags |= CharStateTag.Casting;
                break;

            case CharActionState.ForcedMoving:
                _snap.tags |= CharStateTag.ForcedMove;
                break;

            case CharActionState.HitReact:
                _snap.tags |= CharStateTag.HitReact;
                break;

            case CharActionState.Dead:
                _snap.tags |= CharStateTag.Dead;
                _snap.domCtrlTag = CharStateTag.Dead;
                break;
        }
    }

    private CharStateTag PickDomCtrlTag(CharStateTag tags)
    {
        if ((tags & CharStateTag.Dead) != 0) return CharStateTag.Dead;
        if ((tags & CharStateTag.Stun) != 0) return CharStateTag.Stun;
        if ((tags & CharStateTag.Sleep) != 0) return CharStateTag.Sleep;
        if ((tags & CharStateTag.Root) != 0) return CharStateTag.Root;
        if ((tags & CharStateTag.Silence) != 0) return CharStateTag.Silence;
        if ((tags & CharStateTag.Disarm) != 0) return CharStateTag.Disarm;
        if ((tags & CharStateTag.Invul) != 0) return CharStateTag.Invul;
        return CharStateTag.None;
    }

    private void BindBlackBoard()
    {
        // First migration step: keep status runtime data inside the blackboard
        // so movement, animation and skill systems can read one shared source.
        _blackBoard = GetComponent<CharBlackBoard>();
        if (_blackBoard == null)
        {
            return;
        }

        if (_blackBoard.Status.runtimeStatuses == null)
        {
            _blackBoard.Status.runtimeStatuses = new List<CharStatusRt>();
        }

        if (_blackBoard.Status.snapshot == null)
        {
            _blackBoard.Status.snapshot = new CharStateSnap();
        }

        _list = _blackBoard.Status.runtimeStatuses;
        _snap = _blackBoard.Status.snapshot;
    }

    private void SyncBlackBoardSnapshot()
    {
        if (_blackBoard == null)
        {
            return;
        }

        _blackBoard.SyncFromScene();
        _blackBoard.Motion.canMove = _snap.canMove;
        _blackBoard.Motion.canRotate = _snap.canRotate;

        _blackBoard.Action.state = _snap.actionState;
        _blackBoard.Action.isCasting = (_snap.tags & CharStateTag.Casting) != 0;
        _blackBoard.Action.isAttacking = (_snap.tags & CharStateTag.Atking) != 0;
        _blackBoard.Action.isDead = (_snap.tags & CharStateTag.Dead) != 0;

        if (_blackBoard.Features.useCombat)
        {
            _blackBoard.Combat.castSpeedMul = _snap.castSpdMul;
            _blackBoard.Combat.attackSpeedMul = _snap.atkSpdMul;
            _blackBoard.Combat.damageTakenMul = _snap.dmgTakenMul;
        }
    }
}
