using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/PopupInfoSO", order = 1)]
public class PopupInfo : ScriptableObject
{
    public string headerText;
    public string bodyText;
    public List<ButtonInfo> btnInfo;
    public List<Image> gameUnlocks;
    
}
[System.Serializable]
public class ButtonInfo
{
    public string buttonText;
    public string sceneToLoad;

    public ButtonInfo(string btxt, string scene)
    {
        buttonText = btxt;
        sceneToLoad = scene;
    }
}