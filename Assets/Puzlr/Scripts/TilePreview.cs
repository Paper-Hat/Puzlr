using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
public class TilePreview : MonoBehaviour
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
    
    private IEnumerator PreviewTileCo(Color previewTileColor)
    {
        previewColor.enabled = true;
        previewColor.color = previewTileColor;
        Tween previewTween = previewColor.DOFillAmount(0f, GameManager._instance.timeUntilNewTileDropped);
        yield return new DOTweenCYInstruction.WaitForCompletion(previewTween);
        previewColor.enabled = false;
        previewColor.fillAmount = 1f;
        previewerCo = null;
        yield return null;
    }
}
