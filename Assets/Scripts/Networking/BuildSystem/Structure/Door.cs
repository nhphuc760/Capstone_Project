using Fusion;
using UnityEngine;

public class Door : StructureBase
{



    public override void Spawned()
    {
        base.Spawned();
        Debug.Log("Door spawn");
        Debug.Log("Door Level: " + Level);
    }

    public override void Operation()
    {
       
    }

    public override void UpgradeLogic()
    {
        Debug.Log("Door upgrade");
    }

    private void OnMouseDown()
    {
        Debug.Log("OnMouseDown: " + gameObject.name);
        var canUpgrade = CanUpgrade();
        if (canUpgrade.Success)
        {
            RPC_RequestUpgrade();
        }
        else
        {
            Debug.Log(canUpgrade.Message);
        }
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


}
