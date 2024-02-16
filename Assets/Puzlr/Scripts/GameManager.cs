using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using Random = UnityEngine.Random;


public class GameManager : MonoBehaviour, IPuzlGameComponent
{

    public BoardDisplayHandler DisplayHandler;
    public ScoreHandler Score;
    public ScoreDisplay ScoreDisplay;

    [Header("GameType Agnostic Settings")] 
    public float GameStartDelay = 5f;
    public CountdownTimer gameStartTimer;
    
    [SerializeField] [Range(10, 16)] private int xDimensions;
    [SerializeField] [Range(6, 12)] private int yDimensions;
    [Range(4, 7)] public int distinctTiles = 4;
    public int defaultRowFillCount = 3;
    [Range(3, 5)] public int MatchRequirement = 3;

    
    public static GameType GameMode = GameType.Default;

    [Header("Default Game Settings")] 
    //Time before new tile drops (in seconds)  
    public int timeUntilNewTileDropped;
    
    public Coroutine GameLoop;
    private bool _setup_complete;
    private bool continuePlaying = true;
    public enum GameType
    {
        Special,
        Default
    }

    void Awake()
    {
        
    }
    
#if UNITY_EDITOR
    [Header("Test Scene")] 
    [SerializeField] private GameType testGameType;
    [SerializeField] private bool tester;
    private void Start()
    {
        if (tester)
        {
            StartGame(testGameType);
            return;
        }
        StartGame(GameMode);
    }
#endif
    public void StartGame(GameType selectedGameType)
    {
        SetupGame(selectedGameType);
        GameLoop = StartCoroutine(PlayGame(GameMode));
    }
    

    private void SetupGame(GameType mode)
    {
        if (!_setup_complete) {
            GameMode = mode;
            switch (mode)
            {
                case GameType.Default:
                    
                    Board = new PuzlBoard(xDimensions, yDimensions);
                    
                    Score = new ScoreHandler();
                    Score.SetBoardRef(Board);
                    ScoreDisplay.SetScoreRef(Score);
                    
                    DisplayHandler = BoardDisplayHandler.DisplayHandler;
                    DisplayHandler.SetBoardRef(Board);
                    DisplayHandler.CreateDisplay();
                    
                    Board.TilesRequiredToMatch = MatchRequirement;
                    Board.FillBoardRandom(distinctTiles, defaultRowFillCount);
                    Board.boardOverflow += GameOver;
                    break;
                case GameType.Special:
                    break;
            }
            _setup_complete = true;
        }

    }

    public IEnumerator PlayGame(GameType gameMode)
    {
        yield return new WaitUntil(() => _setup_complete);
        gameStartTimer.StartTimer(GameStartDelay);
        yield return new WaitUntil(() => !gameStartTimer.IsCounting());
        while (continuePlaying) {
            switch (gameMode)
            {
                case GameType.Default:
                    //place random tile at the top of the board on the timer
                    //this gamemode is effectively "endless" mode
                    
                    int tileToPlace = Random.Range(1, distinctTiles);
                    (int x, int y) locationToPlace = Board.RandomTile(true);
                    //Debug.Log("Readying value "+tileToPlace+" to place at ("+locationToPlace.Item1+", "+locationToPlace.Item2+")");
                    DisplayHandler.PreviewTile(locationToPlace.y, tileToPlace);
                    yield return new WaitForSeconds(timeUntilNewTileDropped);
                    Board.PlaceTile(tileToPlace, locationToPlace, true);
                    
                    break;
                case GameType.Special:
                    Debug.LogError("Not yet implemented.");
                    break;
                default:
                    Debug.LogError("Attempted to start game with no eligible game mode selected.");
                    break;
            }
            
            yield return null;
        }
    }

    private void GameOver(EventArgs e)
    {
        continuePlaying = false;
        Debug.Log("Game over.");
        string endScore = "";
        Board.UnsubscribeListeners();
        Score.UnsubscribeListeners();
        PopupHandler._instance.AddPopupToQueue(PopupHandler._instance["GameOver"], endScore);
        //trigger popups at the end of the game
        PopupHandler._instance.TriggerPopups();
    }

    public PuzlBoard Board { get; set; }
}
