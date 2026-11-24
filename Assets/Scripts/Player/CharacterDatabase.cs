using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Scriptable Objects/CharacterDatabase")]
public class CharacterDatabase : ScriptableObject
{
    public List<CharacterVisualData> characters = new();

    public CharacterVisualData GetCharacter(int index)
    {
        if (characters == null || characters.Count == 0) return null;
        int safeIndex = Mathf.Clamp(index, 0, characters.Count - 1);
        return characters[safeIndex];
    }
}
