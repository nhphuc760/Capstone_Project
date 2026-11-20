using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Điều kiện data-driven (ScriptableObject). Gắn vào Action trong Inspector.
    /// Thêm luật mới = thêm 1 class kế thừa, không sửa core.
    /// </summary>
    public abstract class InteractionCondition : ScriptableObject
    {
        [SerializeField] private string _failMessage;   // chỉnh trong Inspector

        public abstract bool Evaluate(in InteractionContext ctx);

        // Mặc định lấy message trong Inspector; Condition riêng có thể override
        public virtual InteractionFail GetFail(in InteractionContext ctx) => new InteractionFail(_failMessage);
    }
}
