using System.Collections.Generic;
using UnityEngine;

namespace SilentCastle.MonsterAI
{

    public static class TargetSelector
    {
        static readonly List<IMonsterTarget> _buf = new List<IMonsterTarget>(32);

        /// <param name="force">true = bỏ qua commit/hysteresis (dùng khi chưa có mục tiêu).</param>
        public static IMonsterTarget Select(MonsterBrain m, bool force)
        {
            var w = m.World;
            var p = m.Profile;
            var cur = m.CurrentTarget;

            BuildCandidates(m, w, p);
            if (_buf.Count == 0) return null;

            bool curValid = cur != null && cur.IsTargetable && _buf.Contains(cur);
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

        // Luật: có player ở ngoài thành -> đánh player. Không có -> đánh cửa.
        // Cửa đã vỡ hết -> đánh player trong thành.
        static void BuildCandidates(MonsterBrain m, WorldContext w, TargetingProfile p)
        {
            _buf.Clear();
            Vector3 pos = m.Position;            

            var players = w.Players;
            Debug.Log($"[TargetSelector] Players count = {players.Count}, Gates count = {w.Gates.Count}");

            for (int i = 0; i < players.Count; i++)
            {
                var t = players[i];
                if (!t.IsTargetable)
                {
                    Debug.Log($"  Player {t.TargetId} chết");
                    continue;
                }

                var netObj = m.Runner.FindObject(t.TargetId);
                if (netObj == null)
                {
                    Debug.LogWarning($"  Không tìm thấy NetworkObject của {t.TargetId}");
                    continue;
                }

                bool inside = w.IsInsideCastle(netObj.InputAuthority, t.Position);
                float dist = (t.Position - pos).magnitude;

                Debug.Log($"  Player {t.TargetId}: Inside={inside}, Dist={dist:F1}, Aggro={p.aggroRadiusOutside}");

                if (inside) continue;
                if (dist > p.aggroRadiusOutside) continue;

                _buf.Add(t);
            }

            if (_buf.Count > 0)
            {
                Debug.Log($"→ Chọn được {_buf.Count} player ngoài thành");
                return;
            }

            var gates = w.Gates;
            for (int i = 0; i < gates.Count; i++)
                if (gates[i].IsTargetable) _buf.Add(gates[i]);

            Debug.Log($"→ Sau khi lấy Gate: {_buf.Count}");

            if (_buf.Count > 0) return;

            // Fallback: mọi player còn sống
            for (int i = 0; i < players.Count; i++)
                if (players[i].IsTargetable) _buf.Add(players[i]);

            Debug.Log($"→ Fallback toàn bộ player: {_buf.Count}");
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
