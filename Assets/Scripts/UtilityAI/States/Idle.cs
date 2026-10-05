using System;
using System.Linq;
using Fusion;
using UnityEngine;
using UtilityAI.Core;




public abstract class EnemyBaseState : IState
{
   

    protected readonly Context context;
    protected EnemyBaseState(Context context)
    {
        this.context = context;
    }

    public abstract void Enter();

    public abstract void Update(float deltaTime);
    public abstract void Exit();

    public abstract EnemyState TryGetNextState();

   
}


public class Idle : EnemyBaseState
{
    public Idle(Context context) : base(context)
    {

    }

    public override void Enter()
    {
        Debug.Log("Enter Idle");
        var target = context.Brain.BestPlayerTarget();
        if (target.player == PlayerRef.None) return;
        context.TargetPlayer = target.player;
        if (StructureManager.Ins != null)
        {
            var doorEntry = StructureManager.Ins.WithCategory(StructureCategory.Defense).WithType(StructureType.Door).WithPlayerRef(target.player).Get().FirstOrDefault();
            if (doorEntry.Value != null)
            {
                context.SetData(ContextKey.DoorTarget, doorEntry.Value);
            }
        }
        
    }

    public override void Update(float deltaTime)
    {

    }

    public override void Exit()
    {
      
    }

    public override EnemyState TryGetNextState()
    {
        if (context.TryGetValue<StructureBase>(ContextKey.DoorTarget, out var door))
        {
            return EnemyState.MoveToDoor;
        }
        else if(context.TargetPlayer != PlayerRef.None)
        {
            return EnemyState.MoveToPlayer;
        }
            return EnemyState.Idle;
    }
}
