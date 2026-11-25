using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Ins;
    public bool DonDestroy = true;

    [System.Serializable]
    public class PoolData
    {
        public string key;
        public GameObject prefab;
        public int defaultCapacity = 10;
        public int maxSize = 100;
    }

    [SerializeField] private List<PoolData> poolsData;

    private readonly Dictionary<string, ObjectPool<GameObject>> pools = new Dictionary<string, ObjectPool<GameObject>>();
    private readonly Dictionary<string, GameObject> prefabsByKey = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Ins != null && Ins != this)
        {
            Destroy(gameObject);
            return;
        }
        Ins = this;
        if (DonDestroy)
        {
            DontDestroyOnLoad(gameObject);
        }
        CreatePools();
    }

    private void CreatePools()
    {
        foreach (var data in poolsData)
        {
            prefabsByKey[data.key] = data.prefab;
            GameObject prefabRef = data.prefab;

            ObjectPool<GameObject> pool = new ObjectPool<GameObject>
            (
                createFunc: () =>
                {
                    GameObject obj = Instantiate(prefabRef);
                    obj.SetActive(false);
                    return obj;
                },
                actionOnGet: (obj) =>
                {
                    if (obj != null)
                        obj.SetActive(true);
                },
                actionOnRelease: (obj) =>
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                        obj.transform.SetParent(transform, false);
                    }
                },
                actionOnDestroy: (obj) =>
                {
                    if (obj != null)
                        Destroy(obj);
                },
                collectionCheck: false,
                defaultCapacity: data.defaultCapacity,
                maxSize: data.maxSize
            );

            pools.Add(data.key, pool);
        }
    }

    public GameObject Get(string key, Transform parent, bool worldPositionStays = true)
    {
        if (!pools.ContainsKey(key))
        {
            Debug.LogError($"Pool {key} does not exist!");
            return null;
        }

        GameObject obj = pools[key].Get();

        // Nếu object trong pool đã bị hủy do đổi scene (Canvas cũ bị destroy), tạo bản mới từ prefab gốc
        if (obj == null && prefabsByKey.TryGetValue(key, out GameObject prefab) && prefab != null)
        {
            obj = Instantiate(prefab);
            obj.SetActive(true);
        }

        if (obj != null)
            obj.transform.SetParent(parent, worldPositionStays);

        return obj;
    }

    public GameObject Get(string key, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(key))
        {
            Debug.LogError($"Pool {key} does not exist!");
            return null;
        }

        GameObject obj = pools[key].Get();

        if (obj == null && prefabsByKey.TryGetValue(key, out GameObject prefab) && prefab != null)
        {
            obj = Instantiate(prefab);
            obj.SetActive(true);
        }

        if (obj != null)
        {
            obj.transform.position = position;
            obj.transform.rotation = rotation;
        }

        return obj;
    }

    public void Release(string key, GameObject obj)
    {
        if (obj == null) return;

        if (!pools.ContainsKey(key))
        {
            Debug.LogError($"Pool {key} does not exist!");
            return;
        }

        pools[key].Release(obj);
    }
}