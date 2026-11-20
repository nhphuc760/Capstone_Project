using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Gắn lên bất kỳ NetworkObject nào có thể tương tác (Door, Tree, Player, Tower...).
    /// Nó chỉ là "bộ chứa" các InteractionAction (đặt cùng object hoặc object con).
    /// Core không biết gì về Door/Axe/Radar - mọi hành vi nằm trong Action.
    /// </summary>
    public class Interactable : NetworkBehaviour
    {
        [SerializeField] private Transform _focusPoint; // Là điểm tương tác của đối tượng gắn script (thường đặt dưới đít)
        [SerializeField, Min(0.1f)] private float _defaultRange = 2.5f; // Phạm vi tương tác mặc định

        private InteractionAction[] _actions;     // theo thứ tự hierarchy => index giống nhau trên mọi peer
        private InteractionAction[] _byPriority; // sự tồn tại của nó để đảm bảo nếu có nhiều action chồng chập trên cùng đối tượng sẽ được quyết định bằng ưu tiên

        public Transform FocusPoint => _focusPoint != null ? _focusPoint : transform;
        public float DefaultRange => _defaultRange;
        public IReadOnlyList<InteractionAction> Actions { get { EnsureCache(); return _actions; } }

        public override void Spawned() => EnsureCache();
        private void EnsureCache()
        {
            if (_actions != null && _actions.Length > 0) return;
            _actions = GetComponentsInChildren<InteractionAction>(true);
            for (int i = 0; i < _actions.Length; i++) _actions[i].Bind(this, (byte)i);
            _byPriority = _actions.OrderByDescending(a => a.Priority).ToArray();
        }

        public InteractionAction GetAction(byte index)
        {
            EnsureCache();
            return index < _actions.Length ? _actions[index] : null;
        }

        /// <summary>Chọn Action ưu tiên cao nhất thoả slot + tầm + mọi điều kiện.</summary>
        public bool TryGetBestAction(int slot, in InteractionContext ctx, float flatDistance,
                              out InteractionAction best, out InteractionFail fail)
        {
            EnsureCache();
            best = null;
            fail = default;

            for (int i = 0; i < _byPriority.Length; i++)
            {
                var a = _byPriority[i];
                if (a.Slot != slot || !a.IsInRange(flatDistance)) continue;

                if (a.CanInteract(ctx, out var f))
                {
                    best = a;
                    fail = default;
                    return true;
                }
                if (fail.IsEmpty) fail = f; // giữ lý do của action ưu tiên cao nhất
            }
            return false;
        }
    }
}
