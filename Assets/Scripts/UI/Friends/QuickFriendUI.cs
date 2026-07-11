using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuickFriendUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] RectTransform Content;
    [SerializeField] Sprite defaultAvatar;
    [SerializeField] GameObject expandFriendPanel;
    public void OnPointerClick(PointerEventData eventData)
    {
        expandFriendPanel.SetActive(true);
        Debug.Log("PointerClick");
    }

    private void Awake()
    {
        foreach (var i in NetworkDataManager.Instance.friendManager.GetFriends())
        {
            var avt = NetworkDataManager.Instance.GetAvatarUser(i);
            if (avt == null)
            {
                avt = defaultAvatar;
            }
            InstantiateImage(avt);
        }
    }

    

    void InstantiateImage(Sprite sprite)
    {
        RectTransform rectTransform = new RectTransform();
        rectTransform.sizeDelta = new Vector2(130, 130);
        Image img = rectTransform.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        rectTransform.SetParent(Content);
    }
}
