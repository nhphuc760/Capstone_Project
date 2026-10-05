using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using Util.Core;

public class StructureManager : NetworkBehaviour
{
    public static StructureManager Ins { get; private set; }
    GetContext _getContext = default;

    [Networked, Capacity(512)]
    private NetworkDictionary<int, NetworkObject> structures => default;

    [Networked, Capacity(288)]
    NetworkDictionary<int, PlayerRef> enclosedZone => default;

    [Networked, Capacity(128)]
    NetworkDictionary<NetworkObject, PlayerRef> escapeWalls => default;


    List<IBuildStategy> buildStategies = new List<IBuildStategy>();

    [SerializeField] StructDatabase _structDatabase;
    [SerializeField] private LayerMask _groundMask;

    [Header("WallBuildStategy")]
    [SerializeField] private LayerMask _layerObstacleBuild;



    public List<Vector3Int> GetEnclosedZone(PlayerRef playerRef)
    {
       return enclosedZone.Where(kvp => kvp.Value == playerRef).Select(kvp => kvp.Key.FromKey()).ToList();
    }


    public void ClearEnclosedZone(PlayerRef player)
    {
        if (!HasStateAuthority) return;
        var keys = enclosedZone.Where(x => x.Value == player).Select(kvp => kvp.Key).ToList();
        foreach (var k in keys)
        {
            enclosedZone.Remove(k);
        }
    }
    public void SetEnclosedZone(PlayerRef playerRef, List<Vector3Int> zones)
    {
        if (!Object.HasStateAuthority) return;        

        foreach (var kvp in enclosedZone)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            if (value != playerRef) continue;
            enclosedZone.Remove(key);
        }
        foreach (var cell in zones)
        {
            var key = cell.ToKey();
            enclosedZone.Add(key, playerRef);
        }
    }

    public IBuildStategy GetStrategyBuild(NetworkString<_8> _structDataID)
    {        
        StructureDataSO structureDataSO = _structDatabase.GetStructSO(_structDataID.Value);
        StructureType type = structureDataSO.structureType;
        if (type == StructureType.Wall || type == StructureType.Door)
        {

            IBuildStategy @var = buildStategies.Find(s => s is WallBuildStrategy);
            if (@var != null)
            {
                return @var;
            }            
            IBuildStategy wallBuildStrategy = new WallBuildStrategy(
                this,
                100, 100,
                _layerObstacleBuild,
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

    public void ClearEscapeWall(PlayerRef playerRef)
    {
        if (this.escapeWalls.ContainsValue(playerRef))
        {
            var oldEscapeWalls = this.escapeWalls.Where(kvp => kvp.Value == playerRef).Select(kvp => kvp.Key).ToList();
            foreach (var i in oldEscapeWalls)
            {
                this.escapeWalls.Remove(i);
            }
        }
    }

    public void SetEscapesWall(PlayerRef player, NetworkObject[] escapeWalls)
    {
        if (!Object.HasStateAuthority)
            return;
        if (escapeWalls == null || escapeWalls.Length == 0)
            return;
        ClearEscapeWall(player);

        foreach (var i in escapeWalls)
        {
            this.escapeWalls.Add(i, player);
        }
    }

    public void RemoveEscapeWall(PlayerRef player, NetworkObject wall)
    {
        if (!HasStateAuthority) return;
        escapeWalls.Remove(wall);
    }

    public void SetEscapesWall(PlayerRef player, Vector3Int[] escapeCells)
    {
        if (!HasStateAuthority) return;
        if (escapeCells == null || escapeCells.Length == 0) return;
        var listEscapeWalls = escapeCells.Select(x => GetStructure(x).Object);
        ClearEscapeWall(player);
        foreach (var i in listEscapeWalls)
        {
            this.escapeWalls.Add(i, player);
        }
    }



    public List<NetworkObject> GetEscapesWall(PlayerRef player)
    {
        return escapeWalls.Where(kvp => kvp.Value == player).Select(kvp => kvp.Key).ToList();
    }

    public List<NetworkObject> GetEscapesWall()
    {
        return escapeWalls.Select(kvp => kvp.Key).ToList();
    }


    public void SetStructure(Vector3Int cell, StructureBase structureObject)
    {
        if (!Object.HasStateAuthority)
            return;

        if (structureObject == null || structureObject.Object == null)
            return;

        int hash = cell.ToKey();

        structures.Set(hash, structureObject.Object);
        var bounds = structureObject.GetComponent<Collider>().bounds;
        Bounds bound = new Bounds((Vector3)cell + Vector3.one * 0.5f, bounds.size);

        EventBus<BuildStategyEvent>.Raise(new BuildStategyEvent
        {
            BuildType = BuildStategyEvent.BuildEventType.Build,
            PlayerRef = structureObject.Object.InputAuthority,
            StructureCategory = structureObject.StructureDataSO.structureCategory,
            StructureType = structureObject.StructureDataSO.structureType,
            cellPosition = cell,
            bounds = bound
        });
    }

    public bool AddStructure(Vector3Int cell, StructureBase structureObject)
    {       
        if (structureObject == null || structureObject.Object == null)
            return false;

        int hash = cell.ToKey();

        return structures.Add(hash, structureObject.Object);
    }

    public bool RemoveStructure(Vector3Int cell)
    {        
        int hash = cell.ToKey();

        return structures.Remove(hash);
    }

    public bool DestroyStructure(NetworkObject structure)
    {
        if (structures.ContainsValue(structure))
        {
            var kvp = structures.Where(kvp => kvp.Value == structure).First();
            if (structures.Remove(kvp.Key))
            {
                StructureBase structBase = structure.GetComponent<StructureBase>();
                BuildStategyEvent buildStategyEvent = new BuildStategyEvent
                {
                    BuildType = BuildStategyEvent.BuildEventType.Destroy,
                    PlayerRef = structure.InputAuthority,
                    StructureCategory = structBase.StructureDataSO.structureCategory,
                    StructureType = structBase.StructureDataSO.structureType,
                    cellPosition = kvp.Key.FromKey(),
                    bounds = structBase.GetComponent<Collider>().bounds                    
                };
                Runner.Despawn(structure);
                EventBus<BuildStategyEvent>.Raise(buildStategyEvent);
                return true;
            }
        }
        return false;
    }

    public StructureBase GetStructure(Vector3Int cell)
    {       
        int hash = cell.ToKey();
        if (structures.TryGet(hash, out var networkObject))
        {
            if (networkObject == null)
                return null;
            return networkObject.GetComponent<StructureBase>();
        }
        return null;
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
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Ins = null;
    }
    public StructureManager WithCategory(StructureCategory structureCategory)
    {
        _getContext.structureCategory = structureCategory;
        return this;
    }

    public StructureManager WithType(
        StructureType structureType)
    {
       _getContext.structureType = structureType;
        return this;
    }



    public StructureManager WithPlayerRef(PlayerRef player)
    {
       _getContext.playerRef = player;
        return this;
    }


    public Dictionary<Vector3Int, StructureBase> Get()
    {
        var structures = GetStructures();
        if (_getContext.IsEmpty)
            return structures;
        if(_getContext.playerRef != default)
        {
            var tmp = structures.Where(kvp => kvp.Value.Object.InputAuthority == _getContext.playerRef)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            structures = tmp;
        }
        if (_getContext.structureCategory != StructureCategory.None)
        {
           var tmp = structures.Where(kvp => kvp.Value.StructureDataSO.structureCategory == _getContext.structureCategory)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            structures = tmp;
        }

        if(_getContext.structureType != StructureType.None)
        {
            var tmp = structures.Where(kvp => kvp.Value.StructureDataSO.structureType == _getContext.structureType)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            structures = tmp;
        }
        _getContext = default;
        return structures;
    }


    public struct GetContext
    {
        public PlayerRef playerRef;
        public StructureCategory structureCategory;
        public StructureType structureType;      

        public bool IsEmpty => this == default;
        public static bool operator == (GetContext a, GetContext b)
        {
            return a.playerRef == b.playerRef &&
                   a.structureCategory == b.structureCategory &&
                   a.structureType == b.structureType;
        }

        public static bool operator != (GetContext a, GetContext b)
        {
            return !(a == b);
        }        

        public override bool Equals(object obj)
        {
            return obj is GetContext getCtx && this == getCtx;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(playerRef, structureCategory, structureType);
        }      
    }
  
}
