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
    public bool hasVolcanoes = false;
    [Tooltip("The prefab for the volcano obstacle.")]
    public GameObject volcanoPrefab;
    [Tooltip("A list of specific points where volcanoes should spawn for THIS wave.")]
    public List<Transform> volcanoSpawnPoints;
    [Header("Volcano Cycle Settings")]
    [Tooltip("How long the volcanoes stay active when triggered.")]
    [SerializeField] private float volcanoActiveDuration = 1.5f;
    [Tooltip("How long the volcanoes stay dormant between eruptions.")]
    [SerializeField] private float volcanoDormantDuration = 2.0f;

    private Coroutine volcanoBrainCoroutine;
    // A list of ALL potential volcano locations for the current wave.
    private List<Transform> potentialVolcanoSpawns = new List<Transform>();
    [Header("Wave Hazards")]
    public bool hasLasers = false;
    [Tooltip("A list of specific Laser Trap GameObjects to activate for THIS wave.")]
    public List<GameObject> laserTrapObjects;
}
[System.Serializable]
public class SpawnPointInfo
{
    public Transform point;
    public float radius = 2.0f;
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
    [Header("Points de Spawn")]
    [Tooltip("Liste de tous les points où les ennemis peuvent apparaître.")]
    public List<SpawnPointInfo> spawnPoints; // This is your main list for the Inspector

    [Tooltip("Le PRÉFABRIQUÉ de l'effet de particules à jouer au spawn/despawn.")]
    [SerializeField] private GameObject spawnEffectPrefab;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private List<PendingSpawn> pendingSpawns = new List<PendingSpawn>();
    private List<GameObject> activeVolcanoes = new List<GameObject>();
    private List<GameObject> activeLasers = new List<GameObject>();
    private Coroutine spawnCheckCoroutine;
    private const float SPAWN_CHECK_INTERVAL = 0.5f;
    private GameObject activeBoss = null;
    private bool bossHasBeenSpawnedForThisWave = false;
    private int currentScore = -1;
    private bool waveIsActive = false;
    private Coroutine currentWaveCoroutine;
    public Transform playerTransform { get; private set; }

    [Tooltip("How much the spawn rate speeds up as the wave progresses. 1.0 = no change. 1.5 = gets 50% faster. 2.0 = gets 100% faster.")]
    [SerializeField] private float wavePacingMultiplier = 1.5f;

    // This is the ONLY activeSpawningPoints variable. It is a list of SpawnPointInfo.
    private List<SpawnPointInfo> activeSpawningPoints = new List<SpawnPointInfo>();

    private PhotonView view;
    private bool isOnlineMode = false;
    private WaveConfig currentWaveConfig;

    [Tooltip("How long the volcanoes stay active when triggered.")]
    [SerializeField] private float volcanoActiveDuration = 1.5f;
    [Tooltip("How long the volcanoes stay dormant between eruptions.")]
    [SerializeField] private float volcanoDormantDuration = 2.0f;
    private Coroutine volcanoBrainCoroutine;
    // A list of ALL potential volcano locations for the current wave.
    private List<Transform> potentialVolcanoSpawns = new List<Transform>();

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
        if (volcanoBrainCoroutine == null)
        {
            volcanoBrainCoroutine = StartCoroutine(VolcanoBrain());
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
        if (volcanoBrainCoroutine != null)
        {
            StopCoroutine(volcanoBrainCoroutine);
            volcanoBrainCoroutine = null;
        }

        // 2. Now it's safe to disable all volcano objects and return them to the pool.
        foreach (GameObject volcano in activeVolcanoes)
        {
            if (volcano != null)
            {
                volcano.SetActive(false);
            }
        }
        activeVolcanoes.Clear();
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

            var uniquePendingSpawnInfos = pendingSpawns.Select(p => p.SpawnInfo).Distinct();

            foreach (SpawnPointInfo spawnInfo in uniquePendingSpawnInfos)
            {
                if (spawnInfo != null && spawnInfo.point != null &&
                    EffectCullingSystem.Instance.IsPositionInRadius(spawnInfo.point.position) &&
                    !activeSpawningPoints.Contains(spawnInfo))
                {
                    Debug.Log($"Player has entered range of spawn point {spawnInfo.point.name}. Activating it.");
                    activeSpawningPoints.Add(spawnInfo);
                    StartCoroutine(SpawnEnemiesAtPoint(spawnInfo, currentWaveConfig));
                }
            }

            yield return checkWait;
        }
    }
    private IEnumerator SpawnEnemiesAtPoint(SpawnPointInfo spawnInfo, WaveConfig config)
    {
        List<PendingSpawn> enemiesToSpawn = pendingSpawns.Where(p => p.SpawnInfo == spawnInfo).ToList();
        int enemyCountForThisPoint = enemiesToSpawn.Count;

        if (enemyCountForThisPoint == 0)
        {
            activeSpawningPoints.Remove(spawnInfo);
            yield break;
        }

        float baseDelay = (enemyCountForThisPoint > 0) ? config.spawnPointDuration / enemyCountForThisPoint : 0;

        foreach (PendingSpawn pending in enemiesToSpawn)
        {
            float waveProgress = 1.0f - ((float)pendingSpawns.Count / (config.enemyCount + 1));
            float pacingFactor = 1.0f - (4.0f * (waveProgress - 0.5f) * (waveProgress - 0.5f));
            pacingFactor = Mathf.Clamp(pacingFactor, 1.0f / config.wavePacingMultiplier, 1.0f);
            float currentDelay = baseDelay * pacingFactor;

            yield return new WaitForSeconds(currentDelay);

            if (pendingSpawns.Contains(pending))
            {
                SpawnSingleEnemy(pending.EnemyPrefab, pending.SpawnInfo);
                pendingSpawns.Remove(pending);
            }
        }

        Debug.Log($"Finished spawning all enemies for {spawnInfo.point.name}.");
        activeSpawningPoints.Remove(spawnInfo);
    }

    private void SpawnSingleEnemy(GameObject enemyPrefab, SpawnPointInfo spawnInfo)
    {
        Transform spawnPoint = spawnInfo.point;
        float currentSpawnRadius = spawnInfo.radius;

        Vector2 randomOffset = Random.insideUnitCircle * currentSpawnRadius;
        Vector3 spawnPosition = spawnPoint.position + new Vector3(randomOffset.x, randomOffset.y, 0);

        GameObject spawnedEnemy = ObjectPoolManager.Instance.SpawnFromPool(enemyPrefab, spawnPosition, spawnPoint.rotation);

        if (spawnEffectPrefab != null && EffectCullingSystem.Instance != null)
        {
            EffectCullingSystem.Instance.SpawnEffect(spawnEffectPrefab, spawnPosition, Quaternion.identity);
        }

        activeEnemies.Add(spawnedEnemy);
        InitializeEnemy(spawnedEnemy);

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
        foreach (GameObject laser in activeLasers)
        {
            if (laser != null) laser.SetActive(false);
        }
        activeLasers.Clear();

        // Now, check if THIS wave has lasers.
        if (config.hasLasers && config.laserTrapObjects != null)
        {
            Debug.Log($"Activating {config.laserTrapObjects.Count} lasers for this wave.");
            // Go through each laser assigned to this wave.
            foreach (GameObject laser in config.laserTrapObjects)
            {
                if (laser != null)
                {
                    // Activate it and add it to our list of active lasers for this wave.
                    laser.SetActive(true);
                    activeLasers.Add(laser);
                }
            }
        }
        pendingSpawns.Clear();
        activeSpawningPoints.Clear();

        if (config.hasBoss && config.bossPrefab != null && !bossHasBeenSpawnedForThisWave)
        {
            bossHasBeenSpawnedForThisWave = true;
            SpawnPointInfo spawnInfo = spawnPoints[Random.Range(0, spawnPoints.Count)];
            Debug.Log("Spawning Boss immediately.");
            SpawnSingleEnemy(config.bossPrefab, spawnInfo);
            yield return new WaitForSeconds(1.5f);
        }

        if (config.hasVolcanoes && config.volcanoPrefab != null)
        {
            foreach (Transform spawnPointTransform in config.volcanoSpawnPoints)
            {
                GameObject volcano = ObjectPoolManager.Instance.SpawnFromPool(
                    config.volcanoPrefab,
                    spawnPointTransform.position,
                    config.volcanoPrefab.transform.rotation
                );

                if (volcano != null)
                {
                    volcano.SetActive(false);
                    activeVolcanoes.Add(volcano);
                }
            }
        }

        for (int i = 0; i < config.enemyCount; i++)
        {
            GameObject enemyPrefab = config.enemyPrefabs[Random.Range(0, config.enemyPrefabs.Count)];
            SpawnPointInfo spawnInfo = spawnPoints[Random.Range(0, spawnPoints.Count)];

            PendingSpawn newPendingSpawn = new PendingSpawn
            {
                EnemyPrefab = enemyPrefab,
                SpawnInfo = spawnInfo
            };
            pendingSpawns.Add(newPendingSpawn);
        }

        Debug.Log($"Finished preparing wave. {pendingSpawns.Count} enemies are now waiting to be spawned.");
        if (activeVolcanoes.Count > 0)
        {
            volcanoBrainCoroutine = StartCoroutine(VolcanoBrain());
        }
        currentWaveCoroutine = null;
        yield return null;

        
    }

    private IEnumerator VolcanoBrain()
    {
        // This dictionary tracks which spawn points have an active volcano.
        Dictionary<Transform, GameObject> activeVolcanoInstances = new Dictionary<Transform, GameObject>();
        WaitForSeconds checkWait = new WaitForSeconds(0.5f); // Check ranges twice per second.

        while (true)
        {
            // Only run the logic if the current wave is supposed to have volcanoes.
            if (currentWaveConfig != null && currentWaveConfig.hasVolcanoes)
            {
                // Go through every potential volcano spawn point for the current wave.
                foreach (Transform spawnPoint in currentWaveConfig.volcanoSpawnPoints)
                {
                    bool isSpawnPointInRange = EffectCullingSystem.Instance.IsPositionInRadius(spawnPoint.position);
                    bool isVolcanoAlreadyActive = activeVolcanoInstances.ContainsKey(spawnPoint);

                    // --- ACTIVATION LOGIC ---
                    // If the spawn point IS in range AND a volcano is NOT already active there...
                    if (isSpawnPointInRange && !isVolcanoAlreadyActive)
                    {
                        // ...then SPAWN a new one!
                        GameObject volcano = ObjectPoolManager.Instance.SpawnFromPool(
                            currentWaveConfig.volcanoPrefab,
                            spawnPoint.position,
                            currentWaveConfig.volcanoPrefab.transform.rotation
                        );

                        if (volcano != null)
                        {
                            // Add it to our dictionary to track it.
                            activeVolcanoInstances[spawnPoint] = volcano;
                            Debug.Log($"Player entered range. Activated volcano at {spawnPoint.name}.");
                        }
                    }
                    // --- DEACTIVATION LOGIC ---
                    // If the spawn point is NOT in range AND a volcano IS currently active there...
                    else if (!isSpawnPointInRange && isVolcanoAlreadyActive)
                    {
                        // ...then DESPAWN it!
                        GameObject volcanoToDespawn = activeVolcanoInstances[spawnPoint];
                        if (volcanoToDespawn != null)
                        {
                            volcanoToDespawn.SetActive(false); // Return to pool.
                        }
                        // Remove it from our tracking dictionary.
                        activeVolcanoInstances.Remove(spawnPoint);
                        Debug.Log($"Player left range. Deactivated volcano at {spawnPoint.name}.");
                    }
                }
            }
            else
            {
                // If the current wave does NOT have volcanoes, clean up any leftovers.
                if (activeVolcanoInstances.Count > 0)
                {
                    foreach (var pair in activeVolcanoInstances)
                    {
                        if (pair.Value != null) pair.Value.SetActive(false);
                    }
                    activeVolcanoInstances.Clear();
                }
            }

            // Wait efficiently before checking all ranges again.
            yield return checkWait;
        }
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
        if (spawnPoints != null && spawnPoints.Count > 0)
        {
            Gizmos.color = Color.cyan;
            foreach (SpawnPointInfo spawnInfo in spawnPoints)
            {
                if (spawnInfo != null && spawnInfo.point != null)
                {
                    Gizmos.DrawWireSphere(spawnInfo.point.position, spawnInfo.radius);
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
    public SpawnPointInfo SpawnInfo;
}