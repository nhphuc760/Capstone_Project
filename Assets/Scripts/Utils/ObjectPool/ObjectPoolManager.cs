using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Ins;

    [System.Serializable]
    public class PoolData
    {
        public string key;
        public GameObject prefab;
        public int defaultCapacity = 10;
        public int maxSize = 100;
    }

    [SerializeField] private List<PoolData> poolsData;

    private Dictionary<string, ObjectPool<GameObject>> pools
        = new Dictionary<string, ObjectPool<GameObject>>();

    private void Awake()
    {
        if (Ins == null)
        {
            Ins = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        CreatePools();
    }

    private void CreatePools()
    {
        foreach (var data in poolsData)
        {
            ObjectPool<GameObject> pool = new ObjectPool<GameObject>
            (
                createFunc: () =>
                {
                    GameObject obj = Instantiate(data.prefab);
                    obj.SetActive(false);
                    return obj;
                },

                actionOnGet: (obj) =>
                {
                    obj.SetActive(true);
                },

                actionOnRelease: (obj) =>
                {
                    obj.SetActive(false);
                },

                actionOnDestroy: (obj) =>
                {
                    Destroy(obj);
                },

                collectionCheck: true,
                defaultCapacity: data.defaultCapacity,
                maxSize: data.maxSize
            );

            pools.Add(data.key, pool);
        }
    }

    public GameObject Get(
        string key,
        Vector3 position,
        Quaternion rotation)
    {
        if (!pools.ContainsKey(key))
        {
            Debug.LogError($"Pool {key} does not exist!");
            return null;
        }

        GameObject obj = pools[key].Get();

        obj.transform.position = position;
        obj.transform.rotation = rotation;

        return obj;
    }

    public void Release(string key, GameObject obj)
    {
        if (!pools.ContainsKey(key))
        {
            Debug.LogError($"Pool {key} does not exist!");
            return;
        }

        pools[key].Release(obj);
    }
}
