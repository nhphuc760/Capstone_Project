using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIBanedElement : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI playerNameText;
    [SerializeField] Image avatar; // tùy chọn

    public void Setup( string name, Sprite avt)
    {       

       if(playerNameText)
            playerNameText.text = name;
       if(avatar)
            avatar.sprite = avt;
        gameObject.SetActive(true);
    }

    public void Clear()
    {
        gameObject.SetActive(false);
    }
}
