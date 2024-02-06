using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public static PuzlBoard Board;
    [Header("GameType Agnostic Settings")]
    [SerializeField] [Range(10, 16)] private int xDimensions;
    [SerializeField] [Range(6, 12)] private int yDimensions;
    public const int TileSize = 64;
    [Range(4, 7)] public int distinctTiles = 4;
    public int defaultRowFillCount = 3;
    public List<Sprite> tileSprites;
    public static GameManager _instance;
    public static GameType GameMode;

    [Header("Default Game Settings")] 
    //Time before new tile drops (in seconds)  
    public int tileDropTimer;
    
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
        
    }

    void Start()
    {
        SetupGame(GameType.Default);
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
                    BoardDisplayHandler._displayHandler.SetBoardRef(Board);
                    BoardDisplayHandler._displayHandler.CreateDisplay();
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
        while (continuePlaying) {
            switch (gameMode)
            {
                case GameType.Default:
                    //place random tile at the top of the board on the timer
                    //this gamemode is effectively "endless" mode
                    yield return new WaitForSeconds(tileDropTimer);
                    Board.PlaceTile(Random.Range(1, distinctTiles), (Board.boardRows - 1, Random.Range(0, Board.boardColumns)), true);
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
    }
    private void FixedUpdate()
    {

    }
}
