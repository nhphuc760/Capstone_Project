using System;
using Fusion;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>Cách một Action phản ứng với 3 trạng thái của nút (press / hold / release).</summary>
    public enum InteractionMode : byte
    {
        Press,      // Thực thi ngay khi nhấn (toggle cửa, nhặt đồ)
        Hold,       // Giữ đủ HoldDuration thì thực thi 1 lần (phá cửa, tăng bảo mật)
        HoldRepeat, // Giữ: sau HoldDuration (windup) thực thi, rồi lặp mỗi RepeatInterval (chém, đào)
        Release,    // Nhả nút mới thực thi, Progress01 = mức nạp (charge) (ném, bắn nạp lực)
        Session     // Nhấn để mở phiên (radar, pháo) - kéo dài đến khi huỷ/nhấn lại/OnSessionTick trả false
    }

    public enum InteractionState : byte { Idle, Holding, Session }

    public enum InteractionEndReason : byte
    {
        Completed, Released, Cancelled, TargetLost, OutOfRange, Moved, ConditionFailed
    }

    public enum InteractionEventType : byte { Begin, Perform, End }    
    /// <summary>Inventory / Equipment của nhân vật implement interface này (đọc từ networked state).</summary>
    public interface IItemHoldProvider
    {
        ItemSO CurrentHold { get; }
    }

    /// <summary>Đối tượng có chủ sở hữu (Door, Chest...). Có thể đổi sang team-based mà không đụng core.</summary>
    public interface IOwnable
    {
        bool IsOwnedBy(PlayerRef player);
    }

    /// <summary>Dữ liệu tự do client gửi lên server trong lúc Hold/Session (vd: toạ độ bắn pháo).</summary>
    public struct InteractionPayload : INetworkStruct
    {
        public int Id;
        public float Value;
        public Vector3 Position;
    }

    /// <summary>Ngữ cảnh của 1 lần tương tác. Truyền xuống mọi Condition/Action.</summary>
    public struct InteractionContext
    {
        [Tooltip("Người tương tác")]
        public InteractionController Interactor;
        [Tooltip("Đối tượng tương tác")]
        public Interactable Target;

        public ItemSO ItemHold; // có thể null (tay không)
        [Tooltip("Giây đã giữ")]
        public float Elapsed;                  // giây đã giữ
        [Tooltip("Báo cáo tiến trình")]
        public float Progress01;               // 0..1 (hold progress / charge)

        public NetworkRunner Runner => Interactor.Runner;
        public PlayerRef Player => Interactor.Object.InputAuthority;
    }

    public readonly struct InteractionEvent
    {
        public readonly InteractionEventType Type;
        public readonly InteractionContext Context;
        public readonly InteractionAction Action;
        public readonly bool IsLocal; // true nếu máy này là input authority của người tương tác

        public InteractionEvent(InteractionEventType type, InteractionContext ctx, InteractionAction action, bool isLocal)
        {
            Type = type; Context = ctx; Action = action; IsLocal = isLocal;
        }
    }

    /// <summary>Dữ liệu trình bày: prompt UI + tên param animator của NGƯỜI TƯƠNG TÁC.</summary>
    [Serializable]
    public class InteractionPresentation
    {
        public string Prompt = "Interact";
        public Sprite Icon;
        [Header("Animator của người tương tác (để trống nếu không dùng)")]
        public string BeginTrigger;
        public string LoopBool;
        public string PerformTrigger;
    }
}
