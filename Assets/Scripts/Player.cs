using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Fusion;

[Flags]
public enum CharacterState 
{
    Normal,
    Stunned, // Choáng, không sử dụng được kĩ năng, không thể di chuyển
    Silenced, // Câm lặng, có thể di chuyển nhưng không thể dùng kĩ năng
    Rooted, // không thể di chuyển nhưng vẫn có thể dùng kĩ năng
    Slowed // bị làm chậm, giảm tốc độ
}

public class Player : MonoBehaviour
{
    [HideInInspector]
    public CharacterState unitState;
    [SerializeField] CharacterStats dataBase;
    Dictionary<StatType, float> statCharacter = new();
    public StatModifier statModifier;
    //IHealthSystem playerHealth;
    private void Awake()
    {

    }



    //public void ApplyResultInteraction(InteractionData interactionData)
    //{
    //    switch (interactionData.EffectType) 
    //    {
    //        case EffectInteract.ModifyHealth:
    //            break;
    //        case EffectInteract.ModifyCurrency:
    //            break;
    //        case EffectInteract.AddInventoryItem:
    //            break;
    //        case EffectInteract.AddBuff:
    //            break;
    //    }

    //}

    

}
