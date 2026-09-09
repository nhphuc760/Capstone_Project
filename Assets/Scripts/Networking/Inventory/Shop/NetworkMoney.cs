using Fusion;
using UnityEngine;

public class NetworkMoney : NetworkBehaviour
{
    [Networked]
    public int Money { get; private set; }


    public bool AddMoney(int amount)
    {
        if (!Object.HasStateAuthority)
            return false;

        if (amount <= 0)
            return false;

        Money += amount;

        return true;
    }


    public bool RemoveMoney(int amount)
    {
        if (!Object.HasStateAuthority)
            return false;

        if (amount <= 0)
            return false;

        if (Money < amount)
            return false;

        Money -= amount;

        return true;
    }


    public bool HasMoney(int amount)
    {
        return amount >= 0 && Money >= amount;
    }
}
