using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Central owner of runtime NavMesh baking.
///
/// Structure build/destroy events mark affected chunks as dirty.
/// Only dirty chunks are updated.
///
/// AI agents do not own or bake NavMeshSurface.
/// </summary>
public class NavMeshBakeManager : MonoBehaviour
{
    public static NavMeshBakeManager Ins { get; private set; }

    [Header("Bake Filter")]
    [Tooltip("Structure types that change walkability and require a NavMesh update.")]
    [SerializeField]
    StructureType[] bakeOnTypes =
    {
        StructureType.Wall,
        StructureType.Door
    };

    [SerializeField] LayerMask IncludeLayer;

    [Header("Bake Throttle")]
    [Tooltip("Minimum seconds between bake batches.")]
    [SerializeField]
    float minBakeInterval = 0f;

    [Header("Map")]
    [SerializeField]
    int mapSize = 150;

    [SerializeField]
    int chunkSize = 50;

    [Tooltip("Extra size added to each chunk to make neighboring chunks overlap.")]
    [SerializeField]
    int overlapChunkSize = 1;

    NavMeshChunk[,] _chunks;

    readonly HashSet<NavMeshChunk> _dirtyChunks = new();

    EventBinding<BuildStategyEvent> _buildBinding;

    bool _bakeRequested;
    float _lastBakeTime = -999f;

    /// <summary>
    /// Raised when the current dirty chunk batch has finished baking.
    /// </summary>
    public static event Action OnNavMeshBaked;


    // ================================================================
    // Unity
    // ================================================================

    void Awake()
    {
        if (Ins != null && Ins != this)
        {
            Debug.LogWarning(
                "[NavMeshBakeManager] Duplicate instance - destroying this."
            );

            Destroy(gameObject);
            return;
        }

        Ins = this;

        CreateChunks();
    }

    void OnEnable()
    {
        _buildBinding =
            new EventBinding<BuildStategyEvent>(OnStructureChanged);

        EventBus<BuildStategyEvent>.Register(_buildBinding);
    }

    void OnDisable()
    {
        EventBus<BuildStategyEvent>.Deregister(_buildBinding);

        if (Ins == this)
            Ins = null;
    }

    void LateUpdate()
    {
        if (!_bakeRequested)
            return;

        if (_dirtyChunks.Count == 0)
        {
            _bakeRequested = false;
            return;
        }

        if (minBakeInterval > 0f &&
            Time.time - _lastBakeTime < minBakeInterval)
        {
            return;
        }

        _bakeRequested = false;

        StartCoroutine(BakeDirtyChunks());
    }


    // ================================================================
    // Structure Event
    // ================================================================

    void OnStructureChanged(BuildStategyEvent evt)
    {
        if (!ShouldBakeFor(evt.StructureType))
            return;
        MarkStructureDirty(evt);
    }

    bool ShouldBakeFor(StructureType type)
    {
        if (bakeOnTypes == null || bakeOnTypes.Length == 0)
        {
            return type == StructureType.Wall ||
                   type == StructureType.Door;
        }

        for (int i = 0; i < bakeOnTypes.Length; i++)
        {
            if (bakeOnTypes[i] == type)
                return true;
        }

        return false;
    }


    // ================================================================
    // Dirty Chunk
    // ================================================================

    void MarkStructureDirty(BuildStategyEvent evt)
    {        
        

        Bounds structureBounds = evt.bounds;
        Debug.Log(structureBounds.center);
        List<NavMeshChunk> affectedChunks =
            GetChunksIntersecting(structureBounds);

        for (int i = 0; i < affectedChunks.Count; i++)
        {
            _dirtyChunks.Add(affectedChunks[i]);
        }

        if (affectedChunks.Count > 0)
            RequestBake();
    }

    public void RequestBake()
    {
        _bakeRequested = true;
    }


    // ================================================================
    // Bake
    // ================================================================

    IEnumerator BakeDirtyChunks()
    {
        if (_dirtyChunks.Count == 0)
            yield break;

        _lastBakeTime = Time.time;

        // Snapshot dirty chunks.
        var chunksToBake = new List<NavMeshChunk>(_dirtyChunks);

        _dirtyChunks.Clear();

        // ------------------------------------------------------------
        // Escape walls
        // ------------------------------------------------------------

        IReadOnlyCollection<NetworkObject> escapeWalls =
            StructureManager.Ins.GetEscapesWall();

        var disabledObjects =
            new List<(GameObject go, bool wasActive)>();

        if (escapeWalls != null)
        {
            foreach (NetworkObject wall in escapeWalls)
            {
                if (wall == null)
                    continue;

                GameObject wallGo = wall.gameObject;

                if (wallGo == null)
                    continue;

                disabledObjects.Add(
                    (wallGo, wallGo.activeSelf)
                );

                wallGo.SetActive(false);
            }
        }

        try
        {
            // --------------------------------------------------------
            // Update only affected chunks
            // --------------------------------------------------------

            for (int i = 0; i < chunksToBake.Count; i++)
            {
                NavMeshChunk chunk = chunksToBake[i];

                if (chunk == null ||
                    chunk.surface == null)
                    continue;

                AsyncOperation operation =
                    UpdateChunk(chunk);

                if (operation != null)
                    yield return operation;
            }

            OnNavMeshBaked?.Invoke();
        }
        finally
        {
            // --------------------------------------------------------
            // Restore escape walls
            // --------------------------------------------------------

            for (int i = 0; i < disabledObjects.Count; i++)
            {
                var (go, wasActive) = disabledObjects[i];

                if (go != null)
                    go.SetActive(wasActive);
            }
        }
    }

    AsyncOperation UpdateChunk(NavMeshChunk chunk)
    {
        NavMeshSurface surface = chunk.surface;

        if (surface.navMeshData == null)
        {
            // First bake for this chunk.
            surface.BuildNavMesh();

            return null;
        }

        // Incrementally update existing NavMeshData.
        return surface.UpdateNavMesh(surface.navMeshData);
    }


    // ================================================================
    // Chunk Creation
    // ================================================================

    void CreateChunks()
    {
        int count = Mathf.CeilToInt(
            (float)mapSize / chunkSize
        );

        _chunks = new NavMeshChunk[count, count];

        for (int x = 0; x < count; x++)
        {
            for (int z = 0; z < count; z++)
            {
                CreateChunk(x, z);
            }
        }
    }

    void CreateChunk(int x, int z)
    {
        GameObject chunkObject =
            new GameObject($"NavMeshChunk_{x}_{z}");

        chunkObject.transform.SetParent(
            transform,
            false
        );

        Vector3 centerWorldSpace =
            new Vector3(
                x * chunkSize + chunkSize * 0.5f,
                0f,
                z * chunkSize + chunkSize * 0.5f
            );

        chunkObject.transform.position =
            centerWorldSpace;

        NavMeshSurface surface =
            chunkObject.AddComponent<NavMeshSurface>();

        surface.collectObjects =
            CollectObjects.Volume;

        Vector3 size =
            new Vector3(
                chunkSize + overlapChunkSize,
                10f,
                chunkSize + overlapChunkSize
            );

        surface.size = size;
        surface.center = Vector3.zero;
        surface.layerMask = IncludeLayer;
        Bounds bounds =
            new Bounds(
                centerWorldSpace,
                size
            );

        NavMeshChunk chunk =
            chunkObject.AddComponent<NavMeshChunk>();

        chunk.coord =
            new Vector2Int(x, z);

        chunk.surface =
            surface;

        chunk.bounds =
            bounds;

        _chunks[x, z] =
            chunk;
    }


    // ================================================================
    // Chunk Query
    // ================================================================

    List<NavMeshChunk> GetChunksIntersecting(Bounds bounds)
    {
        var result =
            new List<NavMeshChunk>();

        int rows =
            _chunks.GetLength(0);

        int columns =
            _chunks.GetLength(1);

        for (int x = 0; x < rows; x++)
        {
            for (int z = 0; z < columns; z++)
            {
                NavMeshChunk chunk =
                    _chunks[x, z];

                if (chunk == null)
                    continue;

                if (chunk.bounds.Intersects(bounds))
                {
                    result.Add(chunk);
                }
            }
        }

        return result;
    }


    public List<NavMeshChunk> WorldToChunk(Vector3 position)
    {
        var result =
            new List<NavMeshChunk>();

        int rows =
            _chunks.GetLength(0);

        int columns =
            _chunks.GetLength(1);

        for (int x = 0; x < rows; x++)
        {
            for (int z = 0; z < columns; z++)
            {
                NavMeshChunk chunk =
                    _chunks[x, z];

                if (chunk == null)
                    continue;

                if (chunk.VolumeContain(position))
                {
                    result.Add(chunk);
                }
            }
        }

        return result;
    }
}
