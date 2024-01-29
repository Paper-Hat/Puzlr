using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile
{
    //-1 is untouchable, 0 is empty
    public int tileValue = 0;
    public bool moving = false;
    public bool resolving = false;
    public Tile(bool moving = false, int tileVal = 0)
    {
        tileValue = tileVal;
        this.moving = moving;
    }
    

}
