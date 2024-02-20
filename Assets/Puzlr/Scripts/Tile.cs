using UnityEngine;
[System.Serializable]
public class Tile
{
    //-1 is untouchable, 0 is empty
    public int tileValue = 0;
    public bool moving = false;
    public bool resolving = false;
    //dropping flag determines whether we should run the dropping coroutine on the tile
    public Coroutine tileDrop = null;
    public Tile(bool moving = false, int tileVal = 0)
    {
        tileValue = tileVal;
        this.moving = moving;
    }
    

}
