using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Player Settings")]
    public GameObject playerPrefab; // Le prefab de votre joueur
    public PlayerSpawnPoint spawnPoint; // Le point où le joueur sera spawn

    void Start()
    {
        SpawnPlayer();
    }

    public void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("PlayerSpawner: Player Prefab n'est pas assigné!");
            return;
        }
        
        if (spawnPoint == null)
        {
            Debug.LogError("PlayerSpawner: Spawn Point n'est pas assigné! Assurez-vous d'avoir un GameObject avec le script PlayerSpawnPoint dans la scène et de l'assigner ici.");
            return;
        }
        
        // Spawner le joueur à la position et rotation du point de spawn
        Instantiate(playerPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
        Debug.Log("Joueur spawné à la position: " + spawnPoint.transform.position);
    }
}

