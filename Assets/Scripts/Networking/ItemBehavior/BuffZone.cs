using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class BuffZone : NetworkBehaviour
{
    [SerializeField] Material buff;
    [SerializeField] Material debuff;
    [SerializeField] public float powerBuff = 20;
    [SerializeField] public float timeActive = 10;
    [SerializeField] MeshRenderer meshRenderer;

    Dictionary<IAffector, List<int>> objInZone = new Dictionary<IAffector, List<int>>();
    [Networked] TickTimer timer { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            timer = TickTimer.CreateFromSeconds(Runner, timeActive);
        }
        if (HasInputAuthority)
        {
            meshRenderer.material = buff;
        }
        else
        {
            meshRenderer.material = debuff;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (timer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        foreach (var keyValue in objInZone)
        {
            foreach (var i in keyValue.Value)
            {
                keyValue.Key.RemoveModifier(i);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority) return;
        if(other.transform.root.TryGetComponent<IAffector>(out var affector))
        {
            bool hasInputAuthority = other.transform.root.GetComponent<NetworkObject>().HasInputAuthority;
            AddBuff(affector, hasInputAuthority);
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (!HasStateAuthority) return;
        if (other.transform.root.TryGetComponent<IAffector>(out var affector))
        {
            RemoveBuff(affector);
        }
    }


    void AddBuff(IAffector affector, bool hasInputAuthority = false) 
    {
        //+Damage

        Modifier damage = new Modifier
        {
            ModID = ModifierIDGenerator.Next(Runner),
            Source = Object,
            StatType = StatsType.Damage,
            ModApplyType = ModApplyType.PercentAdd,
            Value = hasInputAuthority ? powerBuff : -powerBuff,
            IsTimed = false
        };
        //AttackSpeed
        Modifier attackSpeed = new Modifier
        {
            ModID = ModifierIDGenerator.Next(Runner),
            Source = Object,
            StatType = StatsType.AttackSpeed,
            ModApplyType = ModApplyType.PercentAdd,
            Value = hasInputAuthority ? powerBuff : -powerBuff,
            IsTimed = false
        };
        //MoveSpeed       
        Modifier moveSpeed = new Modifier
        {
            ModID = ModifierIDGenerator.Next(Runner),
            Source = Object,
            StatType = StatsType.MoveSpeed,
            ModApplyType = ModApplyType.PercentAdd,
            Value = hasInputAuthority ? powerBuff : -powerBuff,
            IsTimed = false
        };
        objInZone[affector] = new List<int> { damage.ModID, attackSpeed.ModID, moveSpeed.ModID};
        affector.AddModifier(damage);
        affector.AddModifier(attackSpeed);
        affector.AddModifier(moveSpeed);
    }


    void RemoveBuff(IAffector affactor)
    {
        if(objInZone.TryGetValue(affactor, out var list))
        {
            foreach (var i in list)
            {
                affactor.RemoveModifier(i);
            }
            objInZone.Remove(affactor);
        }
    }



}
