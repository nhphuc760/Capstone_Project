using Ookii.Dialogs;
using UnityEngine;
using UnityEngine.UI;

public class ResourceNodeUI : MonoBehaviour
{
    [SerializeField] ResourceNode resourceNode;
    [SerializeField] Image circleBar;
    [SerializeField] float showDuration = 3f;
    float timer = 0f;

    private void Awake()
    {
        resourceNode ??= transform.root.GetComponent<ResourceNode>();
    }

    private void Start()
    {
        if(resourceNode)
            resourceNode.OnAmountChanged += ResourceNode_OnAmountChanged;

        gameObject.SetActive(false);
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if(timer <= 0f)
        {
            gameObject.SetActive(false);
        }
    }


    private void ResourceNode_OnAmountChanged(int arg1, int arg2)
    {
        timer = showDuration;
        gameObject.SetActive(true); 
        if(circleBar)
            circleBar.fillAmount = (float)arg1/arg2;
    }


    private void OnDestroy()
    {
        if(resourceNode)
            resourceNode.OnAmountChanged -= ResourceNode_OnAmountChanged;
    }

    

}
