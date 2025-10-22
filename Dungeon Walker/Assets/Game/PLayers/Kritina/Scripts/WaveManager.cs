using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

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
    [Header("Wave Rhythm")]
    [Tooltip("The total time it should take to spawn all enemies from a SINGLE activated spawn point for THIS wave.")]
    public float spawnPointDuration = 6.0f;

    [Tooltip("How much the spawn rate speeds up as THIS wave progresses. 1.0 = no change. 2.0 = gets 100% faster.")]
    public float wavePacingMultiplier = 1.5f;
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
    private List<PendingSpawn> pendingSpawns = new List<PendingSpawn>();
    private Coroutine spawnCheckCoroutine;
    private const float SPAWN_CHECK_INTERVAL = 0.5f;
    private GameObject activeBoss = null;
    private bool bossHasBeenSpawnedForThisWave = false;
    private int currentScore = -1; // Initialisé à -1 pour forcer la première vague au démarrage
    private bool waveIsActive = false;
   
    private Coroutine currentWaveCoroutine;
    public Transform playerTransform { get; private set; }
    [Tooltip("How much the spawn rate speeds up as the wave progresses. 1.0 = no change. 1.5 = gets 50% faster. 2.0 = gets 100% faster.")]
    [SerializeField] private float wavePacingMultiplier = 1.5f;
    private List<Transform> activeSpawningPoints = new List<Transform>();
    private PhotonView view;
    private bool isOnlineMode = false;
    private WaveConfig currentWaveConfig;

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

        if (spawnCheckCoroutine == null)
        {
            spawnCheckCoroutine = StartCoroutine(SpawnCheckBrain());
        }

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

            currentWaveConfig = configToSpawn;

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

            if (spawnEffectPrefab != null && EffectCullingSystem.Instance != null)
            {
                // We no longer need IsObjectVisible, the culling system handles the check.
                EffectCullingSystem.Instance.SpawnEffect(spawnEffectPrefab, enemy.transform.position, Quaternion.identity);
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
    private IEnumerator SpawnCheckBrain()
    {
        WaitForSeconds checkWait = new WaitForSeconds(SPAWN_CHECK_INTERVAL);

        while (true)
        {
            if (pendingSpawns.Count == 0 || EffectCullingSystem.Instance == null)
            {
                yield return checkWait;
                continue;
            }

            // --- NEW LOGIC: Find a new spawn point to activate ---
            // We check all unique spawn points that have enemies waiting.
            var uniquePendingSpawnPoints = pendingSpawns.Select(p => p.SpawnPoint).Distinct();

            foreach (Transform spawnPoint in uniquePendingSpawnPoints)
            {
                // Check three conditions:
                // 1. Is the spawn point valid?
                // 2. Is it now inside the player's radius?
                // 3. Is it NOT already in our list of active spawners?
                if (spawnPoint != null &&
                    EffectCullingSystem.Instance.IsPositionInRadius(spawnPoint.position) &&
                    !activeSpawningPoints.Contains(spawnPoint))
                {
                    // All conditions met! Activate this spawn point.
                    Debug.Log($"Player has entered range of spawn point {spawnPoint.name}. Activating it.");

                    // Add it to the active list to prevent re-triggering.
                    activeSpawningPoints.Add(spawnPoint);

                    // Start a NEW, DEDICATED coroutine just for this spawn point.
                   StartCoroutine(SpawnEnemiesAtPoint(spawnPoint, currentWaveConfig));
                }
            }

            yield return checkWait;
        }
    }
    private IEnumerator SpawnEnemiesAtPoint(Transform spawnPoint, WaveConfig config)
    {
        // 1. Find all enemies that are waiting at this specific spawn point.
        List<PendingSpawn> enemiesToSpawn = pendingSpawns.Where(p => p.SpawnPoint == spawnPoint).ToList();
        int enemyCountForThisPoint = enemiesToSpawn.Count;

        if (enemyCountForThisPoint == 0)
        {
            // No enemies for this point, so we're done.
            activeSpawningPoints.Remove(spawnPoint); // Clean up
            yield break;
        }

        float baseDelay = config.spawnPointDuration / enemyCountForThisPoint;


        // 3. The Spawning Loop
        foreach (PendingSpawn pending in enemiesToSpawn)
        {
            // --- DYNAMIC PACING LOGIC ---
            // Calculate the progress of the entire wave (0.0 to 1.0).
            float waveProgress = 1.0f - ((float)pendingSpawns.Count / (config.enemyCount + 1)); // +1 to avoid division by zero

            // Use an AnimationCurve-like formula (EaseInOut) to create a nice rhythm.
            // This makes the delay shorter in the middle of the wave and longer at the start/end.
            float pacingFactor = 1.0f - (4.0f * (waveProgress - 0.5f) * (waveProgress - 0.5f)); // Parabolic curve
                                                                                                // Use THIS wave's specific pacing multiplier.
            pacingFactor = Mathf.Clamp(pacingFactor, 1.0f / config.wavePacingMultiplier, 1.0f);


            // Calculate the final delay for this specific spawn.
            float currentDelay = baseDelay * pacingFactor;
            // --- END OF PACING LOGIC ---

            // Wait for the calculated delay.
            yield return new WaitForSeconds(currentDelay);

            // Spawn this enemy and remove it from the master "waiting" list.
            if (pendingSpawns.Contains(pending))
            {
                SpawnSingleEnemy(pending.EnemyPrefab, pending.SpawnPoint);
                pendingSpawns.Remove(pending);
            }
        }

        // 4. Cleanup: Once all enemies for this point are spawned, remove it from the active list.
        Debug.Log($"Finished spawning all enemies for {spawnPoint.name}.");
        activeSpawningPoints.Remove(spawnPoint);
    }


    private void SpawnSingleEnemy(GameObject enemyPrefab, Transform spawnPoint)
    {
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = spawnPoint.position + new Vector3(randomOffset.x, randomOffset.y, 0);

        // Use your object pooler to get the enemy.
        GameObject spawnedEnemy = ObjectPoolManager.Instance.SpawnFromPool(enemyPrefab, spawnPosition, spawnPoint.rotation);

        // Use the culling system to play the spawn effect (it will only play if in range).
        if (spawnEffectPrefab != null && EffectCullingSystem.Instance != null)
        {
            EffectCullingSystem.Instance.SpawnEffect(spawnEffectPrefab, spawnPosition, Quaternion.identity);
        }

        // Add to the list of active enemies and initialize it.
        activeEnemies.Add(spawnedEnemy);
        InitializeEnemy(spawnedEnemy);

        // Add the death listener so we know when it's killed.
        // (You will need to add cases for all your enemy health scripts here)
        if (spawnedEnemy.TryGetComponent(out FleaHealth fleaHealth)) fleaHealth.OnDeath.AddListener(OnEnemyDied);
        if (spawnedEnemy.TryGetComponent(out SprayerHealth sprayerHealth)) sprayerHealth.OnDeath.AddListener(OnEnemyDied);
        if (spawnedEnemy.TryGetComponent(out FlyHealth flyHealth)) flyHealth.OnDeath.AddListener(OnEnemyDied);
        if (spawnedEnemy.TryGetComponent(out InkHealth inkHealth)) inkHealth.OnDeath.AddListener(OnEnemyDied);
        if (spawnedEnemy.TryGetComponent(out RatKingHealth ratKingHealth)) ratKingHealth.OnDeath.AddListener(OnEnemyDied);
    }

    // --- START OF THE FINAL, COMPLETE SPAWNWAVE METHOD ---
    private IEnumerator SpawnWave(WaveConfig config)
    {
        waveIsActive = true;
        Debug.Log($"Master Client preparing wave: {config.waveName}. Adding {config.enemyCount} enemies to the pending list.");

        // Clear any enemies that were waiting from a previous wave.
        pendingSpawns.Clear();
        activeSpawningPoints.Clear();
        // --- PRIORITY 1: HANDLE THE BOSS ---
        if (config.hasBoss && config.bossPrefab != null && !bossHasBeenSpawnedForThisWave)
        {
            bossHasBeenSpawnedForThisWave = true;
            // Bosses are important, so we spawn them immediately regardless of range.
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            Debug.Log("Spawning Boss immediately.");
            SpawnSingleEnemy(config.bossPrefab, spawnPoint);
            yield return new WaitForSeconds(1.5f); // Wait a moment after the boss spawns.
        }

        // --- PRIORITY 2: PREPARE NORMAL ENEMIES ---
        // Instead of spawning, we now add them to the "waiting" list.
        for (int i = 0; i < config.enemyCount; i++)
        {
            GameObject enemyPrefab = config.enemyPrefabs[Random.Range(0, config.enemyPrefabs.Count)];
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

            // Create a new "pending spawn" request and add it to our list.
            PendingSpawn newPendingSpawn = new PendingSpawn
            {
                EnemyPrefab = enemyPrefab,
                SpawnPoint = spawnPoint
            };
            pendingSpawns.Add(newPendingSpawn);
        }

        Debug.Log($"Finished preparing wave. {pendingSpawns.Count} enemies are now waiting to be spawned.");
        currentWaveCoroutine = null;
        yield return null; // End the coroutine.
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
public class PendingSpawn
{
    public GameObject EnemyPrefab;
    public Transform SpawnPoint;
}