using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
[RequireComponent(typeof(Image))]
public class TileDisplay : MonoBehaviour
{
    [SerializeField] private Image tileImage;
    private (int x, int y) tilePos; //cols, rows
    [SerializeField] private Rect worldRect;
    private bool selected = false;
    void Awake()
    {
        Controls.OnDragged += SwapTile;
    }
    
    #if UNITY_EDITOR
    [SerializeField]private int x;
    [SerializeField]private int y;
    void FixedUpdate()
    {
        x = (int)transform.position.x;
        y = (int)transform.position.y;
        
    }
    #endif
    
    public void ConfigureImage(bool hide, Sprite toSet = null)
    {
        Controls.OnDragged -= SwapTile;

        if (hide)
            tileImage.enabled = false;
        else {
            tileImage.enabled = true;
            Controls.OnDragged += SwapTile;
        }
        tileImage.sprite = toSet;
    }
    public void SetPos((int x, int y) pos)
    {
        tilePos = pos;
    }
    
    //if the drag started within the bounds of our display, then attempt to swap the tile based on direction of the swipe
    void SwapTile(((int, int), (int, int)) dragVal)
    {
        if (worldRect == Rect.zero) {
            var realPos = transform.position;
            var rectPos = new Vector3(realPos.x - 0.5f*GameManager.TileSize, realPos.y + 0.5f*GameManager.TileSize);
            worldRect = Rect.MinMaxRect(rectPos.x,
                rectPos.y,
                (rectPos.x + GameManager.TileSize),
                (rectPos.y - GameManager.TileSize));
        }
        
        //did we hit this tile, and is it functional?
        if (!worldRect.Contains(new Vector2(dragVal.Item2.Item1, dragVal.Item2.Item2), true)
            || GameManager.Board[tilePos].tileValue <= 0)
        {
            return;
        }

        Debug.Log("Valid swap location(s): " + tilePos + "\n " + dragVal.Item1);
        Controls.Direction dir = Controls.GetCardinalDirectionFromDrag(dragVal);
        if (Controls.HorizontalSwapsOnly)
        {
            switch (dir) {
                case Controls.Direction.Left:
                    if (tilePos.y - 1 < 0) return;
                    GameManager.Board.SwapTiles((tilePos.x, tilePos.y - 1), (tilePos.x, tilePos.y));
                    break;
                case Controls.Direction.Right:
                    if (tilePos.y >= GameManager.Board.boardColumns) return;
                    GameManager.Board.SwapTiles((tilePos.x, tilePos.y + 1), (tilePos.x, tilePos.y));
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(worldRect.center,new Vector3(worldRect.width, worldRect.height, 0f));
        Handles.Label(worldRect.center, ""+tilePos);
    }
    
}
