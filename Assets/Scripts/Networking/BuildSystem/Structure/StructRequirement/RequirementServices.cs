using Fusion;
using UnityEngine;

public class RequirementServices : MonoBehaviour
{
    public NetworkPlayer player;
    public StructureManager structureManager;
    public NetworkInventory inventory;
    private void Awake()
    {
        player ??= GetComponent<NetworkPlayer>();
        inventory ??= GetComponent<NetworkInventory>();
        structureManager ??= FindAnyObjectByType<StructureManager>();
    }




}
