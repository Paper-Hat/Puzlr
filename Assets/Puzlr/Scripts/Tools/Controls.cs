using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Controls : MonoBehaviour,IDragHandler, IPointerMoveHandler, IPointerClickHandler, IBeginDragHandler, IEndDragHandler, IMoveHandler, IPointerDownHandler, IPointerUpHandler
{
    //for controls handling
    public delegate void OnPtrClick(Vector2 pos);
    public static event OnPtrClick OnPointerClicked;
    public delegate void OnPtrMove(Vector2 pos);
    public static event OnPtrMove OnPointerMoved;
    public delegate void OnDragEnd();
    public static event OnDragEnd OnDragEnded;
    
    public delegate void OnDragStart();

    public static event OnDragStart OnDragStarted;
    
    public static bool HorizontalSwapsOnly = true;
    public float DragThreshold = .1f;
    
    public enum Direction
    {
        Left, Right, Up, Down
    }
    //mousepos
    public static Vector2 MousePos
    {
        get => _mPos;
        set
        {
            _mPos.x = value.x;
            _mPos.y = value.y;
        }
    }

    private static Vector2 _mPos = new Vector2(0, 0);
    
    //drag positions
    public (Vector2 start,Vector2 end) MouseDrag
    {
        get => _mDrag;
        set
        {
            _mDrag.start = value.start;
            _mDrag.end = value.end;
        }
    }

    private (Vector2 start, Vector2 end) _mDrag;
    private bool mouseDown;
    
    public static bool Dragging;
    public static Direction DragDirection;
    public static float DragCompletion;

    //update mouse when it moves, tracking mouse x, y
    void UpdateMousePos(Vector2 pos)
    {
        _mPos = pos;
        OnPointerMoved?.Invoke(pos);
    }

    //determine whether mouse button is held down
    void UpdateClick(bool wasMouseDown, PointerEventData.InputButton mouseDownState)
    {
        if (!wasMouseDown) 
            mouseDown = false;
        else
            mouseDown = (mouseDownState == PointerEventData.InputButton.Left);
    }

    void UpdateDrag(bool startDrag, bool endDrag)
    {
        if (startDrag) {
            Dragging = true;
            MouseDrag = (MousePos, Vector2.zero);
        }
        else if (endDrag)
        {
            MouseDrag = (MouseDrag.start, MousePos);
            Dragging = false;
        }
        else
        {
            MouseDrag = (MouseDrag.start, MousePos);
            Dragging = true;
        }

        DragCompletion = GetDragCompletion();
        DragDirection = GetCardinalDirectionFromDrag(MouseDrag);
    }
    
    public static Direction GetCardinalDirectionFromDrag((Vector2 start, Vector2 end) drag)
    {
        
        float travelX = drag.end.x - drag.start.x;
        float travelY = drag.end.y - drag.start.y;
        
        //we'll make X take priority in the absurd case of perfect equality
        if (Mathf.Abs(travelX) >= Mathf.Abs(travelY) || HorizontalSwapsOnly)
            return (travelX > 0) ? Direction.Right : Direction.Left;
        else
            return (travelY > 0) ? Direction.Up : Direction.Down;
    }
    
    #region Pointer_Handlers
    public void OnDrag(PointerEventData eventData)
    {
        UpdateDrag(false, false);
        Debug.Log("Dragging " + DragDirection);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
       UpdateMousePos(eventData.position);
       //Debug.Log("Mouse: " + MousePos);
       OnPointerMoved?.Invoke(MousePos);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UpdateClick(true, eventData.button);
        OnPointerClicked?.Invoke(MousePos);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        UpdateDrag(true, false);
        OnDragStart dragEvent = OnDragStarted;
        dragEvent?.Invoke();
    } 

    public void OnEndDrag(PointerEventData eventData)
    {

        UpdateDrag(false, true);
        //only trigger drag event on end
        //TODO: filter smaller drag(s) out as unintentional based on multiplier
        if (DragCompletion < 1f)
            return;
        OnDragEnd dragEvent = OnDragEnded;
        dragEvent?.Invoke();
    }
    float GetDragCompletion()
    {
        return Mathf.Abs(MouseDrag.end.x - MouseDrag.start.x)/
               (DragThreshold * BoardDisplayHandler.TileSize);
    }
    //currently unnecessary
    public void OnMove(AxisEventData eventData)
    {
        //throw new System.NotImplementedException();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //throw new System.NotImplementedException();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        UpdateClick(false, PointerEventData.InputButton.Left);
    }
    #endregion
}
