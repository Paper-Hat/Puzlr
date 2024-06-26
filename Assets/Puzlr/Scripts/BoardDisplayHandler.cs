using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BoardDisplayHandler : MonoBehaviour, IPuzlGameComponent
{
    private Dictionary<(int, int), TileDisplay> boardDisplay;
    [SerializeField] [ReadOnly] private List<TilePreview> previewObjects;
    [SerializeField] private RectTransform boardControlsScreen;
    [SerializeField] private RectTransform boardContentRoot;
    [SerializeField] private GameObject gameRowPrefab;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject previewerPrefab;
    [SerializeField] private GameObject gameUIObj;
    public Coroutine HandleTilesCo;
    private bool canDrop;
    public static float TileSize = 64;
    public static Vector3 BoardOffset;
    public List<Color> tileColors;
    #region Configuration
    public static void SetTileSize(float size)
    {
        TileSize = size;
    }

    public PuzlBoard Board { get; set; }

    public void SetBoardRef(PuzlBoard board)
    {
        Board = board;
        Board.boardUpdate += UpdateDisplay;
        Controls.OnDragEnded += SwapTile;
    }
    //set width and height of canvas dependant on board size
    public void CreateDisplay()
    {
        boardDisplay = new();
        
        //configure (use smallest) tile size based on screen size; use the smaller dimension for screen, larger dimension for board
        float screenConstraint = Screen.height;
        float boardConstraint = Board.boardRows;
        //height/rows, room for previews at the top
        float combinedConstraint = (screenConstraint - (screenConstraint / boardConstraint)) / boardConstraint;
        SetTileSize(combinedConstraint);
        //Debug.Log(screenConstraint +", "+ boardConstraint +" : "+TileSize);
        boardControlsScreen.sizeDelta = new Vector2(Board.boardColumns * TileSize, Board.boardRows * TileSize);
        boardContentRoot.sizeDelta = new Vector2(Board.boardColumns * TileSize, Board.boardRows * TileSize);
        Vector2 rowSize = new Vector2(Board.boardColumns * TileSize, TileSize);
        ConfigureBoard(rowSize);
        //position/offset
        var boardTransform= boardContentRoot.transform;
        RectTransform uiRTF = (RectTransform)gameUIObj.transform;
        float uiOffset = uiRTF.sizeDelta.x * uiRTF.parent.transform.localScale.x * 0.5f;
        boardTransform.position = new Vector3((boardContentRoot.sizeDelta.x / 2f) + (Screen.width / (Board.boardColumns / 2f) - uiOffset), boardContentRoot.sizeDelta.y / 2f);
        boardControlsScreen.transform.position = boardTransform.position;
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
        Vector3 previewerRowPos = new Vector3(boardContentRootPos.x,
            Screen.height - (TileSize / 2f), 0f);
        GameObject previewerRowObj = Instantiate(gameRowPrefab, gameObject.transform);
        RectTransform previewerRect = (RectTransform)previewerRowObj.transform;
        previewerRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewerRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewerRect.sizeDelta = new Vector2(rowSize.x, rowSize.y);
        previewerRect.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
        previewerRowObj.transform.position = previewerRowPos;
#if UNITY_EDITOR
        previewerRowObj.name = "Previewers";
#endif
        for (int i = 0; i < Board.boardColumns; i++)
        {
            TilePreview previewer = Instantiate(previewerPrefab, previewerRowObj.transform).GetComponent<TilePreview>();
            previewer.SetBoardRef(Board);
            previewer.ConfigurePreviewerSize(TileSize);
            previewObjects.Add(previewer);
        }
    }

    public void SetReadyForDrop(bool ready)
    {
        canDrop = ready;
    }
    public bool ReadyForDrop()
    {
        return canDrop;
    }
    #endregion
    void UpdateDisplay(List<(int, int)> tilePos)
    {
        foreach (var pos in tilePos) {
            Tile gameTile = Board[pos];
            TileDisplay td = boardDisplay[pos];
            td.ConfigureImage(tileColors[gameTile.tileValue]);
            if(td.indicatingSwap != null && gameTile.tileValue == 0)
                td.ResetIndicator();
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

    public void PreviewRow(int[] values)
    {
        for (int i = 0; i < values.Length; ++i) {
            PreviewTile(i, values[i]);
        }
    }
    
    #region Moving_Tiles
    private IEnumerator MoveDisplay((int, int) tilePos)
    {
        (int, int) posBelow = Board.GetTile(tilePos, PuzlBoard.BoardDir.Below);
        TileDisplay tileToDrop = boardDisplay[tilePos];
        TileDisplay tileBelow = boardDisplay[posBelow];
        
        //if we try to move into a tile that's swapping, wait until it's done first and send the wait upwards
        if (tileBelow.swapping != null)
        {
            tileToDrop.waitingForSwap = true;
            yield return new WaitUntil(() => tileBelow.swapping == null);
        }
        if (tileBelow.waitingForSwap)
        {
            tileToDrop.waitingForSwap = true;
            yield return new WaitUntil(() => tileBelow.waitingForSwap == false);
        }
        //if the tile below us is solid, check again whether we should actually fall
        if (Board[posBelow].tileValue > 0) {
            if (!Board[posBelow].moving) {
                Board[tilePos].moving = false;
                tileToDrop.waitingForSwap = false;
                tileToDrop.moving = null;
                Board.ResolveMatches(tilePos, (-1, -1));
                yield break;
            }
            //otherwise, wait until it starts moving
            if(tileToDrop.waitingForSwap)
                yield return new WaitUntil(() => tileBelow.dropTween != null && tileBelow.dropTween.IsActive());
        }

        tileToDrop.waitingForSwap = false;
        Vector3 initialPos = tileToDrop.GetInitialPos();
        
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
    
    
    void SwapTile()
    {
        TileDisplay selectedTile = GetSelectedTile();
        if (selectedTile == null) return;
        (int x, int y) tilePos = selectedTile.GetPos();
        (int x, int y) swapPos;
        if (Controls.HorizontalSwapsOnly) {
            switch (Controls.DragDirection) {
                //swap if we can, given the direction of the swipe/drag
                case Controls.Direction.Left:
                    swapPos = Board.GetTile(tilePos, PuzlBoard.BoardDir.Left);
                    if(CanSwapDisplay(tilePos, swapPos))
                        selectedTile.swapping = boardDisplay[swapPos].swapping = StartCoroutine(HandleTileSwapping(tilePos, swapPos, PuzlBoard.BoardDir.Left));
                    else
                        selectedTile.Flash();
                    break;
                case Controls.Direction.Right:
                    swapPos = Board.GetTile(tilePos, PuzlBoard.BoardDir.Right);
                    if(CanSwapDisplay(tilePos, swapPos))
                        selectedTile.swapping = boardDisplay[swapPos].swapping = StartCoroutine(HandleTileSwapping(tilePos, swapPos, PuzlBoard.BoardDir.Right));
                    else
                        selectedTile.Flash();
                    break;
                default:
                    Debug.LogError("Should not have reached an up-down result with only horizontal swapping enabled.");
                    break;
            }
        }
        else
        {
            //potential for vertical swapping later
        }
        selectedTile.selected = false;
    }
    
    IEnumerator HandleTileSwapping((int x, int y) tilePos, (int x, int y) swapPos, PuzlBoard.BoardDir direction)
    {
        yield return new WaitForEndOfFrame();
        if (swapPos == (-1, -1)) yield break;
        TileDisplay tileDisplay = boardDisplay[tilePos];
        TileDisplay otherDisplay = boardDisplay[swapPos];
        Board[tilePos].resolving = true;
        Board[swapPos].resolving = true;
        //both tiles should be considered resolving while we tween the swap
        tileDisplay.swapTween = tileDisplay.transform.DOMoveX(otherDisplay.transform.position.x,Board.DropDelay * 0.2f, true);
        otherDisplay.swapTween = otherDisplay.transform.DOMoveX(tileDisplay.transform.position.x, Board.DropDelay * 0.2f, true);
        
        yield return new DOTweenCYInstruction.WaitForCompletion(tileDisplay.swapTween);
        yield return new DOTweenCYInstruction.WaitForCompletion(otherDisplay.swapTween);
        
        //tile on the left is always first in the swap
        if (direction == PuzlBoard.BoardDir.Left) {
            Board.SwapTiles(swapPos, tilePos);
        }
        else if (direction == PuzlBoard.BoardDir.Right) {
            Board.SwapTiles( tilePos, swapPos);
        }

        tileDisplay.transform.position = tileDisplay.GetInitialPos();
        otherDisplay.transform.position = otherDisplay.GetInitialPos();
        
        Board[tilePos].resolving = false;
        Board[swapPos].resolving = false;
        tileDisplay.swapping = null;
        otherDisplay.swapping = null;
        yield return null;
    }
    
    /// <summary>
    /// drops falling tiles until control is set to false at the rate of modifiable var DropDelay
    /// </summary>
    /// <param name="control"></param>
    /// <returns></returns>
    public IEnumerator HandleFallingTiles(bool control)
    {
        while (control) {
            yield return new WaitForSeconds(Board.DropDelay);
            DropFallingTiles();
            yield return null;
        }
    }
    void DropFallingTiles()
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
    
    #endregion
    #region Helpers
    TileDisplay GetSelectedTile()
    {
        return boardDisplay.FirstOrDefault(x => x.Value.selected).Value;
    }

    public bool AllPreviewsCompleted()
    {
        foreach(TilePreview p in previewObjects)
            if (p.previewerCo != null)
                return false;
        return true;
    }
    


    public bool CanSwapDisplay((int x, int y) tilePos, (int x, int y) swapPos)
    {
        //if we can't swap via board logic
        if (!Board.CanSwap(tilePos, swapPos))
            return false;
        (int, int) posAbove = Board.GetTile(swapPos, PuzlBoard.BoardDir.Above);
        if (posAbove == (-1, -1)) return true;
        TileDisplay tileAboveSwap = boardDisplay[posAbove];
        //if the tiles above are currently falling into the tiles we're swapping into
        if (tileAboveSwap.dropTween is { active: true } || tileAboveSwap.moving != null)
            return false;
        return true;
    }

    private float GetSwapTimer()
    {
        return Board.DropDelay * 0.2f;
    }
    public void UnsubscribeListeners()
    {
        if(Board != null)
            Board.boardUpdate -= UpdateDisplay;
        Controls.OnDragEnded -= SwapTile;
    }
    #endregion
    private void OnDisable()
    {
        UnsubscribeListeners();
    }
}
