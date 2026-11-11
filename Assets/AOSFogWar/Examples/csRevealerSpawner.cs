/*
 * Created :    Spring 2023
 * Author :     SeungGeon Kim (keithrek@hanmail.net)
 * Project :    FogWar
 * Filename :   csRevealerSpawner.cs (non-static monobehaviour module)
 * 
 * All Content (C) 2022 Unlimited Fischl Works, all rights reserved.
 */



using UnityEngine;          // Monobehaviour



namespace FischlWorks_FogWar
{



    public class csRevealerSpawner : MonoBehaviour
    {
        [SerializeField]
        private csFogWar fogWar = null;

        [SerializeField]
        private GameObject exampleRevealer = null;



        [Header("Player Revealer Settings")]
        [SerializeField] private int sightRange = 10;
        [SerializeField] private bool updateOnlyOnMove = true;

        private int playerRevealerIndex = -1;

        private void Awake()
        {
            if (fogWar == null)
            {
                fogWar = FindFirstObjectByType<csFogWar>();
            }
        }
        // Called when Photon Fusion spawns the local Player
        public void RegisterPlayer(Transform playerTransform)
        {
            if (fogWar == null || playerTransform == null)
            {
                Debug.LogError("[FogWar] FogWar or Player Transform is missing!");
                return;
            }

            if (playerRevealerIndex >= 0)
            {
                UnregisterPlayer();
            }

            playerRevealerIndex = fogWar.AddFogRevealer(
                new csFogWar.FogRevealer(
                    playerTransform,
                    sightRange,
                    updateOnlyOnMove
                )
            );

            Debug.Log("[FogWar] Player Revealer Registered!");
        }

        // Called when Photon Fusion despawns the local Player
        public void UnregisterPlayer()
        {
            if (fogWar == null || playerRevealerIndex < 0)
                return;

            if (playerRevealerIndex < fogWar._FogRevealers.Count)
            {
                fogWar.RemoveFogRevealer(playerRevealerIndex);
            }

            playerRevealerIndex = -1;

            Debug.Log("[FogWar] Player Revealer Removed!");
        }

    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.R))
    //    {
    //        Vector3 randomPoint = new Vector3(
    //            Random.Range(-fogWar.levelData.levelDimensionX / 2.5f, fogWar.levelData.levelDimensionX / 2.5f),
    //            fogWar._LevelMidPoint.position.y + 0.5f,
    //            Random.Range(-fogWar.levelData.levelDimensionY / 2.5f, fogWar.levelData.levelDimensionY / 2.5f));

    //        // Instantiating & fetching the revealer Transform
    //        Transform randomTransform = Instantiate(exampleRevealer, randomPoint, Quaternion.identity).GetComponent<Transform>();

    //        // Utilizing the constructor, setting updateOnlyOnMove to true will not update the fog texture immediately
    //        int index = fogWar.AddFogRevealer(new csFogWar.FogRevealer(randomTransform, 3, false));
    //    }

    //    if (Input.GetKeyDown(KeyCode.C))
    //    {
    //        if (fogWar._FogRevealers.Count > 2)
    //        {
    //            fogWar.RemoveFogRevealer(fogWar._FogRevealers.Count - 1);
    //        }
    //    }
    //}
    }



}