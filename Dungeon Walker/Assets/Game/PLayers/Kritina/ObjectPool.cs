using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    // Singleton instance to make it easily accessible from other scripts.
    public static ObjectPoolManager Instance;

    [Header("Enemy Prefabs to Pre-Pool")]
    public GameObject fleaPrefab;
    public GameObject sprayerPrefab; // Example for your other enemy
    public GameObject flyPrefab;     // Example for your other enemy
    public GameObject inkPrefab;     // Example for your other enemy
    public GameObject bossPrefab;    // This is your Rat King Boss

    [Header("Enemy Pool Sizes")]
    public int normalEnemyPoolSize = 60; // 60 of each of the 4 types = 240
    public int bossPoolSize = 10;        // 10 for the boss

    [Header("Effect Prefabs to Pre-Pool")]
    public GameObject dustExplosionEffectPrefab;

    // ADD THIS under your other pool sizes
    [Header("Effect Pool Sizes")]
    public int effectPoolSize = 50;
    // A dictionary to hold different pools. The key is the prefab of the object to pool.
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

        // --- THIS IS THE NEW, SMART LOGIC ---
        // 1. Find ALL MonoBehaviours in the entire scene, including inactive ones.
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>(true);
        Debug.Log("--- Pre-warming all ENEMY object pools... ---");
        if (fleaPrefab != null) CreatePool(fleaPrefab, normalEnemyPoolSize);
        if (sprayerPrefab != null) CreatePool(sprayerPrefab, normalEnemyPoolSize);
        if (flyPrefab != null) CreatePool(flyPrefab, normalEnemyPoolSize);
        if (inkPrefab != null) CreatePool(inkPrefab, normalEnemyPoolSize);
        if (bossPrefab != null) CreatePool(bossPrefab, bossPoolSize);
        if (dustExplosionEffectPrefab != null) CreatePool(dustExplosionEffectPrefab, effectPoolSize);
        Debug.Log("--- Enemy object pools ready. ---");


        foreach (MonoBehaviour script in allScripts)
        {
            // Check if the script we found has "signed the contract" of our IPoolable interface.
            if (script is IPoolable)
            {
                // If it has, we can safely cast it and call the method.
                IPoolable poolable = (IPoolable)script;
                poolable.CreatePools();
            }
        }
    }

    /// <summary>
    /// Creates a new object pool for a specific prefab.
    /// </summary>
    /// <param name="prefab">The GameObject prefab to be pooled.</param>
    /// <param name="poolSize">The initial number of objects to create in the pool.</param>
    public void CreatePool(GameObject prefab, int poolSize)
    {
        if (poolDictionary.ContainsKey(prefab))
        {
            Debug.LogWarning($"Pool for prefab '{prefab.name}' already exists.");
            return;
        }

        // --- HIERARCHY FIX: Create a parent object for this pool ---
        GameObject poolParent = new GameObject(prefab.name + " Pool");
        // Optional: Keep the main scene clean by parenting this to the manager itself.
        poolParent.transform.SetParent(this.transform);
        // --- END OF HIERARCHY FIX ---

        Queue<GameObject> objectPool = new Queue<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab);

            // --- HIERARCHY FIX: Set the parent of the new object ---
            obj.transform.SetParent(poolParent.transform);

            obj.SetActive(false);
            objectPool.Enqueue(obj);
        }

        poolDictionary.Add(prefab, objectPool);
    }
    /// <summary>
    /// Spawns an object from the pool.
    /// </summary>
    /// <param name="prefab">The prefab of the object to spawn.</param>
    /// <param name="position">The position to spawn the object at.</param>
    /// <param name="rotation">The rotation to spawn the object with.</param>
    /// <returns>The spawned GameObject from the pool.</returns>
    // --- REPLACE the old SpawnFromPool method with this one ---
    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            Debug.LogError($"Pool for prefab '{prefab.name}' doesn't exist. Create it first.");
            return null;
        }

        // Get an object from the pool's queue.
        GameObject objectToSpawn = poolDictionary[prefab].Dequeue();

        // --- ROBUSTNESS FIX ---
        // If the object is somehow already active, log a warning. This helps debug future issues.
        if (objectToSpawn.activeInHierarchy)
        {
            Debug.LogWarning($"Re-spawning an object '{objectToSpawn.name}' that was already active. This may indicate a pooling issue.");
        }
        // --- END OF FIX ---

        // Activate it and set its position and rotation.
        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // Add the object back to the end of the queue so it can be reused later.
        poolDictionary[prefab].Enqueue(objectToSpawn);

        return objectToSpawn;
    }

   
}
public interface IPoolable
{
    void CreatePools();
}