using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using DG.Tweening;
using Unity.VisualScripting;
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
    public Coroutine swapping;
    public Tween dropTween;
    private bool selected;
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
    void IndicateSwap()
    {
        //filter based on contact with the controls screen, only start coroutine if we've got the right tile
        if (worldRect.Contains(Controls.MousePos, true)) {
            //if we can't swap this tile, flash
            if (moving != null || Board[tilePos].resolving) {
                Debug.Log("Flashing.");
                Flash();
                return;
            }
            if (Board[tilePos].tileValue <= 0) {
                Debug.Log("Can't swap empty tiles.");
                return;
            }
            selected = true;
            swapping = StartCoroutine(HandleSwapIndicator());
        }

        
    }
    public IEnumerator HandleSwapIndicator()
    {
        swapIndicator.enabled = true;
        swapIndicator.fillMethod = Image.FillMethod.Vertical;
        swapIndicator.fillAmount = 0f;
        //parent object contains mask
        var indicatorRot = swapIndicator.gameObject.transform.parent;
        while (Controls.Dragging)
        {
            Debug.Log(Controls.DragDirection);
            //while dragging, set the rotation of the arrow based on the direction of the drag
            switch (Controls.DragDirection)
            {
                case Controls.Direction.Left:
                    indicatorRot.eulerAngles = new(0f, 0f, -90f);
                    break;
                case Controls.Direction.Right:
                    indicatorRot.eulerAngles = new(0f, 0f, 90f);
                    break;
                case Controls.Direction.Up:
                    break;
                case Controls.Direction.Down:
                    break;
            }
            
            //set the fill amount on the arrow based as a percentage of the drag threshold to better indicate swapping
            swapIndicator.fillAmount = Controls.DragCompletion;
            yield return null;
        }
        swapIndicator.transform.rotation = Quaternion.identity;
        swapIndicator.enabled = false;
        swapping = null;
        yield return null;
    }

    //indicate with a semi-transparent frame
    void Flash()
    {
        Debug.Log("Not yet implemented.");
    }
    #endregion
    
    
    void SwapTile()
    {
        if (!selected) return;
        (int, int) swapPos = (-1, -1);
        
        if (Controls.HorizontalSwapsOnly)
        {
            switch (Controls.DragDirection) {
                case Controls.Direction.Left:
                    //tile on the left is always first in the swap
                    swapPos = Board.GetTile(tilePos, PuzlBoard.BoardDir.Left);
                    Board.SwapTiles(swapPos, tilePos);
                    break;
                case Controls.Direction.Right:
                    swapPos = Board.GetTile(tilePos, PuzlBoard.BoardDir.Right);
                    Board.SwapTiles( tilePos, swapPos);
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
        selected = false;
    }

    #if UNITY_EDITOR
    [SerializeField]private Tile tileInfo;
    [SerializeField][ReadOnly(true)] private int tileX;
    [SerializeField][ReadOnly(true)] private int tileY;
    [SerializeField] [ReadOnly(true)] private Rect visualWorldRect;
    void LateUpdate()
    {
        if(Board != null)
            tileInfo = Board[tilePos];
        tileX = tilePos.x;
        tileY = tilePos.y;
        visualWorldRect = worldRect;
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
        Controls.OnDragStarted -= IndicateSwap;
        Controls.OnDragEnded -= SwapTile;
        moving = null;
        swapping = null;
        dropTween?.Kill();
    }

    public PuzlBoard Board { get; set; }

    public void SetBoardRef(PuzlBoard board)
    {
        Board = board;
    }
}
