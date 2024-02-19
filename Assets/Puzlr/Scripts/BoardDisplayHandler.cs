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
    [SerializeField] private RectTransform boardViewport;
    [SerializeField] private RectTransform boardContentRoot;
    [SerializeField] private GameObject gameRowPrefab;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject previewerPrefab;
    public static int TileSize = 64;
    public List<Color> tileColors;
    

    public static void SetTileSize(int size)
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
        int screenConstraint = (Screen.width <= Screen.height) ? Screen.width : Screen.height;
        int boardConstraint = (Board.boardColumns >= Board.boardRows) ? Board.boardColumns : Board.boardRows;
        //subtract by a factor of 1 tile to make room for indicators
        int combinedConstraint = (screenConstraint - (screenConstraint / boardConstraint)) / boardConstraint;
        SetTileSize(combinedConstraint);
        Debug.Log(screenConstraint +", "+ boardConstraint +" : "+TileSize);
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
            boardContentRootPos.y + (0.5f * contentRootRect.rect.height) + (0.25f * TileSize), 0f);
        GameObject previewerRowObj = Instantiate(gameRowPrefab, gameObject.transform);
        RectTransform previewerRect = (RectTransform)previewerRowObj.transform;
        previewerRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewerRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewerRect.sizeDelta = new Vector2(rowSize.x, rowSize.y / 2);
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
    void UpdateDisplay(List<(int, int)> tilePos)
    {
        foreach (var pos in tilePos) {
            Tile gameTile = Board[pos];
            TileDisplay td = boardDisplay[pos];
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
                    if(Board.CanSwap(tilePos, swapPos))
                        selectedTile.moving = boardDisplay[swapPos].moving = StartCoroutine(HandleTileSwapping(tilePos, swapPos, PuzlBoard.BoardDir.Left));
                    break;
                case Controls.Direction.Right:
                    swapPos = Board.GetTile(tilePos, PuzlBoard.BoardDir.Right);
                    if(Board.CanSwap(tilePos, swapPos))
                        selectedTile.moving = boardDisplay[swapPos].moving = StartCoroutine(HandleTileSwapping(tilePos, swapPos, PuzlBoard.BoardDir.Right));
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
        tileDisplay.moving = null;
        otherDisplay.moving = null;
        yield return null;
    }

    TileDisplay GetSelectedTile()
    {
        return boardDisplay.FirstOrDefault(x => x.Value.selected).Value;
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
        Controls.OnDragEnded -= SwapTile;
    }
}
