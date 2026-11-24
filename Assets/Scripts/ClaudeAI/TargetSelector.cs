using System.Collections.Generic;
using UnityEngine;

namespace SilentCastle.MonsterAI
{
    public static class TargetSelector
    {
        // Mức ưu tiên (số nhỏ = ưu tiên cao)
        public const int TierExposedPlayer = 0;  // player lộ diện ngoài thành
        public const int TierGate = 1;           // cửa thành
        public const int TierFallback = 2;       // cửa vỡ hết -> player trong thành

        static readonly List<IMonsterTarget> _buf = new List<IMonsterTarget>(32);

        /// <param name="force">true = bỏ qua commit/hysteresis (dùng khi chưa có mục tiêu hoặc khẩn cấp).
        /// Nếu xuất hiện mục tiêu ở tier ưu tiên cao hơn mục tiêu hiện tại thì tự động force.</param>
        public static IMonsterTarget Select(MonsterBrain m, bool force)
        {
            var w = m.World;
            var p = m.Profile;
            var cur = m.CurrentTarget;

            BuildCandidates(m, w, p, out int tier);
            if (_buf.Count == 0) return null;

            bool curValid = cur != null && cur.IsTargetable && _buf.Contains(cur);

            // Có mục tiêu ở tier tốt hơn (vd đang đánh cửa mà player lộ diện) -> đổi ngay, không hysteresis
            if (curValid && TierOf(w, cur) > tier) force = true;

            if (curValid && !force && m.TimeOnTarget < p.minCommitSeconds) return cur;

            IMonsterTarget best = null;
            float bestScore = float.MinValue, curScore = float.MinValue;
            for (int i = 0; i < _buf.Count; i++)
            {
                var t = _buf[i];
                float s = Score(m, w, p, t, cur);
                if (t == cur) curScore = s;
                if (s > bestScore) { bestScore = s; best = t; }
            }

            if (curValid && !force && best != cur && bestScore < curScore + p.switchMargin)
                return cur;
            return best;
        }

        /// <summary>Kiểm tra rẻ: hiện có ứng viên ở tier ưu tiên cao hơn mục tiêu đang nhắm không?</summary>
        public static bool HasBetterTier(MonsterBrain m)
        {
            var cur = m.CurrentTarget;
            if (cur == null) return false;
            BuildCandidates(m, m.World, m.Profile, out int tier);
            return _buf.Count > 0 && TierOf(m.World, cur) > tier;
        }

        /// <summary>Mục tiêu có còn "đúng luật" không (cửa, hoặc player đang lộ diện)? Dùng cho khoá chống lật.</summary>
        public static bool IsLegit(WorldContext w, IMonsterTarget t) =>
            t != null && t.IsTargetable && TierOf(w, t) != TierFallback;

        static int TierOf(WorldContext w, IMonsterTarget t)
        {
            if (t.Kind == TargetKind.Gate) return TierGate;
            return w.IsExposed(t.TargetId) ? TierExposedPlayer : TierFallback;
        }

        // Luật: có player lộ diện ngoài thành (và chưa quá số quái đuổi) -> đánh player.
        // Không có -> đánh cửa. Cửa vỡ hết -> đánh player trong thành.
        static void BuildCandidates(MonsterBrain m, WorldContext w, TargetingProfile p, out int tier)
        {
            _buf.Clear();
            Vector3 pos = m.Position;
            var cur = m.CurrentTarget;

            var players = w.Players;
            for (int i = 0; i < players.Count; i++)
            {
                var t = players[i];
                if (!t.IsTargetable) continue;
                if (!w.IsExposed(t.TargetId)) continue;

                float dist = (t.Position - pos).magnitude;
                if (dist > p.aggroRadiusOutside) continue;

                // Mỗi player chỉ bị tối đa N quái đuổi; mục tiêu hiện tại luôn được giữ chỗ
                if (t != cur && w.GetClaims(t.TargetId) >= p.maxChasersPerPlayer) continue;

                _buf.Add(t);
            }

            if (_buf.Count > 0) { tier = TierExposedPlayer; return; }

            var gates = w.Gates;
            for (int i = 0; i < gates.Count; i++)
                if (gates[i].IsTargetable) _buf.Add(gates[i]);

            if (_buf.Count > 0) { tier = TierGate; return; }

            // Fallback: cửa vỡ hết -> mọi player còn sống
            for (int i = 0; i < players.Count; i++)
                if (players[i].IsTargetable) _buf.Add(players[i]);

            tier = TierFallback;
        }

        static float Score(MonsterBrain m, WorldContext w, TargetingProfile p, IMonsterTarget t, IMonsterTarget cur)
        {
            float d = Vector3.Distance(m.Position, t.Position);
            float s = -d * p.distanceWeight;

            int claims = Mathf.Max(0, w.GetClaims(t.TargetId) - (t == cur ? 1 : 0)); // không tính chính mình
            s -= p.loadWeight * claims * (1f + 0.25f * claims);                       // càng đông càng bị phạt nặng

            s += (1f - Mathf.Clamp01(t.Health01)) * p.lowHealthBonus;
            s += Noise(m.Seed, (int)t.TargetId.Raw) * p.noiseAmount;

            if (t == cur)
            {
                float k = Mathf.Lerp(1f, p.stickMinFactor, Mathf.Clamp01(m.TimeOnTarget / Mathf.Max(0.01f, p.stickDecaySeconds)));
                s += p.stickBonus * k;
            }
            return s;
        }

        static float Noise(int seed, int id)
        {
            float v = Mathf.Sin((seed % 1000) * 12.9898f + (id % 1000) * 78.233f) * 43758.5453f;
            return (v - Mathf.Floor(v)) * 2f - 1f; // -1..1, ổn định
        }
    }
}
