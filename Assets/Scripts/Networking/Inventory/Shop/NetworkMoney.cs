using Fusion;
using UnityEngine;

public class NetworkMoney : NetworkBehaviour
{
    private const int StartingMoney = 1000;

    [Networked]
    private int Money { get; set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Money = StartingMoney;
        }

        Debug.Log($"[NetworkMoney] Spawned | Player={Object.InputAuthority} | Money={Money}");
    }

    public bool HasMoney(int amount)
    {
        if (Object == null || !Object.IsValid)
        {
            Debug.LogWarning("[NetworkMoney] HasMoney failed | NetworkObject is invalid");
            return false;
        }

        if (amount < 0)
        {
            Debug.LogWarning("[NetworkMoney] HasMoney failed | Invalid amount");
            return false;
        }

        return Money >= amount;
    }

    public bool AddMoney(int amount)
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return false;

        if (amount <= 0)
            return false;

        Money += amount;
        Debug.Log($"[NetworkMoney] AddMoney | Amount={amount} | Total={Money}");
        return true;
    }

    public bool RemoveMoney(int amount)
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return false;

        if (amount <= 0 || Money < amount)
            return false;

        Money -= amount;
        Debug.Log($"[NetworkMoney] RemoveMoney | Amount={amount} | Total={Money}");
        return true;
    }

    public int GetMoney()
    {
        if (Object == null || !Object.IsValid)
            return 0;

        return Money;
    }
}
