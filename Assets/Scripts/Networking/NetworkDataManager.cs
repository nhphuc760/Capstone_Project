using System.Collections.Generic;
using UnityEngine;

public class NetworkDataManager : MonoBehaviour
{
    public bool dontDestroy = true;
    public static NetworkDataManager Instance;

    [Header("Data")]
    public List<User> listFriends= new List<User>();
    public List<Invite> invites = new List<Invite>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }        
        Instance = this;
        if (dontDestroy)
        {
            DontDestroyOnLoad(gameObject);
        }
    }
    private void Start()
    {
        
    }
}
