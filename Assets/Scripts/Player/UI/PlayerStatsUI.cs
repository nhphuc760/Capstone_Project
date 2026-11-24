using TMPro;
using UnityEngine;

public class PlayerStatsUI : MonoBehaviour
{
    [SerializeField] Stats playerStats;
    [SerializeField] TextMeshProUGUI Lv;
    [SerializeField] TextMeshProUGUI MoveSpeed;
    [SerializeField] TextMeshProUGUI AttackSpeed;
    [SerializeField] TextMeshProUGUI Damage;


    private void Awake()
    {
        Util.Core.Utils.DelayCall(1f, OnStatsChanged).Forget();
    }
    private void Start()
    {
        playerStats.DirtyChanged += OnStatsChanged;        
    }


    void OnStatsChanged()
    {
        Lv.text = playerStats.GetBehaviour<NetworkPlayer>().Level.ToString();
        MoveSpeed.text = playerStats.Get(StatsType.MoveSpeed).ToString("F2");
        AttackSpeed.text = playerStats.Get(StatsType.AttackSpeed).ToString("F2");
        Damage.text = playerStats.Get(StatsType.Damage).ToString("F2");
    }

    private void OnDestroy()
    {
        if(playerStats)
            playerStats.DirtyChanged -= OnStatsChanged;
    }
}
