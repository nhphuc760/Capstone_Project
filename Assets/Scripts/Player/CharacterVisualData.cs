using UnityEngine;

[CreateAssetMenu(fileName = "CharacterVisualData", menuName = "Scriptable Objects/CharacterVisualData")]
public class CharacterVisualData : ScriptableObject
{
    public string characterName;
    public Sprite avatarIcon;
    public Mesh bodyMesh;
    public Material[] materials;
}
