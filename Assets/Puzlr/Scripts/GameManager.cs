using System;
using System.Collections;
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

    
    public static GameType GameMode = GameType.Endless;

    [Header("Default Game Settings")] 
    //Time before new tile drops (in seconds)  
    public int timeUntilNewTileDropped;
    
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
    
    public IEnumerator PlayGame(GameType gameMode)
    {
        yield return new WaitUntil(() => _setup_complete);
        gameStartTimer.StartTimer(GameStartDelay);
        yield return new WaitUntil(() => !gameStartTimer.IsCounting());
        DisplayHandler.dropButton.gameObject.SetActive(GameMode == GameType.Fidget);
        //declare vars in outer scope
        while (continuePlaying) {
            int tileToPlace;
            (int x, int y) locationToPlace;
            switch (gameMode)
            {
                case GameType.Endless:
                    //place random tile at the top of the board on the timer
                    //this gamemode is effectively "endless" mode
                    
                    tileToPlace = Random.Range(1, distinctTiles);
                    locationToPlace = Board.RandomTile(true);
                    //Debug.Log("Readying value "+tileToPlace+" to place at ("+locationToPlace.Item1+", "+locationToPlace.Item2+")");
                    DisplayHandler.PreviewTile(locationToPlace.y, tileToPlace);
                    yield return new WaitForSeconds(timeUntilNewTileDropped);
                    Board.PlaceTile(tileToPlace, locationToPlace, true);
                    
                    break;
                case GameType.Speed:
                    
                    tileToPlace = Random.Range(1, distinctTiles);
                    locationToPlace = Board.RandomTile(true);
                    //Debug.Log("Readying value "+tileToPlace+" to place at ("+locationToPlace.Item1+", "+locationToPlace.Item2+")");
                    DisplayHandler.PreviewTile(locationToPlace.y, tileToPlace);
                    yield return new WaitForSeconds(timeUntilNewTileDropped);
                    Board.PlaceTile(tileToPlace, locationToPlace, true);
                    break;
                case GameType.Fidget:
                    //trigger a row to drop after the drop button is pressed
                    yield return new WaitUntil(() => DisplayHandler.ReadyForDrop());
                    //simple switch pattern to enable/disable drop button
                    DisplayHandler.dropButton.enabled = false;
                    int[] rowVals = new int[Board.boardColumns];
                    for (int i = 0; i < rowVals.Length; ++i) {
                        rowVals[i] = Random.Range(1, distinctTiles);
                    }
                    DisplayHandler.PreviewRow(rowVals);
                    yield return new WaitForSeconds(timeUntilNewTileDropped);
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
        DisplayHandler.dropButton.gameObject.SetActive(false);
        GameLoop = null;
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
