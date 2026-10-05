using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.AI;


namespace UtilityAI.Core
{


    public enum ContextKey
    {
        None,
        WidthMap,
        HeightMap,
        DoorTarget,
        CandidatePlaceTarget,
    }

    public class Context
    {

        public AIBrain Brain { get; private set; }
        public AIController Controller { get; private set; }
        public NavMeshAgent Agent { get; private set; }

        public PlayerRef TargetPlayer;
        public Context(AIBrain brain)
        {
            this.Brain = brain;
            this.Agent = brain.gameObject.GetComponent<NavMeshAgent>();
            this.Controller = brain.gameObject.GetComponent<AIController>();
        }

        Dictionary<ContextKey, object> boards = new Dictionary<ContextKey, object>();


        public T Get<T>(ContextKey key) => boards.TryGetValue(key, out var value) ? (T)value : default(T);
        public bool TryGetValue<T>(ContextKey key, out T value)
        {
            var _value = Get<T>(key);
            value = _value;
            return _value != null;
        }

        public void SetData(ContextKey key, object value) => boards[key] = value;       
    }
}



