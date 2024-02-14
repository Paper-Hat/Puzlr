using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerScoreText;

    private void Awake()
    {
        StartCoroutine(SetupScore());
    }

    private IEnumerator SetupScore()
    {
        yield return new WaitUntil(() => GameManager.Score != null);
        
        //TODO: Fancy +[score_value] next to player score that "vacuums" itself into score value
        //GameManager.Score.pointsGained += UpdatePlayerScoreText;
        GameManager.Score.scoreUpdated += UpdatePlayerScoreText;
        GameManager.Score.ResetScore();
        yield return null;
    }
    void UpdatePlayerScoreText()
    {
        playerScoreText.text = ""+GameManager.Score.Score;
    }

    private void OnDisable()
    {
        GameManager.Score.scoreUpdated -= UpdatePlayerScoreText;
    }
}
