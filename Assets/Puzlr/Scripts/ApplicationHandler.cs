using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ApplicationHandler : MonoBehaviour
{
    public static ApplicationHandler Loader;

    private void Awake()
    {
        Loader = this;
    }
    public void LoadScene(string sceneTitle)
    {
        SceneManager.LoadScene(sceneTitle);
    }

    public void LoadGame(string gameScene)
    {
        LoadScene(gameScene);
        SceneManager.sceneLoaded += OnGameLoaded;
    }
    private void OnGameLoaded(Scene scene, LoadSceneMode mode)
    {
        GameManager._instance.StartGame(GameManager.GameMode);
        SceneManager.sceneLoaded -= OnGameLoaded;
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    
    
}
