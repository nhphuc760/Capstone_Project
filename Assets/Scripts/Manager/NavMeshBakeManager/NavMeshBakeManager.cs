
using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Single-surface runtime NavMesh baker.
/// Structure build/destroy marks the surface dirty; one shared NavMeshSurface is updated.
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

    [Header("Surface Volume")]
    [SerializeField]
    int mapSize = 150;

    [SerializeField]
    float surfaceHeight = 10f;

    [SerializeField]
    NavMeshSurface _surface;

    EventBinding<BuildStategyEvent> _buildBinding;

    bool _bakeRequested;
    float _lastBakeTime = -999f;


    [SerializeField] GameObject EnemyObjectTest;
    /// <summary>
    /// Raised when a bake batch has finished.
    /// </summary>
    public static event Action OnNavMeshBaked;

    // ================================================================
    // Unity
    // ================================================================

    void Awake()
    {
        if (Ins != null && Ins != this)
        {
            Debug.LogWarning("[NavMeshBakeManager] Duplicate instance - destroying this.");
            Destroy(gameObject);
            return;
        }

        Ins = this;
    }

    void OnEnable()
    {
        _buildBinding = new EventBinding<BuildStategyEvent>(OnStructureChanged);
        EventBus<BuildStategyEvent>.Register(_buildBinding);
    }

    void OnDisable()
    {
        EventBus<BuildStategyEvent>.Deregister(_buildBinding);

        if (Ins == this)
            Ins = null;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            EnemyObjectTest.SetActive(true);
        }
    }

    void LateUpdate()
    {
        if (!_bakeRequested)
            return;

        if (minBakeInterval > 0f &&
            Time.time - _lastBakeTime < minBakeInterval)
        {
            return;
        }

        _bakeRequested = false;
        StartCoroutine(BakeSurface());
    }

    // ================================================================
    // Structure Event
    // ================================================================

    void OnStructureChanged(BuildStategyEvent evt)
    {
        if (ShouldBakeFor(evt.StructureType))
            RequestBake();
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

    public void RequestBake()
    {
        _bakeRequested = true;
    }

    // ================================================================
    // Bake
    // ================================================================

    IEnumerator BakeSurface()
    {
        if (_surface == null)
            yield break;

        _lastBakeTime = Time.time;

        // Temporarily hide escape walls so they are not baked as obstacles.
        IReadOnlyCollection<NetworkObject> escapeWalls =
            StructureManager.Ins != null
                ? StructureManager.Ins.GetEscapesWall()
                : null;

        var disabledObjects = new List<(GameObject go, bool wasActive)>();

        if (escapeWalls != null)
        {
            foreach (NetworkObject wall in escapeWalls)
            {
                if (wall == null)
                    continue;

                GameObject wallGo = wall.gameObject;
                if (wallGo == null)
                    continue;

                disabledObjects.Add((wallGo, wallGo.activeSelf));
                wallGo.SetActive(false);
            }
        }

        try
        {
            AsyncOperation operation = UpdateSurface();

            if (operation != null)
                yield return operation;

            OnNavMeshBaked?.Invoke();
        }
        finally
        {
            for (int i = 0; i < disabledObjects.Count; i++)
            {
                var (go, wasActive) = disabledObjects[i];
                if (go != null)
                    go.SetActive(wasActive);
            }
        }
    }

    AsyncOperation UpdateSurface()
    {
        if (_surface.navMeshData == null)
        {
            // First bake.
            _surface.BuildNavMesh();
            return null;
        }

        // Incremental update of existing NavMeshData.
        return _surface.UpdateNavMesh(_surface.navMeshData);
    }

    // ================================================================
    // Surface Creation
    // ================================================================    

    // ================================================================
    // Public API
    // ================================================================

    public void BakeAll()
    {
        if (_surface == null)
            return;

        _surface.BuildNavMesh();
        OnNavMeshBaked?.Invoke();
    }

    public NavMeshSurface Surface => _surface;
}