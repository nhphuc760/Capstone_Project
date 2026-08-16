using UnityEngine;

public class IgnoreCollision : MonoBehaviour
{
    [SerializeField]
    Collider thisCollider;
    [SerializeField]
    Collider[] collidersToIgnore;
    private void Start()
    {
        
        if(collidersToIgnore == null || collidersToIgnore.Length == 0) return;
        foreach (var i in collidersToIgnore)
        {
            Physics.IgnoreCollision(thisCollider, i, true);
        }
    }
}
