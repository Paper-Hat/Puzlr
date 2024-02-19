using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
public class TilePreview : MonoBehaviour, IPuzlGameComponent
{
    [SerializeField] private Image previewColor;
    public Coroutine previewerCo;

    void Awake()
    {
        previewColor.fillMethod = Image.FillMethod.Vertical;
    }
    public void PreviewTile(Color previewTileColor)
    {
        previewerCo = StartCoroutine(PreviewTileCo(previewTileColor));
    }

    public void ConfigurePreviewerSize(int tileSize)
    {
        RectTransform objRect= (RectTransform)transform;
        Vector2 size = new Vector2(tileSize, tileSize / 2f);
        objRect.sizeDelta = size;
        float childDimension = (size.x <= size.y) ? size.x : size.y;
        Vector2 childDimensions = new Vector2(childDimension, childDimension);
        foreach (Transform t in GetComponentsInChildren<Transform>())
            ((RectTransform)t).sizeDelta = childDimensions;
    }
    private IEnumerator PreviewTileCo(Color previewTileColor)
    {
        previewColor.enabled = true;
        previewColor.color = previewTileColor;
        Tween previewTween = previewColor.DOFillAmount(1f, Board.TimeForNewTile);
        yield return new DOTweenCYInstruction.WaitForCompletion(previewTween);
        previewColor.enabled = false;
        previewColor.fillAmount = 0f;
        previewerCo = null;
        yield return null;
    }

    public PuzlBoard Board { get; set; }

    public void SetBoardRef(PuzlBoard board)
    {
        Board = board;
    }
}
