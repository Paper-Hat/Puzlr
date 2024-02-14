using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

public class CountdownTimer : MonoBehaviour
{
    private Coroutine timerCo;
    [SerializeField] private TextMeshProUGUI timerText;
    //set a root if timer should disable the object upon completion
    [SerializeField] private GameObject objectRoot;
    private bool counting;
    public void StartTimer(float timeToWait)
    {
        if(objectRoot != null && !objectRoot.activeSelf)
            objectRoot.SetActive(true);
        timerCo = StartCoroutine(StartTimerCo(timeToWait));
    }

    public bool IsCounting() { return counting;}
    private IEnumerator StartTimerCo(float timeToWait)
    {
        float timerDelta = 0.0f;
        counting = true;
        do
        {
            timerDelta += Time.deltaTime;
            timerText.text = "" + Mathf.Ceil(timeToWait - timerDelta);
            yield return null;
        } while (timerDelta < timeToWait);

        if (objectRoot != null) {
            objectRoot.SetActive(false);
        }

        counting = false;
        timerCo = null;
        

    }
}
