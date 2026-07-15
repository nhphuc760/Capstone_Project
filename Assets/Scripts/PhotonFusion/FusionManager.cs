using UnityEngine;
using Fusion;

public class FusionManager : MonoBehaviour
{
    public static FusionManager Instance;

    [Header("Database")]
    [SerializeField] private ItemDatabase itemDatabase;

    public ItemDatabase ItemDatabase => itemDatabase;

    [Header("Fusion")]
    public NetworkRunner Runner { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetRunner(NetworkRunner runner)
    {
        Runner = runner;
    }
}