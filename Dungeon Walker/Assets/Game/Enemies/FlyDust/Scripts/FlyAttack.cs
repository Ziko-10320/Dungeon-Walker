using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyAttack : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public GameObject dustProjectilePrefab;
    public Transform projectileSpawnPoint;
    public GameObject dustExplosionEffect;

    [Header("Attack Settings")]
    public float attackIntervalMin = 2f;
    public float attackIntervalMax = 5f;

    [Header("Player Targeting")]
    public Vector2 playerTargetOffset = Vector2.zero;

    [Header("Projectile Settings")]
    public float projectileSpeed = 10f;
    public int projectileDamage = 10;
    public float projectileLifetime = 3f;
    public LayerMask groundLayer;
    public LayerMask playerLayer;
    public float playerKnockbackForce = 5f;
    public Vector2 playerKnockbackDirection = Vector2.up;

    [Header("Explosion Settings")]
    public float explosionRadius = 2f;
    public int explosionDamage = 15;

    [Header("Double Attack Settings")]
    [Range(0f, 1f)] public float doubleAttackChance = 0.3f;
    public float delayBetweenProjectiles = 0.2f;
    public Vector2 secondProjectileOffset = new Vector2(0f, 1f);

    [Header("Attack Sound Settings")]
    public AudioClip attackSoundClip;
    [Range(0f, 1f)] public float attackSoundVolume = 0.7f; // Volume slider added here

    private AudioSource audioSource;
    private Animator animator;
    private float nextAttackTime;
    private Vector2 lastKnownPlayerPosition;

    public int projectilePoolSize = 5; // How many projectiles this fly keeps ready
    private Queue<GameObject> projectilePool;
    private GameObject projectilePoolParent;
    private List<GameObject> activeProjectiles = new List<GameObject>();

    private WaitForSeconds delayBetweenProjectilesWait;
    private WaitForSeconds projectileLifetimeWait;
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = attackSoundVolume; // Set initial volume
        delayBetweenProjectilesWait = new WaitForSeconds(delayBetweenProjectiles);
        projectileLifetimeWait = new WaitForSeconds(projectileLifetime);
    }
    public void Initialize(Transform player)
    {
        playerTransform = player;

        // --- FAILSAFE FIX ---
        // If the player reference is STILL null, try to get it from the WaveManager directly.
        if (playerTransform == null && FindObjectOfType<WaveManager>() != null)
        {
            playerTransform = FindObjectOfType<WaveManager>().playerTransform;
        }
        // --- END OF FIX ---

        animator = GetComponent<Animator>();
        SetNextAttackTime();
        activeProjectiles.Clear();
        StopAllCoroutines();
    }

    void Start()
    {
        projectilePool = new Queue<GameObject>();
        // Create a clean parent object for this fly's projectiles
        projectilePoolParent = new GameObject(gameObject.name + " Projectile Pool");
        projectilePoolParent.transform.SetParent(this.transform); // Parent it to the fly itself

        for (int i = 0; i < projectilePoolSize; i++)
        {
            GameObject projectile = Instantiate(dustProjectilePrefab);
            projectile.transform.SetParent(projectilePoolParent.transform);
            projectile.SetActive(false);
            projectilePool.Enqueue(projectile);
        }
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            
            enabled = false;
        }
      
        if (dustProjectilePrefab == null)
        {
            
            enabled = false;
        }
        if (projectileSpawnPoint == null)
        {
            
            enabled = false;
        }
     
    }

    void Update()
    {
        bool playerInvisible = false;
        if (playerTransform != null)
        {
            PlayerInvisibility invis = playerTransform.GetComponent<PlayerInvisibility>();
            if (invis != null) playerInvisible = invis.IsInvisible();
        }
        if (playerTransform != null)
        {
            PlayerInvisibility3antix invis3antix = playerTransform.GetComponent<PlayerInvisibility3antix>();
            if (invis3antix != null) playerInvisible = invis3antix.IsInvisible();
        }
        // Stop attack if player invisible
        if (playerInvisible)
        {
            animator.ResetTrigger("Attack"); // stop throw animation
            return;
        }

        lastKnownPlayerPosition = (Vector2)playerTransform.position + playerTargetOffset;

        if (Time.time >= nextAttackTime)
        {
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
            SetNextAttackTime();
        }

        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            GameObject projectile = activeProjectiles[i];

            // If the projectile was somehow disabled, remove it from the list and skip.
            if (projectile == null || !projectile.activeInHierarchy)
            {
                activeProjectiles.RemoveAt(i);
                continue;
            }

            // Check for collision with the player
            Collider2D playerHit = Physics2D.OverlapCircle(projectile.transform.position, 0.2f, playerLayer);
            if (playerHit != null)
            {
                // We hit the player!
                PlayerHealth playerHealth = playerHit.GetComponent<PlayerHealth>();
                if (playerHealth != null) playerHealth.TakeDamage(projectileDamage, playerKnockbackForce, playerKnockbackDirection);

                L3antixHealth l3antixHealth = playerHit.GetComponent<L3antixHealth>();
                if (l3antixHealth != null) l3antixHealth.TakeDamage(projectileDamage, playerKnockbackForce, playerKnockbackDirection);

                // Disable projectile and remove it from the active list.
                projectile.SetActive(false);
                activeProjectiles.RemoveAt(i);
                continue; // Go to the next projectile
            }

            // Check for collision with the ground
            Collider2D groundHit = Physics2D.OverlapCircle(projectile.transform.position, 0.2f, groundLayer);
            if (groundHit != null)
            {
                // We hit the ground!
                // Spawn explosion effect
                if (dustExplosionEffect != null)
                {
                    ObjectPoolManager.Instance.SpawnFromPool(dustExplosionEffect, projectile.transform.position, Quaternion.identity);
                }

                // Damage player in radius
                Collider2D[] playersInExplosion = Physics2D.OverlapCircleAll(projectile.transform.position, explosionRadius, playerLayer);
                foreach (var player in playersInExplosion)
                {
                    PlayerHealth pHealth = player.GetComponent<PlayerHealth>();
                    if (pHealth != null) pHealth.TakeDamage(explosionDamage, playerKnockbackForce, playerKnockbackDirection);

                    L3antixHealth lHealth = player.GetComponent<L3antixHealth>();
                    if (lHealth != null) lHealth.TakeDamage(explosionDamage, playerKnockbackForce, playerKnockbackDirection);
                }

                // Disable projectile and remove it from the active list.
                projectile.SetActive(false);
                activeProjectiles.RemoveAt(i);
            }
        }

    }

    public void ThrowDust()
    {
        if (playerTransform == null) return;

        PlayerInvisibility invis = playerTransform.GetComponent<PlayerInvisibility>();
        PlayerInvisibility3antix invis3antix = playerTransform.GetComponent<PlayerInvisibility3antix>();
        if (invis != null && invis.IsInvisible()) return;
        if (invis3antix != null && invis3antix.IsInvisible()) return;
      
        if (dustProjectilePrefab == null || projectileSpawnPoint == null || playerTransform == null)
        {
           
            return;
        }

        // Play attack sound with adjustable volume
        if (attackSoundClip != null && audioSource != null)
        {
            audioSource.volume = attackSoundVolume;
            audioSource.PlayOneShot(attackSoundClip);
        }

        StartCoroutine(ThrowDustCoroutine());
    }

    private IEnumerator ThrowDustCoroutine()
    {
        InstantiateAndInitializeProjectile(lastKnownPlayerPosition);
        if (Random.value < doubleAttackChance)
        {
            yield return delayBetweenProjectilesWait;
            InstantiateAndInitializeProjectile(lastKnownPlayerPosition + secondProjectileOffset);
        }
    }


    private void InstantiateAndInitializeProjectile(Vector2 targetPlayerPosition)
    {
        GameObject dust = projectilePool.Dequeue();
        projectilePool.Enqueue(dust);

        dust.transform.position = projectileSpawnPoint.position;
        dust.SetActive(true);

        // Add to our list of active projectiles to manage
        activeProjectiles.Add(dust);

        Vector2 predictedPlayerPosition = targetPlayerPosition;
        // ... (your player prediction code can stay here)

        Rigidbody2D dustRb = dust.GetComponent<Rigidbody2D>();
        if (dustRb != null)
        {
            Vector2 direction = (predictedPlayerPosition - (Vector2)dust.transform.position).normalized;
            dustRb.velocity = direction * projectileSpeed;
        }

        // Start a coroutine to handle the lifetime
        StartCoroutine(LifetimeCountdown(dust));
    }

    private IEnumerator LifetimeCountdown(GameObject projectile)
    {
        yield return projectileLifetimeWait;

        if (projectile != null && projectile.activeInHierarchy)
        {
            // If time runs out, treat it like it hit the ground (create an explosion)
            if (dustExplosionEffect != null)
            {
                ObjectPoolManager.Instance.SpawnFromPool(dustExplosionEffect, projectile.transform.position, Quaternion.identity);
            }
            projectile.SetActive(false);
        }
    }
    void SetNextAttackTime()
    {
        nextAttackTime = Time.time + Random.Range(attackIntervalMin, attackIntervalMax);
    }

    // Internal class to manage projectile behavior
    public class ProjectileController : MonoBehaviour
    {
        private float speed;
        private int damage;
        private float lifetime;
        private LayerMask groundLayer;
        private LayerMask playerLayer;
        private GameObject explosionEffect;
        private float knockbackForce;
        private Vector2 knockbackDirection;
        private float explosionRadius;
        private int explosionDamage;
        private Vector2 targetPosition;
        private Rigidbody2D rb;
        private bool hasBeenDestroyed = false;
        public Queue<GameObject> ownerPool;
        public void InitializeProjectile(
            Vector2 targetPos,
            float projSpeed,
            int projDamage,
            float projLifetime,
            LayerMask gLayer,
            LayerMask pLayer,
            GameObject expEffect,
            float kbForce,
            Vector2 kbDirection,
            float expRadius,
            int expDamage)
        {
            hasBeenDestroyed = false;
            StopAllCoroutines();
            targetPosition = targetPos;
            speed = projSpeed;
            damage = projDamage;
            lifetime = projLifetime;
            groundLayer = gLayer;
            playerLayer = pLayer;
            explosionEffect = expEffect;
            knockbackForce = kbForce;
            knockbackDirection = kbDirection;
            explosionRadius = expRadius;
            explosionDamage = expDamage;
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError("ProjectileController: Rigidbody2D not found.");
                enabled = false;
                return;
            }

            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            rb.velocity = direction * speed;
            StartCoroutine(LifetimeCountdown());
        }

        private IEnumerator LifetimeCountdown()
        {
            yield return new WaitForSeconds(lifetime);

            // If the projectile is still active after its lifetime has expired...
            if (gameObject.activeInHierarchy && !hasBeenDestroyed)
            {
                // ...call the master method and tell it to trigger a full explosion.
                DestroyProjectile(transform.position, true);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            // If already handled, ignore.
            if (hasBeenDestroyed) return;

            // --- IF WE HIT THE GROUND ---
            if (((1 << other.gameObject.layer) & groundLayer) != 0)
            {
                // Call the master method and tell it to trigger a full explosion.
                DestroyProjectile(transform.position, true);
            }
            // --- IF WE HIT THE PLAYER ---
            else if (((1 << other.gameObject.layer) & playerLayer) != 0)
            {
                // First, apply the direct hit damage.
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage, knockbackForce, knockbackDirection);
                }
                L3antixHealth l3antixHealth = other.GetComponent<L3antixHealth>();
                if (l3antixHealth != null)
                {
                    l3antixHealth.TakeDamage(damage, knockbackForce, knockbackDirection);
                }

                // Now, call the master method and tell it NOT to trigger the area explosion.
                // This will just disable the projectile.
                DestroyProjectile(transform.position, false);
            }
        }


        public void DestroyProjectile(Vector2 explosionPosition, bool triggerExplosion)
        {
            // If this projectile has already been handled in this frame, do nothing.
            if (hasBeenDestroyed) return;
            hasBeenDestroyed = true; // Mark it as "used" immediately.

            // --- LOGIC FOR EXPLOSION DAMAGE ---
            if (triggerExplosion)
            {
                // Find all players within the explosion radius.
                Collider2D[] hitColliders = Physics2D.OverlapCircleAll(explosionPosition, explosionRadius, playerLayer);
                foreach (Collider2D hitCollider in hitColliders)
                {
                    PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(explosionDamage, knockbackForce, knockbackDirection);
                    }
                    L3antixHealth l3antixHealth = hitCollider.GetComponent<L3antixHealth>();
                    if (l3antixHealth != null)
                    {
                        l3antixHealth.TakeDamage(explosionDamage, knockbackForce, knockbackDirection);
                    }
                }
            }
            // --- END OF EXPLOSION LOGIC ---

            // --- LOGIC FOR SPAWNING THE VISUAL EFFECT ---
            // Only spawn the effect if it's assigned AND an explosion was triggered.
            if (triggerExplosion && explosionEffect != null)
            {
                ObjectPoolManager.Instance.SpawnFromPool(explosionEffect, explosionPosition, Quaternion.identity);
            }
            // --- END OF VISUAL EFFECT LOGIC ---

            // --- FINAL ACTION: RETURN TO POOL ---
            // No matter what happened, disable the projectile to return it to the pool.
            gameObject.SetActive(false);
        }


        void OnDestroy()
        {
            hasBeenDestroyed = true;
        }
    }
}