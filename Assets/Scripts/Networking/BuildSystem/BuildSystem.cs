using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using Util.Core;
using System.Linq;
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
    [SerializeField] GameObject gridLine;


    [OnChangedRender(nameof(OnWallAmountChange))]
    [Networked] public int wallAmount { get; set; }

    Dictionary<StructureDataSO, IRequirement[]> structureBuildRequirement = new();






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

    void InitBuildRequirement()
    {
        foreach (var i in _structDatabase.Alls)
        {
            if (i.structureType == StructureType.Wall || i.structureType == StructureType.Door) continue;
            RequirementServices services = GetComponent<RequirementServices>();
            IRequirement[] requirements = i.buildRequirement
                .Select((x) =>
                {
                    IRequirement requirement = x.CreateRuntime(services, Object);
                    requirement.Start();
                    return requirement;
                })
                .ToArray();
            structureBuildRequirement[i] = requirements;
        }
    }


    public override void Spawned()
    {
        if (_playerCam == null) _playerCam = GetComponentInChildren<Camera>();
        if (!Object.HasInputAuthority) _playerCam.gameObject.SetActive(false);
        if (Object.HasInputAuthority)
        {
            Utils.IntervalLoop(IntervalPhysicsCast, 100, token: Object.GetCancellationTokenOnDestroy()).Forget();
            var radialmenu = GetComponentInChildren<RadialMenu>(includeInactive: true);
            if (radialmenu != null)
            {
                radialmenu.onSelectIndex += OnRadialMenuSelected;
            }
            GetComponentInChildren<RadialMenuItem>(includeInactive: true)?.UpdateAmount(wallAmount);
        }
        if (HasStateAuthority)
        {
            wallAmount = 30;
        }
        InitBuildRequirement();
    }
    private void OnWallAmountChange()
    {
        GetComponentInChildren<RadialMenuItem>(includeInactive: true)?.UpdateAmount(wallAmount);
    }

    private void OnRadialMenuSelected(int obj)
    {
        if (obj == 2)
        {
            var wallID = GetComponentInChildren<RadialMenuItem>(includeInactive: true).wallSO._id;
            PickStruct(wallID.Value);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        var radialmenu = GetComponentInChildren<RadialMenu>(includeInactive: true);
        if (radialmenu != null)
        {
            radialmenu.onSelectIndex -= OnRadialMenuSelected;
        }
    }

    private void Update()
    {
        if (!Object.HasInputAuthority) return;
        if (Input.GetMouseButtonDown(0) && _curStructureSO != null)
        {
            // Request build
            if (buildValidationResult.Success)
            {

                RPC_BuildRequest(_curStructureSO._id, _cellPos, previewRot);
            }
            else
            {
                Debug.Log(buildValidationResult.Message);
                buildFailReason?.Invoke(buildValidationResult);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            //Hủy hành động build
            _curStructureSO = null;
            DisableLine();
        }
    }

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
            previewPos = _cellPos + new Vector3(.5f, 0f, .5f);
            //UpdateLine(previewPos);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            previewRot *= Quaternion.Euler(0, 90, 0);
        }
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_BuildRequest(NetworkString<_8> _idStruct, Vector3Int buildPos, Quaternion buildRot)
    {
        if (!CheckBuildRequirement(_structDatabase.GetStructSO(_idStruct.Value)).Success)
        {
            return;
        }
        IBuildStategy buildStrategy = StructureManager.Ins.GetStrategyBuild(_idStruct, Object.InputAuthority);
        //Debug.Log("BuildStategy exist " + buildStrategy != null);
        if (buildStrategy != null)
        {

            BuildValidationResult buildValidation = buildStrategy.CanBuild(Runner, Object.InputAuthority, buildPos);
            if (buildValidation.Success)
            {
                Debug.Log("Can build" + _structDatabase.GetStructSO(_idStruct.Value)._name + $" for {Object.InputAuthority}", this);
                buildStrategy.Build(Runner, Object.InputAuthority, buildPos);
                if (structureBuildRequirement.TryGetValue(_structDatabase.GetStructSO(_idStruct.Value), out var requirements))
                {
                    foreach (var item in structureBuildRequirement[_structDatabase.GetStructSO(_idStruct.Value)])
                    {
                        item.Effect();
                    }
                }                
            }
            else
            {
                Debug.Log(buildValidation.Message, this);
            }


        }
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_DestroyRequest()
    {

    }

    public CheckMetResult CheckBuildRequirement(StructureDataSO structureDataSO)
    {
        if(structureBuildRequirement.TryGetValue(structureDataSO, out var requirements))
        {
            foreach (var i in structureBuildRequirement[structureDataSO])
            {
                var result = i.Met();
                if (!result.Success)
                    return result;
            }
        }
        return default;       
    }

    public void PickStruct(string _idStruct)
    {
        Debug.Log("PickStructure");
        StructureDataSO _structSO = _structDatabase.GetStructSO(_idStruct);
        if (_structSO == null) return;
        meshFilters = _structSO.prefabs.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0)
        {
            Debug.LogWarning("Prefabs không có mesh");
            return;
        }
        _curStructureSO = _structSO;
        EnableLine();
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
            Graphics.DrawMesh(i.sharedMesh, matrix, buildValidationResult.Success ? _transparentGreenMaterial : _transparentRedMaterial, 0, camera: _playerCam, 0, null, UnityEngine.Rendering.ShadowCastingMode.Off, false);
        }
        UpdateLine(previewPos);
    }

    public override void Render()
    {
        if (_curStructureSO == null) return;
        RenderPreview();
    }

    Collider[] results = new Collider[2];

    BuildValidationResult CheckValidCell()
    {

        Vector3 center = _cellPos + Vector3.one * 0.5f;
        Vector3 halfExtents = new Vector3(0.5f, 0.5f, 0.5f) + new Vector3(-0.01f, 0, -0.01f);
        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, results, Quaternion.identity, _layerObstacleBuild);
        DrawLog.DrawCube(center, halfExtents, Color.blue, Time.deltaTime);
        if (count == 0)
        {
            return StructureManager.Ins.GetStrategyBuild(_curStructureSO._id, Object.InputAuthority).CanBuild(Runner, Object.InputAuthority, _cellPos);
        }
        else
        {
            return BuildValidationResult.Fail(BuildFailReason.InvalidPosition, "Không thể xây, có vật cản");
        }
    }

    void EnableLine()
    {
        gridLine?.SetActive(true);
    }    

    void UpdateLine(Vector3 pos)
    {
        gridLine.transform.position = pos;
    }

    void DisableLine()
    {           
        gridLine?.SetActive(false);
    }
}

