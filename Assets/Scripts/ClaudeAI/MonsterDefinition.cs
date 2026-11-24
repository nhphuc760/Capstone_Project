using System.Collections.Generic;
using UnityEngine;

namespace SilentCastle.MonsterAI
{
    /// <summary>
    /// Toàn bộ "cá tính" của một loại quái nằm ở đây (data-driven).
    /// Tạo nhiều asset khác nhau => nhiều loại quái với hành vi khác nhau mà không sửa code.
    /// </summary>
    [CreateAssetMenu(menuName = "SilentCastle/Monster Definition")]
    public class MonsterDefinition : ScriptableObject
    {
        [Header("Level & Exp")]
        public int startLevel = 1;
        public int baseExp = 30;
        public int modFactor = 20;
        [Range(0f, 1f)] public float levelUpHealPercent = 0.25f;

        [Header("Chỉ số theo level (index 0 = level 1)")]
        public StatsBase[] statsByLevel;

        public int MaxLevel => statsByLevel.Length;
        public StatsBase GetStats(int lvl) => statsByLevel[Mathf.Clamp(lvl - 1, 0, statsByLevel.Length - 1)];
        public float hpGrowth = 1.2f;              // nhân mỗi level
        public float damageGrowth = 1.15f;
        public float moveSpeedPerLevel = 0.15f;    // cộng mỗi level
        public float attackIntervalGrowth = 0.96f; // nhân mỗi level (nhỏ hơn 1 = đánh nhanh dần)
        public float minAttackInterval = 0.5f;
        public float attackRange = 1.8f;

        [Header("Bỏ chạy (level thấp = nhát, level cao = lì)")]
        [Range(0f, 1f)] public float fleeHpAtLevel1 = 0.6f;     // % máu để bắt đầu chạy ở level 1
        [Range(0f, 1f)] public float fleeHpAtMaxLevel = 0.08f;  // % máu để bắt đầu chạy ở level tối đa
        [Range(0f, 0.5f)] public float personalityRange = 0.2f; // mỗi con lệch ±20% => không con nào giống con nào
        [Range(0f, 1f)] public float rageFleeSuppression = 0.5f; // đang nộ thì ngưỡng chạy nhân hệ số này
        public float fleeSpeedMultiplier = 1.25f;


        [Header("Animation đòn đánh")]
        public float attackClipLength = 1.0f;                 // độ dài clip đánh ở speed 1 (giây), lấy từ clip.length
        [Range(0.05f, 0.95f)] public float attackHitNormalized = 0.45f; // frame chạm nằm ở % nào của clip
        public float minAttackAnimSpeed = 0.5f;
        public float maxAttackAnimSpeed = 3f;


        [Header("Hồi máu")]
        [Range(0f, 1f)] public float healPercentPerSecond = 0.06f;

        [Header("Ngày / Đêm")]
        public float dawnSafetyMultiplier = 1.25f;   // nhân thời gian chạy về (đệm an toàn)
        public float dawnSafetyBufferSeconds = 2f;
        public float minSortieSeconds = 8f;          // còn ít hơn mức này (+ thời gian về) thì không xuất kích nữa
        public float daylightSurvivalSeconds = 12f;  // ở ngoài nắng quá lâu sẽ chết

        [Header("Khác")]
        public float corpseSeconds = 2f;
        public TargetingProfile targeting;
        public List<MonsterSkillDef> skills = new List<MonsterSkillDef>();

        // ---- công thức theo level ----
        public float MaxHp(float baseHealth, int lvl) => baseHealth * Mathf.Pow(hpGrowth, lvl - 1);
        public float Damage(float baseDamage, int lvl) => baseDamage * Mathf.Pow(damageGrowth, lvl - 1);
        public float MoveSpeed(float baseMoveSpeed, int lvl) => baseMoveSpeed + moveSpeedPerLevel * (lvl - 1);
        public float AttackInterval(float baseAttackInterval, int lvl) =>
            Mathf.Max(minAttackInterval, baseAttackInterval * Mathf.Pow(attackIntervalGrowth, lvl - 1));
        /// <summary>
        /// lv1 = 30, lv2 = 50, truyền vào lv hiện tại
        /// </summary>
        /// <param name="lvl"></param>
        /// <returns></returns>
        public int ExpToNext(int lvl) => baseExp + modFactor * (lvl - 1);

        public float FleeThreshold01(int lvl)
        {
            float t = MaxLevel <= 1 ? 1f : Mathf.Clamp01((lvl - 1) / (float)(MaxLevel - 1));
            return Mathf.Lerp(fleeHpAtLevel1, fleeHpAtMaxLevel, t);
        }
    }
}
