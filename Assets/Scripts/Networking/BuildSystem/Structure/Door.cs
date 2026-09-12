using Fusion;
using UnityEngine;

public class Door : StructureBase
{


    
    public override void Spawned()
    {
        base.Spawned();             
    }

    public override void Operation()
    {

    }

    public override void UpgradeLogic()
    {
        Debug.Log("Door upgrade");
    }

    


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestUpgrade()
    {

        var canUpgrade = CanUpgrade();

        if (canUpgrade.Success)
        {
            Upgrade();
            Debug.Log("");
        }
        else
        {
            Debug.Log(canUpgrade.Message);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]

    public void RPC_SetActiveNetworked(bool value)
    {        
       gameObject.SetActive(value);
    }

   

}
