using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WaveSpawner : MonoBehaviour
{
    [Header("Dependencies")]
    public CheckpointManager checkpointManager; // Référence au CheckpointManager pour le score

    [Header("Spawn Points")]
    public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
    public float spawnRadiusOverride = -1f; // -1 pour utiliser le rayon du SpawnPoint, sinon utilise cette valeur

    [Header("Enemy Prefabs (for initial spawn if no waves are defined)")]
    public List<EnemyData> initialEnemyTypes;
    public int initialMinEnemies = 7;
    public int initialMaxEnemies = 8;

    [Header("Wave Settings")]
    public List<EnemyWave> waves = new List<EnemyWave>();
    public bool spawnWavesSequentially = true; // Si les vagues doivent être spawnées dans l'ordre
    public bool respawnIfScoreZero = true; // Réapparition si le score est à 0 et tous les ennemis sont tués
    public float respawnDelay = 5f; // Délai avant la réapparition

    [Header("Events")]
    public UnityEvent OnWaveStarted;
    public UnityEvent OnWaveCompleted;
    public UnityEvent<int> OnEnemySpawned;
    public UnityEvent<int> OnEnemyKilled;
    public UnityEvent OnAllEnemiesKilled;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private int currentWaveIndex = -1; // -1 pour le spawn initial, 0 pour la première vague
    private bool waveInProgress = false;
    private int enemiesKilledInCurrentWave = 0;
    private int totalEnemiesInCurrentWave = 0;

    void Start()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("WaveSpawner: Aucun point de spawn n'est assigné. Veuillez ajouter des objets SpawnPoint à la liste.");
            enabled = false; // Désactive le script si aucun point de spawn
            return;
        }

        if (checkpointManager == null)
        {
            Debug.LogWarning("WaveSpawner: CheckpointManager non assigné. La logique de réapparition basée sur le score pourrait ne pas fonctionner.");
        }

        StartCoroutine(InitialSpawnOrFirstWave());
    }

    void Update()
    {
        // Vérifier si tous les ennemis sont tués et si la réapparition est nécessaire
        if (respawnIfScoreZero && !waveInProgress && activeEnemies.Count == 0 && checkpointManager != null && checkpointManager.TotalScore == 0)
        {
            Debug.Log("Tous les ennemis tués et score à zéro. Préparation à la réapparition...");
            StartCoroutine(RespawnAfterDelay());
        }
    }

    IEnumerator InitialSpawnOrFirstWave()
    {
        yield return new WaitForSeconds(1f); // Petit délai au démarrage

        if (waves.Count > 0)
        {
            StartNextWave();
        }
        else if (initialEnemyTypes.Count > 0)
        {
            Debug.Log("Aucune vague définie. Spawning des ennemis initiaux.");
            SpawnInitialEnemies();
        }
        else
        {
            Debug.LogWarning("WaveSpawner: Aucune vague ni type d'ennemi initial défini. Rien à spawner.");
        }
    }

    void SpawnInitialEnemies()
    {
        waveInProgress = true;
        enemiesKilledInCurrentWave = 0;
        totalEnemiesInCurrentWave = Random.Range(initialMinEnemies, initialMaxEnemies + 1);
        Debug.Log($"Spawning {totalEnemiesInCurrentWave} ennemis initiaux.");

        StartCoroutine(SpawnEnemiesCoroutine(initialEnemyTypes, totalEnemiesInCurrentWave, 0.5f));
    }

    IEnumerator SpawnEnemiesCoroutine(List<EnemyData> enemyTypesToSpawn, int count, float delayBetweenSpawns)
    {
        for (int i = 0; i < count; i++)
        {
            if (enemyTypesToSpawn.Count == 0) break;

            EnemyData enemyData = enemyTypesToSpawn[Random.Range(0, enemyTypesToSpawn.Count)];
            SpawnEnemy(enemyData);
            yield return new WaitForSeconds(delayBetweenSpawns);
        }
        CheckWaveCompletion();
    }

    void StartNextWave()
    {
        if (spawnWavesSequentially)
        {
            currentWaveIndex++;
            if (currentWaveIndex < waves.Count)
            {
                StartCoroutine(SpawnWave(waves[currentWaveIndex]));
            }
            else
            {
                Debug.Log("Toutes les vagues séquentielles ont été complétées.");
                // Optionnel: Boucler les vagues ou déclencher un événement de fin de jeu
            }
        }
        else
        {
            // Logique pour spawner des vagues non séquentielles (ex: basées sur le score)
            TrySpawnRandomWave();
        }
    }

    void TrySpawnRandomWave()
    {
        if (checkpointManager == null) return; // Ne peut pas spawner de vague aléatoire sans score

        List<EnemyWave> availableWaves = new List<EnemyWave>();
        foreach (EnemyWave wave in waves)
        {
            if (wave.CanTrigger(checkpointManager.TotalScore))
            {
                availableWaves.Add(wave);
            }
        }

        if (availableWaves.Count > 0)
        {
            EnemyWave waveToSpawn = availableWaves[Random.Range(0, availableWaves.Count)];
            StartCoroutine(SpawnWave(waveToSpawn));
        }
        else
        {
            Debug.Log("Aucune vague disponible à spawner pour le score actuel.");
            // Si aucun ennemi actif et pas de vague disponible, et score 0, respawn
            if (activeEnemies.Count == 0 && checkpointManager.TotalScore == 0 && respawnIfScoreZero)
            {
                StartCoroutine(RespawnAfterDelay());
            }
        }
    }

    IEnumerator SpawnWave(EnemyWave wave)
    {
        waveInProgress = true;
        enemiesKilledInCurrentWave = 0;
        totalEnemiesInCurrentWave = 0;
        OnWaveStarted?.Invoke();
        Debug.Log($"Starting wave: {wave.waveName}");

        yield return new WaitForSeconds(wave.delayBeforeWave);

        foreach (EnemyWaveData enemyTypeData in wave.enemyTypes)
        {
            int count = enemyTypeData.GetRandomCount();
            totalEnemiesInCurrentWave += count;
            Debug.Log($"Spawning {count} of {enemyTypeData.enemyType.enemyName}");

            for (int i = 0; i < count; i++)
            {
                SpawnEnemy(enemyTypeData.enemyType);
                yield return new WaitForSeconds(enemyTypeData.spawnDelay);
            }
            yield return new WaitForSeconds(wave.delayBetweenEnemyTypes); // Délai entre les types d'ennemis
        }
        Debug.Log($"Total ennemis dans la vague {wave.waveName}: {totalEnemiesInCurrentWave}");
        CheckWaveCompletion();
    }

    void SpawnEnemy(EnemyData enemyData)
    {
        if (enemyData == null || enemyData.enemyPrefab == null) return;
        if (spawnPoints.Count == 0) return;

        SpawnPoint randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        Vector3 spawnPosition = randomSpawnPoint.GetRandomSpawnPosition();

        // Appliquer l'override du rayon si défini
        if (spawnRadiusOverride > 0)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadiusOverride;
            spawnPosition = randomSpawnPoint.transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        }

        GameObject enemy = Instantiate(enemyData.enemyPrefab, spawnPosition, Quaternion.identity);
        activeEnemies.Add(enemy);
        OnEnemySpawned?.Invoke(activeEnemies.Count);

        // Assurez-vous que l'ennemi a un composant qui peut notifier sa mort
        // Par exemple, un script EnemyHealth qui appelle OnEnemyKilled() du WaveSpawner
        FleaHealth FleaHealth = enemy.GetComponent<FleaHealth>();
        if (FleaHealth != null)
        {
            FleaHealth.OnDeath.AddListener(OnEnemyDeath);
        }

        FlyHealth FlyHealth = enemy.GetComponent<FlyHealth>();
        if (FlyHealth != null)
        {
            FlyHealth.OnDeath.AddListener(OnEnemyDeath);
        }

        else
        {
            Debug.LogWarning($"L'ennemi {enemy.name} n'a pas de composant EnemyHealth. La détection de mort ne fonctionnera pas.");
        }
    }

    public void OnEnemyDeath(GameObject deadEnemy)
    {
        if (activeEnemies.Contains(deadEnemy))
        {
            activeEnemies.Remove(deadEnemy);
            enemiesKilledInCurrentWave++;
            OnEnemyKilled?.Invoke(enemiesKilledInCurrentWave);
            Debug.Log($"Ennemi tué. Restant: {activeEnemies.Count}/{totalEnemiesInCurrentWave}");
            CheckWaveCompletion();
        }
    }

    void CheckWaveCompletion()
    {
        if (waveInProgress && activeEnemies.Count == 0 && enemiesKilledInCurrentWave >= totalEnemiesInCurrentWave)
        {
            Debug.Log("Vague complétée!");
            waveInProgress = false;
            OnWaveCompleted?.Invoke();

            if (spawnWavesSequentially)
            {
                StartNextWave(); // Démarre la prochaine vague séquentielle
            }
            else
            {
                // Si non séquentiel, attendre la prochaine condition de spawn ou réapparition
                if (checkpointManager != null && checkpointManager.TotalScore == 0 && respawnIfScoreZero)
                {
                    StartCoroutine(RespawnAfterDelay());
                }
            }
        }
    }

    IEnumerator RespawnAfterDelay()
    {
        Debug.Log($"Réapparition dans {respawnDelay} secondes...");
        yield return new WaitForSeconds(respawnDelay);
        Debug.Log("Réapparition des ennemis.");
        if (waves.Count > 0)
        {
            // Si des vagues sont définies, on relance la logique de vague aléatoire ou la première vague
            if (spawnWavesSequentially)
            {
                currentWaveIndex = 0; // Réinitialise à la première vague
                StartCoroutine(SpawnWave(waves[currentWaveIndex]));
            }
            else
            {
                TrySpawnRandomWave();
            }
        }
        else if (initialEnemyTypes.Count > 0)
        {
            SpawnInitialEnemies(); // Réapparition des ennemis initiaux
        }
    }

    // Méthode pour nettoyer les ennemis actifs (utile si le jeu se termine ou se réinitialise)
    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        activeEnemies.Clear();
        enemiesKilledInCurrentWave = 0;
        totalEnemiesInCurrentWave = 0;
        waveInProgress = false;
    }
}


