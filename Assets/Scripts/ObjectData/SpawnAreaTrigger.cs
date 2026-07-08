using UnityEngine;

public class SpawnAreaTrigger : MonoBehaviour
{
    public SpawnArea spawnArea;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }
        else
        {
            spawnArea.Activate();
        }    
    }
}