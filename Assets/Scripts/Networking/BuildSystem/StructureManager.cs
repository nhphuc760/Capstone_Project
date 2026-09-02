using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class StructureManager : NetworkBehaviour
{
    public static StructureManager Ins { get; private set; }


    [Networked, Capacity(512)]
    private NetworkDictionary<int, NetworkObject> structures => default;   

    List<IBuildStategy> buildStategies = new List<IBuildStategy>();

    [SerializeField] StructDatabase _structDatabase;
    [SerializeField] private LayerMask _groundMask;

    [Header("WallBuildStategy")]
    [SerializeField] private LayerMask _layerObstacleBuild;

    public IBuildStategy GetStrategyBuild(NetworkString<_8> _structDataID)
    {
        StructureDataSO structureDataSO = _structDatabase.GetStructSO(_structDataID.Value);
        StructureType type = structureDataSO.structureType;
        if (type == StructureType.Wall)
        {
            IBuildStategy @var = buildStategies.Find(s => s is WallBuildStrategy);
            if (@var != null)
            {
                Debug.Log("Found existing WallBuildStrategy");
                return @var;
            }
            else
            {
                Debug.Log("Creating new WallBuildStrategy");
            }
            IBuildStategy wallBuildStrategy = new WallBuildStrategy(
                this,
                100, 100,
                _layerObstacleBuild,
                _groundMask,
                structureDataSO,
                _structDatabase.GetStructSO("1094")

            );
            buildStategies.Add(wallBuildStrategy);
            return wallBuildStrategy;
        }
        else if (type == StructureType.Turret)
        {
            return null;
        }

        return null;
    }


    public void SetStructure(Vector3Int cell, StructureBase structureObject)
    {
        if (!Object.HasStateAuthority)
            return;

        if (structureObject == null || structureObject.Object == null)
            return;

        int hash = cell.ToKey();

        structures.Set(hash, structureObject.Object);
    }

    public bool AddStructure(Vector3Int cell, StructureBase structureObject)
    {
        if (!Object.HasStateAuthority)
            return false;

        if (structureObject == null || structureObject.Object == null)
            return false;

        int hash = cell.ToKey();

        return structures.Add(hash, structureObject.Object);
    }

    public bool RemoveStructure(Vector3Int cell)
    {
        if (!Object.HasStateAuthority)
            return false;

        int hash = cell.ToKey();

        return structures.Remove(hash);
    }

    public Dictionary<Vector3Int, StructureBase> GetStructures()
    {       
        Dictionary<Vector3Int, StructureBase> result = new(structures.Count);

        foreach (var pair in structures)
        {
            NetworkObject networkObject = pair.Value;

            if (networkObject == null)
                continue;

            StructureBase structure = networkObject.GetComponent<StructureBase>();

            if (structure == null)
                continue;

            Vector3Int cell = pair.Key.FromKey();

            result.Add(cell, structure);
        }

        return result;
    }

    public override void Spawned()
    {
        Ins = this;       
    }

    public Dictionary<Vector3Int, StructureBase> GetStructuresByCategory(
        StructureCategory structureCategory)
    {
        if (structures.Count == 0)
            return null;

        Dictionary<Vector3Int, StructureBase> result = new();

        foreach (var pair in structures)
        {
            NetworkObject networkObject = pair.Value;

            if (networkObject == null)
                continue;

            StructureBase structure = networkObject.GetComponent<StructureBase>();

            if (structure == null)
                continue;

            if (structure.StructureDataSO.structureCategory != structureCategory)
                continue;

            Vector3Int cell = pair.Key.FromKey();

            result.Add(cell, structure);
        }

        return result;
    }

    public Dictionary<Vector3Int, StructureBase> GetStructuresByType(
        StructureType structureType)
    {
        if (structures.Count == 0)
            return null;

        Dictionary<Vector3Int, StructureBase> result = new();

        foreach (var pair in structures)
        {
            NetworkObject networkObject = pair.Value;

            if (networkObject == null)
                continue;

            StructureBase structure = networkObject.GetComponent<StructureBase>();

            if (structure == null)
                continue;

            if (structure.StructureDataSO.structureType != structureType)
                continue;

            Vector3Int cell = pair.Key.FromKey();

            result.Add(cell, structure);
        }

        return result;
    }

}
