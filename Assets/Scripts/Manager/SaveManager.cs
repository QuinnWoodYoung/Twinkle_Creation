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
        CharacterData_SO playerData = ResolvePlayerCharacterData();
        if (playerData == null)
        {
            return;
        }

        Save(playerData, playerData.name);
    }

    public void SaveToLoadPlayer()
    {
        CharacterData_SO playerData = ResolvePlayerCharacterData();
        if (playerData == null)
        {
            return;
        }

        SaveToLoad(playerData, playerData.name);
        SavePlayerPosition();
    }

    public void LoadPlayerData()
    {
        CharacterData_SO playerData = ResolvePlayerCharacterData();
        if (playerData == null)
        {
            return;
        }

        Load(playerData, playerData.name);
    }

    public void Save(Object data, string key)
    {
        var jsonData = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString(key, jsonData);
        PlayerPrefs.SetString(sceneName, SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
    }

    public void SaveToLoad(Object data, string key)
    {
        var jsonData = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString(key, jsonData);
        PlayerPrefs.SetString(sceneName, SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
    }

    public void Load(Object data, string key)
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
        Transform playerTransform = ResolvePlayerTransform();
        if (playerTransform == null)
        {
            return;
        }

        Vector3 playerPosition = playerTransform.position;
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
            Transform playerTransform = ResolvePlayerTransform();
            if (playerTransform != null)
            {
                playerTransform.position = playerPosition;
            }
        }
    }

    private CharacterData_SO ResolvePlayerCharacterData()
    {
        if (GameManager.Instance == null)
        {
            return null;
        }

        return GameManager.Instance.PlayerCharacterData;
    }

    private Transform ResolvePlayerTransform()
    {
        if (GameManager.Instance == null)
        {
            return null;
        }

        return GameManager.Instance.PlayerTransform;
    }
}
