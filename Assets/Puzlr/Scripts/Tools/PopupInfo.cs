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

    public override string ToString()
    {
        string buttons = "Buttons: ";
        foreach (ButtonInfo bi in btnInfo)
        {
            buttons += "\n " + bi.buttonText + " : " + bi.sceneToLoad;
        }

        string unlocks = "Unlocks: ";
        foreach (Image unlock in gameUnlocks)
        {
            unlocks += "\n " + unlock;
        }
        return "Header: " + headerText + 
               "\n Body: " + bodyText + 
               "\n "+ buttons +
               "\n "+ unlocks;
    }
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