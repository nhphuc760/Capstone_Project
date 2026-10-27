using UnityEngine;

public class IconOnMap : MonoBehaviour
{

    [SerializeField] bool updateRealTime = false;


    private void Start()
    {
        transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    private void LateUpdate()
    {
        if(updateRealTime)
            transform.rotation = Quaternion.Euler(90, 0, 0);
    }
}
