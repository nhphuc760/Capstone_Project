using UnityEngine;
using System.Collections.Generic;
using TriInspector;
using DG.Tweening;
using System.Threading.Tasks;
using System;

public class RadialMenu : MonoBehaviour, IPlayerUI
{
    [Header("Layout")]
    public float radius = 150f;
    public float startAngle = 90f;
    public List<RectTransform> items = new();

    [Header("Configs")]
    public float durationTween = 0.5f;
    public float delayBetween = 0.1f;

    public float TweeningTime => durationTween + (items.Count - 1) * delayBetween;

    [Header("Button Design")]
    public Color defaultColor = Color.white;
    public Color hoverColor = Color.cyan;
    public float hoverScale = 1.15f;

    public event Action<int> onSelectIndex;


    List<Vector2> originalPositions = new List<Vector2>();
    RectTransform thisRect;
    int _currentIndex = 0;

    private void Awake()
    {
        thisRect = GetComponent<RectTransform>();
        if (items == null || items.Count == 0) return;
        foreach (var element in items)
        {
            originalPositions.Add(element.anchoredPosition);
        }
        GetComponentInParent<PlayerUIComponent>().RegisterPlayerUI(PlayerUIComponent.OpenUI.RadialMenu, this);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        CalculateIndex();
        //if (Input.GetKeyUp(KeyCode.BackQuote))
        //    Hide();
    }


  



    void CalculateIndex()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(thisRect, Input.mousePosition, null, out var localPoint);
        Vector2 dir = (localPoint - (Vector2)(thisRect.anchoredPosition)).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0)
        {
            angle += 360f;
        }
        float sector = 360f / items.Count;
        float cwAngle = (360f - angle + startAngle + sector / 2f) % 360f;
        int index = Mathf.FloorToInt(cwAngle / sector);

        if (_currentIndex != index)
        {
            UnChooseElement(_currentIndex);
            ChooseElement(index);
            _currentIndex = index;
        }




    }

    [Button("UpdateLayout")]
    void UpdateLayout()
    {
        int count = items.Count;
        float step = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle - step * i;
            float rad = angle * Mathf.Deg2Rad;
            items[i].anchoredPosition = new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);
        }
    }


    void ChooseElement(int index)
    {
        RadialMenuItem item = items[index].GetComponent<RadialMenuItem>();
        item.OnEnter();
    }

    void UnChooseElement(int index)
    {
        RadialMenuItem item = items[index].GetComponent<RadialMenuItem>();
        item.OnExit();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        for (int i = 0; i < items.Count; i++)
        {
            var element = items[i];
            element.DOKill();
            RadialMenuItem item = element.GetComponent<RadialMenuItem>();
            item.isTweening = true;
            // Reset trước khi tween
            element.anchoredPosition = Vector2.zero;
            element.localScale = Vector3.zero;
            element.gameObject.SetActive(true);

            // Tween độc lập cho từng element
            element.DOAnchorPos(originalPositions[i], durationTween)
                   .SetEase(Ease.OutBack)
                   .SetDelay(i * delayBetween);

            element.DOScale(Vector3.one, durationTween)
                   .SetEase(Ease.OutBack)
                   .SetDelay(i * delayBetween).OnComplete(() => item.isTweening = false);
        }
    }

    public async void Close()
    {
        for (int i = 0; i < items.Count; i++)
        {
            var element = items[i];
            element.DOKill();

            // Reset trước khi tween
            element.anchoredPosition = originalPositions[i];
            element.localScale = Vector3.one;

            element.DOAnchorPos(Vector2.zero, durationTween)
                   .SetEase(Ease.InBack)
                   .SetDelay(i * delayBetween)
                   .OnComplete(() => element.gameObject.SetActive(false));

            element.DOScale(Vector3.zero, durationTween)
                   .SetEase(Ease.InBack)
                   .SetDelay(i * delayBetween);
        }
        await Task.Delay((int)((durationTween + (items.Count - 1) * delayBetween) * 1000));
        onSelectIndex?.Invoke(_currentIndex);
        gameObject.SetActive(false);
    }
}
