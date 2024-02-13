using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class GameManager : MonoBehaviour
{
    public static GameManager _instance;
    public static PuzlBoard Board;
    public static BoardDisplayHandler DisplayHandler;
    public static ScoreHandler Score;
    public static PopupHandler Popups;
    [Header("GameType Agnostic Settings")] 
    public float GameStartDelay = 5f;
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
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
#if UNITY_EDITOR
    [Header("Test Scene")] 
    [SerializeField] private GameType testGameType;
    [SerializeField] private bool fromStartMenu;
    private void Start()
    {
        if(!fromStartMenu)
            StartGame(testGameType);
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
                    DisplayHandler = BoardDisplayHandler.DisplayHandler;
                    DisplayHandler.SetBoardRef(Board);
                    DisplayHandler.CreateDisplay();
                    Board.TilesRequiredToMatch = MatchRequirement;
                    Board.FillBoardRandom(distinctTiles, defaultRowFillCount);
                    PuzlBoard.boardOverflow += GameOver;
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
        yield return new WaitForSeconds(GameStartDelay);
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
        Popups.AddPopupToQueue(Popups["GameOver"], endScore);
        //trigger popups at the end of the game
        Popups.TriggerPopups();
    }
}
