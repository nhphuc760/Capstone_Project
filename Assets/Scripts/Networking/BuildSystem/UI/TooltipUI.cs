
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util.Core;
public class TooltipUI : MonoBehaviour
{
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private Canvas canvas;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] float showDuration = 5f;
    
    CancellationTokenSource tokenSource = new();


    Vector3 followPos;

    public void Show( string description, Vector2 screenPosition, Vector3 followPos)
    {
        descriptionText.text = description;

        gameObject.SetActive(true);

        this.followPos =  followPos;
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        
        if(tokenSource != null && !tokenSource.IsCancellationRequested)
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }
        tokenSource = new CancellationTokenSource();
        tokenSource.Token.Register(() => Debug.Log("Token Cancel"));
        Utils.DelayCall(showDuration, () => Hide(), token: tokenSource.Token).Forget();
    }
    

    private void LateUpdate()
    {        
        if(followPos != Vector3.zero)
        {
            RectTransform canvasRect = canvas.transform as RectTransform;
            var toolTipPos = Camera.main.WorldToScreenPoint(followPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                toolTipPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera,
                out Vector2 localPoint
            );
            localPoint.x = Mathf.Round(localPoint.x);
            localPoint.y = Mathf.Round(localPoint.y);   
            tooltipRect.anchoredPosition = localPoint;
        }
    }

    public void Hide()
    {
        if(tokenSource != null && !tokenSource.IsCancellationRequested)
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
            tokenSource = null;
        }
        followPos = Vector3.zero;
        gameObject.SetActive(false);
    }
}
