using Fusion;
using UnityEngine;

public abstract class FusionBehaviour : NetworkBehaviour
{
    protected ItemDatabase Database
    {
        get
        {
            if (FusionItemManager.Instance == null)
            {
                Debug.LogError("[FusionBehaviour] FusionItemManager chưa tồn tại.");
                return null;
            }

            return FusionItemManager.Instance.ItemDatabase;
        }
    }
}