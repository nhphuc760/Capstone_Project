using UnityEngine;

public class Singleton<T> where T : MonoBehaviour
{
   public static T Instance { get; private set; }

}
