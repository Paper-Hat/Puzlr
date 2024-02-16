using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using DG.Tweening;
using TMPro;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UI;

public class BoardDisplayHandler : MonoBehaviour, IPuzlGameComponent
{
    private Dictionary<(int, int), TileDisplay> boardDisplay;
    [SerializeField] [ReadOnly] private List<TilePreview> previewObjects;
    [SerializeField] private RectTransform boardViewport;
    [SerializeField] private RectTransform boardContentRoot;
    [SerializeField] private GameObject gameRowPrefab;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject previewerPrefab;
    public static int TileSize = 64;
    public List<Color> tileColors;
    public static BoardDisplayHandler DisplayHandler;
    void Awake()
    {
        DisplayHandler = this;
    }


    public void SetTileSize(int size)
    {
        TileSize = size;
    }

    public PuzlBoard Board { get; set; }

    public void SetBoardRef(PuzlBoard board)
    {
        Board = board;
        Board.boardUpdate += UpdateDisplay;
    }
    //set width and height of canvas dependant on board size
    //let's assume cell size is always 64x64 squares
    public void CreateDisplay()
    {
        boardDisplay = new();
        boardViewport.sizeDelta = new Vector2(Board.boardColumns * TileSize, Board.boardRows * TileSize);
        boardContentRoot.sizeDelta = new Vector2(Board.boardColumns * TileSize, Board.boardRows * TileSize);
        Vector2 rowSize = new Vector2(Board.boardColumns * TileSize, TileSize);
        ConfigureBoard(rowSize);
        ConfigurePreviews(rowSize);
    }

    void ConfigureBoard(Vector2 rowSize)
    {
        
        //create playable game board
        for (int i = 0; i < Board.boardRows; i++) {
            
            //create row object
            GameObject rowObj = Instantiate(gameRowPrefab, boardContentRoot);
            
            //adjust it to content delta
            RectTransform rowRect = rowObj.GetComponent<RectTransform>();
            rowRect.sizeDelta = rowSize;
            
            //create display cells within each row
            for (int j = 0; j < Board.boardColumns; j++)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowRect);
                TileDisplay display = Instantiate(tilePrefab, rowObj.transform).GetComponent<TileDisplay>();
                display.SetBoardRef(Board);
                display.SetDisplaySize(TileSize);
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
            boardContentRootPos.y + (0.5f * contentRootRect.rect.height) + (0.5f * TileSize), 0f);
        GameObject previewerRowObj = Instantiate(gameRowPrefab, gameObject.transform);
        RectTransform previewerRect = (RectTransform)previewerRowObj.transform;
        previewerRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewerRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewerRect.sizeDelta = rowSize;
        previewerRowObj.transform.position = previewerRowPos;
#if UNITY_EDITOR
        previewerRowObj.name = "Previewers";
#endif
        for (int i = 0; i < Board.boardColumns; i++)
        {
            TilePreview previewer = Instantiate(previewerPrefab, previewerRowObj.transform).GetComponent<TilePreview>();
            previewer.SetBoardRef(Board);
            RectTransform previewerObjRect= (RectTransform)previewer.transform;
            previewerObjRect.sizeDelta = new Vector2(TileSize, TileSize);
            previewObjects.Add(previewer);
        }
    }
    void UpdateDisplay(List<(int, int)> tilePos)
    {
        foreach (var pos in tilePos) {
            Tile gameTile = Board[(pos)];
            TileDisplay td = boardDisplay[(pos)];
            td.ConfigureImage(tileColors[gameTile.tileValue]);
        }
    }

    public void PreviewTile(int column, int value)
    {
        if(previewObjects[column].previewerCo == null)
            previewObjects[column].PreviewTile(tileColors[value]);
        else
        {
            Debug.Log("Attempted to preview when a preview was already playing.");
        }
    }
    
    private IEnumerator MoveDisplay((int, int) tilePos)
    {
        (int, int) posBelow = Board.GetTile(tilePos, PuzlBoard.BoardDir.Below);
        TileDisplay tileToDrop = boardDisplay[tilePos];
        TileDisplay tileBelow = boardDisplay[posBelow];
        Vector3 initialPos = tileToDrop.GetInitialPos();
        
        //if the tile below us is solid, it should move, so we wait
        if (Board[posBelow].tileValue > 0) {
            yield return new WaitUntil(() => tileBelow.dropTween != null);
        }
        //don't allow swapping into tile positions that we're dropping into, or moving tiles for that matter
        Board[posBelow].resolving = true;
        
        //wait for "animation" to end before dropping the tile in data
        boardDisplay[tilePos].dropTween = boardDisplay[tilePos].transform.DOMoveY(initialPos.y - TileSize, Board.DropDelay, true);
        yield return boardDisplay[tilePos].dropTween.WaitForCompletion();
        Board[tilePos].tileDrop = StartCoroutine(Board.DropTile(tilePos));
        yield return new WaitUntil(() => Board[tilePos].tileDrop == null);
        
        //once tile is dropped in data, refresh the tile's position and end the coroutine
        boardDisplay[tilePos].transform.position = initialPos;
        tileToDrop.moving = null;
    }
    private void LateUpdate()
    {
        if(Board != null && Board.GetFallingTiles().Any()) {
            foreach (var tilePos in Board.GetFallingTiles()) {
                //drop the tile if it isn't already
                if (Board[tilePos].tileDrop == null && boardDisplay[tilePos].moving == null) {
                    boardDisplay[tilePos].moving = StartCoroutine(MoveDisplay(tilePos));
                }
            }
        }
    }

    private void OnDisable()
    {
        Board.boardUpdate -= UpdateDisplay;
    }
}
