using System.Threading;
using Gamelogic.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util.Core;
public class StructureUI : MonoBehaviour
{
    StructureBase m_Structure;

    [SerializeField] TextMeshProUGUI upgradeText;
    [SerializeField] TextMeshProUGUI destroyText;
    [SerializeField] GameObject UpgradePanel;
    [SerializeField] GameObject DestroyPanel;
    [SerializeField] Button UpgradeButton;
    [SerializeField] Button DestroyButton;
    [SerializeField] Canvas canvas;
    [SerializeField] float showDuration = 5f;
    CancellationTokenSource cts;

    private void OnEnable()
    {
        UpgradeButton.onClick.AddListener(UpgradeAction);
        DestroyButton.onClick.AddListener(DestroyAction);
    }


    public void Show(StructureBase structure)
    {
        this.m_Structure = structure;
        gameObject.SetActive(true);
        if (m_Structure.IsUpgradeable)
        {          
            UpgradePanel.SetActive(true);
            DestroyPanel.SetActive(m_Structure.StructureDataSO.structureType != StructureType.Door ? true : false);
            upgradeText.text = structure.GetInforUpgrade();
            destroyText.text = structure.GetResourceDestroy().info;
            (this.transform as RectTransform).sizeDelta = DestroyPanel.activeSelf ? new Vector2(300 , 200) : new Vector2(300, 100);
        }
        else
        {
            UpgradePanel.SetActive(false);
            destroyText.text = structure.GetResourceDestroy().info;
            (this.transform as RectTransform).sizeDelta = new Vector2(300, 100);
        }
        if(cts != null && !cts.IsCancellationRequested)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
        cts = new CancellationTokenSource();
        Utils.DelayCall(showDuration, () => Hide(), token: cts.Token).Forget();
    }
    

    private void LateUpdate()
    {
        if (m_Structure != null)
        {

            RectTransform canvasRect = canvas.transform as RectTransform;
            var toolTipPos = Camera.main.WorldToScreenPoint(m_Structure.transform.position);
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
            (transform as RectTransform).anchoredPosition = localPoint;

        }
    }

    private void OnDisable()
    {
        UpgradeButton.onClick.RemoveListener(UpgradeAction);
        DestroyButton.onClick.RemoveListener(DestroyAction);
    }

    public void Hide()
    {
        if(cts != null && !cts.IsCancellationRequested)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
        m_Structure = null;
        gameObject.SetActive(false);
    }



    public void UpgradeAction()
    {

        var checkUpdateResult = m_Structure.CanUpgrade();
        if (checkUpdateResult.Success)
        {
            m_Structure.RPC_RequireUpgrade();
        }
        else
        {
            Debug.Log(checkUpdateResult.Message);
        }
    }
    public void DestroyAction()
    {
        m_Structure.RPC_RequireDestroy();
        Hide();
    }
}
