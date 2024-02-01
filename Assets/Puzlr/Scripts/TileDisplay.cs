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
    private (int x, int y) tilePos; //column pos, row pos
    [SerializeField] private Rect worldRect;
    void Awake()
    {
        Controls.OnDragged += SwapTile;
    }
    
    public void ConfigureImage(bool hide, Sprite toSet = null)
    {
        tileImage.enabled = !hide;
        tileImage.sprite = toSet;
    }
    public void SetPos((int x, int y) pos)
    {
        tilePos = pos;
    }

    private void ConfigureWorldRect(Vector3 position)
    {
        var realPos = position;
        var rectPos = new Vector3(realPos.x - 0.5f*GameManager.TileSize, realPos.y + 0.5f*GameManager.TileSize);
        worldRect = Rect.MinMaxRect(rectPos.x,
            rectPos.y,
            (rectPos.x + GameManager.TileSize),
            (rectPos.y - GameManager.TileSize));
    }
    //if the drag started within the bounds of our display, then attempt to swap the tile based on direction of the swipe
    void SwapTile(((int, int), (int, int)) dragVal)
    {
        ConfigureWorldRect(transform.position);
        Tile thisTile = GameManager.Board[tilePos];
        //did we hit this tile, is it a swappable tile type, is it falling, or is it resolving?
        if (!worldRect.Contains(new Vector2(dragVal.Item2.Item1, dragVal.Item2.Item2), true)
            || thisTile.tileValue < 0 || thisTile.resolving || thisTile.moving)
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
                    //tile on the left is always first in the swap
                    GameManager.Board.SwapTiles((tilePos.x, tilePos.y - 1), (tilePos.x, tilePos.y));
                    break;
                case Controls.Direction.Right:
                    if (tilePos.y >= GameManager.Board.boardColumns) return;
                    GameManager.Board.SwapTiles((tilePos.x, tilePos.y), (tilePos.x, tilePos.y + 1));
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
