using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    // Singleton instance to make it easily accessible from other scripts.
    public static ObjectPoolManager Instance;

    // A dictionary to hold different pools. The key is the prefab of the object to pool.
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        // Singleton pattern setup
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
