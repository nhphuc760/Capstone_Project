
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


    GameObject followObj;

    public void Show( string description, Vector2 screenPosition, GameObject followObject)
    {
        descriptionText.text = description;

        gameObject.SetActive(true);

        followObj =  followObject;
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
        if(followObj != null)
        {
            RectTransform canvasRect = canvas.transform as RectTransform;
            var toolTipPos = Camera.main.WorldToScreenPoint(followObj.transform.position);
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
        followObj = null;
        gameObject.SetActive(false);
    }
}
