using System.Collections.Generic;
using System.Linq;
using Fusion;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using Util.Core;
using UtilityAI.Considerations;

namespace UtilityAI.Core
{
    public class AIBrain : NetworkBehaviour
    {
        [SerializeField]
        AIController controller;
        [SerializeField]
        ConsiderationCollection doorEvaluate;
        [SerializeField]
        ConsiderationCollection healEscapeThreshold;

        [SerializeField] int baseExp = 30;
        [SerializeField] int modFactorExp = 20;

        public int MaxLevel = 10;
        [Networked]
        public int Level { get; set; }
        [Networked]
        public int CurrentExp { get; set; }


        Dictionary<Vector3, HashSet<Vector2Int>> recoverZones = new Dictionary<Vector3, HashSet<Vector2Int>>(); //center - cells

        public event System.Action OnLevelUp;

        public override void Spawned()
        {
            controller ??= GetComponent<AIController>();
            Level = 1;
            CurrentExp = 0;
            if (controller)
                controller.OnAttack += OnAttack;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (controller)
                controller.OnAttack -= OnAttack;
        }

        void OnAttack()
        {
            CurrentExp++;
            if (CurrentExp >= ExpToUpLevel(Level + 1))
            {
                LevelUp();
            }
        }

        public void LevelUp()
        {
            Level++;
            CurrentExp = 0;
            OnLevelUp?.Invoke();
        }

        public int ExpToUpLevel(int level) => baseExp + modFactorExp * (level - 1);
        public TargetEvaluateContext BestPlayerTarget()
        {

            if (StructureManager.Ins == null)
            {
                Debug.LogWarning("[AIBrain] StructureManager is null");
                return default;
            }

            var doors = StructureManager.Ins
                .WithCategory(StructureCategory.Defense)
                .WithType(StructureType.Door)
                .Get();

            List<TargetEvaluateContext> scores = new List<TargetEvaluateContext>();

            foreach (var player in Runner.ActivePlayers)
            {
                var doorEntry = doors
                    .Where(x => x.Value != null && x.Value.Object != null && x.Value.Object.InputAuthority == player)
                    .FirstOrDefault();

                if (doorEntry.Value != null)
                {
                    // Has door → evaluate attractiveness
                    var context = controller.worldInformation;
                    context.SetData(ContextKey.CandidatePlaceTarget, doorEntry.Value.transform.position);
                    float score = doorEvaluate != null
                        ? doorEvaluate.ScoreConsiderations(context)
                        : 0.5f;
                    scores.Add(new TargetEvaluateContext { player = player, score = Mathf.Max(score, 0.01f) });
                }
                else
                {
                    // No door → highest priority, rush the player
                    scores.Add(new TargetEvaluateContext { player = player, score = 1f });
                }
            }

            if (scores.Count == 0)
                return default;

            int index = Utils.RandomWeight(scores.Select(x => x.score).ToArray());
            return scores[index];
        }

        /// <summary>
        /// Find a safe recovery point on the NavMesh, far from the current combat target
        /// and preferably toward the map edge / opposite side of the fight.
        /// </summary>
        public Vector3? BestRecoverZone()
        {

            UpdateRecoverZone();
            var redZone = MapManager.Ins?.ZoneGroupsByType[ZoneType.Red];
            NavMeshSurface surface = NavMeshBakeManager.Ins?.Surface;
            if (surface == null) return null;
            (float, Vector3) tupple = new (float.MaxValue, -Vector3.one);
            foreach (var i in redZone)
            {
                Vector3 center = i.center;
                if (recoverZones[center].Count < 1)
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(center - transform.position);
                if (distance < tupple.Item1)
                {
                    tupple = (distance, center);
                }              
            }
            if (tupple.Item2 != -Vector3.one)
            {
                var cell = recoverZones[tupple.Item2].RandomElement();
                var pos = new Vector3(cell.x + 0.5f, 0, cell.y + 0.5f);
                if(NavMesh.SamplePosition(pos, out var hit, 2f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }
            return transform.position;
        }


        void UpdateRecoverZone()
        {
            var redZones = MapManager.Ins?.ZoneGroupsByType[ZoneType.Red];
            var structureManager = StructureManager.Ins != null ? StructureManager.Ins : FindAnyObjectByType<StructureManager>();
            foreach (var i in redZones)
            {
                if (!recoverZones.ContainsKey(i.center))
                {
                    recoverZones.Add(i.center, new HashSet<Vector2Int>());
                }
                else
                {
                    recoverZones[i.center].Clear();
                }

                Vector3 cornerBound = i.center + new Vector3(i.bounds.extents.x, 0, i.bounds.extents.y);
                float radius = (i.center - cornerBound).sqrMagnitude;
                List<PlayerRef> playerInZoneI = new List<PlayerRef>();
                Vector3Int key = default;
                foreach (var j in i.cells)
                {
                    key.x = j.x;
                    key.z = j.y;    
                    if (!structureManager.ContainsStructureKey(key))
                    {
                        recoverZones[i.center].Add(j);
                    }
                }                
            }
        }


        public float GetEscapeThreshold()
        {
            if (healEscapeThreshold == null || controller?.worldInformation == null)
                return 0.3f; // default 30% HP
            return healEscapeThreshold.ScoreConsiderations(controller.worldInformation);
        }

        public struct TargetEvaluateContext
        {
            public PlayerRef player;
            public float score;
        }
    }  
}
