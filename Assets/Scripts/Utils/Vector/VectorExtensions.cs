using Unity.VisualScripting;
using UnityEngine;

namespace Util
{
    public static class VectorExtensions 
    {
        public static Vector3Int ToInt(this Vector3 source)
        {
            return new Vector3Int((int)source.x,
            (int)source.y,
            (int)source.z
            );                       
        }

        public static Vector2Int ToInt(this Vector2 source)
        {
            return new Vector2Int((int)source.x,
            (int)source.y
            );
        }

    }

}
