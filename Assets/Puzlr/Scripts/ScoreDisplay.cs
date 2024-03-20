
using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerTotalScore;
    [SerializeField] private TextMeshProUGUI playerNumMatches;
    [SerializeField] private TextMeshProUGUI playerNumTotalMatchedTiles;
    private ScoreHandler score;
    private void Awake()
    {
        StartCoroutine(SetupScore());
    }

    public void SetScoreRef(ScoreHandler sh)
    {
        score = sh;
    }

    private IEnumerator SetupScore()
    {
        yield return new WaitUntil(() => score != null);
        
        //TODO: Fancy +[score_value] animation
        //GameManager.Score.pointsGained += UpdatePlayerScoreText;
        score.scoreUpdated += UpdatePlayerScoreText;
        score.ResetScore();
        yield return null;
    }
    void UpdatePlayerScoreText()
    {
        playerTotalScore.text = score.Score.ToString();
        playerNumMatches.text = score.GetTotalMatches().ToString();
        playerNumTotalMatchedTiles.text = score.GetTotalTilesMatched().ToString();
    }

    private void OnDisable()
    {
        score.scoreUpdated -= UpdatePlayerScoreText;
    }
}
