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
    /// <summary>
    /// Idle, Holding, Session
    /// </summary>
    public enum InteractionState : byte { Idle, Holding, Session }

    public enum InteractionEndReason : byte
    {
        Completed, Released, Cancelled, TargetLost, OutOfRange, Moved, ConditionFailed
    }
    public readonly struct InteractionFail
    {
        public readonly string Message;
        public readonly int Code;      // tuỳ chọn: dùng để localize / icon
        public InteractionFail(string message, int code = 0) { Message = message; Code = code; }
        public bool IsEmpty => string.IsNullOrEmpty(Message);
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
        public T Get<T>() where T : class
        {
            return Interactor != null ? Interactor.GetComponentInChildren<T>(true) : null;
        }
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

    [Header("Tên State trong Animator của người tương tác (để trống nếu không dùng)")]
    [Tooltip("Phát khi bắt đầu. Với Press: phát ở thời điểm thực thi (one-shot).")]
    public string BeginState;
    [Tooltip("Phát sau BeginState (hoặc ngay khi bắt đầu nếu BeginState trống). Clip phải bật Loop Time.")]
    public string LoopState;
    [Tooltip("Phát khi kết thúc, thường là Idle/Locomotion để trả nhân vật về trạng thái thường.")]
    public string EndState;
}


    [Serializable]
    public class ClipTiming
    {
        [Min(0.01f)] public float ClipLength = 1f;   // giây, khi clip chạy tốc độ 1x
        [Min(1)] public int TotalFrames = 30;
        [Min(0)] public int HitFrame = 15;

        public float HitNormalized => Mathf.Clamp01((float)HitFrame / Mathf.Max(1, TotalFrames));

        public static float Cycle(float rate) => 1f / Mathf.Max(0.01f, rate);
        public float HitDelay(float rate) => Cycle(rate) * HitNormalized;
        public float AnimSpeed(float rate) => ClipLength * Mathf.Max(0.01f, rate);
    }
}
