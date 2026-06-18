using System;
using UnityEngine;
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
   
    private void Awake()
    {

    }       
}
