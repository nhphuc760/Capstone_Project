using UnityEngine;

namespace SilentCastle.MonsterAI
{
    // =====================================================================
    //  Kỹ năng cơ bản: TÍCH NỘ
    // =====================================================================
    [CreateAssetMenu(menuName = "SilentCastle/Skills/Rage")]
    public class RageSkillDef : MonsterSkillDef
    {
        public float ragePerHit = 12f;
        public float rageMax = 100f;
        public float duration = 6f;
        public float extendPerHit = 0.3f;       // đang nộ mà vẫn đánh trúng thì kéo dài thêm chút
        public float moveSpeedMul = 1.3f;
        public float attackSpeedMul = 1.4f;
        public float damageMul = 1.5f;

        public override MonsterSkillRuntime CreateRuntime(MonsterBrain owner) => new RageSkillRuntime(owner, this);
    }
}
