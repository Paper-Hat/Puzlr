using System;
using System.Collections;
using Unity.Collections;
using UnityEngine;
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
    
    [Header("Speed Gameplay Settings")]
    [SerializeField] [Range(.1f, .5f)] private float maxSpeed;
    [SerializeField] [Range(3f, 5f)] private float startSpeed;
    [SerializeField] [Range(10f, 30f)] private float speedIncrement;
    [SerializeField] [Range(10, 100)] private int matchesUntilSpeedup;
    private int matchCounter = 1;
    private int numTilesDropped;
    public static GameType GameMode = GameType.Endless;
    public Coroutine GameLoop;
    private bool _setup_complete;
    private bool continuePlaying = true;
    public enum GameType
    {
        Endless,
        Speed,
        Fidget,
    }

    void Awake()
    {
        
    }
    
#if UNITY_EDITOR
    [Header("Test Scene")] 
    [SerializeField] private GameType testGameType;
    [SerializeField] private bool tester;
    [SerializeField] [ReadOnly] private float currentSpeed;
    private void Start()
    {
        if (tester)
        {
            
            StartGame(testGameType);
            return;
        }
        StartGame(GameMode);
    }

    private void FixedUpdate()
    {
        if (Board != null) {
            currentSpeed = Board.TimeForNewTile;
        }
    }
#endif
    public void StartGame(GameType selectedGameType)
    {
        SetupGame(selectedGameType);
        GameLoop = StartCoroutine(PlayGame(selectedGameType));
    }
    

    private void SetupGame(GameType mode)
    {
        if (!_setup_complete) {
            GameMode = mode;
            switch (mode)
            {
                case GameType.Endless:
                    ConfigureGame_Default();
                    Board.SetupSpeed(startSpeed);
                    Board.FillBoardRandom(distinctTiles, defaultRowFillCount);
                    
                    break;
                case GameType.Speed:
                    ConfigureGame_Default();
                    Board.FillBoardRandom(distinctTiles, defaultRowFillCount);
                    break;
                //fill entire board for "fidget" mode
                case GameType.Fidget:
                    ConfigureGame_Default();
                    Board.FillBoardRandom(distinctTiles, xDimensions);
                    break;
            }
            _setup_complete = true;
        }

    }

    void ConfigureGame_Default()
    {
        Board = new PuzlBoard(xDimensions, yDimensions);
        Score = new ScoreHandler();
        Score.SetBoardRef(Board);
        ScoreDisplay.SetScoreRef(Score);
        DisplayHandler.SetBoardRef(Board);
        DisplayHandler.CreateDisplay();
        Board.TilesRequiredToMatch = MatchRequirement;
        Board.boardOverflow += GameOver;
    }
    
    int tileToPlace;
    (int x, int y) locationToPlace;
    private int[] rowVals;
    public IEnumerator PlayGame(GameType gameMode)
    {
        yield return new WaitUntil(() => _setup_complete);
        gameStartTimer.StartTimer(GameStartDelay);
        yield return new WaitUntil(() => !gameStartTimer.IsCounting());
        DisplayHandler.dropButton.gameObject.SetActive(GameMode == GameType.Fidget);

        DisplayHandler.HandleTilesCo = StartCoroutine(DisplayHandler.HandleFallingTiles(continuePlaying));
        while (continuePlaying) {
            
            switch (gameMode)
            {
                case GameType.Endless:
                    //place random tile at the top of the board on the timer
                    //this gamemode is effectively "endless" mode
                    tileToPlace = Random.Range(1, distinctTiles);
                    locationToPlace = Board.RandomTile(true);
                    DisplayHandler.PreviewTile(locationToPlace.y, tileToPlace);
                    yield return new WaitForSeconds(Board.TimeForNewTile);
                    Board.PlaceTile(tileToPlace, locationToPlace, true);
                    
                    break;
                case GameType.Speed:
                    //if we've reached enough tiles to speed up, do so
                    if (Score.TotalMatches / matchesUntilSpeedup == matchCounter) {
                        matchCounter++;
                        Board.ChangeSpeed(1,speedIncrement, maxSpeed);
                    }
                    //every 10 matches, drop an entire row!
                    if (numTilesDropped >= 10 && numTilesDropped % 10 == 0) {
                        rowVals = Board.RandomRow(distinctTiles);
                        DisplayHandler.PreviewRow(rowVals);
                        yield return new WaitForSeconds(Board.TimeForNewTile);
                        Board.PlaceRow(rowVals);
                    }
                    else
                    {
                        tileToPlace = Random.Range(1, distinctTiles);
                        locationToPlace = Board.RandomTile(true);
                        DisplayHandler.PreviewTile(locationToPlace.y, tileToPlace);
                        yield return new WaitForSeconds(Board.TimeForNewTile);
                        Board.PlaceTile(tileToPlace, locationToPlace, true);
                        ++numTilesDropped;
                    }
                    break;
                case GameType.Fidget:
                    //trigger a row to drop after the drop button is pressed
                    yield return new WaitUntil(() => DisplayHandler.ReadyForDrop());
                    //simple switch pattern to enable/disable drop button
                    DisplayHandler.dropButton.enabled = false;
                    rowVals = Board.RandomRow(distinctTiles);
                    DisplayHandler.PreviewRow(rowVals);
                    yield return new WaitForSeconds(Board.TimeForNewTile);
                    Board.PlaceRow(rowVals);
                    DisplayHandler.SetReadyForDrop(false);
                    DisplayHandler.dropButton.enabled = true;
                    
                    break;
                default:
                    Debug.LogError("Attempted to start game with no eligible game mode selected.");
                    break;
            }
            yield return null;
        }
    }

    public void TriggerGameOver()
    {
        GameOver(EventArgs.Empty);
    }
    private void GameOver(EventArgs e)
    {
        continuePlaying = false;
        Debug.Log("Game over.");
        string endScore = "";
        Board.UnsubscribeListeners();
        Score.UnsubscribeListeners();
        DisplayHandler.HandleTilesCo = null;
        DisplayHandler.dropButton.gameObject.SetActive(false);
        DisplayHandler.UnsubscribeListeners();
        GameLoop = null;
        matchCounter = 1;
        numTilesDropped = 0;
        #if UNITY_EDITOR
        if (tester)
            return;
        #endif
        PopupHandler._instance.AddPopupToQueue(PopupHandler._instance["GameOver"], endScore);
        //trigger popups at the end of the game
        PopupHandler._instance.TriggerPopups();
        
    }

    public PuzlBoard Board { get; set; }
}
