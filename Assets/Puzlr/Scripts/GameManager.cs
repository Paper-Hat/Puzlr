using System;
using System.Collections;
using DG.Tweening;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
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
    [Range(0, 4)] private float speedUpCooldown = 2f;
    [SerializeField] private Button speedUpButton;
    
    [Header("Speed Gameplay Settings")]
    [SerializeField] [Range(.1f, .5f)] private float maxSpeed;
    [SerializeField] [Range(1.5f, 5f)] private float startSpeed;
    [SerializeField] [Range(10f, 100f)] private float speedFactor;
    [SerializeField] [Range(10, 100)] private int matchesUntilSpeedup;

    [Header("Fidget Gameplay Setting(s)")] [SerializeField]
    private Button dropButton;
    
    private int matchCounter = 1;
    private int numTilesDropped;
    public static GameType GameMode = GameType.Endless;
    private Coroutine GameLoop;
    private Coroutine speedingUp;
    private bool _setup_complete;
    private bool continuePlaying = true;
    private bool speedUp;
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
                    dropButton.gameObject.SetActive(true);
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
    private IEnumerator PlayGame(GameType gameMode)
    {
        yield return new WaitUntil(() => _setup_complete);
        gameStartTimer.StartTimer(GameStartDelay);
        yield return new WaitUntil(() => !gameStartTimer.IsCounting());
        DisplayHandler.HandleTilesCo = StartCoroutine(DisplayHandler.HandleFallingTiles(continuePlaying));
        
        while (continuePlaying) {
            if (speedUp) {
                SpeedUp();
            }
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
                        Board.ChangeSpeed(1,speedFactor, maxSpeed);
                    }
                    //every 10 matches, drop an entire row!
                    if (numTilesDropped >= 10 && numTilesDropped % 10 == 0) {
                        rowVals = Board.RandomRow(distinctTiles);
                        DisplayHandler.PreviewRow(rowVals);
                        yield return new WaitForSeconds(Board.TimeForNewTile);
                        Board.PlaceRow(rowVals);
                        ++numTilesDropped;
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
                    dropButton.interactable = false;
                    rowVals = Board.RandomRow(distinctTiles);
                    DisplayHandler.PreviewRow(rowVals);
                    yield return new WaitForSeconds(Board.TimeForNewTile);
                    Board.PlaceRow(rowVals);
                    DisplayHandler.SetReadyForDrop(false);
                    dropButton.interactable = true;
                    
                    break;
                default:
                    Debug.LogError("Attempted to start game with no eligible game mode selected.");
                    break;
            }
            yield return null;
        }
    }

    //ui button agnostic to game mode to allow the game to speed up at user discretion, on a preset cooldown
    public void SpeedUp()
    {
        speedingUp = StartCoroutine(SpeedCo(speedUpCooldown));
    }

    private IEnumerator SpeedCo(float cooldown)
    {
        speedUp = false;
        Board.ChangeSpeed(1, speedFactor, maxSpeed);
        speedUpButton.interactable = false;
        yield return new WaitForSeconds(cooldown);
        speedUpButton.interactable = true;
        yield return null;
    }
    public void TriggerGameOver()
    {
        GameOver(EventArgs.Empty);
    }
    private void GameOver(EventArgs e)
    {
        //turn off/disable all gameplay elements
        continuePlaying = false;
        Debug.Log("Game over.");
        string endScore = "";
        
        Board.UnsubscribeListeners();
        Score.UnsubscribeListeners();
        StopCoroutine(DisplayHandler.HandleTilesCo);
        DisplayHandler.HandleTilesCo = null;
        StopCoroutine(speedingUp);
        speedingUp = null;
        dropButton.gameObject.SetActive(false);
        DisplayHandler.UnsubscribeListeners();
        DOTween.Clear();
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
