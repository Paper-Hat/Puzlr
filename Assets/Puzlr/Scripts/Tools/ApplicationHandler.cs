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
    public static void LoadScene(string sceneTitle)
    {
        SceneManager.LoadScene(sceneTitle);
    }
 
    public void QuitGame()
    {
        Application.Quit();
    }
    
    
}
