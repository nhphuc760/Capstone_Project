using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StructureUI : MonoBehaviour
{
    StructureBase m_Structure;

    [SerializeField] TextMeshProUGUI upgradeText;
    [SerializeField] TextMeshProUGUI destroyText;
    [SerializeField] GameObject UpgradePanel;
    [SerializeField] GameObject DestroyPanel;
    [SerializeField] Button UpgradeButton;
    [SerializeField] Button DestroyButton;





    public void Show(StructureBase structure)
    {
        this.m_Structure = structure;
        if (m_Structure.IsUpgradeable)
        {
            UpgradePanel.SetActive(true);
            DestroyPanel.SetActive(true);
            upgradeText.text = structure._currentUpgradeData.Information();
            (this.transform as RectTransform).sizeDelta = new Vector2(300, 200);
        }
        else
        {
            UpgradePanel.SetActive(false);
            (this.transform as RectTransform).sizeDelta = new Vector2(300, 100);
        }
        gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
       if(m_Structure != null)
        {

        }
    }



    public void Hide()
    {
        m_Structure = null;
        gameObject.SetActive(false);
    }



    public void UpgradeAction()
    {

    }
    public void DestroyAction()
    {

    }

}
