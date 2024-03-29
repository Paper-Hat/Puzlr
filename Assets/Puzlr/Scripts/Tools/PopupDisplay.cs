using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class PopupDisplay : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI bodyText;
    private List<ButtonInfo> buttonInfo;
    //currently unused, tbd whether will move into an unlock system
    private List<Image> unlocks;

    [SerializeField] private Button[] displayButtons;
    //we'll reuse the same popup object instead of creating duplicates
    //will also just turn off the object instead of deleting/instantiating it
    [SerializeField] private GameObject popupRoot;
    [Header("Default Popup Sizes (Height)")]
    [SerializeField] private float smallSize = 400f;
    [SerializeField] private float mediumSize = 650f;
    [SerializeField] private float largeSize = 900f;
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        displayButtons = GetComponentsInChildren<Button>(true);
    }

    public bool PopupOnDisplay()
    {
        return popupRoot.activeSelf;
    }
    public void Popup()
    {
        SetPopupInfo(PopupHandler._instance.PopupsToDisplay.Dequeue());
        ConfigureButtons();
        popupRoot.SetActive(true);
    }

    public bool Active()
    {
        return popupRoot.activeSelf;
    }
    void SetPopupInfo(PopupInfo info)
    {
        headerText.text = info.headerText;
        bodyText.text = info.bodyText;
        buttonInfo = info.btnInfo;
        unlocks = info.gameUnlocks;
        ConfigureRoot(info.height);
    }

    void ConfigureRoot(PopupInfo.PopupSize height)
    {
        RectTransform rtf = (RectTransform)popupRoot.transform;
        switch (height)
        {
            case PopupInfo.PopupSize.Small:
                rtf.sizeDelta = new Vector2(rtf.sizeDelta.x, smallSize);
                break;
            case PopupInfo.PopupSize.Medium:
                rtf.sizeDelta = new Vector2(rtf.sizeDelta.x, mediumSize);
                break;
            case PopupInfo.PopupSize.Large:
                rtf.sizeDelta = new Vector2(rtf.sizeDelta.x, largeSize);
                break;
            default:
                rtf.sizeDelta = new Vector2(rtf.sizeDelta.x, mediumSize);
                break;
        }
    }
    #region Button_Config
    void ConfigureButtons()
    {
        ResetButtons();
        //if our button array is empty, then we just provide a "Continue" button that closes the popup
        if (!buttonInfo.Any())
        {
            GameObject singleButtonGO = displayButtons[0].gameObject;
            displayButtons[0].onClick.AddListener(ClosePopup);
            SetButtonText(displayButtons[0], "n i c e");
            singleButtonGO.SetActive(true);
            
        }
        
        //set up each button
        for (int i = 0; i < buttonInfo.Count; i++)
        {
            int iterVal = i;
            SetButtonText(displayButtons[i], buttonInfo[i].buttonText);
            displayButtons[i].gameObject.SetActive(true);
            if (string.IsNullOrEmpty(buttonInfo[i].sceneToLoad)) {
                displayButtons[i].onClick.AddListener(ClosePopup);
                displayButtons[i].onClick.AddListener(ApplicationHandler._instance.TogglePause);
            }
            else
            {
                displayButtons[i].onClick.AddListener(()=>ApplicationHandler.LoadScene(buttonInfo[iterVal].sceneToLoad));
            }
        }
    }
    
    void SetButtonText(Button b, string text)
    {
        TextMeshProUGUI buttonText = b.GetComponentInChildren<TextMeshProUGUI>();
        buttonText.text = text;
    }

    void ResetButtons()
    {
        foreach (Button b in displayButtons) {
            b.onClick.RemoveAllListeners();
            b.gameObject.SetActive(false);
        }
    }
    #endregion
    public void ClosePopup()
    {
        ResetButtons();
        popupRoot.SetActive(false);
        //if we have more queued popups, display the next one
        if (PopupHandler._instance.PopupsToDisplay.Any()) {
            Popup();
        }
    }
}
