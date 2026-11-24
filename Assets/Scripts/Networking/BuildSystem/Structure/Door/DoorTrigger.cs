using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    [SerializeField] NetworkObject netObj;
    [SerializeField] public float powerBuff = 20;

    Dictionary<IAffector, List<int>> objInZone = new Dictionary<IAffector, List<int>>();
    public NetworkRunner Runner => netObj.Runner;


    private void OnEnable()
    {
        if (!netObj.IsValid) return;
        foreach (var i in Runner.ActivePlayers)
        {
            NetworkObject playerObject = Runner.GetPlayerObject(i);

            if (StructureManager.Ins.IsInsideEnclosedZone(netObj.InputAuthority, playerObject.transform.position, false))
            {
                IAffector affector = Runner.GetPlayerObject(i).GetComponent<IAffector>();
                AddBuff(affector, i == netObj.InputAuthority);
            }                     
        }
    }

    private void OnDisable()
    {
        if(!netObj.IsValid) return;
        foreach (var keyValue in objInZone)
        {
            foreach (var i in keyValue.Value)
            {
                keyValue.Key.RemoveModifier(i);
            }
        }
    }      


    private void OnTriggerExit(Collider other)
    {
        if (!netObj.HasStateAuthority) return;

        Debug.Log("Door Trigger Exit");

        if (StructureManager.Ins == null) return;

        if (other.transform.root.TryGetComponent<NetworkObject>(out var affector))
        {

            //Chỉ quan tâm vị trí
            if(StructureManager.Ins.IsInsideEnclosedZone(netObj.InputAuthority, affector.transform.position, false))
            {
                AddBuff(affector.GetComponent<IAffector>(), affector.InputAuthority == netObj.InputAuthority);
            }
            else
            {
                RemoveBuff(affector.GetComponent<IAffector>());
            }
        }       
    }


    void AddBuff(IAffector affector, bool hasInputAuthority = false)
    {
        //+Damage

        Modifier damage = new Modifier
        {
            ModID = ModifierIDGenerator.Next(Runner),
            Source = netObj,
            StatType = StatsType.Damage,
            ModApplyType = ModApplyType.PercentAdd,
            Value = hasInputAuthority ? powerBuff : -powerBuff,
            IsTimed = false
        };
        //AttackSpeed
        Modifier attackSpeed = new Modifier
        {
            ModID = ModifierIDGenerator.Next(Runner),
            Source = netObj,
            StatType = StatsType.AttackSpeed,
            ModApplyType = ModApplyType.PercentAdd,
            Value = hasInputAuthority ? powerBuff : -powerBuff,
            IsTimed = false
        };
        //MoveSpeed       
        Modifier moveSpeed = new Modifier
        {
            ModID = ModifierIDGenerator.Next(Runner),
            Source = netObj,
            StatType = StatsType.MoveSpeed,
            ModApplyType = ModApplyType.PercentAdd,
            Value = hasInputAuthority ? powerBuff : -powerBuff,
            IsTimed = false
        };
        objInZone[affector] = new List<int> { damage.ModID, attackSpeed.ModID, moveSpeed.ModID };
        affector.AddModifier(damage);
        affector.AddModifier(attackSpeed);
        affector.AddModifier(moveSpeed);
    }


    void RemoveBuff(IAffector affactor)
    {
        if (objInZone.TryGetValue(affactor, out var list))
        {
            foreach (var i in list)
            {
                affactor.RemoveModifier(i);
            }
            objInZone.Remove(affactor);
        }
    }
}
