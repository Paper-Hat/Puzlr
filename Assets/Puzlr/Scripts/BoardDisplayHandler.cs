using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UI;

public class BoardDisplayHandler : MonoBehaviour
{
    private Dictionary<(int, int), TileDisplay> boardDisplay;
    [SerializeField] private RectTransform boardViewport;
    [SerializeField] private RectTransform boardContentRoot;
    [SerializeField] private GameObject gameRowPrefab;
    [SerializeField] private GameObject tilePrefab;
    public static BoardDisplayHandler _displayHandler;
    private PuzlBoard _board;
    void Awake()
    {
        _displayHandler = this;
        
        PuzlBoard.boardUpdate += UpdateDisplay;
    }

    public void SetBoardRef(PuzlBoard board)
    {
        _board = board;
    }
    //set width and height of canvas dependant on board size
    //let's assume cell size is always 64x64 squares
    public void CreateDisplay()
    {
        boardDisplay = new();
        boardViewport.sizeDelta = new Vector2(_board.boardColumns * GameManager.TileSize, _board.boardRows * GameManager.TileSize);
        boardContentRoot.sizeDelta = new Vector2(_board.boardColumns * GameManager.TileSize, _board.boardRows * GameManager.TileSize);
        
        
        for (int i = 0; i < _board.boardRows; i++) {
            
            //create row object
            GameObject rowObj = Instantiate(gameRowPrefab, boardContentRoot);
            
            //adjust it to content delta
            RectTransform rowRect = rowObj.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(_board.boardColumns * GameManager.TileSize, GameManager.TileSize);
            
            //create display cells within each row
            for (int j = 0; j < _board.boardColumns; j++)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowRect);
                TileDisplay display = Instantiate(tilePrefab, rowObj.transform).GetComponent<TileDisplay>();
                display.SetPos((i, j));
                boardDisplay.Add((i, j), display);
                
            }
        }
        
        UpdateDisplay(boardDisplay.Keys.ToList());
    }
    
    void UpdateDisplay(List<(int, int)> tilePos)
    {
        PuzlBoard board = GameManager.Board;
        foreach (var pos in tilePos) {
            Tile gameTile = board[(pos)];
            TileDisplay td = boardDisplay[(pos)];
            if (gameTile.tileValue == 0) {
                td.ConfigureImage(true);
                continue;
            }
            td.ConfigureImage(false, GameManager._instance.tileSprites[gameTile.tileValue]);
        }
    }

    private void LateUpdate()
    {
        if(_board.GetFallingTiles().Any()) {
            foreach (var tilePos in _board.GetFallingTiles()) {
                _board[tilePos].tileDrop ??= StartCoroutine(_board.DropTile(tilePos));
            }
        }
    }

    
}
