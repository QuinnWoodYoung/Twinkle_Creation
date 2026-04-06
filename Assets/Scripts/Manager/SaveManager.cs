using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : Singleton<SaveManager>
{
    string sceneName = "";

    public string SceneName { get { return PlayerPrefs.GetString(sceneName); } }
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
    }
    
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && SceneController.Instance.name != "start")
        {
            SceneController.Instance.TransitionToMain();
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("已成功保存");
            SavePlayerData();
            SaveToLoadPlayer();
        }

        
    }
    
    public void SavePlayerData()
    {
        Save(GameManager.Instance.playerStats.characterData,GameManager.Instance.playerStats.characterData.name);
    }
    public void SaveToLoadPlayer()
    {
        SaveToLoad(GameManager.Instance.playerStats.characterData, GameManager.Instance.playerStats.characterData.name);
        SavePlayerPosition();
    }
    
    public void LoadPlayerData()
    {
        Load(GameManager.Instance.playerStats.characterData,GameManager.Instance.playerStats.characterData.name);
    }
    
    public void Save(Object data,string key)
    {
        var jsonData = JsonUtility.ToJson(data,true);
        PlayerPrefs.SetString(key,jsonData);
        PlayerPrefs.SetString(sceneName,SceneManager.GetActiveScene().name);

        PlayerPrefs.Save();
    }

    public void SaveToLoad(Object data, string key)
    {
        var jsonData = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString(key, jsonData);
        PlayerPrefs.SetString(sceneName, SceneManager.GetActiveScene().name);

        PlayerPrefs.Save();
    }


    public void Load(Object data,string key)
    {
        string scene = PlayerPrefs.GetString(sceneName);
        if (PlayerPrefs.HasKey(key))
        {
            if (SceneManager.GetActiveScene().name == scene)
            {
                JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(key), data);
                LoadPlayerPosition();
            }
            else
            {
                SavePlayerPosition();
                JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(key), data);
            }
        }
    }
    public void SavePlayerPosition()
    {
        Vector3 playerPosition = GameManager.Instance.playerStats.transform.position;
        string playerPositionJson = JsonUtility.ToJson(playerPosition);
        PlayerPrefs.SetString("PlayerPosition", playerPositionJson);
        PlayerPrefs.Save();
    }

    public void LoadPlayerPosition()
    {
        if (PlayerPrefs.HasKey("PlayerPosition"))
        {
            string playerPositionJson = PlayerPrefs.GetString("PlayerPosition");
            Vector3 playerPosition = JsonUtility.FromJson<Vector3>(playerPositionJson);
            GameManager.Instance.playerStats.transform.position = playerPosition;
        }
    }
}
