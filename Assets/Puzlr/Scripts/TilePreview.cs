using System.Collections;
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

    public void ConfigurePreviewerSize(float tileSize)
    {
        RectTransform objRect= (RectTransform)transform;
        Vector2 size = new Vector2(tileSize, tileSize);
        objRect.sizeDelta = size;
        foreach (Transform t in GetComponentsInChildren<Transform>())
            ((RectTransform)t).sizeDelta = size;
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
