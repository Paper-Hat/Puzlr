using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

//TODO: Score display, show next tile on game board before it's spawned, transition from start to game screen
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
    public int TilesRequiredToMatch = 3;
    public float DropDelay = 1f;
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
    
    //TODO: edit s.t. spawned tiles do not create matches before gameplay begins
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
    public bool SwapTiles((int, int) a, (int, int) b, bool force = false)
    {
        if(!force){
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
            //List<(int, int)> matches = ResolveMatches(a, b);
            ResolveMatches(a, b);
            tilesSwapped?.Invoke(swappedTiles);
        }
        else
        {
            (board[a].tileValue, board[b].tileValue) = (board[b].tileValue, board[a].tileValue);
            List<(int, int)> swappedTiles = new List<(int, int)> { a, b };
            ResolveMatches(b, (-1, -1));
            boardUpdate?.Invoke(swappedTiles);
        }
        return true;
    }

    //Tiles only "move" when they fall
    //Tile below must be empty in order for another (filled tile) to fall into it
    //We should only call this on tiles flagged for moving
    public IEnumerator DropTile((int x, int y) tilePos)
    {
        yield return new WaitForSeconds(DropDelay);
        (int x, int y) newPos = GetTile(tilePos, BoardDir.Below);
        yield return new WaitUntil(() => board[newPos].tileValue == 0);
        //swapped values
        
        //now handle the flags
        //Debug.Log(tilePos);
        //empty tile is not moving, but is resolving if there's another tile above it
        board[tilePos].moving = false;
        board[tilePos].resolving =  (tilePos.x == boardRows - 1 || board[GetTile(tilePos, BoardDir.Above)].tileValue > 0);
        //tile we swapped into is no longer resolving, but continues to move if tile below is empty
        board[newPos].resolving = false;
        board[newPos].moving = (newPos.x - 1 >= 0) && (board[GetTile(newPos, BoardDir.Below)].tileValue == 0 || board[GetTile(newPos, BoardDir.Below)].moving);
        SwapTiles(tilePos, newPos, true);
        //if tile hits a spot where it's no longer moving, see if there are any matches
        if (!board[newPos].moving)
        {
            yield return new WaitForSeconds(DropDelay*0.75f);
            ResolveMatches(newPos, (-1, -1));
        }

        board[tilePos].tileDrop = null;
    }

    #region Tile_Matching
    //checks rows and columns of swapped tiles for matches
    //our match checks assume that tiles can only be swapped horizontally
    private /*List<(int, int)>*/void ResolveMatches((int x, int y) coordinate1, (int x, int y) coordinate2){
        List<(int, int)> allMatches = new();
        bool singleCoord = (coordinate2 == (-1, -1));
        
        allMatches = allMatches.Concat(MatchesHorizontal(coordinate1))
            .Concat(MatchesVertical(coordinate1)).ToList();
        if (!singleCoord) {
            allMatches = allMatches.Concat(MatchesHorizontal(coordinate2)).Concat(MatchesVertical(coordinate2)).ToList();
        }

        allMatches = allMatches.Distinct().ToList();
        if (allMatches.Count > 0) {
            foreach (var pos in allMatches) {
                board[pos].tileValue = 0;
            }
            //Debug.Log("Found matches: " + allMatches);
            boardUpdate?.Invoke(allMatches);
            foundMatches?.Invoke(allMatches);
        }
        //return allMatches;
    }

    //checks horizontal matches to the right given a coordinate
    private List<(int, int)> MatchesHorizontal((int x, int y) coord){
        List<(int, int)> matches = new List<(int, int)> {coord};
        Tile checkingTile = board[coord];
        //empty tile should not be checked against
        if (checkingTile.tileValue == 0) return new List<(int, int)>();
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

        matches = matches.Distinct().ToList();
        if(matches.Count < TilesRequiredToMatch)
            matches.Clear();
        return matches;
    }
    
    //checks for vertical matches on a tile given its coordinate
    private List<(int, int)> MatchesVertical((int x, int y) coord)
    {
        List<(int, int)> matches = new List<(int, int)>{coord};
        Tile checkingTile = board[coord];
        //empty tile should not be checked against
        if (checkingTile.tileValue == 0) return new List<(int, int)>();
        //check downward until we reach a tile that doesn't match
        for (int i = coord.x - 1; i >= 0; i--) {
            Tile toCheckAgainst = board[(i, coord.y)];
            if (checkingTile.tileValue == toCheckAgainst.tileValue && !toCheckAgainst.moving) {
                matches.Add((i, coord.y));
            }
            else {
                break;
            }
        }

        //check upwards, again until we reach a tile that doesn't match
        for (int j = coord.x + 1; j < boardRows; j++) {
            Tile toCheckAgainst = board[(j, coord.y)];
            if (checkingTile.tileValue == toCheckAgainst.tileValue && !toCheckAgainst.moving) {
                matches.Add((j, coord.y));
            }
            else
            {
                break;
            }
        }
        matches = matches.Distinct().ToList();
        if(matches.Count < TilesRequiredToMatch) matches.Clear();
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

                board[tile].resolving = board[GetTile(tile, BoardDir.Above)].tileValue > 0;
                for (int i = tile.x + 1; i < boardRows; i++) {
                    var iterTile = board[(i, tile.y)];
                    if (iterTile.tileValue is -1 or 0)
                        break;
                    iterTile.moving = true;

                }
                
            }
            //if it's a non-zero tile, check to see if it needs to fall by iterating downward
            else
            {
                if (tile.x - 1 < 0) continue;
                for (int i = tile.x - 1; i >= 0;i--)
                {
                    var iterTile = board[(i, tile.y)];
                    if (iterTile.tileValue == -1)
                        break;
                    else if (iterTile.tileValue == 0)
                    {
                        iterTile.resolving = true;
                        thisTile.moving = true;
                    }
                    else if (iterTile.moving)
                    {
                        thisTile.moving = true;
                    }
                    
                }
                //(int, int) belowPos = GetTile(tile, BoardDir.Below);
                //if(board[belowPos].tileValue == 0 || board[belowPos].moving){
                //    board[belowPos].resolving = true;
                //    thisTile.moving = true;
                //}
                
            }
        }
    }
    #endregion
    
    #region Tile Spawning

    public void PlaceTile(int value, (int x, int y) coordinate, bool fromTopOfBoard = false)
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
        if (board[GetTile(coordinate, BoardDir.Below)].tileValue == 0)
            modifiedTile.moving = true;
    }
    
    #endregion
    
    #region Helpers

    public enum BoardDir
    {
        Left, Right, Above, Below
    }

    public (int, int) GetTile((int x, int y) tilePos, BoardDir direction)
    {
        switch (direction)
        {
            case BoardDir.Above:
            {
                if (tilePos.x + 1 >= boardRows) {
                    Debug.LogError("Tile out of bounds: " + (tilePos.x + 1, tilePos.y));
                    return (-1, -1);
                }
                return (tilePos.x + 1, tilePos.y);
            }

            case BoardDir.Below:
            {
                if (tilePos.x - 1 < 0) {
                    Debug.LogError("Tile out of bounds: " + (tilePos.x - 1, tilePos.y));
                    return (-1, -1);
                }
                return (tilePos.x - 1, tilePos.y);
            }
            case BoardDir.Left:
            {
                if (tilePos.y + 1 >= boardColumns) {
                    Debug.LogError("Tile out of bounds: " + (tilePos.x, tilePos.y + 1));
                    //newPos = null;
                    return (-1, -1);
                }
                //newPos = board[(tilePos.x, tilePos.y)];
                return (tilePos.x, tilePos.y);
            }
            case BoardDir.Right:
            {
                if (tilePos.y - 1 < 0) {
                    Debug.LogError("Tile out of bounds: " + (tilePos.x, tilePos.y));
                    //newPos = null;
                    return (-1, -1);
                }

                //newPos = board[(tilePos.x, tilePos.y)];
                return (tilePos.x, tilePos.y);
            }
            default:
            {
                Debug.LogError("Attempted to get a tile in a direction without a valid direction selected");
                //newPos = null;
                return (-1, -1);
            }
                
        }
    }

    public List<(int, int)> GetFallingTiles()
    {
        List<(int, int)> fallingTiles = board.Keys.Where(x => board[x].moving).ToList();
        //var stragglers = board.Keys.Where(x => board[x].resolving && !board[GetTile(x, BoardDir.Above)].moving);
        //handle unresolved tiles
        return fallingTiles.ToList();
    }
    #endregion
}
