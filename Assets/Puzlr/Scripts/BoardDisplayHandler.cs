using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Unity.Collections;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UI;

public class BoardDisplayHandler : MonoBehaviour
{
    private Dictionary<(int, int), TileDisplay> boardDisplay;
    [SerializeField] [ReadOnly] private List<TilePreview> previewObjects;
    [SerializeField] private RectTransform boardViewport;
    [SerializeField] private RectTransform boardContentRoot;
    [SerializeField] private GameObject gameRowPrefab;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject previewerPrefab;
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
        Vector2 rowSize = new Vector2(_board.boardColumns * GameManager.TileSize, GameManager.TileSize);
        ConfigureBoard(rowSize);
        ConfigurePreviews(rowSize);
    }

    void ConfigureBoard(Vector2 rowSize)
    {
        
        //create playable game board
        for (int i = 0; i < _board.boardRows; i++) {
            
            //create row object
            GameObject rowObj = Instantiate(gameRowPrefab, boardContentRoot);
            
            //adjust it to content delta
            RectTransform rowRect = rowObj.GetComponent<RectTransform>();
            rowRect.sizeDelta = rowSize;
            
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
    //previews placed directly above content root
    void ConfigurePreviews(Vector2 rowSize)
    {
        //create previewers
        Vector3 boardContentRootPos = boardContentRoot.transform.position;
        RectTransform contentRootRect = (RectTransform)boardContentRoot;
        Vector3 previewerRowPos = new Vector3(boardContentRootPos.x,
            boardContentRootPos.y + (0.5f * contentRootRect.rect.height) + (0.5f * GameManager.TileSize), 0f);
        GameObject previewerRowObj = Instantiate(gameRowPrefab, gameObject.transform);
        RectTransform previewerRect = (RectTransform)previewerRowObj.transform;
        previewerRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewerRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewerRect.sizeDelta = rowSize;
        previewerRowObj.transform.position = previewerRowPos;
#if UNITY_EDITOR
        previewerRowObj.name = "Previewers";
#endif
        for (int i = 0; i < _board.boardColumns; i++)
        {
            TilePreview previewer = Instantiate(previewerPrefab, previewerRowObj.transform).GetComponent<TilePreview>();
            previewObjects.Add(previewer);
        }
    }
    void UpdateDisplay(List<(int, int)> tilePos)
    {
        PuzlBoard board = GameManager.Board;
        foreach (var pos in tilePos) {
            Tile gameTile = board[(pos)];
            TileDisplay td = boardDisplay[(pos)];
            td.ConfigureImage(GameManager._instance.tileColors[gameTile.tileValue]);
        }
    }

    public void PreviewTile(int column, int value)
    {
        if(previewObjects[column].previewerCo == null)
            previewObjects[column].PreviewTile(GameManager._instance.tileColors[value]);
        else
        {
            Debug.Log("Attempted to preview when a preview was already playing.");
        }
    }
    private void LateUpdate()
    {
        if(_board != null && _board.GetFallingTiles().Any()) {
            foreach (var tilePos in _board.GetFallingTiles()) {
                _board[tilePos].tileDrop ??= StartCoroutine(_board.DropTile(tilePos));
            }
        }
    }

    
}
