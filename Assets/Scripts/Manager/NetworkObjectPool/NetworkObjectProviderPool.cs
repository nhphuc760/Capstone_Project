
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkObjectProviderPool : NetworkObjectProviderDefault
{
    [System.Serializable]
    public class PoolData
    {
        public string key;
        public NetworkObject prefab;

        [Min(0)]
        public int maxSize = 100;
    }

    [Header("Pooling")]
    [SerializeField] private List<PoolData> poolsData = new();

    // Prefab -> Pool configuration
    private readonly Dictionary<NetworkObject, PoolData> _dataByPrefab = new();

    // Key -> Pool configuration
    private readonly Dictionary<string, PoolData> _dataByKey = new();

    // Prefab -> reusable inactive instances
    private readonly Dictionary<NetworkObject, Stack<NetworkObject>> _available = new();

    // Instance -> Pool configuration
    private readonly Dictionary<NetworkObject, PoolData> _dataByInstance = new();

    protected virtual void Awake()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        foreach (PoolData data in poolsData)
        {
            if (data == null || data.prefab == null)
            {
                Debug.LogWarning(
                    $"[{nameof(NetworkObjectProviderPool)}] Pool entry has no prefab.", this);
                continue;
            }

            if (_dataByPrefab.ContainsKey(data.prefab))
            {
                Debug.LogWarning(
                    $"[{nameof(NetworkObjectProviderPool)}] Duplicate prefab: {data.prefab.name}", this);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(data.key))
            {
                if (_dataByKey.ContainsKey(data.key))
                {
                    Debug.LogWarning(
                        $"[{nameof(NetworkObjectProviderPool)}] Duplicate key: {data.key}", this);
                }
                else
                {
                    _dataByKey.Add(data.key, data);
                }
            }

            _dataByPrefab.Add(data.prefab, data);
            _available.Add(data.prefab, new Stack<NetworkObject>());
        }
    }

    public bool TryGetPrefab(string key, out NetworkObject prefab)
    {
        prefab = null;

        if (string.IsNullOrWhiteSpace(key) ||
            !_dataByKey.TryGetValue(key, out PoolData data))
        {
            return false;
        }

        prefab = data.prefab;
        return prefab != null;
    }

    /// <summary>
    /// Called by NetworkObjectProviderDefault.AcquirePrefabInstance().
    /// Reuses a cached instance or creates one through the default provider.
    /// </summary>
    protected override NetworkObject InstantiatePrefab(
        NetworkRunner runner,
        NetworkObject prefab)
    {
        // Prefab without a pool configuration uses default Fusion behavior.
        if (!_dataByPrefab.TryGetValue(prefab, out PoolData data))
            return base.InstantiatePrefab(runner, prefab);

        Stack<NetworkObject> stack = _available[prefab];
        NetworkObject instance = null;

        // Ignore destroyed instances that may remain in the stack.
        while (stack.Count > 0 && instance == null)
            instance = stack.Pop();

        if (instance == null)
        {
            instance = base.InstantiatePrefab(runner, prefab);
        }
        else
        {
            instance.gameObject.SetActive(true);
        }

        if (instance != null)
            _dataByInstance[instance] = data;

        return instance;
    }

    /// <summary>
    /// Called by NetworkObjectProviderDefault.ReleaseInstance().
    /// Caches the instance instead of destroying it when possible.
    /// </summary>
    protected override void DestroyPrefabInstance(
        NetworkRunner runner,
        NetworkPrefabId prefabId,
        NetworkObject instance)
    {
        if (instance == null)
            return;

        if (!_dataByInstance.TryGetValue(instance, out PoolData data) ||
            data == null ||
            data.prefab == null ||
            !_available.TryGetValue(data.prefab, out Stack<NetworkObject> stack))
        {
            base.DestroyPrefabInstance(runner, prefabId, instance);
            return;
        }

        // maxSize = 0 means released instances are not retained.
        if (data.maxSize <= 0 || stack.Count >= data.maxSize)
        {
            _dataByInstance.Remove(instance);
            base.DestroyPrefabInstance(runner, prefabId, instance);
            return;
        }

        // Reset custom local-only state here if needed.
        // Do not call Runner.Despawn() here: Fusion is already releasing it.
        instance.gameObject.SetActive(false);
        stack.Push(instance);
    }

    public override void ReleaseInstance(
        NetworkRunner runner,
        in NetworkObjectReleaseContext context)
    {
        // Fusion can bypass DestroyPrefabInstance when the object is
        // being destroyed permanently. Remove our bookkeeping in that case.
        if (context.IsBeingDestroyed && context.Object != null)
            _dataByInstance.Remove(context.Object);

        base.ReleaseInstance(runner, context);
    }

    protected virtual void OnDestroy()
    {
        // Clean up cached instances when this provider is destroyed.
        foreach (Stack<NetworkObject> stack in _available.Values)
        {
            while (stack.Count > 0)
            {
                NetworkObject instance = stack.Pop();

                if (instance != null)
                    Destroy(instance.gameObject);
            }
        }

        _available.Clear();
        _dataByInstance.Clear();
    }
}
