using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    public static PuzlBoard Board;
    
    [SerializeField] [Range(10, 16)] private int xDimensions;
    [SerializeField] [Range(6, 12)]  private int yDimensions;
    public const int TileSize = 64;
    public int distinctTiles = 4;
    public int defaultRowFillCount = 3;
    public List<Sprite> tileSprites;
    public static GameManager _instance;

    void Awake()
    {
        _instance = this;
        
    }

    void Start()
    {
        Board = new PuzlBoard(xDimensions, yDimensions);
        BoardDisplayHandler._displayHandler.CreateDisplay(); 
        Board.FillBoardRandom(distinctTiles, defaultRowFillCount);
        
    }
    

}
