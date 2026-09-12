using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RadialMenuItem : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] Image icon;
    RadialMenu menu;
    RectTransform thisRect;

    Vector3 baseScale;
    Vector3 targetScale;
    [SerializeField] float hoverSmooth = 10f;


    public UnityEvent onClick; 
    public RectTransform IconRect => icon ? icon.rectTransform : null;
    public Image BackGroundImage => background;

    public bool isTweening = true;



    private void Awake()
    {
        menu = GetComponentInParent<RadialMenu>();
        thisRect = GetComponent<RectTransform>();
        baseScale = thisRect.localScale;
        targetScale = baseScale;
    }


    private void Update()
    {
        if (!isTweening)
            thisRect.localScale = Vector3.Lerp(thisRect.localScale, targetScale, Time.unscaledDeltaTime * hoverSmooth);
    }


    private void OnEnable()
    {
        EnsureReferences();
    }

    public void EnsureReferences()
    {
        if (!background)
            background = GetComponent<Image>();     
    }
    

    public void Invoke()
    {
        onClick?.Invoke();
    }


    public void OnEnter()
    {
        targetScale = baseScale * menu.hoverScale;
        if (background)
        {
            background.color = menu.hoverColor;
        }
    }

    public void OnExit()
    {
        targetScale = baseScale;
        if (background)
        {
            background.color = menu.defaultColor;

        }
    }

}
