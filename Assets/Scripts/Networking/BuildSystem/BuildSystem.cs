using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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

    Vector3Int _cellPos;
    BuildValidationResult buildValidationResult = default;

    public event Action<BuildValidationResult> buildFailReason;
    public Vector3Int CellPos
    {
        get => _cellPos; private set
        {
            if (value != _cellPos)
            {
                _cellPos = value;
                buildValidationResult = CheckValidCell();
            }
        }
    }




    public override void Spawned()
    {
        if (_playerCam == null) _playerCam = GetComponentInChildren<Camera>();
        if (!Object.HasInputAuthority) _playerCam.gameObject.SetActive(false);
        if (Object.HasInputAuthority)
            Utils.IntervalLoop(IntervalPhysicsCast, 100, token: Object.GetCancellationTokenOnDestroy()).Forget();
    }

    private void Update()
    {
        if (!Object.HasInputAuthority) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PickStruct("1093");
            
        }



        if (Input.GetMouseButtonDown(0))
        {
            // Request build
            if (buildValidationResult.Success)
            {
                RPC_BuildRequest(_curStructureSO._id, _cellPos, previewRot);
            }
            else
            {
                buildFailReason?.Invoke(buildValidationResult);
                Utils.EditorLogOnly(buildValidationResult.Message);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            //Hủy hành động build
            _curStructureSO = null;
        }
    }


    //BuildValidationResult CheckCostBuild(StructureBase structure)
    //{

    //}

    void IntervalPhysicsCast()
    {
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
            CellPos = new Vector3Int(x, y, z);
            previewPos = _cellPos + Vector3.one * 0.5f;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            previewRot *= Quaternion.Euler(0, 90, 0);
        }
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_BuildRequest(NetworkString<_8> _idStruct, Vector3Int buildPos, Quaternion buildRot)
    {

        Utils.EditorLogOnly("This fuction called on Host");
        Utils.EditorLogOnly($"Has StructManager: {StructureManager.Ins != null} \n structureCount: {(StructureManager.Ins != null ? StructureManager.Ins.GetStructures()?.Count ?? 0 : 0)}");
        IBuildStategy buildStrategy = StructureManager.Ins.GetStrategyBuild(_idStruct);
        //Debug.Log("BuildStategy exist " + buildStrategy != null);
        if (buildStrategy != null)
        {

            BuildValidationResult buildValidation = buildStrategy.CanBuild(Runner, Object.InputAuthority, buildPos);
            Debug.Log("Check CanBuild");
            if (buildValidation.Success)
            {
                Utils.EditorLogOnly("Can build" + _structDatabase.GetStructSO(_idStruct.Value)._name + $" for {Object.InputAuthority}");
                buildStrategy.Build(Runner, Object.InputAuthority, buildPos);
            }
            else
            {
                Utils.EditorLogOnly(buildValidation.Message);
            }


        }



    }

    public void PickStruct(string _idStruct)
    {
        StructureDataSO _structSO = _structDatabase.GetStructSO(_idStruct);
        if (_structSO == null) return;
        meshFilters = _structSO.prefabs.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0)
        {
            Debug.LogWarning("Prefabs không có mesh");
            return;
        }
        _curStructureSO = _structSO;
    }

    void RenderPreview()
    {
        if (meshFilters == null || meshFilters.Length == 0) return;
        foreach (var i in meshFilters)
        {
            if (i.sharedMesh == null) continue;
            Matrix4x4 matrix = Matrix4x4.TRS(
                previewPos,
                previewRot,
                i.transform.localScale
                );
            Graphics.DrawMesh(i.sharedMesh, matrix, buildValidationResult.Success ? _transparentGreenMaterial : _transparentRedMaterial, 0);
        }
    }

    private void OnRenderObject()
    {
        if (_curStructureSO == null) return;
        RenderPreview();
    }

    BuildValidationResult CheckValidCell()
    {
        return StructureManager.Ins.GetStrategyBuild(_curStructureSO._id).CanBuild(Runner, Object.InputAuthority, _cellPos);
    }
}
