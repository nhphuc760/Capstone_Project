using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Chạy animation tương tác của người chơi trên MỌI client (kể cả người khác nhìn thấy mình phá cửa/chém).
    /// Tên param lấy từ InteractionPresentation của Action => thêm animation mới không cần sửa code.
    /// </summary>
    public class InteractorAnimator : MonoBehaviour
    {
        [SerializeField] private InteractionController _controller;
        [SerializeField] private Animator _animator;

        private string _loopBool;

        private void OnEnable() { if (_controller != null) _controller.Presented += OnPresented; }
        private void OnDisable() { if (_controller != null) _controller.Presented -= OnPresented; }

        private void OnPresented(InteractionEvent e)
        {
            if (_animator == null || e.Action == null) return;
            var p = e.Action.Presentation;

            switch (e.Type)
            {
                case InteractionEventType.Begin:
                    Trigger(p.BeginTrigger);
                    if (!string.IsNullOrEmpty(p.LoopBool)) { _loopBool = p.LoopBool; _animator.SetBool(_loopBool, true); }
                    break;
                case InteractionEventType.Perform:
                    Trigger(p.PerformTrigger);
                    break;
                case InteractionEventType.End:
                    if (!string.IsNullOrEmpty(_loopBool)) { _animator.SetBool(_loopBool, false); _loopBool = null; }
                    break;
            }
        }

        private void Trigger(string name)
        {
            if (!string.IsNullOrEmpty(name)) _animator.SetTrigger(name);
        }
    }
}
