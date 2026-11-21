using UnityEngine;

namespace SilentCastle.MonsterAI
{
    /// <summary>Trọng số chọn mục tiêu. Mỗi loại quái có thể dùng profile riêng.</summary>
    [CreateAssetMenu(menuName = "SilentCastle/Targeting Profile")]
    public class TargetingProfile : ScriptableObject
    {
        public float aggroRadiusOutside = 50f;   // player ngoài thành trong bán kính này mới bị coi là mục tiêu

        [Header("Điểm số")]
        public float distanceWeight = 1f;        // trừ điểm theo mét
        public float loadWeight = 8f;            // trừ điểm cho mỗi quái KHÁC đang nhắm mục tiêu đó (dàn trải)
        public float lowHealthBonus = 6f;        // mục tiêu yếu hấp dẫn hơn
        public float noiseAmount = 2f;           // cá tính riêng, ổn định theo (quái, mục tiêu) -> không nhấp nháy

        [Header("Bám mục tiêu (chống chạy qua chạy lại)")]
        public float stickBonus = 12f;           // cộng điểm cho mục tiêu hiện tại
        [Range(0f, 1f)] public float stickMinFactor = 0.25f;
        public float stickDecaySeconds = 25f;    // bám lâu thì giảm dần -> không đứng im mãi một nhà
        public float switchMargin = 5f;          // mục tiêu mới phải hơn hẳn mới đổi
        public float minCommitSeconds = 4f;      // tối thiểu đánh bao lâu mới được cân nhắc đổi

        [Header("Đánh giá lại")]
        public float reevalInterval = 2f;
        public float reevalJitter = 0.7f;
    }
}
