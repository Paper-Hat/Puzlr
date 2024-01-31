using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

public class PuzlBoard
{
    
    public Tile this[int x, int y]{
        get{
            return board[(x, y)];
        }
        set{
            board[(x, y)] = value;
        }
    }
    public Tile this[(int, int) coord]{
        get{
            return board[(coord.Item1, coord.Item2)];
        }
        set{
            board[(coord.Item1, coord.Item2)] = value;
        }
    }

    public int boardRows, boardColumns;
    private bool CanMatchVertical = false;
    private Dictionary<(int, int), Tile> board;
    public delegate void OnTileSwap(List<(int, int)> tilesSwapped);
    public delegate void OnFoundMatches(List<(int, int)> matchingCoords);
    public delegate void OnBoardUpdated(List<(int, int)> tilePos);
    public static event OnBoardUpdated boardUpdate;
    public static event OnTileSwap tilesSwapped;
    public static event OnFoundMatches foundMatches;

    public delegate void OnBoardOverflow(EventArgs e);

    public static event OnBoardOverflow boardOverflow;
    
    public PuzlBoard(int rows, int columns)
    {
        boardRows = rows;
        boardColumns = columns;
        InitBoard(rows, columns);
        foundMatches += SetFallingTiles;
        tilesSwapped += SetFallingTiles;
    }
    
    void InitBoard(int rows, int cols)
    {
        board = new Dictionary<(int, int), Tile>();
        for(int r = 0; r < rows;r++){
            for(int c = 0; c < cols; c++){
                board.Add((r, c), new Tile(false, 0));
            }
        }
        //board completed
    }
    public void FillBoardRandom(int numUniqueTileTypes, int defaultRowFillCount)
    {
        List<(int, int)> filledPositions = new();
        //Fill board with random tiles- if we're filling the top row, allow empty tiles to be "created"
        for(int i = 0; i < defaultRowFillCount; i++){
            for(int j = 0; j < boardColumns; j++){
                board[(i, j)].tileValue = (i == defaultRowFillCount - 1) ? Random.Range(0, numUniqueTileTypes + 1)
                                                                         : Random.Range(1, numUniqueTileTypes + 1);
                filledPositions.Add((i, j));
            }
        }
        boardUpdate?.Invoke(filledPositions);
    }

    //swaps tiles at two given coordinates
    //swap rules: 
    //  cannot swap moving tiles
    //  cannot swap two empty tiles
    //  cannot swap with unmovable tiles
    public bool SwapTiles((int, int) a, (int, int) b)
    {
        if(board[a].moving || board[b].moving){
            Debug.Log("Swap failed due to moving tile(s)");
            return false;
        }
        if(board[a].tileValue == 0 && board[b].tileValue == 0){
            Debug.Log("Attempted to swap two blank tiles.");
            return false;
        }
        if(board[a].tileValue == -1 || board[b].tileValue == -1){
            Debug.Log("Attempted to swap with a special tile");
            return false;
        }
        //Debug.Log("Succeeded in swapping.");
        //Swap tiles via deconstruction
        (board[a].tileValue, board[b].tileValue) = (board[b].tileValue, board[a].tileValue);
        List<(int, int)> swappedTiles = new List<(int, int)> { a, b };
        boardUpdate?.Invoke(swappedTiles);
        List<(int, int)> matches = ResolveMatches(a, b);
        //update board if matches are found
        if (matches.Count > 0)
        {
            Debug.Log("Found matches: " + matches);
            boardUpdate?.Invoke(matches);
            foundMatches?.Invoke(matches);
        }
        tilesSwapped?.Invoke(swappedTiles);
        return true;

    }

    #region Tile_Matching
    //checks rows and columns of swapped tiles for matches
    //our match checks assume that tiles can only be swapped horizontally
    private List<(int, int)> ResolveMatches((int x, int y) coordinate1, (int x, int y) coordinate2){
        List<(int, int)> allMatches = new();
        allMatches = allMatches.Concat(MatchesHorizontal(coordinate1))
                                .Concat(MatchesHorizontal(coordinate2))
                                .Concat(MatchesVertical(coordinate1))
                                .Concat(MatchesVertical(coordinate2)).ToList();
        if (allMatches.Count > 0) {
            foreach (var pos in allMatches) {
                board[pos].tileValue = 0;
            }
        }

        return allMatches;
    }

    //checks horizontal matches to the right given a coordinate
    private List<(int, int)> MatchesHorizontal((int x, int y) coord){
        List<(int, int)> matches = new List<(int, int)> {coord};
        Tile checkingTile = board[coord];
        //empty tile should not be checked against
        if (checkingTile.tileValue == 0) return matches;
        //iterate right, stopping if we find a tile that doesn't match
        for(int i = coord.y + 1; i < boardColumns; i++){
            Tile toCheckAgainst = board[(coord.x, i)];
            if(checkingTile.tileValue == toCheckAgainst.tileValue && !toCheckAgainst.moving){
                matches.Add((coord.x, i));
            }
            else{
                break;
            }
        }
        //iterate left, stopping if we find a tile that doesn't match
        for(int i = coord.y - 1; i >= 0; i--){
            Tile toCheckAgainst = board[(coord.x, i)];
            if(checkingTile.tileValue == toCheckAgainst.tileValue && !toCheckAgainst.moving){
                matches.Add((coord.x, i));
            }
            else{
                break;
            }
        }
        if(matches.Count <= 2)
            matches.Clear();
        return matches;
    }
    
    //checks for vertical matches on a tile given its coordinate
    private List<(int, int)> MatchesVertical((int x, int y) coord)
    {
        List<(int, int)> matches = new List<(int, int)>{coord};
        Tile checkingTile = board[coord];
        //empty tile should not be checked against
        if (checkingTile.tileValue == 0) return matches;
        //check downward
        for (int i = coord.x - 1; i >= 0; i--) {
            Tile toCheckAgainst = board[(i, coord.y)];
            if (checkingTile.tileValue == toCheckAgainst.tileValue && !toCheckAgainst.moving) {
                matches.Add((i, coord.y));
            }
            else {
                break;
            }
        }

        //check upwards
        for (int i = coord.x + 1; i < boardColumns; i++) {
            Tile toCheckAgainst = board[(i, coord.y)];
            if (checkingTile.tileValue == toCheckAgainst.tileValue && !toCheckAgainst.moving) {
                matches.Add((i, coord.y));
            }
            else
            {
                break;
            }
        }

        if(matches.Count <= 2) matches.Clear();
        return matches;
    }
    #endregion
    
    #region Falling_Tiles

    //falling tiles occur for three reasons
    //1: newly spawned tiles at the top of the board
    //2: Tiles are matched, causing tiles above them to fall
    //3: Tiles are swapped over an empty space
    void SetFallingTiles(List<(int, int)> tiles)
    {
        foreach ((int x, int y) tile in tiles) {
            Tile thisTile = board[tile];
            
            //if we submitted an empty tile, make any existing tiles above it fall
            if (thisTile.tileValue == 0) {
                if (tile.x + 1 >= boardRows) continue;
                Tile tileAbove = board[(tile.x + 1, tile.y)];
                board[tile].resolving = tileAbove.tileValue > 0;
                for (int i = tile.x + 1; i < boardColumns; i++) {
                    var iterTile = board[(i, tile.y)];
                    if (iterTile.tileValue == -1)
                        break;
                    if (iterTile.tileValue > 0) {
                        tileAbove.moving = true;
                    }
                }
                
            }
            //if it's a non-zero tile, check to see if it needs to fall
            else
            {
                if (tile.x - 1 < 0) continue;
                Tile tileBelow = board[(tile.x - 1, tile.y)];
                if (tileBelow.tileValue == 0) {
                    tileBelow.resolving = true;
                    thisTile.moving = true;
                }
                
            }
        }
    }
    #endregion
    
    #region Tile Spawning

    void PlaceTile(int value, (int x, int y) coordinate, bool fromTopOfBoard = false)
    {
        if(coordinate.x >= boardRows || coordinate.y >= boardColumns)
            Debug.LogError("Coordinate to place exceeds board size.");
        if (fromTopOfBoard) {
            if (board[coordinate].tileValue > 0) {
                boardOverflow?.Invoke(EventArgs.Empty);
                return;
            }
        }
        
        Tile modifiedTile = board[coordinate];
        modifiedTile.tileValue = value;
        modifiedTile.resolving = true;
        if (board[(coordinate.x - 1, coordinate.y)].tileValue == 0)
            modifiedTile.moving = true;
    }
    
    #endregion
    
}
