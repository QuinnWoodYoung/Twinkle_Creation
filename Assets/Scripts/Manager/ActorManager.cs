using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorManager : MonoBehaviour
{
    public GameObject model;
    public BattleManager bm;  //����BattleManager
    public StateManager sm;     //����StateManager
    


    // Start is called before the first frame update
    void Awake()
    {
        GameObject sensor = transform.Find("sensor").gameObject;  //�ҵ���ɫ���ص�sensor��ײ�壬����ײ�����ڹ�������
        bm = sensor.GetComponent<BattleManager>(); //��ȡsensor�Ϲ��ص�BM���
        bm.am = this; //����������ص���ɫ��BattleManager��ȥ
        

        sm = GetComponent<StateManager>(); //��ȡ��ɫ���ص�StateManager���
        sm.am = this;//����������ص���ɫ��BattleManager��ȥ
        bm.sm = sm;

    }

    // Update is called once per frame


}
