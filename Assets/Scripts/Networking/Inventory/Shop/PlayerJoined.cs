using Fusion;
using UnityEngine;

public class PlayerJoined : MonoBehaviour
{
    public void OnPlayerJoined(
        NetworkRunner runner,
        PlayerRef player)
    {
        Debug.Log(
            $"[PlayerJoined] Player Joined | " +
            $"Player={player}"
        );

        int playerCount = 0;

        foreach (PlayerRef activePlayer in runner.ActivePlayers)
        {
            playerCount++;

            Debug.Log(
                $"[PlayerJoined] Active Player: " +
                $"{activePlayer}"
            );
        }

        Debug.Log(
            $"[PlayerJoined] Active Players = " +
            $"{playerCount}"
        );
    }
}