using Fusion;
using UnityEngine;

public class BuildSystem : NetworkBehaviour
{
    MeshFilter[] meshFilters;
    [SerializeField] Material _transparentRedMaterial;
    [SerializeField] Material _transparentGreenMaterial;

    Vector3 previewPos = Vector3.one;
    Quaternion previewRot = Quaternion.identity;
    [SerializeField] Camera _playerCam;
    
    public StructDatabase _structDatabase;
    StructureDataSO _curStructureSO;
    [SerializeField] LayerMask _layerObstacleBuild;
    [SerializeField] LayerMask _groundMask;
    public override void Spawned()
    {
       if(_playerCam == null) _playerCam = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (!Object.HasInputAuthority) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PickStruct("1093");
        }

        if (_playerCam == null) 
        {
            Debug.LogWarning("PlayerCam null");
            return; 
        }
        if (_curStructureSO == null) return;
        Ray ray = _playerCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfor, 100f, _groundMask))
        {
            int x = Mathf.FloorToInt(hitInfor.point.x);
            int y = Mathf.FloorToInt(hitInfor.point.y);
            int z = Mathf.FloorToInt(hitInfor.point.z);
            previewPos = new Vector3(x, y, z) + Vector3.one * 0.5f;
            Vector2Int cellPos = new Vector2Int(x, z);          
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            previewRot *= Quaternion.Euler(0, 90, 0);
        }

        if (Input.GetMouseButtonDown(0))
        {
            // Request build
            RPC_BuildRequest(_curStructureSO._id, previewPos, previewRot);
        }

        if (Input.GetMouseButtonDown(1))
        {
            //Hủy hành động build
            _curStructureSO = null;
        }

    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_BuildRequest(NetworkString<_8> _idStruct, Vector3 buildPos, Quaternion buildRot)
    {
        if (Object.HasStateAuthority)
        {
            Debug.Log("This fuction called on Host");
            StructureDataSO structSO = _structDatabase.GetStructSO(_idStruct.Value);
            if (structSO == null)
            {
                Debug.Log("Struct invalid");
                return;
            }
            Runner.Spawn(structSO.prefabs, buildPos, buildRot);
        }
    }    

    public void PickStruct(string _idStruct)
    {
        StructureDataSO _struct = _structDatabase.GetStructSO(_idStruct);
        if (_struct == null) return;
        meshFilters = _struct.prefabs.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0)
        {
            Debug.LogWarning("Prefabs không có mesh");
            return;
        }
        _curStructureSO = _struct;
    }

    void RenderPreview()
    {
        if(meshFilters == null || meshFilters.Length == 0) return;
        foreach (var i in meshFilters)
        {
            if(i.sharedMesh == null) continue;
            Matrix4x4 matrix = Matrix4x4.TRS(
                previewPos,
                previewRot,
                i.transform.localScale
                );
            Graphics.DrawMesh(i.sharedMesh, matrix, CheckValidCell() ? _transparentGreenMaterial : _transparentRedMaterial, 0);
        }
    }

    private void OnRenderObject()
    {
        if (_curStructureSO == null) return;
        RenderPreview();
    }

    Collider[] results = new Collider[2];
    public bool CheckValidCell()
    {
        Vector3 center = previewPos;
        Vector3 halfExtents = new Vector3(0.5f, 0.5f, 0.5f);
        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, results, Quaternion.identity, _layerObstacleBuild);
        DrawLog.DrawCube(center, halfExtents, Color.blue, Time.deltaTime);
        if (count == 0) return true;
        return false;
    }    
}
