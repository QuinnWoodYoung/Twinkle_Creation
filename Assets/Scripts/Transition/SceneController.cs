using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

public class SceneController : Singleton<SceneController>
{
    public GameObject playerPrefab;
    //public SceneFader sceneFaderPrefab;
    bool fadeFinished;


    GameObject player;
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
    }
    public void TransitionToDestination(TransitionPoint transitionPoint)
    {
        switch(transitionPoint.transitionType)
        {
            case TransitionPoint.TransitionType.SameScene:
                StartCoroutine(Transition(SceneManager.GetActiveScene().name,transitionPoint.destinationTag));
                break;
            case TransitionPoint.TransitionType.DifferentScene:
                StartCoroutine(Transition(transitionPoint.sceneName, transitionPoint.destinationTag));
                break;
        }
    }

    IEnumerator Transition(string sceneName, TransitionDestination.DestinationTag destinationTag)
    {
        SaveManager.Instance.SavePlayerData();
        InventoryManager.Instance.SaveData();

        if (SceneManager.GetActiveScene().name != sceneName)
        {
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return Instantiate(playerPrefab, GetDestination(destinationTag).transform.position, transform.rotation);
            SaveManager.Instance.LoadPlayerData();

            yield break;
        }
        else
        {
            player = GameManager.Instance.PlayerUnit;
            if (player != null)
            {
                player.transform.SetPositionAndRotation(GetDestination(destinationTag).transform.position, transform.rotation);
            }
            yield return null;
        }
    }
    private TransitionDestination GetDestination(TransitionDestination.DestinationTag destinationTag)
    {
        var entrances = FindObjectsOfType<TransitionDestination>();
        for(int i=0; i < entrances.Length; i++)
        {
            if(entrances[i].destinationTag == destinationTag)
                return entrances[i];
        }
        return null;
    }
    public void TransitionToMain()
    {
        StartCoroutine(LoadMain());
    }

    public void TransitionToLoadGame()
    {
        StartCoroutine(LoadLevel(SaveManager.Instance.SceneName));
    }

    public void TransitionToFirstLevel()
    {
        StartCoroutine(LoadLevel("Game"));
    }
    IEnumerator LoadLevel(string scene)
    {
        //SceneFader fade = Instantiate(sceneFaderPrefab);
        if (scene != "")
        {
            //yield return StartCoroutine(fade.FadeOut(2f));
            yield return SceneManager.LoadSceneAsync(scene);
            yield return player = Instantiate(playerPrefab, GameManager.Instance.GetEntrance().position, transform.rotation);

            //��������
            SaveManager.Instance.SavePlayerData();
            InventoryManager.Instance.SaveData();
            //yield return StartCoroutine(fade.FadeIn(2f));
            yield break;
        }
    }

    IEnumerator LoadMain()
    {
        InventoryManager.Instance.SaveData();
        //SceneFader fade = Instantiate(sceneFaderPrefab);
        //yield return StartCoroutine(fade.FadeOut(2f));
        yield return SceneManager.LoadSceneAsync("Main Menu");
        //yield return StartCoroutine(fade.FadeIn(2f));
        yield break;
    }

    /*public void EndNotify()
    {
        if (fadeFinished)
        {
            fadeFinished = false;
            StartCoroutine(LoadMain());
        }
    }*/
}

