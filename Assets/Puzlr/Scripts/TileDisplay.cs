using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
public class TileDisplay : MonoBehaviour, IPuzlGameComponent
{
    [SerializeField] private Image tileImage;
    [SerializeField] private Image swapIndicator;
    [ReadOnly(true)] private (int x, int y) tilePos; //column pos, row pos
    [SerializeField] private Rect worldRect;
    private Vector3 initialPos;
    public Coroutine moving;
    public Tween dropTween;
    
    #region Setup
    void Awake()
    {
        Controls.OnDragStarted += IndicateSwap;
        Controls.OnDragEnded += SwapTile;
    }

    private void Start()
    {
        StartCoroutine(WaitForFrame());
    }

    private IEnumerator WaitForFrame()
    {
        yield return new WaitForEndOfFrame();
        Invoke("SetInitialValues", 0.01f);
    }
    
    //rect values do not update until the end of the first frame due to parent layoutgroup, so we set values here
    public void SetInitialValues()
    {
        initialPos = transform.position;
        ConfigureWorldRect(transform.position);
    }

    public Vector3 GetInitialPos()
    {
        return initialPos;
    }
    public void ConfigureImage(Color tileColor)
    {
        if (tileColor.a == 0f)
        {
            tileImage.enabled = false;
        }
        else
        {
            tileImage.enabled = true;
            tileImage.color = tileColor;
        }
    }

    public void SetDisplaySize(int displaySize)
    {
        RectTransform thisRect = (RectTransform)transform;
        thisRect.sizeDelta = new Vector2(displaySize, displaySize);
        foreach (RectTransform t in transform) {
            t.sizeDelta = new Vector2(displaySize, displaySize);
        }
    }
    public void SetPos((int x, int y) pos)
    {
        tilePos = pos;
    }

    private void ConfigureWorldRect(Vector3 position)
    {
        var realPos = position;
        var rectPos = new Vector3(realPos.x - 0.5f*BoardDisplayHandler.TileSize, realPos.y + 0.5f*BoardDisplayHandler.TileSize);
        worldRect = Rect.MinMaxRect(rectPos.x,
            rectPos.y,
            (rectPos.x + BoardDisplayHandler.TileSize),
            (rectPos.y - BoardDisplayHandler.TileSize));
    }
    #endregion
    
    #region Handling_Visuals

    //fire this off on starting a drag
    public void IndicateSwap((int x, int y) dragStartPos)
    {
        //filter based on contact with the controls screen, only start coroutine if we've got the right tile
        //if we can't swap this tile, flash
    }
    public IEnumerator HandleSwapIndicator()
    {
        while (Controls.Dragging)
        {
            //while dragging, set the rotation of the arrow based on the direction of the drag
            //set the fill amount on the arrow based as a percentage of the drag threshold to better indicate swapping
            yield return null;
        }
        yield return null;
    }

    //indicate with a semi-transparent frame
    void Flash()
    {
        
    }
    #endregion
    //if the drag started within the bounds of our display, then attempt to swap the tile based on direction of the swipe
    void SwapTile(((int, int), (int, int)) dragVal)
    {
        Controls.Direction dir = Controls.GetCardinalDirectionFromDrag(dragVal);
        Tile thisTile = Board[tilePos];
        //did we hit this tile, is it a swappable tile type, is it falling, or is it resolving?
        if (!worldRect.Contains(new Vector2(dragVal.Item2.Item1, dragVal.Item2.Item2), true)
            || thisTile.tileValue < 0 || thisTile.resolving || thisTile.moving)
        {
            return;
        }
        
        
        if (Controls.HorizontalSwapsOnly)
        {
            switch (dir) {
                case Controls.Direction.Left:
                    if (tilePos.y - 1 < 0) return;
                    //tile on the left is always first in the swap
                    Board.SwapTiles(Board.GetTile(tilePos, PuzlBoard.BoardDir.Left), tilePos);
                    break;
                case Controls.Direction.Right:
                    if (tilePos.y > Board.boardColumns - 1) return;
                    Board.SwapTiles(tilePos, Board.GetTile(tilePos, PuzlBoard.BoardDir.Right));
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
    }

    #if UNITY_EDITOR
    [SerializeField]private Tile tileInfo;
    [SerializeField][ReadOnly(true)] private int tileX;
    [SerializeField][ReadOnly(true)] private int tileY;
    void LateUpdate()
    {
        if(Board != null)
            tileInfo = Board[tilePos];
        tileX = tilePos.x;
        tileY = tilePos.y;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(worldRect.center,new Vector3(worldRect.width, worldRect.height, 0f));
        Handles.Label(worldRect.center, ""+tilePos);
    }
    #endif
    private void OnDisable()
    {
        Controls.OnDragEnded -= SwapTile;
    }

    public PuzlBoard Board { get; set; }

    public void SetBoardRef(PuzlBoard board)
    {
        Board = board;
    }
}
