using UnityEngine;
using Photon.Pun; // Required for Photon functions

public class PS : MonoBehaviour
{
    // This is the name of your player prefab.
    // It MUST be located inside a "Resources" folder in your Assets.
    public string playerPrefabName = "PlayerCharacter"; // <<< CHANGE THIS to match your prefab's exact filename.

    // These are optional spawn points. If you leave them empty, it will spawn at (0,0,0).
    public Transform[] spawnPoints;

    void Start()
    {
        Debug.Log("Player Spawner is running...");

        // Choose a spawn point.
        Vector3 spawnPosition;
        if (spawnPoints.Length > 0)
        {
            // Use the player's number to pick a spawn point (e.g., Player 1 gets spawn 0, Player 2 gets spawn 1)
            int playerNumber = PhotonNetwork.LocalPlayer.ActorNumber - 1;
            int spawnIndex = playerNumber % spawnPoints.Length; // Use modulo to prevent errors if there are more players than spawns
            spawnPosition = spawnPoints[spawnIndex].position;
        }
        else
        {
            // Default spawn position if no points are set
            spawnPosition = new Vector3(0, 1, 0);
            Debug.LogWarning("No spawn points set in PlayerSpawner. Spawning at default position.");
        }

        // This is the most important line. It tells Photon to create a networked instance of the prefab.
        PhotonNetwork.Instantiate(playerPrefabName, spawnPosition, Quaternion.identity);

        Debug.Log("Player instantiated for client.");
    }
}
