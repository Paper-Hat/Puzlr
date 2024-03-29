using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SliderSettingsValueHandler : MonoBehaviour
{
    public TextMeshProUGUI valueOutput;
    public Slider sliderToWatch;

    public float XDimensions // 10-16
    {
        get => SettingsManager._instance.XDimensions;
        set => SettingsManager._instance.XDimensions = Mathf.RoundToInt(value);
    }

    public float YDimensions //6-12
    {
        get => SettingsManager._instance.YDimensions; 
        set => SettingsManager._instance.YDimensions = Mathf.RoundToInt(value);
    }

    public float DistinctTiles // 4-7
    {
        get => SettingsManager._instance.DistinctTiles;
        set => SettingsManager._instance.DistinctTiles = Mathf.RoundToInt(value);
    }

    public float MatchRequirement //3-5
    {
        get => SettingsManager._instance.MatchRequirement;
        set => SettingsManager._instance.MatchRequirement = Mathf.RoundToInt(value);
    }
    
    private void Start()
    {
        SetText();
    }
    
    public void SetText()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(sliderToWatch.value);
        valueOutput.text = sb.ToString();
    }


}
