using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Controls : MonoBehaviour,IDragHandler, IPointerMoveHandler, IPointerClickHandler, IBeginDragHandler, IEndDragHandler, IMoveHandler, IPointerDownHandler, IPointerUpHandler
{
    //for controls handling
    public delegate void OnPtrClick((int, int) pos);
    public static event OnPtrClick OnPointerClicked;
    public delegate void OnPtrMove((int, int) pos);
    public static event OnPtrMove OnPointerMoved;
    public delegate void OnPtrDrag(((int sdX, int sdY),(int edX, int edY)) dragValue);
    public static event OnPtrDrag OnDragged;
    public static bool HorizontalSwapsOnly = true;
    public float DragThreshold = .1f;
    
    public enum Direction
    {
        Left, Right, Up, Down
    }
    //mousepos
    public static (int mouseX, int mouseY) MousePos
    {
        get => _mPos;
        set
        {
            _mPos.mouseX = value.mouseX;
            _mPos.mouseY = value.mouseY;
        }
    }

    private static (int mouseX, int mouseY) _mPos = (0, 0);
    
    //drag positions
    public ((int startDragX, int startDragY) startDrag,
            (int endDragX, int endDragY) endDrag) MouseDrag
    {
        get => _mDrag;
        set
        {
            _mDrag.sDrag = value.startDrag;
            _mDrag.eDrag = value.endDrag;
        }
    }

    private ((int sdX, int sdY) sDrag, (int edX, int edY) eDrag) _mDrag;
    private bool mouseDown;
    private bool dragging;
    

    //update mouse when it moves, tracking mouse x, y
    void UpdateMousePos(int mouseX, int mouseY)
    {
        _mPos = (mouseX, mouseY);
        OnPointerMoved?.Invoke(_mPos);
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
            dragging = true;
            MouseDrag = (MousePos, (0, 0));
        }
        else if (endDrag)
        {
            MouseDrag = (MouseDrag.startDrag, MousePos);
            
            dragging = false;
        }
        else
            dragging = true;
    }
    
    public static Direction GetCardinalDirectionFromDrag(((int sdX, int sdY) dragStart, (int edX, int edY) dragEnd) dragVar)
    {
        
        float travelX = dragVar.dragEnd.edX - dragVar.dragStart.sdX;
        float travelY = dragVar.dragEnd.edY - dragVar.dragStart.sdY;
        
        //we'll make X take priority in the absurd case of perfect equality
        if (Mathf.Abs(travelX) >= Mathf.Abs(travelY) || HorizontalSwapsOnly)
            return (travelX > 0) ? Direction.Left : Direction.Right;
        else
            return (travelY > 0) ? Direction.Up : Direction.Down;
    }
    
    #region Pointer_Handlers
    public void OnDrag(PointerEventData eventData)
    {
        UpdateDrag(false, false);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
       UpdateMousePos((int)eventData.position.x, (int)eventData.position.y);
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
    } 

    public void OnEndDrag(PointerEventData eventData)
    {

        UpdateDrag(false, true);
        //only trigger drag event on end
        //TODO: filter smaller drag(s) out as unintentional based on multiplier
        if (Mathf.Abs(MouseDrag.endDrag.endDragX - MouseDrag.startDrag.startDragX) <
            DragThreshold * BoardDisplayHandler.TileSize)
            return;
        OnPtrDrag dragEvent = OnDragged;
        dragEvent?.Invoke(MouseDrag);
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
