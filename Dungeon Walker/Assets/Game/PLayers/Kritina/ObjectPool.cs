using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance;

    [Header("Pool Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int poolSize = 20; // How many bullets to create at the start

    private List<GameObject> bulletPool;

    void Awake()
    {
        // Singleton pattern to make it easily accessible
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        bulletPool = new List<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false); // Start with the bullet inactive
            bulletPool.Add(bullet);
        }
    }

    public GameObject GetBullet()
    {
        // Find an inactive bullet in the pool
        foreach (GameObject bullet in bulletPool)
        {
            if (!bullet.activeInHierarchy)
            {
                return bullet;
            }
        }

        // If no inactive bullets are found, you can optionally expand the pool
        // For now, we'll just return null, but expanding is a good idea for robustness
        Debug.LogWarning("No available bullets in the pool! Consider increasing the pool size.");
        return null;
    }
}
