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
    void Awake()
    {
        _displayHandler = this;
        PuzlBoard.boardUpdate += UpdateDisplay;
    }

    //set width and height of canvas dependant on board size
    //let's assume cell size is always 64x64 squares
    public void CreateDisplay()
    {
        PuzlBoard board = GameManager.Board;
        boardDisplay = new();
        boardViewport.sizeDelta = new Vector2(board.boardColumns * 64, board.boardRows * 64);
        boardContentRoot.sizeDelta = new Vector2(board.boardColumns * 64, board.boardRows * 64);
        
        
        for (int i = 0; i < board.boardRows; i++) {
            
            //create row object
            GameObject rowObj = Instantiate(gameRowPrefab, boardContentRoot);
            
            //adjust it to content delta
            RectTransform rowRect = rowObj.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(board.boardColumns * 64, 64);
            
            //create display cells within each row
            for (int j = 0; j < board.boardColumns; j++)
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
            //td.ConfigureWorldRect(td.transform.position);
            if (gameTile.tileValue == 0) {
                td.ConfigureImage(true);
                continue;
            }
            td.ConfigureImage(false, GameManager._instance.tileSprites[gameTile.tileValue]);
        }
    }
}
