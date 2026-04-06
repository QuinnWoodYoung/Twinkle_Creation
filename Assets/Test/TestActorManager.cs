
using UnityEngine;

public class TestActorManager : MonoBehaviour
{

    public StateManager sm;
    
    void Awake()
    {
        sm = GetComponent<StateManager>(); 
    }
    void OnEnable()
    {
        GameManager.Instance.RigisterPlayer(sm);
        Cursor.lockState = CursorLockMode.Locked;
    }
    void Start()
    {
        SaveManager.Instance.LoadPlayerData();
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
    }
    

}