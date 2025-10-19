using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

// Classe de configuration pour chaque vague.
// [System.Serializable] permet de la voir et de la modifier dans l'inspecteur Unity.
[System.Serializable]
public class WaveConfig
{
    public string waveName; // Juste pour l'organisation
    public int scoreThreshold; // Le score nécessaire pour activer cette vague
    public List<GameObject> enemyPrefabs; // Les types d'ennemis pouvant apparaître
    public int enemyCount; // Le nombre total d'ennemis à faire apparaître
    public bool hasBoss; // Cette vague inclut-elle un boss ?
    public GameObject bossPrefab; // Le prefab du boss
}

public class WaveManager : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Fais glisser ici le GameObject qui a le script CheckpointManager.")]
    public CheckpointManager checkpointManager;

    [Header("Configuration des Vagues")]
    [Tooltip("Configure ici toutes tes vagues, dans l'ordre croissant des scores.")]
    public List<WaveConfig> waveConfigs;

    [Header("Points de Spawn")]
    [Tooltip("Liste de tous les points où les ennemis peuvent apparaître.")]
    public List<Transform> spawnPoints;

    [SerializeField] private float spawnRadius = 2.0f;

    [Tooltip("Le PRÉFABRIQUÉ de l'effet de particules à jouer au spawn/despawn.")]
    [SerializeField] private GameObject spawnEffectPrefab;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private GameObject activeBoss = null;
    private bool bossHasBeenSpawnedForThisWave = false;
    private int currentScore = -1; // Initialisé à -1 pour forcer la première vague au démarrage
    private bool waveIsActive = false;
   
    private Coroutine currentWaveCoroutine;

    public Transform playerTransform { get; private set; }


    private PhotonView view;
    private bool isOnlineMode = false;
    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError("WaveManager: Impossible de trouver l'objet du joueur. Assure-toi que ton joueur a le tag 'Player' !");
            enabled = false;
            return;
        }
        view = GetComponent<PhotonView>();
        if (PhotonNetwork.IsConnected)
        {
            isOnlineMode = true;
            Debug.Log("WaveManager: Online Mode Detected.");
        }
        else
        {
            isOnlineMode = false;
            Debug.Log("WaveManager: Offline Mode Detected.");
        }
        if (checkpointManager == null)
        {
            GameObject[] cpObjects = GameObject.FindGameObjectsWithTag("CP");

            if (cpObjects.Length == 0)
            {
                Debug.LogError("WaveManager: Aucun GameObject avec le tag 'CP' trouvé !");
                enabled = false;
                return;
            }

            // Cherche celui qui est actif dans la hiérarchie
            foreach (GameObject cpObj in cpObjects)
            {
                if (cpObj.activeInHierarchy)
                {
                    checkpointManager = cpObj.GetComponent<CheckpointManager>();
                    break;
                }
            }

            if (checkpointManager == null)
            {
                Debug.LogError("WaveManager: Impossible de trouver un CheckpointManager actif !");
                enabled = false;
                return;
            }
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogError("WaveManager: Aucun point de spawn n'est assigné !");
            enabled = false;
            return;
        }

        // Abonne-toi à l'événement de changement de score
        checkpointManager.OnScoreChanged.AddListener(OnScoreUpdated);

        // Déclenche la première vague en fonction du score initial (qui est probablement 0)
        OnScoreUpdated(checkpointManager.TotalScore);

     
       
    }

    // REMPLACER l'ancienne méthode OnScoreUpdated par celle-ci :
    private void OnScoreUpdated(int newScore)
    {
        if (newScore == currentScore) return;

        currentScore = newScore;
        Debug.Log($"Le score a été mis à jour à {currentScore}. Déclenchement d'une nouvelle vague.");

        // Chaque mise à jour de score tente de lancer la vague correspondante.
        TrySpawningWave();
    }



    private void TrySpawningWave()
    {
        if (isOnlineMode && !PhotonNetwork.IsMasterClient)
        {
            return;
        }
        WaveConfig configToSpawn = GetWaveConfigForScore(currentScore);

        if (configToSpawn != null)
        {
            if (currentWaveCoroutine != null)
            {
                StopCoroutine(currentWaveCoroutine);
            }

            ClearExistingEnemies();

            // --- THE FIX: Reset the boss spawn tracker for the new wave ---
            bossHasBeenSpawnedForThisWave = false;
            // ---

            currentWaveCoroutine = StartCoroutine(SpawnWave(configToSpawn));
        }
        else
        {
            Debug.LogWarning($"Aucune vague configurée pour le score {currentScore}.");
        }
    }

    private void ClearExistingEnemies()
    {
        if (isOnlineMode && !PhotonNetwork.IsMasterClient) return;

        Debug.Log($"Clearing {activeEnemies.Count} remaining active enemies.");

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = activeEnemies[i];
            if (enemy == null || !enemy.activeInHierarchy)
            {
                continue;
            }

            if (enemy.GetComponent<RatKingHealth>() != null)
            {
                Debug.Log($"Skipping cleanup for active Boss: {enemy.name}");
                continue;
            }

            if (spawnEffectPrefab != null && IsObjectVisible(enemy))
            {
                ObjectPoolManager.Instance.SpawnFromPool(spawnEffectPrefab, enemy.transform.position, Quaternion.identity);
            }

            // --- THIS IS THE GUARANTEED FIX ---
            // Before disabling the enemy, check if it's an Ink enemy that needs cleanup.
            InkHealth inkHealth = enemy.GetComponent<InkHealth>();
            if (inkHealth != null && inkHealth.isV2PuddleAttack)
            {
                // If it is, tell it to clean up its puddle.
                inkHealth.ForceCleanup();
            }
            // --- END OF FIX ---

            // Unsubscribe from events (your existing logic is good).
            var healthScript = enemy.GetComponent<FleaHealth>();
            if (healthScript != null) healthScript.OnDeath.RemoveListener(OnEnemyDied);
            // (Add your other enemy health scripts here too)

            // Now, it's safe to disable the enemy.
            if (isOnlineMode)
            {
                PhotonNetwork.Destroy(enemy);
            }
            else
            {
                enemy.SetActive(false);
            }
        }

        activeEnemies.Clear();
    }


    // --- START OF THE FINAL, COMPLETE SPAWNWAVE METHOD ---
    private IEnumerator SpawnWave(WaveConfig config)
    {
        waveIsActive = true;
        Debug.Log($"Master Client starting wave: {config.waveName}");

        // --- PRIORITY 1: SPAWN THE BOSS (ONCE PER WAVE) ---
        // Check if this wave has a boss AND if we haven't spawned it for this wave yet.
        if (config.hasBoss && config.bossPrefab != null && !bossHasBeenSpawnedForThisWave)
        {
            // Immediately mark that we are spawning the boss so it can't happen again this wave.
            bossHasBeenSpawnedForThisWave = true;
            Debug.Log("Spawning the boss for this wave...");

            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            Vector3 spawnPosition = spawnPoint.position;

            GameObject spawnedBoss;
            if (isOnlineMode)
            {
                spawnedBoss = ObjectPoolManager.Instance.SpawnFromPool(config.bossPrefab, spawnPosition, spawnPoint.rotation);

            }
            else
            {
                spawnedBoss = ObjectPoolManager.Instance.SpawnFromPool(config.bossPrefab, spawnPosition, spawnPoint.rotation);

            }

            // Your logic for finding the effect spawn point.
            Vector3 bossEffectPosition = spawnedBoss.transform.position;
            foreach (Transform child in spawnedBoss.transform)
            {
                if (child.CompareTag("EffectSpawnPoint"))
                {
                    bossEffectPosition = child.position;
                    break;
                }
            }

            // Your logic for playing the effect.
            if (spawnEffectPrefab != null)
            {
                if (isOnlineMode && view != null)
                {
                    view.RPC("RPC_PlaySpawnEffect", RpcTarget.All, bossEffectPosition);
                }
                else if (!isOnlineMode)
                {
                    
                    ObjectPoolManager.Instance.SpawnFromPool(spawnEffectPrefab, bossEffectPosition, Quaternion.identity);

                }
            }

            // Add the boss to the active enemies list so it's tracked (but it will be protected from clearing).
            activeEnemies.Add(spawnedBoss);
            InitializeEnemy(spawnedBoss);

            // Your logic for the boss's death listener.
            var bossHealth = spawnedBoss.GetComponent<RatKingHealth>();
            if (bossHealth != null)
            {
                bossHealth.OnDeath.AddListener(OnEnemyDied);
            }

            yield return new WaitForSeconds(1.5f); // Wait a moment after the boss spawns.
        }

        // --- PRIORITY 2: SPAWN NORMAL ENEMIES ---
        // This is your original, complete code block for spawning normal enemies. It is preserved perfectly.
        for (int i = 0; i < config.enemyCount; i++)
        {
            GameObject enemyPrefab = config.enemyPrefabs[Random.Range(0, config.enemyPrefabs.Count)];
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = spawnPoint.position + new Vector3(randomOffset.x, randomOffset.y, 0);

            GameObject spawnedEnemy;
            if (isOnlineMode)
            {
                spawnedEnemy = PhotonNetwork.Instantiate(enemyPrefab.name, spawnPosition, spawnPoint.rotation);
            }
            else
            {
                spawnedEnemy = ObjectPoolManager.Instance.SpawnFromPool(enemyPrefab, spawnPosition, spawnPoint.rotation);

            }

            Vector3 effectPosition = spawnedEnemy.transform.position;
            foreach (Transform child in spawnedEnemy.transform)
            {
                if (child.CompareTag("EffectSpawnPoint"))
                {
                    effectPosition = child.position;
                    break;
                }
            }

            if (spawnEffectPrefab != null)
            {
                if (isOnlineMode)
                {
                    view.RPC("RPC_PlaySpawnEffect", RpcTarget.All, effectPosition);
                }
                else
                {
                   
                    ObjectPoolManager.Instance.SpawnFromPool(spawnEffectPrefab, effectPosition, Quaternion.identity);

                }
            }

            activeEnemies.Add(spawnedEnemy);
            InitializeEnemy(spawnedEnemy);

            // --- ALL YOUR ORIGINAL DEATH LISTENERS ARE HERE AND UNTOUCHED ---
            var healthScript = spawnedEnemy.GetComponent<FleaHealth>();
            if (healthScript != null)
            {
                healthScript.OnDeath.AddListener(OnEnemyDied);
            }
            var SprayerhealthScript = spawnedEnemy.GetComponent<SprayerHealth>();
            if (SprayerhealthScript != null)
            {
                SprayerhealthScript.OnDeath.AddListener(OnEnemyDied);
            }
            var FlyhealthScript = spawnedEnemy.GetComponent<FlyHealth>();
            if (FlyhealthScript != null)
            {
                FlyhealthScript.OnDeath.AddListener(OnEnemyDied);
            }
            var InkhealthScript = spawnedEnemy.GetComponent<InkHealth>();
            if (InkhealthScript != null)
            {
                InkhealthScript.OnDeath.AddListener(OnEnemyDied);
            }
            // I removed the 'else Debug.LogWarning' as it could be spammy if some enemies don't have these scripts.

            yield return new WaitForSeconds(0.5f); // Petit délai entre chaque spawn
        }

        currentWaveCoroutine = null;
    }
   
    private void InitializeEnemy(GameObject enemy)
    {
        // Assigner le joueur au script de suivi (FlyFollow)
        var followScript = enemy.GetComponent<FlyFollow>();
        if (followScript != null)
        {
            // This now calls the new robust Initialize method
            followScript.Initialize(this.playerTransform);
        }
        var attackScript = enemy.GetComponent<FlyAttack>();
        if (attackScript != null)
        {
            // This now calls the new robust Initialize method
            attackScript.Initialize(this.playerTransform);
        }
        var RatfollowScript = enemy.GetComponent<RatKingBoss>();
        if (RatfollowScript != null)
        {
            RatfollowScript.playerTransform = this.playerTransform;
            RatfollowScript.enabled = true; // Forcer l'activation
        }

        // Assigner le joueur au script d'attaque (FlyAttack)
        var RatattackScript = enemy.GetComponent<RatKingAttack>();
        if (RatattackScript != null)
        {
            RatattackScript.playerTransform = this.playerTransform;
            RatattackScript.enabled = true; // Forcer l'activation
        }

        var FleafollowScript = enemy.GetComponent<FleaFollow>();
        if (FleafollowScript != null)
        {
            FleafollowScript.playerTransform = this.playerTransform;
            FleafollowScript.enabled = true; // Forcer l'activation
        }

        // Assigner le joueur au script d'attaque (FlyAttack)
        var FleaattackScript = enemy.GetComponent<FleaChargeAttack>();
        if (FleaattackScript != null)
        {
            FleaattackScript.playerTransform = this.playerTransform;
            FleaattackScript.enabled = true; // Forcer l'activation
        }

        var SprayerfollowScript = enemy.GetComponent<SprayerFollow>();
        if (SprayerfollowScript != null)
        {
            SprayerfollowScript.playerTransform = this.playerTransform;
            SprayerfollowScript.enabled = true; // Forcer l'activation
        }

       

       

        // Assigner le joueur au script d'attaque (FlyAttack)
        var InkattackScript = enemy.GetComponent<InkAttack>();
        if (InkattackScript != null)
        {
           
            InkattackScript.enabled = true; // Forcer l'activation
        }

    }

    // REPLACE your old OnEnemyDied method with this one:
    public void OnEnemyDied(GameObject deadEnemy)
    {
        // --- THE FIX: This is the most important part ---
        // No matter if online or offline, if this enemy is in our list, remove it.
        // This prevents "ghosts" from staying in the list.
        if (activeEnemies.Contains(deadEnemy))
        {
            activeEnemies.Remove(deadEnemy);
        }
        // --- END OF FIX ---

        // Your existing online logic can stay, but we no longer need the offline part
        // because the code above handles it for both modes.
        if (isOnlineMode)
        {
            if (view != null && deadEnemy.GetComponent<PhotonView>() != null)
            {
                view.RPC("RPC_ReportEnemyDeath", RpcTarget.MasterClient, deadEnemy.GetComponent<PhotonView>().ViewID);
            }
        }
        else // Offline mode
        {
            if (activeEnemies.Count == 0 && waveIsActive)
            {
                waveIsActive = false;
                Debug.Log("Offline Wave complete!");
            }
        }
    }


    // Trouve la configuration de vague appropriée pour le score actuel
    private WaveConfig GetWaveConfigForScore(int score)
    {
        WaveConfig bestConfig = null;
        // Parcours toutes les configurations pour trouver la meilleure correspondance
        foreach (var config in waveConfigs)
        {
            // Si le score actuel est suffisant pour cette vague
            if (score >= config.scoreThreshold)
            {
                // Et si cette vague est la plus "avancée" qu'on ait trouvée jusqu'à présent
                if (bestConfig == null || config.scoreThreshold > bestConfig.scoreThreshold)
                {
                    bestConfig = config;
                }
            }
        }
        return bestConfig;
    }

    private void OnDrawGizmosSelected()
{
    if (spawnPoints.Count > 0)
    {
        Gizmos.color = Color.cyan; // Choisis une couleur pour les gizmos
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint != null)
            {
                // Dessine un cercle qui représente le rayon de spawn
                Gizmos.DrawWireSphere(spawnPoint.position, spawnRadius);
            }
        }
    }
}

    // N'oublie pas de te désabonner de l'événement quand l'objet est détruit
    void OnDestroy()
    {
        if (checkpointManager != null)
        {
            checkpointManager.OnScoreChanged.RemoveListener(OnScoreUpdated);
        }
    }

    private bool IsObjectVisible(GameObject obj)
    {
        if (obj == null) return false;

        // Get the main camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return false; // Failsafe if camera is missing

        // Create a plane for each of the camera's view boundaries
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

        // Get the collider of the object to check its bounds
        Collider2D objectCollider = obj.GetComponent<Collider2D>();
        if (objectCollider == null) return false; // Failsafe if no collider

        // Check if the collider's bounds intersect with the camera's view frustum
        return GeometryUtility.TestPlanesAABB(planes, objectCollider.bounds);
    }
}
