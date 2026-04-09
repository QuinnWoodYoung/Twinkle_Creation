using System;
using UnityEngine;

public class CharActionCtrl : MonoBehaviour
{
    // ActionStart fires when the action actually begins.
    // For face-before-cast actions this happens after the turn is complete.
    public event Action<CharActionReq> ActionStart;
    public event Action<CharActionReq> ActionEnd;
    public event Action<CharActionReq, string> ActionIntd;

    [Header("Debug")]
    [SerializeField] private CharActionState _state = CharActionState.Idle;
    [SerializeField] private CharActionReq _curReq;

    private float _remain;
    private CharStatusCtrl _statusCtrl;
    private CharCtrl _charCtrl;
    private CharBlackBoard _blackBoard;
    private bool _waitFace;
    private bool _started;

    public CharActionState State
    {
        get { return _state; }
    }

    public CharActionReq CurReq
    {
        get { return _curReq; }
    }

    private void Awake()
    {
        _statusCtrl = GetComponent<CharStatusCtrl>();
        _charCtrl = GetComponent<CharCtrl>();
        _blackBoard = GetComponent<CharBlackBoard>();
        // CharActionReq 是可序列化引用类型。场景里哪怕只是留下一个默认对象，
        // 运行时也会被误判成“当前已有动作”，从而把新的攻击/施法全部挡掉。
        // 因此这里强制把动作控制器重置到纯净的 Idle 起点。
        _curReq = null;
        _remain = 0f;
        _waitFace = false;
        _started = false;
        _state = CharActionState.Idle;
        SyncState();
    }

    private void Update()
    {
        if (_curReq != null && _waitFace)
        {
            UpdateWaitFace();
            return;
        }

        if (_curReq != null && _state != CharActionState.Idle && _state != CharActionState.Moving && _remain <= 0f)
        {
            EndCur();
            return;
        }

        if (_state == CharActionState.Idle || _state == CharActionState.Moving)
        {
            return;
        }

        _remain -= Time.deltaTime;
        if (_remain <= 0f)
        {
            EndCur();
        }
    }

    public bool TryStart(CharActionReq req)
    {
        if (req == null)
        {
            return false;
        }

        if (!CanStart(req))
        {
            return false;
        }

        if (_curReq != null && _state != CharActionState.Idle)
        {
            if (_curReq.interruptible)
            {
                Interrupt("replace");
            }
            else
            {
                return false;
            }
        }

        _curReq = req;
        _state = ResolveState(req);
        _waitFace = req.waitFace && req.faceDir.sqrMagnitude > 0.001f;
        _started = false;
        _remain = _waitFace ? 0f : Mathf.Max(0f, req.dur);

        if (_blackBoard != null)
        {
            _blackBoard.Action.isInterrupted = false;
        }

        BeginReqRuntime();
        SyncState();

        if (!_waitFace)
        {
            StartCur();
        }

        return true;
    }

    public void EndCur()
    {
        if (_curReq == null)
        {
            _state = CharActionState.Idle;
            SyncState();
            return;
        }

        CharActionReq lastReq = _curReq;
        bool wasStarted = _started;
        EndReqRuntime();
        _curReq = null;
        _remain = 0f;
        _waitFace = false;
        _started = false;
        _state = CharActionState.Idle;

        if (_blackBoard != null)
        {
            _blackBoard.Action.isInterrupted = false;
        }

        SyncState();

        if (wasStarted)
        {
            ActionEnd?.Invoke(lastReq);
        }
    }

    public bool Interrupt(string reason)
    {
        if (_curReq == null)
        {
            return false;
        }

        if (!_curReq.interruptible && _state != CharActionState.Dead)
        {
            return false;
        }

        CharActionReq lastReq = _curReq;
        EndReqRuntime();
        _curReq = null;
        _remain = 0f;
        _waitFace = false;
        _started = false;
        _state = CharActionState.Idle;
        SyncState();

        if (_blackBoard != null)
        {
            _blackBoard.Action.isInterrupted = true;
        }

        ActionIntd?.Invoke(lastReq, reason);
        return true;
    }

    public bool IsMoveLocked()
    {
        return _curReq != null && _curReq.lockMove;
    }

    public bool IsRotateLocked()
    {
        return _curReq != null && _curReq.lockRotate;
    }

    public bool IsWaitingFace()
    {
        return _curReq != null && _waitFace;
    }

    public bool IsFaceReady()
    {
        return _curReq != null && !_waitFace;
    }

    private bool CanStart(CharActionReq req)
    {
        CharStateSnap snap = null;
        if (_blackBoard != null && _blackBoard.Features.useStatus)
        {
            snap = _blackBoard.Status.snapshot;
        }
        else if (_statusCtrl != null)
        {
            snap = _statusCtrl.Snap;
        }

        if (snap == null)
        {
            return true;
        }

        switch (req.type)
        {
            case CharActionType.Move:
                return snap.canMove;
            case CharActionType.Atk:
                return snap.canAtk;
            case CharActionType.Cast:
                return snap.canCast;
            case CharActionType.Channel:
                return snap.canCast && (snap.restricts & CharRestrict.Channel) == 0;
            case CharActionType.Dead:
                return true;
            default:
                return true;
        }
    }

    private CharActionState ResolveState(CharActionReq req)
    {
        if (req.state != CharActionState.None)
        {
            return req.state;
        }

        switch (req.type)
        {
            case CharActionType.Move:
                return CharActionState.Moving;
            case CharActionType.Atk:
                return CharActionState.AtkWindup;
            case CharActionType.Cast:
                return CharActionState.CastPoint;
            case CharActionType.Channel:
                return CharActionState.Channeling;
            case CharActionType.Dodge:
                return CharActionState.Dodging;
            case CharActionType.ForcedMove:
                return CharActionState.ForcedMoving;
            case CharActionType.HitReact:
                return CharActionState.HitReact;
            case CharActionType.Dead:
                return CharActionState.Dead;
            default:
                return CharActionState.Idle;
        }
    }

    private void SyncState()
    {
        // During migration, action state is written to both the status bridge
        // and the blackboard so old and new readers stay in sync.
        if (_statusCtrl != null)
        {
            _statusCtrl.SetActionState(_state);
        }

        SyncBlackBoardAction();
    }

    private void BeginReqRuntime()
    {
        if (_charCtrl == null || _curReq == null)
        {
            return;
        }

        if (_waitFace)
        {
            _charCtrl.BeginSkillFacing(_curReq.faceDir);
        }
    }

    private void StartCur()
    {
        if (_curReq == null || _started)
        {
            return;
        }

        _started = true;
        ActionStart?.Invoke(_curReq);
    }

    private void EndReqRuntime()
    {
        if (_charCtrl == null)
        {
            return;
        }

        _charCtrl.EndSkillFacing();
    }

    private void UpdateWaitFace()
    {
        if (_curReq == null)
        {
            return;
        }

        if (_charCtrl == null)
        {
            _waitFace = false;
            _remain = Mathf.Max(0f, _curReq.dur);
            SyncState();
            StartCur();
            return;
        }

        _charCtrl.BeginSkillFacing(_curReq.faceDir);
        if (!_charCtrl.IsFacingDirection(_curReq.faceDir, _curReq.faceTol))
        {
            return;
        }

        _waitFace = false;
        _remain = Mathf.Max(0f, _curReq.dur);
        SyncState();
        StartCur();
    }

    private void SyncBlackBoardAction()
    {
        if (_blackBoard == null)
        {
            return;
        }

        CharActionSlice action = _blackBoard.Action;
        action.state = _state;
        action.hasAction = _curReq != null;
        action.isWaitingFace = _curReq != null && _waitFace;
        action.isCasting =
            _state == CharActionState.CastPoint ||
            _state == CharActionState.CastRelease ||
            _state == CharActionState.Channeling;
        action.isAttacking =
            _state == CharActionState.AtkWindup ||
            _state == CharActionState.AtkRelease ||
            _state == CharActionState.AtkRecover;
        action.isDead = _state == CharActionState.Dead;
        action.animKey = _curReq != null ? _curReq.animKey : null;
        action.source = _curReq != null ? _curReq.src : null;

        _blackBoard.MarkRuntimeChanged(CharBlackBoardChangeMask.Action);
    }
}
