using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharStatusCtrl))]
public class CharStatusVfxCtrl : MonoBehaviour
{
    [System.Serializable]
    private sealed class RtVfx
    {
        public int rtId;
        public CharStatusRt rt;
        public GameObject loopObj;
    }

    [Header("状态特效挂点")]
    [Tooltip("身体挂点。留空时会优先用角色主体 Animator 的胸口骨骼，否则退回到角色根节点。")]
    [SerializeField] private Transform _bodyVfxRoot;
    [Tooltip("头顶挂点。留空时会优先用角色主体 Animator 的头骨骼，否则退回到身体挂点。")]
    [SerializeField] private Transform _headVfxRoot;
    [Tooltip("脚底挂点。留空时会退回到角色根节点。")]
    [SerializeField] private Transform _feetVfxRoot;

    [Header("调试")]
    [Tooltip("开启后，会在 Console 输出状态特效的创建与销毁日志。")]
    [SerializeField] private bool _debugLog;

    private readonly Dictionary<int, RtVfx> _active = new Dictionary<int, RtVfx>();
    private readonly HashSet<int> _seenRtIds = new HashSet<int>();
    private CharStatusCtrl _statusCtrl;
    private CharAnimCtrl _animCtrl;
    private CharBlackBoard _blackBoard;

    private void Awake()
    {
        CacheRefs();
        CacheMounts();
    }

    private void OnEnable()
    {
        CacheRefs();
        CacheMounts();

        if (_statusCtrl == null)
        {
            return;
        }

        _statusCtrl.StatusAdd += OnStatusAdd;
        _statusCtrl.StatusUpd += OnStatusUpd;
        _statusCtrl.StatusRemove += OnStatusRemove;

        if (_blackBoard != null)
        {
            _blackBoard.RuntimeChanged += OnBlackBoardChanged;
        }

        IReadOnlyList<CharStatusRt> list = GetRuntimeList();
        if (list == null)
        {
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            CharStatusRt rt = list[i];
            if (rt != null)
            {
                BindRt(rt);
            }
        }
    }

    private void OnDisable()
    {
        if (_statusCtrl != null)
        {
            _statusCtrl.StatusAdd -= OnStatusAdd;
            _statusCtrl.StatusUpd -= OnStatusUpd;
            _statusCtrl.StatusRemove -= OnStatusRemove;
        }

        if (_blackBoard != null)
        {
            _blackBoard.RuntimeChanged -= OnBlackBoardChanged;
        }

        ClearAll();
    }

    private void OnValidate()
    {
        CacheRefs();
        CacheMounts();
    }

    private void LateUpdate()
    {
        // Blackboard events are now the preferred sync path.
    }

    private void OnStatusAdd(CharStatusRt rt)
    {
        if (!IsStatusModuleEnabled())
        {
            return;
        }

        BindRt(rt);
    }

    private void OnStatusUpd(CharStatusRt rt)
    {
        if (!IsStatusModuleEnabled())
        {
            return;
        }

        if (rt == null || rt.def == null || !rt.def.useStatusVfx)
        {
            return;
        }

        RtVfx data;
        if (!_active.TryGetValue(rt.rtId, out data) || data == null)
        {
            BindRt(rt);
            return;
        }

        switch (rt.def.statusVfxRefresh)
        {
            case CharStatusVfxRefreshMode.ReplayEnter:
                SpawnOnAdd(rt);
                break;

            case CharStatusVfxRefreshMode.RestartLoop:
                RestartLoop(data);
                break;

            case CharStatusVfxRefreshMode.RestartAll:
                SpawnOnAdd(rt);
                RestartLoop(data);
                break;
        }
    }

    private void OnStatusRemove(CharStatusRt rt)
    {
        if (!IsStatusModuleEnabled())
        {
            return;
        }

        if (rt == null)
        {
            return;
        }

        RtVfx data;
        if (_active.TryGetValue(rt.rtId, out data))
        {
            if (data.loopObj != null)
            {
                Destroy(data.loopObj);
            }

            _active.Remove(rt.rtId);
        }

        SpawnOnRemove(rt);
    }

    private void OnBlackBoardChanged(CharBlackBoard board, CharBlackBoardChangeMask changeMask)
    {
        if (board != _blackBoard)
        {
            return;
        }

        if ((changeMask & CharBlackBoardChangeMask.Status) == 0)
        {
            return;
        }

        SyncFromBlackBoard();
    }

    private void BindRt(CharStatusRt rt)
    {
        if (rt == null || rt.def == null || !rt.def.useStatusVfx)
        {
            return;
        }

        RtVfx data;
        if (!_active.TryGetValue(rt.rtId, out data) || data == null)
        {
            data = new RtVfx
            {
                rtId = rt.rtId,
                rt = rt,
            };
            _active[rt.rtId] = data;
        }
        else
        {
            data.rt = rt;
        }

        SpawnOnAdd(rt);
        EnsureLoop(data);
    }

    private void EnsureLoop(RtVfx data)
    {
        if (data == null || data.rt == null || data.rt.def == null)
        {
            return;
        }

        if (data.loopObj != null)
        {
            return;
        }

        GameObject prefab = data.rt.def.statusVfxLoop;
        if (prefab == null)
        {
            return;
        }

        data.loopObj = SpawnVfx(data.rt.def, prefab, data.rt.def.statusVfxFollow, 0f);
        Log($"loop add rt={data.rtId} def={data.rt.def.statusId} prefab={prefab.name}");
    }

    private void RestartLoop(RtVfx data)
    {
        if (data == null)
        {
            return;
        }

        if (data.loopObj != null)
        {
            Destroy(data.loopObj);
            data.loopObj = null;
        }

        EnsureLoop(data);
    }

    private void SpawnOnAdd(CharStatusRt rt)
    {
        if (rt == null || rt.def == null || rt.def.statusVfxOnAdd == null)
        {
            return;
        }

        GameObject instance = SpawnVfx(rt.def, rt.def.statusVfxOnAdd, false, rt.def.statusVfxOnAddLife);
        Log($"enter add rt={rt.rtId} def={rt.def.statusId} prefab={rt.def.statusVfxOnAdd.name} inst={(instance != null ? instance.name : "null")}");
    }

    private void SpawnOnRemove(CharStatusRt rt)
    {
        if (rt == null || rt.def == null || rt.def.statusVfxOnRemove == null)
        {
            return;
        }

        GameObject instance = SpawnVfx(rt.def, rt.def.statusVfxOnRemove, false, rt.def.statusVfxOnRemoveLife);
        Log($"exit add rt={rt.rtId} def={rt.def.statusId} prefab={rt.def.statusVfxOnRemove.name} inst={(instance != null ? instance.name : "null")}");
    }

    private GameObject SpawnVfx(CharStatusDef def, GameObject prefab, bool followMount, float autoDestroyDelay)
    {
        if (def == null || prefab == null)
        {
            return null;
        }

        Transform mount = ResolveMount(def.statusVfxMount);
        if (mount == null)
        {
            mount = transform;
        }

        Quaternion localRot = Quaternion.Euler(def.statusVfxEuler);
        Vector3 localOffset = def.statusVfxOffset;
        GameObject instance;

        if (followMount)
        {
            instance = Instantiate(prefab, mount.position, mount.rotation * localRot, mount);
            instance.transform.localPosition = localOffset;
            instance.transform.localRotation = localRot;
        }
        else
        {
            Vector3 worldPos = mount.TransformPoint(localOffset);
            Quaternion worldRot = mount.rotation * localRot;
            instance = Instantiate(prefab, worldPos, worldRot);
        }

        if (autoDestroyDelay > 0f)
        {
            Destroy(instance, autoDestroyDelay);
        }

        return instance;
    }

    private Transform ResolveMount(CharStatusVfxMount mount)
    {
        switch (mount)
        {
            case CharStatusVfxMount.Head:
                return _headVfxRoot != null ? _headVfxRoot : (_bodyVfxRoot != null ? _bodyVfxRoot : transform);

            case CharStatusVfxMount.Feet:
                return _feetVfxRoot != null ? _feetVfxRoot : transform;

            case CharStatusVfxMount.Body:
                return _bodyVfxRoot != null ? _bodyVfxRoot : transform;

            default:
                return transform;
        }
    }

    private void CacheRefs()
    {
        if (_blackBoard == null)
        {
            _blackBoard = GetComponent<CharBlackBoard>();
        }

        if (_statusCtrl == null)
        {
            _statusCtrl = GetComponent<CharStatusCtrl>();
        }

        if (_animCtrl == null)
        {
            _animCtrl = GetComponent<CharAnimCtrl>();
        }
    }

    private void CacheMounts()
    {
        Animator bodyAnim = _animCtrl != null ? _animCtrl.BodyAnim : GetComponentInChildren<Animator>(true);
        if (bodyAnim == null)
        {
            if (_bodyVfxRoot == null) _bodyVfxRoot = transform;
            if (_headVfxRoot == null) _headVfxRoot = transform;
            if (_feetVfxRoot == null) _feetVfxRoot = transform;
            return;
        }

        if (_bodyVfxRoot == null)
        {
            Transform chest = bodyAnim.isHuman ? bodyAnim.GetBoneTransform(HumanBodyBones.Chest) : null;
            _bodyVfxRoot = chest != null ? chest : bodyAnim.transform;
        }

        if (_headVfxRoot == null)
        {
            Transform head = bodyAnim.isHuman ? bodyAnim.GetBoneTransform(HumanBodyBones.Head) : null;
            _headVfxRoot = head != null ? head : _bodyVfxRoot;
        }

        if (_feetVfxRoot == null)
        {
            _feetVfxRoot = bodyAnim.transform;
        }
    }

    private void SyncFromBlackBoard()
    {
        IReadOnlyList<CharStatusRt> list = GetRuntimeList();
        if (list == null)
        {
            if (_active.Count > 0)
            {
                ClearAll();
            }

            return;
        }

        _seenRtIds.Clear();
        for (int i = 0; i < list.Count; i++)
        {
            CharStatusRt rt = list[i];
            if (rt == null)
            {
                continue;
            }

            _seenRtIds.Add(rt.rtId);
            if (!_active.ContainsKey(rt.rtId))
            {
                BindRt(rt);
            }
        }

        if (_active.Count == 0)
        {
            return;
        }

        List<int> staleIds = null;
        foreach (KeyValuePair<int, RtVfx> pair in _active)
        {
            if (_seenRtIds.Contains(pair.Key))
            {
                continue;
            }

            if (staleIds == null)
            {
                staleIds = new List<int>();
            }

            staleIds.Add(pair.Key);
        }

        if (staleIds == null)
        {
            return;
        }

        for (int i = 0; i < staleIds.Count; i++)
        {
            int rtId = staleIds[i];
            RtVfx data;
            if (!_active.TryGetValue(rtId, out data) || data == null)
            {
                continue;
            }

            if (data.loopObj != null)
            {
                Destroy(data.loopObj);
            }

            if (data.rt != null)
            {
                SpawnOnRemove(data.rt);
            }

            _active.Remove(rtId);
        }
    }

    private IReadOnlyList<CharStatusRt> GetRuntimeList()
    {
        if (_blackBoard != null)
        {
            if (!_blackBoard.Features.useStatus)
            {
                return null;
            }

            return _blackBoard.Status.runtimeStatuses;
        }

        return _statusCtrl != null ? _statusCtrl.List : null;
    }

    private bool IsStatusModuleEnabled()
    {
        return _blackBoard == null || _blackBoard.Features.useStatus;
    }

    private void ClearAll()
    {
        foreach (KeyValuePair<int, RtVfx> pair in _active)
        {
            RtVfx data = pair.Value;
            if (data != null && data.loopObj != null)
            {
                Destroy(data.loopObj);
            }
        }

        _active.Clear();
    }

    private void Log(string msg)
    {
        if (_debugLog)
        {
            Debug.Log($"[CharStatusVfxCtrl] {msg}", this);
        }
    }
}
