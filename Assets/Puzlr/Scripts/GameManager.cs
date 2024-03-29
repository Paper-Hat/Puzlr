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
    public CountdownTimer gameStartTimer;
    private SettingsManager settings;
    [SerializeField] private Button speedUpButton;
    
    [Header("Fidget Gameplay Setting(s)")] [SerializeField]
    private Button dropButton;
    
    private int matchCounter = 1;
    private int numTilesDropped;
    
    private Coroutine GameLoop;
    private Coroutine speedingUp;
    private bool _setup_complete;
    private bool continuePlaying = true;
    private bool speedUp;


    void Awake()
    {
        
    }
    
#if UNITY_EDITOR
    [Header("Test Scene")] 
    [SerializeField] private SettingsManager.GameType testGameType;
    [SerializeField] private bool tester;
    [SerializeField] [ReadOnly] private float currentSpeed;
#endif
    private void Start()
    {
#if UNITY_EDITOR
        if (tester)
        {
            if (SettingsManager._instance == null)
            {
                this.gameObject.AddComponent<SettingsManager>();
                settings = SettingsManager._instance;
                settings.GameMode = testGameType;
            }
            StartGame();
            return;
        }
#endif
        if (Time.timeScale == 0)
            Time.timeScale = 1;
        StartGame();
    }

    private void FixedUpdate()
    {
        if (Board != null) {
            currentSpeed = Board.TimeForNewTile;
        }

        if (Input.GetKey(KeyCode.Escape) && !PopupHandler._instance.PopupActive()) {
            PauseGame();
        }
    }


    private void StartGame()
    {
        if (settings == null)
        {
            settings = SettingsManager._instance;
        }
        SetupGame();
        GameLoop = StartCoroutine(PlayGame());
    }
    

    private void SetupGame()
    {
        if (!_setup_complete) {
            switch (SettingsManager._instance.GameMode)
            {
                case SettingsManager.GameType.Endless:
                    ConfigureGame_Default();
                    Board.FillBoardRandom(settings.DistinctTiles, settings.DefaultRowFillCount);
                    
                    break;
                case SettingsManager.GameType.Speed:
                    ConfigureGame_Default();
                    Board.FillBoardRandom(settings.DistinctTiles, settings.DefaultRowFillCount);
                    break;
                //fill entire board for "fidget" mode
                case SettingsManager.GameType.Fidget:
                    ConfigureGame_Default();
                    dropButton.gameObject.SetActive(true);
                    speedUpButton.gameObject.SetActive(false);
                    Board.FillBoardRandom(settings.DistinctTiles, settings.XDimensions);
                    break;
            }
            _setup_complete = true;
        }

    }

    void ConfigureGame_Default()
    {
        Board = new PuzlBoard(settings.XDimensions, settings.YDimensions);
        Score = new ScoreHandler();
        Score.SetBoardRef(Board);
        ScoreDisplay.SetScoreRef(Score);
        DisplayHandler.SetBoardRef(Board);
        DisplayHandler.CreateDisplay();
        Board.TilesRequiredToMatch = settings.MatchRequirement;
        Board.SetupSpeed(SettingsManager._instance.startSpeed);
        Board.boardOverflow += GameOver;
    }
    
    int tileToPlace;
    (int x, int y) locationToPlace;
    private int[] rowVals;
    private IEnumerator PlayGame()
    {
        yield return new WaitUntil(() => _setup_complete);
        gameStartTimer.StartTimer(settings.GameStartDelay);
        yield return new WaitUntil(() => !gameStartTimer.IsCounting());
        DisplayHandler.HandleTilesCo = StartCoroutine(DisplayHandler.HandleFallingTiles(continuePlaying));
        
        while (continuePlaying) {
            if (speedUp) {
                SpeedUp();
            }
            switch (SettingsManager._instance.GameMode)
            {
                case SettingsManager.GameType.Endless:
                    //place random tile at the top of the board on the timer
                    //this gamemode is effectively "endless" mode
                    tileToPlace = Random.Range(1, settings.DistinctTiles);
                    locationToPlace = Board.RandomTile(true);
                    DisplayHandler.PreviewTile(locationToPlace.y, tileToPlace);
                    yield return new WaitForSeconds(Board.TimeForNewTile);
                    Board.PlaceTile(tileToPlace, locationToPlace, true);
                    break;
                case SettingsManager.GameType.Speed:
                    //if we've reached enough tiles to speed up, do so
                    if (Score.TotalMatches / settings.matchesUntilSpeedup == matchCounter) {
                        matchCounter++;
                        Board.ChangeSpeed(1,settings.speedFactor, settings.maxSpeed);
                    }
                    //every 10 matches, drop an entire row!
                    if (numTilesDropped >= 10 && numTilesDropped % 10 == 0) {
                        rowVals = Board.RandomRow(settings.DistinctTiles);
                        DisplayHandler.PreviewRow(rowVals);
                        yield return new WaitForSeconds(Board.TimeForNewTile);
                        Board.PlaceRow(rowVals);
                        ++numTilesDropped;
                    }
                    else
                    {
                        tileToPlace = Random.Range(1, settings.DistinctTiles);
                        locationToPlace = Board.RandomTile(true);
                        DisplayHandler.PreviewTile(locationToPlace.y, tileToPlace);
                        yield return new WaitForSeconds(Board.TimeForNewTile);
                        Board.PlaceTile(tileToPlace, locationToPlace, true);
                        ++numTilesDropped;
                    }
                    break;
                case SettingsManager.GameType.Fidget:
                    //trigger a row to drop after the drop button is pressed
                    yield return new WaitUntil(() => DisplayHandler.ReadyForDrop());
                    //simple switch pattern to enable/disable drop button
                    dropButton.interactable = false;
                    rowVals = Board.RandomRow(settings.DistinctTiles);
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
        speedingUp = StartCoroutine(SpeedCo(settings.SpeedUpCooldown));
    }

    private IEnumerator SpeedCo(float cooldown)
    {
        speedUp = false;
        Board.ChangeSpeed(1, settings.speedFactor, settings.maxSpeed);
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
        dropButton.gameObject.SetActive(false);
        string endScoreText = "Total Score: " + Score.Score + "\nTotal Matches: " + Score.TotalMatches;
        Disable();
        PopupHandler._instance.AddPopupToQueue(PopupHandler._instance["GameOver"], endScoreText);
        //trigger popups at the end of the game
        PopupHandler._instance.TriggerPopups();
        
    }

    private void OnDisable()
    {
        Disable();
    }

    void Disable()
    {
        if (DisplayHandler.HandleTilesCo != null) {
            StopCoroutine(DisplayHandler.HandleTilesCo);
            DisplayHandler.HandleTilesCo = null;
        }
        if (speedingUp != null) {
            StopCoroutine(speedingUp);
            speedingUp = null;
        }
        //DisplayHandler.UnsubscribeListeners();
        Board?.UnsubscribeListeners();
        //Score.UnsubscribeListeners();
        DOTween.Clear();
        GameLoop = null;
        matchCounter = 1;
        numTilesDropped = 0;
    }
    public void PauseGame()
    {
        ApplicationHandler._instance.TogglePause();
        PopupHandler._instance.AddPopupToQueue(PopupHandler._instance["Pause"]);
        PopupHandler._instance.TriggerPopups();

    }

    public PuzlBoard Board { get; set; }
}
