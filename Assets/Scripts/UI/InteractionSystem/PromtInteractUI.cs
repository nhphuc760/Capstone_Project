using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PromtInteractUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI Promt;
    [SerializeField] Image Icon;
    [SerializeField] Camera _cam;


    private void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam != null) transform.forward = _cam.transform.forward;
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
