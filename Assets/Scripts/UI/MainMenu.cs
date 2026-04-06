using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    Button newGameB;
    Button LoadGameB;
    Button exitGameB;

    void Awake()
    {
        newGameB = transform.GetChild(1).GetComponent<Button>();
        LoadGameB = transform.GetChild(2).GetComponent<Button>();
        exitGameB = transform.GetChild(3).GetComponent<Button>();

        newGameB.onClick.AddListener(NewGame);
        LoadGameB.onClick.AddListener(LoadGame);
        exitGameB.onClick.AddListener(QuitGame);
    }

    void NewGame()
    {
        PlayerPrefs.DeleteAll();
        SceneController.Instance.TransitionToFirstLevel();
    }

    void LoadGame()
    {
        SceneController.Instance.TransitionToLoadGame();
    }


    void QuitGame()
    {
        Application.Quit();
    }
}
