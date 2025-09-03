using UnityEngine;
using Photon.Pun;

// We can go back to using the simple MonoBehaviour. No callbacks needed for this approach.
public class PS : MonoBehaviour
{
    [Header("Player Prefab to Spawn")]
    public GameObject defaultPlayerPrefab; // Fallback for testing

    [Header("Optional Spawn Points")]
    public Transform[] spawnPoints;

    // We use the Start() function, which runs automatically when the scene loads.
    void Start()
    {
        // --- THIS IS THE CRUCIAL FIX ---
        // We check if we are actually connected and in a room.
        // If we are not in a room, we do nothing. This prevents all errors.
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("Tried to run Player Spawner but we are not in a room. Aborting.");
            return;
        }
        // -----------------------------

        Debug.Log("PS.cs is starting. We are in a room. Proceeding to spawn.");

        // The rest of the logic is the same as before.
        string prefabToSpawnName;
        var playerProperties = PhotonNetwork.LocalPlayer.CustomProperties;

        if (playerProperties.ContainsKey("character"))
        {
            prefabToSpawnName = (string)playerProperties["character"];
            Debug.Log("Found character choice: " + prefabToSpawnName);
        }
        else
        {
            if (defaultPlayerPrefab != null)
            {
                prefabToSpawnName = defaultPlayerPrefab.name;
                Debug.LogWarning("No character choice found. Spawning default: " + prefabToSpawnName);
            }
            else
            {
                Debug.LogError("No character choice found and no default prefab is set. Cannot spawn.");
                return;
            }
        }

        Vector3 spawnPosition;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int playerNumber = PhotonNetwork.LocalPlayer.ActorNumber - 1;
            int spawnIndex = playerNumber % spawnPoints.Length;
            spawnPosition = spawnPoints[spawnIndex].position;
        }
        else
        {
            spawnPosition = new Vector3(0, 1, 0);
        }

        PhotonNetwork.Instantiate(prefabToSpawnName, spawnPosition, Quaternion.identity);
    }
}
