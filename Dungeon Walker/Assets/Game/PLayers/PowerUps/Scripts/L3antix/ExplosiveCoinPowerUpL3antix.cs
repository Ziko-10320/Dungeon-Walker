using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CoinType3antix { Golden, Silver, Bronze }

public class ExplosiveCoinsPowerUpL3antix : MonoBehaviour
{
    [Header("Spawn / Chance")]
    [Range(0f, 1f)] public float spawnChance = 0.2f;
    public Transform spawnPoint;
    [Header("Audio")]
    [SerializeField] private AudioClip coinSpawnSound;
    [Range(0f, 1f)]
    [SerializeField] private float coinSpawnVolume = 1f;

    [SerializeField] private AudioClip goldenExplosionSound;
    [Range(0f, 1f)]
    [SerializeField] private float goldenExplosionVolume = 1f;

    [SerializeField] private AudioClip silverExplosionSound;
    [Range(0f, 1f)]
    [SerializeField] private float silverExplosionVolume = 1f;

    [SerializeField] private AudioClip bronzeExplosionSound;
    [Range(0f, 1f)]
    [SerializeField] private float bronzeExplosionVolume = 1f;
    [Header("Single launch force (controls diagonal launch)")]
    public float launchForce = 6f;
    public float splitForceMultiplier = 1.3f;

    [Header("Timing & explosion")]
    public float splitDelay = 0.25f;
    public float explosionDuration = 1f;
    public float explosionRadius = 2f;
    public float damageTickInterval = 0.1f;

    [Header("Layers")]
    public LayerMask enemyLayer;
    public LayerMask playerLayer;
    public LayerMask groundLayer;

    [Header("Prefabs")]
    public GameObject goldenCoinPrefab;
    public GameObject silverCoinPrefab;
    public GameObject bronzeCoinPrefab;

    [Header("Damage values")]
    public float goldenDamage = 30f;
    public float silverDamage = 20f;
    public float bronzeDamage = 10f;

    [Header("Particles")]
    public ParticleSystem[] goldenExplosionParticles;
    public ParticleSystem[] silverExplosionParticles;
    public ParticleSystem[] bronzeExplosionParticles;
    public float coinRotationSpeed = 360f;

    [Header("Local Pooling Settings")]
    public int goldenCoinPoolSize = 5;
    public int silverCoinPoolSize = 10;
    public int bronzeCoinPoolSize = 20;
    public int explosionEffectPoolSize = 10; // For each type

    private Queue<GameObject> goldenCoinPool;
    private Queue<GameObject> silverCoinPool;
    private Queue<GameObject> bronzeCoinPool;

    // A dictionary to hold pools for each particle system prefab
    private Dictionary<ParticleSystem, Queue<GameObject>> explosionPools;
    private Transform poolParent;
    void Awake()
    {
        // Create a parent object to keep the hierarchy clean
        poolParent = new GameObject("ExplosiveCoins_Pool").transform;
      

        // Create pools for each coin type
        goldenCoinPool = CreatePool(goldenCoinPrefab, goldenCoinPoolSize);
        silverCoinPool = CreatePool(silverCoinPrefab, silverCoinPoolSize);
        bronzeCoinPool = CreatePool(bronzeCoinPrefab, bronzeCoinPoolSize);

        // Create pools for each explosion particle system
        explosionPools = new Dictionary<ParticleSystem, Queue<GameObject>>();
        CreateExplosionPoolsFor(goldenExplosionParticles);
        CreateExplosionPoolsFor(silverExplosionParticles);
        CreateExplosionPoolsFor(bronzeExplosionParticles);
    }

    // Helper method to create a pool for a specific prefab
    private Queue<GameObject> CreatePool(GameObject prefab, int size)
    {
        Queue<GameObject> newPool = new Queue<GameObject>();
        if (prefab == null) return newPool;
        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab, poolParent);
            obj.SetActive(false);
            newPool.Enqueue(obj);
        }
        return newPool;
    }

    // Helper method to create pools for an array of particle systems
    private void CreateExplosionPoolsFor(ParticleSystem[] particles)
    {
        if (particles == null) return;
        foreach (var psPrefab in particles)
        {
            if (psPrefab != null && !explosionPools.ContainsKey(psPrefab))
            {
                Queue<GameObject> newPool = CreatePool(psPrefab.gameObject, explosionEffectPoolSize);
                explosionPools.Add(psPrefab, newPool);
            }
        }
    }
    public void TrySpawnCoin()
    {
        if (spawnPoint == null) return;
        if (Random.value <= spawnChance)
        {
            SpawnCoin(goldenCoinPrefab, CoinType3antix.Golden, spawnPoint.position);
        }
    }

    private void SpawnCoin(GameObject prefab, CoinType3antix type, Vector3 pos)
    {
       
        if (prefab == null) return;

        // --- GET FROM POOL INSTEAD OF INSTANTIATE ---
        Queue<GameObject> sourcePool = GetPoolForType(type);
        if (sourcePool.Count == 0) return; // Pool is empty, can't spawn

        GameObject go = sourcePool.Dequeue();
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;
        go.SetActive(true);
        // --- END OF CHANGE ---

        var cb = go.GetComponent<CoinBehaviour>() ?? go.AddComponent<CoinBehaviour>();
        float initialForce = (type == CoinType3antix.Golden) ? launchForce * splitForceMultiplier : launchForce;
        cb.Initialize(this, type, initialForce);
    }

    public void SpawnSplitCoins(GameObject prefab, CoinType3antix type, Vector3 pos)
    {
      
        for (int i = 0; i < 2; i++)
        {
            // --- GET FROM POOL INSTEAD OF INSTANTIATE ---
            Queue<GameObject> sourcePool = GetPoolForType(type);
            if (sourcePool.Count == 0) continue; // Skip if pool is empty

            GameObject go = sourcePool.Dequeue();
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            go.SetActive(true);
            // --- END OF CHANGE ---

            var cb = go.GetComponent<CoinBehaviour>() ?? go.AddComponent<CoinBehaviour>();
            float force = launchForce * splitForceMultiplier;
            int bias = (i == 0) ? -1 : 1;
            cb.Initialize(this, type, force, bias);
        }
    }

    // Helper method to get the correct pool for a coin type
    private Queue<GameObject> GetPoolForType(CoinType3antix type)
    {
        switch (type)
        {
            case CoinType3antix.Golden: return goldenCoinPool;
            case CoinType3antix.Silver: return silverCoinPool;
            case CoinType3antix.Bronze: return bronzeCoinPool;
            default: return null;
        }
    }
    public void ReturnCoinToPool(GameObject coin, CoinType3antix type)
    {
        coin.SetActive(false);
        Queue<GameObject> targetPool = GetPoolForType(type);
        if (targetPool != null)
        {
            targetPool.Enqueue(coin);
        }
    }
    public GameObject GetEffectFromPool(ParticleSystem prefabKey)
    {
        if (prefabKey != null && explosionPools.TryGetValue(prefabKey, out Queue<GameObject> pool))
        {
            if (pool.Count > 0)
            {
                // This is where we use the circular queue logic from the ObjectPoolManager
                GameObject objectToSpawn = pool.Dequeue();
                pool.Enqueue(objectToSpawn); // Immediately put it back in the queue
                return objectToSpawn;
            }
        }
        return null; // Return null if the pool doesn't exist or is empty
    }
    public void DamageInArea(Vector2 pos, float radius, float damage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius, enemyLayer | playerLayer);
        foreach (var col in hits)
        {
            if (col == null) continue;
            if (col.TryGetComponent(out FleaHealth flea)) flea.TakeDamage((int)damage, Vector2.zero);
            if (col.TryGetComponent(out FleaHealthV2 fleaV2)) fleaV2.TakeDamage((int)damage, Vector2.zero);
            if (col.TryGetComponent(out FlyHealth fly)) fly.TakeDamage((int)damage, Vector2.zero);
            if (col.TryGetComponent(out SprayerHealth sprayer)) sprayer.TakeDamage((int)damage, Vector2.zero);
            if (col.TryGetComponent(out InkHealth ink)) ink.TakeDamage((int)damage, Vector2.zero);
            if (col.TryGetComponent(out RatKingHealth rat)) rat.TakeDamage((int)damage);
            if (col.TryGetComponent(out PlayerHealth player)) player.TakeDamage((int)damage, 0f, Vector2.zero);
        }
    }

    // ---------------- Inner Coin Behaviour ----------------
    private class CoinBehaviour : MonoBehaviour
    {
        public Rigidbody2D rb;
        private CircleCollider2D mainCollider;
        private CircleCollider2D sensorCollider;
        private Animator anim;

        private ExplosiveCoinsPowerUpL3antix manager;
        private CoinType3antix type;

        private bool exploded = false;
        private bool landed = false;
        private bool explosionTriggered = false;

        public void Initialize(ExplosiveCoinsPowerUpL3antix manager, CoinType3antix type, float forceMagnitude, int horizontalBias = 0)
        {
            // --- 1. THE "FACTORY RESET" ---
            // This runs every time a coin is taken from the pool.

            this.manager = manager;
            this.type = type;

            // Reset all state flags to their default values
            exploded = false;
            landed = false;
            explosionTriggered = false;

            // Stop any coroutines that might be lingering from its previous life
            StopAllCoroutines();

            // --- 2. RESET PHYSICS AND COLLIDERS ---
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Unfreeze the coin and reset its physics state
                rb.constraints = RigidbodyConstraints2D.None;
                rb.gravityScale = 1f;
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (mainCollider == null)
            {
                // Find or create colliders if they don't exist yet (only happens once)
                CircleCollider2D[] existing = GetComponents<CircleCollider2D>();
                foreach (var c in existing) if (!c.isTrigger) { mainCollider = c; break; }
                if (mainCollider == null) mainCollider = gameObject.AddComponent<CircleCollider2D>();

                foreach (var c in existing) if (c.isTrigger) { sensorCollider = c; break; }
                if (sensorCollider == null)
                {
                    sensorCollider = gameObject.AddComponent<CircleCollider2D>();
                    sensorCollider.isTrigger = true;
                    sensorCollider.radius = Mathf.Max(mainCollider.radius * 0.9f, 0.2f);
                }
            }

            // Reset collider states for a new life
            mainCollider.enabled = false;
            sensorCollider.enabled = true;

            // --- 3. APPLY NEW FORCES ---
            if (anim == null) anim = GetComponent<Animator>();
            if (anim != null) anim.SetBool("isRolling", true);

            ApplyDiagonalVelocity(forceMagnitude, horizontalBias);
            rb.angularVelocity = manager.coinRotationSpeed;
        }

        private void ApplyDiagonalVelocity(float magnitude, int horizontalBias)
        {
            float angleDeg = Random.Range(30f, 60f);
            float angleRad = angleDeg * Mathf.Deg2Rad;
            int side = horizontalBias == 0 ? (Random.value < 0.5f ? -1 : 1) : (horizontalBias < 0 ? -1 : 1);
            Vector2 dir = new Vector2(Mathf.Cos(angleRad) * side, Mathf.Sin(angleRad));
            rb.velocity = dir * magnitude;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            int otherLayer = other.gameObject.layer;

            // --- LAND ON GROUND ---
            if (!landed && ((manager.groundLayer.value & (1 << otherLayer)) != 0))
            {
                landed = true;
                mainCollider.enabled = true;
                sensorCollider.enabled = false;

                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;

                if (!exploded) TriggerExplosion();
                manager.ReturnCoinToPool(gameObject, type);
                return;
            }

            // --- AIR COLLISION (split) ---
            if (!exploded && ((manager.enemyLayer.value & (1 << otherLayer)) != 0 ||
                              (manager.playerLayer.value & (1 << otherLayer)) != 0))
            {
                exploded = true;
                StartCoroutine(AirCollisionSequence());
                return;
            }
        }

        private IEnumerator AirCollisionSequence()
        {
            if (!exploded) exploded = true;
            TriggerExplosion();
            yield return new WaitForSeconds(manager.splitDelay);

            if (type == CoinType3antix.Golden)
                manager.SpawnSplitCoins(manager.silverCoinPrefab, CoinType3antix.Silver, transform.position);
            else if (type == CoinType3antix.Silver)
                manager.SpawnSplitCoins(manager.bronzeCoinPrefab, CoinType3antix.Bronze, transform.position);

            manager.ReturnCoinToPool(gameObject, type);
        }

        private void TriggerExplosion()
        {
            if (explosionTriggered) return;
            explosionTriggered = true;

            ParticleSystem[] parts = null;
            float dmg = 0f;
            AudioClip explosionSound = null;
            float explosionVolume = 1f;
            switch (type)
            {
                case CoinType3antix.Golden: parts = manager.goldenExplosionParticles; dmg = manager.goldenDamage; explosionSound = manager.goldenExplosionSound;
                    explosionVolume = manager.goldenExplosionVolume; break;
                case CoinType3antix.Silver: parts = manager.silverExplosionParticles; dmg = manager.silverDamage; explosionSound = manager.silverExplosionSound;
                    explosionVolume = manager.silverExplosionVolume; break;
                case CoinType3antix.Bronze: parts = manager.bronzeExplosionParticles; dmg = manager.bronzeDamage; explosionSound = manager.bronzeExplosionSound;
                    explosionVolume = manager.bronzeExplosionVolume; break;
            }
            if (explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(explosionSound, transform.position, explosionVolume);
            }

            if (parts != null)
            {
                foreach (var psPrefab in parts)
                {
                    if (psPrefab == null) continue;

                    // 1. Ask the manager for an effect from the correct pool.
                    GameObject effectInstance = manager.GetEffectFromPool(psPrefab);
                    if (effectInstance == null) continue;

                    // 2. Position it and activate it.
                    effectInstance.transform.position = transform.position;
                    effectInstance.SetActive(true);

                    // 3. THAT'S IT.
                    // Your 'PoolableParticle' script will automatically call .Play().
                    // The particle's "Stop Action: Disable" will automatically set it to inactive.
                    // The ObjectPoolManager will handle the rest.
                }
            }


            StartCoroutine(ExplosionDamageCoroutine(dmg));
        }
     
        private IEnumerator ExplosionDamageCoroutine(float damage)
        {
            float elapsed = 0f;
            while (elapsed < manager.explosionDuration)
            {
                manager.DamageInArea(transform.position, manager.explosionRadius, damage);
                elapsed += manager.damageTickInterval;
                yield return new WaitForSeconds(manager.damageTickInterval);
            }
        }
    }
}

