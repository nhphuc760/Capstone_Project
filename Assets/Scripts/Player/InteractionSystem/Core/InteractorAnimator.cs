using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Chạy animation tương tác trên MỌI client, chỉ dùng Animator.Play + 1 float param tốc độ.
    /// Begin  : set ActionSpeed, phát BeginState (-> LoopState khi xong) hoặc vào thẳng LoopState
    /// Perform: Press => phát BeginState one-shot; mode khác => cập nhật ActionSpeed (buff/debuff)
    /// End    : phát EndState. KHÔNG reset ActionSpeed (tránh đổi tốc độ clip chặt giữa lúc đang blend ra)
    /// </summary>
    public class InteractorAnimator : MonoBehaviour
    {
        [SerializeField] private InteractionController _controller;
        [SerializeField] private Animator _animator;
        [SerializeField] private int _layer = 0;

        [Tooltip("Float param làm Speed Multiplier của state chặt/đào. Để trống nếu không dùng.")]
        [SerializeField] private string _speedParam = "ActionSpeed";

        [Tooltip("0 = Play (cắt thẳng). >0 = CrossFadeInFixedTime (giây) khi vào EndState.")]
        [SerializeField, Min(0f)] private float _endBlend = 0.1f;

        private int _speedHash;
        private int _pendingLoop;   // hash LoopState đang chờ BeginState chạy xong, 0 = không chờ
        private int _beginHash;
        private int _waitFrame;

        private void Awake()
        {
            if (!string.IsNullOrEmpty(_speedParam)) _speedHash = Animator.StringToHash(_speedParam);
        }

        private void OnEnable() { if (_controller != null) _controller.Presented += OnPresented; }
        private void OnDisable() { if (_controller != null) _controller.Presented -= OnPresented; }

        InteractionPresentation _cached;
        private void OnPresented(InteractionEvent e)
        {
            if (_animator == null) return;

            if (e.Action != null) _cached = e.Action.Presentation;   // check kiểu Unity, chỉ cập nhật khi còn sống
            var p = _cached;
            if (p == null) return;

            switch (e.Type)
            {
                case InteractionEventType.Begin:
                    if (e.Action == null) return;
                    SetSpeed(e.Action.GetAnimationSpeed(e.Context));   // set TRƯỚC khi Play để chạy đúng tốc độ từ frame 0
                    if (!string.IsNullOrEmpty(p.BeginState))
                    {
                        PlayNow(p.BeginState);
                        if (!string.IsNullOrEmpty(p.LoopState))
                        {
                            _beginHash = Animator.StringToHash(p.BeginState);
                            _pendingLoop = Animator.StringToHash(p.LoopState);
                            _waitFrame = Time.frameCount + 1;          // state info chỉ cập nhật sau khi Animator evaluate
                        }
                    }
                    else PlayNow(p.LoopState);
                    break;

                case InteractionEventType.Perform:
                    if(e.Action == null) return;
                    SetSpeed(e.Action.GetAnimationSpeed(e.Context));
                    // Press không có Begin/End nên phát one-shot ở đây
                    if (e.Action.Mode == InteractionMode.Press)
                        PlayNow(!string.IsNullOrEmpty(p.BeginState) ? p.BeginState : p.LoopState);
                    break;

                case InteractionEventType.End:
                    _pendingLoop = 0;
                    if (p != null && !string.IsNullOrEmpty(p.EndState))
                    {
                        int h = Animator.StringToHash(p.EndState);
                        if (_endBlend > 0f) _animator.CrossFadeInFixedTime(h, _endBlend, _layer);
                        else _animator.Play(h, _layer, 0f);
                    }
                    else
                    {
                        _animator.Play("Idle", _layer);
                    }
                        break;
            }
        }

        // BeginState chạy xong thì chuyển sang LoopState, không cần transition trong Animator
        private void Update()
        {
            if (_pendingLoop == 0 || _animator == null || Time.frameCount < _waitFrame) return;
            var info = _animator.GetCurrentAnimatorStateInfo(_layer);
            if (info.shortNameHash != _beginHash) { _pendingLoop = 0; return; }   // bị state khác chen vào
            if (info.normalizedTime >= 1f)
            {
                _animator.Play(_pendingLoop, _layer, 0f);
                _pendingLoop = 0;
            }
        }

        private void PlayNow(string state)
        {
            if (!string.IsNullOrEmpty(state)) _animator.Play(Animator.StringToHash(state), _layer, 0f);
        }

        private void SetSpeed(float s)
        {
            if (_speedHash != 0) _animator.SetFloat(_speedHash, s);
        }
    }
}