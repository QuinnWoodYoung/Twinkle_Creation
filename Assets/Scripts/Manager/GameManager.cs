using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class GameManager : Singleton<GameManager>
{
    // Legacy field kept for save/load systems that still read CharacterData from
    // StateManager. New gameplay code should prefer PlayerUnit.
    public StateManager playerStats;
    [SerializeField] private GameObject _playerUnit;

    public CinemachineVirtualCamera followCamera;
    public GameObject PlayerUnit => _playerUnit != null ? _playerUnit : (playerStats != null ? playerStats.gameObject : null);
    public Transform PlayerTransform => PlayerUnit != null ? PlayerUnit.transform : null;
    public StateManager PlayerState => playerStats != null ? playerStats : (PlayerUnit != null ? PlayerUnit.GetComponent<StateManager>() : null);
    public CharacterData_SO PlayerCharacterData => PlayerState != null ? PlayerState.characterData : null;

    List<IEndGameObserver> endGameObservers = new List<IEndGameObserver>();

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
    }
    public void RigisterPlayer(StateManager player)
    {
        RigisterPlayer(player != null ? player.gameObject : null);
        playerStats = player;
    }

    public void RigisterPlayer(GameObject player)
    {
        _playerUnit = player;
        playerStats = player != null ? player.GetComponent<StateManager>() : null;

        followCamera = FindObjectOfType<CinemachineVirtualCamera>();

        if(followCamera != null && PlayerUnit != null)
        {
            followCamera.Follow = PlayerUnit.transform;
            followCamera.LookAt = PlayerUnit.transform;
        }
    }

    public void RigisterEnemy(StateManager player)
    {
        // Enemy registration is kept only for legacy compatibility.
        // Do not overwrite the tracked player unit here.
    }

    public void AddObserver(IEndGameObserver observer)
    {
        endGameObservers.Add(observer);
    }

    public void RemoveObserver(IEndGameObserver observer)
    {
        endGameObservers.Remove(observer);
    }

    public void NotifyObservers()
    {
        foreach (var observer in endGameObservers)
        {
            observer.EndNotify();
        }
    }

    public Transform GetEntrance()
    {
        foreach (var item in FindObjectsOfType<TransitionDestination>())
        {
            if (item.destinationTag == TransitionDestination.DestinationTag.ENTER)
                return item.transform;
        }
        return null;
    }
}
