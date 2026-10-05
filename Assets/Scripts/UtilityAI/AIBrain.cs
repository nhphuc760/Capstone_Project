using System.Collections.Generic;
using System.Linq;
using Fusion;
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
        public Vector3 BestRecoverZone()
        {
            return default;
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
