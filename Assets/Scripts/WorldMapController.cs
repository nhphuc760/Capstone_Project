using UnityEngine;

public class WorldMapController : MonoBehaviour
{
    [SerializeField] private GameObject worldMapPanel;
    private bool isWorldMapOpen = false;
    
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
