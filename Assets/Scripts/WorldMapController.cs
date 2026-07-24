using UnityEngine;

public class WorldMapController : MonoBehaviour
{
    public GameObject worldMapPanel;
    public bool isWorldMapOpen = false;
    
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.M))
        {
            isWorldMapOpen = !isWorldMapOpen;
            worldMapPanel.SetActive(isWorldMapOpen);    

            Cursor.visible = isWorldMapOpen;

            Cursor.lockState = isWorldMapOpen ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}
