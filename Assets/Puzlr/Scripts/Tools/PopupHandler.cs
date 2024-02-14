using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class PopupHandler : MonoBehaviour
{
    public PopupInfo this[string popupHeader] => popups[popupHeader];
    public Queue<PopupInfo> PopupsToDisplay;
    private Dictionary<string, PopupInfo> popups;
    public static PopupHandler _instance;
    private PopupDisplay display;
    void Awake()
    {
        _instance = this;
        PopupsToDisplay = new Queue<PopupInfo>();
        display = GameObject.Find("PopupDisplay").GetComponent<PopupDisplay>();
        SceneManager.sceneLoaded += ClearOnTransition;
        
        ConfigureAvailablePopups();
    }

    public void AddPopupToQueue(PopupInfo popupInfo, string bodyTextOverride = null)
    {
        if (!string.IsNullOrEmpty(bodyTextOverride))
            popupInfo.bodyText = bodyTextOverride;
        PopupsToDisplay.Enqueue(popupInfo);
    }

    public void AddPopupsToQueue(List<PopupInfo> popupInfos)
    {
        foreach (var info in popupInfos) {
             PopupsToDisplay.Enqueue(info);
        }
    }

    public void TriggerPopups()
    {
        display.Popup();
    }

    private void ClearOnTransition(Scene arg0, LoadSceneMode loadSceneMode)
    {
        ClearPopups();
    }
    public void ClearPopups()
    {
        if(display.Active())
            display.ClosePopup();
        PopupsToDisplay.Clear();
    }
    
    void ConfigureAvailablePopups()
    {
        popups = new Dictionary<string, PopupInfo>();
        Object[] popupObjs = Resources.LoadAll("Popups", typeof(ScriptableObject));
        foreach (var obj in popupObjs) {
            PopupInfo info = (PopupInfo)obj;
            popups.Add(info.headerText.Replace(" ", string.Empty), info);
        }
        /*#if UNITY_EDITOR
        Debug.Log("Loaded " + popups.Count + " popups: ");
        foreach (PopupInfo popup in popups.Values) {
            Debug.Log(popup.headerText.Replace(" ", string.Empty));
        }
        #endif*/        
    }
    
    //popup types:
    //game over (win, or lose) (Generic Text w/ continue/quit)
    //Unlock - not asap
    //new record - not asap
}
