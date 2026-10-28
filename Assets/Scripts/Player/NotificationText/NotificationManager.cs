using System.Collections.Generic;
using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    [SerializeField] private NotificationItem[] allItems;

    [Header("Layout")]
    [SerializeField] private float spacing = 100f;

    [Header("Life Time")]
    [SerializeField] float visibleTime = 3f;

    private readonly List<NotificationItem> activeItems = new();

    public static NotificationManager Ins { get; private set; }

    int nextItemIndex;

    readonly float[] scales =
    {
        1f,
        0.9f,
        0.8f
    };

    readonly float[] alphas =
    {
        1f,
        .8f,
        .6f
    };


    private void Awake()
    {       
        Ins = this;
        foreach (var item in allItems)
        {
            item.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //test
            ShowNotification("Test notification");
        }
    }
    /// <summary>
    /// flags: 0 = null, 1 = success, 2 = warning
    /// </summary>
    /// <param name="message"></param>
    /// <param name="flags"></param>

    public void ShowNotification(string message, int flags = 0)
    {
        NotificationItem item = allItems[nextItemIndex];

        nextItemIndex++;

        if(nextItemIndex >= allItems.Length) nextItemIndex = 0;

        item.CancelHide();

        activeItems.Remove(item);
      
        item.SetData(message, flags);

        // Đưa item mới lên đầu stack
        activeItems.Insert(0, item);

        item.Show();

        Rearrange();

        item.StartHide(visibleTime, () =>
        {
            activeItems.Remove(item);
            Rearrange();
        });

    } 

    private void Rearrange()
    {
        for (int i = 0; i < activeItems.Count; i++)
        {
            NotificationItem item = activeItems[i];


            float scale = i< scales.Length ? scales[i] : scales[scales.Length - 1];


            float alpha =
                i < alphas.Length
                ? alphas[i]
                : alphas[alphas.Length - 1];

            Vector2 pos = new Vector2(0, -spacing * i);

            item.UpdateVisual(pos, scale, alpha, 0.25f);
            item.transform.SetAsLastSibling();
        }
    }
}