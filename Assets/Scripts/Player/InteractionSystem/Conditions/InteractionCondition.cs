using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Điều kiện data-driven (ScriptableObject). Gắn vào Action trong Inspector.
    /// Thêm luật mới = thêm 1 class kế thừa, không sửa core.
    /// </summary>
    public abstract class InteractionCondition : ScriptableObject
    {
        public abstract bool Evaluate(in InteractionContext ctx);
    }
}
