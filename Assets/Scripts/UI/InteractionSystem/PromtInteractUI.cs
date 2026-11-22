using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PromtInteractUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI Promt;
    [SerializeField] Image Icon;



    private void LateUpdate()
    {
        transform.rotation = Quaternion.LookRotation((transform.position - Camera.main.transform.position).normalized);
    }


    public void Show(string promt, Sprite icon = null)
    {
        gameObject.SetActive(true);
        if(Promt) Promt.text = promt;
        if(Icon) Icon.sprite = icon;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }


}
