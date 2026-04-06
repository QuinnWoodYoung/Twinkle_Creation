using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class GameManager : Singleton<GameManager>
{
     
    public StateManager playerStats;

    public CinemachineVirtualCamera followCamera;

    List<IEndGameObserver> endGameObservers = new List<IEndGameObserver>();

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
    }
    public void RigisterPlayer(StateManager player)
    {
        playerStats = player;

        followCamera = FindObjectOfType<CinemachineVirtualCamera>();

        if(followCamera != null)
        {
            followCamera.Follow = playerStats.transform;
            followCamera.LookAt = playerStats.transform;
        }
    }

    public void RigisterEnemy(StateManager player)
    {
        playerStats = player;
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
