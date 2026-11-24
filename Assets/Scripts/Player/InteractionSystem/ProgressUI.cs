using UnityEngine;
using UnityEngine.UI;

public class ProgressUI : MonoBehaviour
{
    [SerializeField] Image progress;
    [SerializeField] Image Icon;
    public void UpdateProgress(float fillAmount)
    {
        if(progress != null) 
            progress.fillAmount = fillAmount;
    }

    public void UpdateIcon(Sprite icon)
    {
        if(Icon)
            Icon.sprite = icon != null ? icon : null;
    }

}
