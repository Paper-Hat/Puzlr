using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

//Player score is based upon four pillars:
//1) Tile drop rate (gameplay speed)
//2) Total number of matched tiles
//3) Number of overall matches
//4) Number of matched tiles above 3 in a given match
public class ScoreHandler : IPuzlGameComponent
{
    public delegate void OnPointsGained(); 
    public event OnPointsGained pointsGained;

    public delegate void OnScoreUpdate();

    public event OnScoreUpdate scoreUpdated;
    public int Score
    {
        get => playerScore;
        set => playerScore = value;
    }
    private int playerScore = 0;
    public int TotalMatches => numTotalMatches;
    [SerializeField][ReadOnly(true)] private int numTotalMatches = 0;
    [SerializeField] private int totalMatchThreshold = 10;
    private int totalMatchMultiplier = 1;
    
    [SerializeField][ReadOnly(true)] private int numMatchedTiles = 0;
    [SerializeField] private int matchedTileThreshold = 100;
    private int matchedTileMultiplier = 1;

    [SerializeField] private int excessMatchMultiplier = 3;
    
    public ScoreHandler()
    {
        playerScore = 0;
    }

    public PuzlBoard Board { get; set; }

    public void SetBoardRef(PuzlBoard board)
    {
        Board = board;
        Board.foundMatches += UpdateScoreWithMatches;
    }
    public void ResetScore()
    {
        playerScore = 0;
        numTotalMatches = 0;
        numMatchedTiles = 0;
        scoreUpdated?.Invoke();
    }
    
    void UpdateScoreWithMatches(List<(int, int)> matches)
    {
        //base score increment is the total number of tiles in the match multiplied by speed
        //we further add to this if the match requirement exceeds the base
        //Debug.Log(matches.Count);
        playerScore += (matches.Count 
            * (int)(10 - Board.DropDelay) 
            + (excessMatchMultiplier * (Board.TilesRequiredToMatch - 3)));
        
        pointsGained?.Invoke();
        numMatchedTiles += matches.Count;
        numTotalMatches++;

        //total matches bonus
        if (numTotalMatches % totalMatchThreshold == 0)
        {
            playerScore += (totalMatchThreshold * totalMatchMultiplier);
            totalMatchMultiplier++;
            totalMatchThreshold += 10;
            pointsGained?.Invoke();
        }
        
        //total matched tile bonus
        if (numMatchedTiles % matchedTileThreshold == 0)
        {
            playerScore += (matchedTileThreshold * matchedTileMultiplier);
            matchedTileMultiplier++;
            matchedTileThreshold += 100;
            pointsGained?.Invoke();
        }
        scoreUpdated?.Invoke();
    }
    
    //getters
    public int GetTotalMatches()
    {
        return numTotalMatches;
    }

    public int GetTotalTilesMatched()
    {
        return numMatchedTiles;
    }

    public void UnsubscribeListeners()
    {
        Board.foundMatches -= UpdateScoreWithMatches;
    }

}
