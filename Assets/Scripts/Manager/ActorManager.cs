using UnityEngine;

public class ActorManager : MonoBehaviour
{
    public GameObject model;
    public BattleManager bm;  // 兼容旧 sensor 上的 BattleManager
    public StateManager sm;   // 兼容旧 StateManager，可为空

    void Awake()
    {
        if (model == null)
        {
            model = gameObject;
        }

        if (sm == null)
        {
            sm = GetComponent<StateManager>();
        }

        Transform sensor = transform.Find("sensor");
        if (bm == null && sensor != null)
        {
            bm = sensor.GetComponent<BattleManager>();
        }

        if (bm == null)
        {
            bm = GetComponentInChildren<BattleManager>();
        }

        if (bm != null)
        {
            bm.am = this;
            bm.sm = sm;
        }

        if (sm != null)
        {
            sm.am = this;
        }
    }
}
