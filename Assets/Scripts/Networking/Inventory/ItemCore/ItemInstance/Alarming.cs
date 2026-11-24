using System;
using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

namespace GameItems
{
    /// <summary>
    /// Báo động cục bộ, đặt trên player prefab (child ở đầu player).
    /// Chỉ hoạt động trên máy của chủ nhân (HasInputAuthority): hiện icon + phát âm thanh.
    /// Không có logic mạng: StateAuthority đã bắn Door.OnDoorToggle, class này chỉ lắng nghe.
    ///
    /// Dùng:  alarming.Arm(door);   // sau khi chủ nhân interact cửa bằng item Alarming
    ///        alarming.Disarm();    // gỡ báo động
    ///
    /// Giả định về Door:  public event Action&lt;bool, PlayerRef&gt; OnDoorToggle;  (true = cửa MỞ)
    /// và sự kiện này phải tới được máy của chủ nhân (không chỉ bắn trên server).
    /// </summary>
    public class Alarming : MonoBehaviour
    {
        [Header("Chu kỳ")]
        [SerializeField] float _beepDuration = 1.5f;
        [SerializeField] float _pauseDuration = 1f;
        [SerializeField] Vector3 spawnOffset;


        [Header("Luật")]
        [Tooltip("true: chỉ chủ nhân đóng cửa mới tắt báo động. false: ai đóng cũng tắt.")]
        [SerializeField] bool _onlyOwnerSilences = true;

        [Header("Hiển thị")]
        [Tooltip("Gốc chứa Canvas/Image. Tự tắt nếu không phải chủ nhân.")]
        [SerializeField] GameObject _visualRoot;
        [SerializeField] Image _icon;
        [SerializeField] AudioSource _audio;

        public bool IsArmed => _door != null;
        public bool IsRinging { get; private set; }

        NetworkObject _owner;
        Door _door;
        Coroutine _cycle;

        bool IsLocalOwner => _owner != null && _owner.IsValid && _owner.HasInputAuthority;

        void Awake()
        {
            _owner = GetComponentInParent<NetworkObject>();
            SetBeep(false);
            if (_visualRoot != null) _visualRoot.SetActive(false);

            transform.localPosition += spawnOffset;

        }

        void OnDisable() => Disarm();

        /// <summary>Bắt đầu theo dõi 1 cửa. Chỉ có tác dụng trên máy chủ nhân.</summary>
        public void Arm(Door door)
        {
            if (_owner == null) _owner = GetComponentInParent<NetworkObject>(); // lấy lại nếu Awake chưa tìm được
            if (door == null) { Debug.LogWarning("[Alarming] Arm: door null"); return; }
            if (!IsLocalOwner)
            {
                Debug.LogWarning($"[Alarming] Arm bị bỏ qua. owner={(_owner ? _owner.name : "NULL")} " +
                                 $"hasInputAuth={(_owner && _owner.IsValid && _owner.HasInputAuthority)}");
                return;
            }
            Disarm();
            _door = door;
            _door.OnDoorToggle += HandleDoorToggle;
            if (_visualRoot != null) _visualRoot.SetActive(true);
            SetBeep(false);
            Debug.Log($"[Alarming] Armed door={door.name}");
        }

        public void Disarm()
        {
            if (_door != null) _door.OnDoorToggle -= HandleDoorToggle;
            _door = null;
            StopAlarm();
            if (_visualRoot != null) _visualRoot.SetActive(false);
        }

        void HandleDoorToggle(bool isOpen, PlayerRef who)
        {
            Debug.Log($"[Alarming] toggle open={isOpen} who={who} me={(_owner ? _owner.InputAuthority.ToString() : "null")}");
            if (!IsLocalOwner) return;
            PlayerRef me = _owner.InputAuthority;

            if (isOpen)
            {
                if (who == me || IsRinging) return; // chủ nhân mở, hoặc đang kêu rồi
                StartAlarm(who);
            }
            else
            {
                if (!IsRinging) return;
                if (_onlyOwnerSilences && who != me) return;
                StopAlarm();
            }
        }

        void StartAlarm(PlayerRef intruder)
        {
            IsRinging = true;
            _cycle = StartCoroutine(Cycle());
        }

        void StopAlarm()
        {
            bool was = IsRinging;
            IsRinging = false;
            if (_cycle != null) { StopCoroutine(_cycle); _cycle = null; }
            SetBeep(false);
        }

        // kêu -> nghỉ -> kêu ... cho tới khi StopAlarm
        IEnumerator Cycle()
        {
            var beep = new WaitForSeconds(_beepDuration);
            var pause = new WaitForSeconds(_pauseDuration);
            while (true)
            {
                SetBeep(true);
                yield return beep;
                SetBeep(false);
                yield return pause;
            }
        }

        // Icon nhấp nháy và âm thanh cùng theo pha beep
        void SetBeep(bool on)
        {
            if (_icon != null) _icon.enabled = on;
            if (_audio == null) return;

            if (on) { _audio.loop = true; if (!_audio.isPlaying) _audio.Play(); }
            else if (_audio.isPlaying) _audio.Stop();
        }
    }
}
