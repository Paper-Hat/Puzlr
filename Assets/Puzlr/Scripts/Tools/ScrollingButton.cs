using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using DG.Tweening;
using TMPro;
using Unity.Collections;

/// <summary>
/// Fixed scrolling for a set of button-activated functions
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(RectMask2D))]
public class ScrollingButton : MonoBehaviour
{
    [Serializable]
    public enum Scroll
    {
        Up,
        Down,
        Left,
        Right,
        Empty
    }
    [Header("Required fields")]
    //Prefab to use for our scrolling button
    //MUST HAVE RECT TRANSFORM COMPONENT ON BASE FOR SIZING
    //ensure button prefab gameobject rects are middle-aligned
    [SerializeField] private GameObject buttonPrefab;
    /// <summary>
    /// Number of unmasked buttons available in our scroll to press
    /// We will always have 1 more button available than the number set so we can pool these objects
    /// </summary>
    [SerializeField] private int numActiveButtons = 1;
    [Header("What the button(s) do:")]
    //We will always start with our active buttons in the "center" of the array
    [SerializeField] private ButtonParams[] buttonFunctions;

    [Header("Optional fields")] 
    //Control buttons are just that; they should be enabled when we can scroll, and disabled when we can't
    //we should at least have one of these if we cannot scroll via scroll wheel
    [SerializeField] private Button[] controlButtons;
    [SerializeField] private bool scrollWithScrollWheel = false;
    /// <summary>
    /// Direction to scroll elements
    /// Selected direction ensures the opposite direction is the "reverse"
    /// For example: setting "Up" sets reverse direction to "Down"
    /// </summary>
    [SerializeField] private Scroll dir = Scroll.Right;
    [SerializeField] private float scrollSpeed;
    
    [SerializeField][ReadOnly] private Button[] scrollingBtns;
    private bool shouldScroll;
    private float zeroX, zeroY, firstX, firstY, lastX, lastY;
    private Vector3 offset;
    private Vector2 buttonDelta;
    private RectTransform maskTransform;
    void Awake()
    {
        if (transform.position != Vector3.zero) offset = transform.position;
        if (numActiveButtons >= buttonFunctions.Length - 1 || buttonPrefab == null) {
            Debug.LogError("Disabling scroll functionality due to error in config.");
            shouldScroll = false;
        }
        
    }
    private void Start()
    {
        shouldScroll = true;
        ConfigureMask();
        CreateButtons();
    }

    private void FixedUpdate()
    {
        //scroll on positive axes
        //if (!scrollWithScrollWheel || !shouldScroll) return;
        
    }

    //our mask will be the size of the number of buttons in our scroll
    void ConfigureMask()
    {
        RectTransform rtf = (RectTransform)transform;
        Vector2 btnSize = ((RectTransform)buttonPrefab.transform).sizeDelta;
        buttonDelta = btnSize;
        
        rtf.sizeDelta = dir switch
        {
            Scroll.Down or Scroll.Up => new Vector2(btnSize.x, btnSize.y * numActiveButtons),
            Scroll.Left or Scroll.Right => new Vector2(btnSize.x * numActiveButtons, btnSize.y),
            _ => rtf.sizeDelta
        };
        rtf.position = offset;
        maskTransform = rtf;
    }
    
    void CreateButtons()
    {
        scrollingBtns = new Button[numActiveButtons + 1];
        //assuming our button prefab rects are center-aligned
        //our starting pos is the center of our furthest element in the negative direction
        bool horizontal = dir is Scroll.Left or Scroll.Right;
        
        firstX = (horizontal) ?  maskTransform.rect.xMin + 0.5f * buttonDelta.x : 0f;
        firstY = (horizontal) ? 0f : maskTransform.rect.yMin + 0.5f * buttonDelta.y;
        zeroX = (horizontal) ? firstX - buttonDelta.x : 0f;
        zeroY = (horizontal) ? 0f : firstY - buttonDelta.y;
        //Debug.Log(new Vector3(zeroX, zeroY));
        
        for (int i = 0; i < scrollingBtns.Length; ++i)
        {
            Vector3 buttonPos = (horizontal) ? new Vector3(firstX + i * buttonDelta.x, firstY)
                                             : new Vector3(firstX, firstY + i * buttonDelta.y);
            GameObject buttonObj = Instantiate(buttonPrefab, transform, false);
            buttonObj.transform.localPosition = buttonPos;
            scrollingBtns[i] = buttonObj.GetComponent<Button>();
        }

        lastX = (horizontal) ? scrollingBtns[^1].gameObject.transform.localPosition.x: 0f;
        lastY = (horizontal) ? 0f : scrollingBtns[^1].gameObject.transform.localPosition.y;
        
        for (int i = 0; i < numActiveButtons; ++i) {
            SetupButton(scrollingBtns[i], buttonFunctions[i]);
        }
    }
    
    //tween button in the direction provided, set new button position
    //left or right-shift arrays of our gameobjects
    public void Move(bool reverse)
    {
        if (!shouldScroll) return;
        StartCoroutine(TweenButtonsCo(reverse ? GetOppositeDirection(dir) : dir));
        
    }

    //TODO: tween buttons & configure
    private IEnumerator TweenButtonsCo(Scroll direction)
    {
        //while tweening, disable controls
        ToggleInteraction();
        //ensure 'caps' are enabled regardless of scroll
        scrollingBtns[0].gameObject.SetActive(true);
        scrollingBtns[^1].gameObject.SetActive(true);
        
        switch (direction)
        {
            case Scroll.Up:
                break;
            case Scroll.Down:
                break;
            case Scroll.Left:
                //Debug.Log("Scrolling left...");
                foreach (var btn in scrollingBtns) {
                    var btnTransform = btn.gameObject.transform;
                    btnTransform.DOLocalMoveX(btnTransform.localPosition.x - buttonDelta.x, scrollSpeed, true);
                }
                Shift<Button>(true, scrollingBtns);
                Shift<ButtonParams>(true, buttonFunctions);
                for (int i = 0; i < numActiveButtons; ++i) {
                    SetupButton(scrollingBtns[i], buttonFunctions[i]);
                }
                yield return new WaitForSeconds(scrollSpeed);
                scrollingBtns[^1].gameObject.transform.localPosition = new Vector3(lastX, lastY);
                
                break;
            case Scroll.Right:
                //set end object to zero pos, then tween all buttons, then set button functions
                scrollingBtns[^1].gameObject.transform.localPosition = new Vector3(zeroX, zeroY);
                foreach (var btn in scrollingBtns) {
                    var btnTransform = btn.gameObject.transform;
                    btnTransform.DOLocalMoveX(btnTransform.localPosition.x + buttonDelta.x, scrollSpeed, true);
                }
                Shift<Button>(false, scrollingBtns);
                Shift<ButtonParams>(false, buttonFunctions);
                for (int i = 0; i < numActiveButtons; ++i) {
                    SetupButton(scrollingBtns[i], buttonFunctions[i]);
                }
                yield return new WaitForSeconds(scrollSpeed);
                
                break;
        }
        
        scrollingBtns[0].gameObject.SetActive(true);
        scrollingBtns[^1].gameObject.SetActive(false);
        
       ToggleInteraction();
       yield return null;
    }
    
    void SetupButton(Button btn, ButtonParams p)
    {
        //inefficient, but we're just trying to finish the game at this point
        btn.GetComponentInChildren<TextMeshProUGUI>().text = p.btnName;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => p.btnFn.Invoke());
    }
    
    #region Helpers
    Scroll GetOppositeDirection(Scroll direction)
    {
        switch (direction)
        {
            case Scroll.Up:
                return Scroll.Down;
            case Scroll.Down:
                return Scroll.Up;
            case Scroll.Left:
                return Scroll.Right;
            case Scroll.Right:
                return Scroll.Left;
        }
        Debug.LogError("Attempted to get opposing direction with no scroll set");
        return Scroll.Empty;
    }
    
    /// <summary>
    /// shift elements in our array in a direction, wrapping at end/start
    /// this will allow us to maintain reference to a specific position in our button layout
    /// this should occur after we've reset the position of the end element in the direction of the shift
    /// i.e. with 5 elements, shifting right, this occurs after arr[4].pos = arr[0].pos
    /// </summary>
    /// <param name="left"></param>
    /// <param name="arr"></param>
    /// <param name="numShifts"></param>
    void Shift<T>(bool left, T[] arr, int numShifts = 1)
    {
        T[] wrapped = new T[numShifts];
        
        if (left) {
            //objects that get shifted out temporarily referenced in wrapped arr
            Array.Copy(arr, 0, wrapped, 0, numShifts);
            //shift arr over
            Array.Copy(arr, numShifts, arr, 0, arr.Length - numShifts);
            //copy back our references to the original array
            Array.Copy(wrapped, 0, arr, arr.Length - numShifts, numShifts);
        }
        else
        {
            Array.Copy(arr, arr.Length - numShifts, wrapped, 0, numShifts);
            Array.Copy(arr, 0, arr, numShifts, arr.Length - numShifts);
            Array.Copy(wrapped, 0, arr, 0, numShifts);
        }
    }

    void ToggleInteraction()
    {
        foreach (Button b in scrollingBtns)
            b.interactable = !b.interactable;
        foreach (Button b in controlButtons)
            b.interactable = !b.interactable;
        shouldScroll = !shouldScroll;
    }
    
    #endregion

    void OnDisable()
    {
        foreach (Button b in scrollingBtns) {
            b.interactable = true;
            b.onClick.RemoveAllListeners();
            shouldScroll = true;
        }
        foreach (Button b in controlButtons) { b.interactable = true; }

        DOTween.Clear();

    }
}
[Serializable]
class ButtonParams
{
    public UnityEvent btnFn;
    public string btnName;
}