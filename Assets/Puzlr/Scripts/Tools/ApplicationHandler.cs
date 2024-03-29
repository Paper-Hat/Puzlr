using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ApplicationHandler : MonoBehaviour
{
    public static ApplicationHandler _instance;

    private void Awake()
    {
        _instance = this;
        SceneManager.sceneLoaded += HandleTimeScale;
    }

    private void HandleTimeScale(Scene arg0, LoadSceneMode arg1)
    {
        if (Time.timeScale == 0)
            Time.timeScale = 1;
    }

    public void TogglePause() 
    {
        Time.timeScale = (Time.timeScale == 0) ? 1 : 0;
    }
    public static void LoadScene(string sceneTitle)
    {
        SceneManager.LoadScene(sceneTitle);
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }
    
    
}
