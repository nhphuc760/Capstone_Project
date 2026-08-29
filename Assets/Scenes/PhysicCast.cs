using UnityEngine;

public class PhysicCast : MonoBehaviour
{
    [SerializeField] GameObject cube;
    Vector3 previewPos;
    GameObject _curCube;

    [SerializeField] LayerMask groundMask;

    private void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfor, 100f, groundMask))
        {
            int x = Mathf.FloorToInt( hitInfor.point.x);
            int y = Mathf.FloorToInt( hitInfor.point.y);
            int z = Mathf.FloorToInt( hitInfor.point.z);

            previewPos = new Vector3(x, y, z) + Vector3.one * 0.5f;

        }
        ;
        if (Input.GetMouseButtonDown(0))
        {
            _curCube = Instantiate(cube, previewPos, Quaternion.identity);
        }
        if (_curCube != null)
        {
            _curCube.transform.position = previewPos;
        }
    }

}
