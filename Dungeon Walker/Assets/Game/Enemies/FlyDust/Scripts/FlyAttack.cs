using UnityEngine;
using System.Collections;

public class FlyAttack : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform; // Reference to the player\"s transform
    public GameObject dustProjectilePrefab; // Prefab of the dust projectile (now just a visual representation)
    public Transform projectileSpawnPoint; // Point from where the projectile will be launched
    public GameObject dustExplosionEffect; // Particle system for explosion

    [Header("Attack Settings")]
    public float attackIntervalMin = 2f; // Minimum time between attacks
    public float attackIntervalMax = 5f; // Maximum time between attacks

    [Header("Player Targeting")]
    public Vector2 playerTargetOffset = Vector2.zero; // Manual offset for targeting the player\"s actual collider/center

    [Header("Projectile Settings")]
    public float projectileSpeed = 10f;
    public int projectileDamage = 10;
    public float projectileLifetime = 3f;
    public LayerMask groundLayer; // Layer for ground obstacles
    public LayerMask playerLayer; // Layer for the player
    public float playerKnockbackForce = 5f; // Force to apply to player on hit
    public Vector2 playerKnockbackDirection = Vector2.up; // Default knockback direction

    [Header("Explosion Settings")]
    public float explosionRadius = 2f; // Radius for the dust explosion damage zone
    public int explosionDamage = 15; // Damage dealt by the dust explosion

    [Header("Double Attack Settings")]
    [Range(0f, 1f)]
    public float doubleAttackChance = 0.3f; // Chance to perform a second attack (0 to 1)
    public float delayBetweenProjectiles = 0.2f; // Delay between first and second projectile
    public Vector2 secondProjectileOffset = new Vector2(0f, 1f); // Offset for the second projectile relative to the player

    // Attack Sound Variables
    public AudioClip attackSoundClip; // Audio clip to play when attacking
    private AudioSource audioSource; // Reference to the AudioSource component

    private Animator animator; // Reference to the Animator component
    private float nextAttackTime; // When the next attack can occur
    private Vector2 lastKnownPlayerPosition; // Store the last known player position

    void Awake()
    {
        // Get or add the AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false; // Ensure it doesn\"t play automatically
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("FlyAttack: Animator not found on this GameObject. Please add one.");
            enabled = false;
        }
        if (playerTransform == null)
        {
            Debug.LogError("FlyAttack: Player Transform not assigned. Please assign the player\"s Transform in the Inspector.");
            enabled = false;
        }
        if (dustProjectilePrefab == null)
        {
            Debug.LogError("FlyAttack: Dust Projectile Prefab not assigned. Please assign the DustProjectile prefab in the Inspector. This prefab should only contain visual components and a Collider2D set to Is Trigger.");
            enabled = false;
        }
        if (projectileSpawnPoint == null)
        {
            Debug.LogError("FlyAttack: Projectile Spawn Point not assigned. Please create an empty GameObject as a child and assign it.");
            enabled = false;
        }
        if (dustExplosionEffect == null)
        {
            Debug.LogWarning("FlyAttack: Dust Explosion Effect not assigned. No explosion effect will play on projectile destruction.");
        }

        SetNextAttackTime();
    }

    void Update()
    {
        if (playerTransform != null)
        {
            lastKnownPlayerPosition = (Vector2)playerTransform.position + playerTargetOffset; // Continuously update last known position with offset
        }

        if (Time.time >= nextAttackTime)
        {
            // Trigger attack animation
            if (animator != null)
            {
                animator.SetTrigger("Attack"); // Assuming you have an \"Attack\" trigger in your Animator
            }
            SetNextAttackTime();
        }
    }

    // This method will be called by an Animation Event at the exact frame the dust should be thrown
    public void ThrowDust()
    {
        Debug.Log("ThrowDust method called.");

        if (dustProjectilePrefab == null || projectileSpawnPoint == null || playerTransform == null)
        {
            Debug.LogError("FlyAttack: Missing references for ThrowDust. Check dustProjectilePrefab, projectileSpawnPoint, or playerTransform.");
            return;
        }

        // Play attack sound if assigned
        if (attackSoundClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSoundClip);
        }

        StartCoroutine(ThrowDustCoroutine());
    }

    private IEnumerator ThrowDustCoroutine()
    {
        // First projectile
        InstantiateAndInitializeProjectile(lastKnownPlayerPosition);

        // Check for double attack chance
        if (Random.value < doubleAttackChance)
        {
            yield return new WaitForSeconds(delayBetweenProjectiles);
            // Second projectile with offset
            InstantiateAndInitializeProjectile(lastKnownPlayerPosition + secondProjectileOffset);
        }
    }

    private void InstantiateAndInitializeProjectile(Vector2 targetPlayerPosition)
    {
        Debug.Log("All references are valid. Instantiating projectile...");

        // Calculate predicted player position
        Vector2 predictedPlayerPosition = targetPlayerPosition;
        Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();

        if (playerRb != null)
        {
            // Estimate time to reach player (simple estimation)
            float distanceToPlayer = Vector2.Distance(projectileSpawnPoint.position, targetPlayerPosition);
            float timeToReachPlayer = distanceToPlayer / projectileSpeed;

            // Predict player\"s future position
            predictedPlayerPosition += playerRb.velocity * timeToReachPlayer;
            Debug.Log($"Predicted player position: {predictedPlayerPosition}");
        }
        else
        {
            Debug.LogWarning("Player Rigidbody2D not found. Cannot predict player movement. Targeting last known position.");
        }

        // Instantiate the projectile at the spawn point
        GameObject dust = Instantiate(dustProjectilePrefab, projectileSpawnPoint.position, Quaternion.identity);

        // Add Rigidbody2D if not present (should be on prefab, but as a fallback)
        Rigidbody2D dustRb = dust.GetComponent<Rigidbody2D>();
        if (dustRb == null)
        {
            dustRb = dust.AddComponent<Rigidbody2D>();
            dustRb.gravityScale = 0; // Dust projectiles usually don\"t have gravity
            dustRb.isKinematic = true; // Controlled by script
        }

        // Add a Collider2D if not present (should be on prefab, but as a fallback)
        Collider2D dustCollider = dust.GetComponent<Collider2D>();
        if (dustCollider == null)
        {
            CapsuleCollider2D capsule = dust.AddComponent<CapsuleCollider2D>();
            capsule.isTrigger = true;
            Debug.LogWarning("FlyAttack: DustProjectile prefab missing Collider2D. Added CapsuleCollider2D as fallback. Please add one to the prefab.");
        }
        else
        {
            dustCollider.isTrigger = true; // Ensure it\"s a trigger for collision detection
        }

        // Add the ProjectileController to the instantiated dust GameObject
        ProjectileController projectileController = dust.AddComponent<ProjectileController>();
        projectileController.InitializeProjectile(
            predictedPlayerPosition,
            projectileSpeed,
            projectileDamage,
            projectileLifetime,
            groundLayer,
            playerLayer,
            dustExplosionEffect,
            playerKnockbackForce,
            playerKnockbackDirection,
            explosionRadius,
            explosionDamage
        );

        Debug.Log("Projectile instantiated and initialized.");
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
        private bool hasBeenDestroyed = false; // Flag to prevent multiple destructions

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
                Debug.LogError("ProjectileController: Rigidbody2D not found on this GameObject. Cannot move projectile.");
                enabled = false;
                return;
            }

            // Calculate direction from the projectile\"s current position to the target position
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            rb.velocity = direction * speed;
            Debug.Log($"ProjectileController: Initialized with target {targetPosition} and velocity {rb.velocity}");

            // Start lifetime countdown
            StartCoroutine(LifetimeCountdown());
        }

        private IEnumerator LifetimeCountdown()
        {
            yield return new WaitForSeconds(lifetime);
            if (!hasBeenDestroyed)
            {
                Debug.Log("ProjectileController: Lifetime ended. Destroying projectile.");
                DestroyProjectile(transform.position, true); // Trigger explosion on lifetime end
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (hasBeenDestroyed) return; // Prevent multiple calls if already destroying

            Debug.Log($"ProjectileController: Trigger detected with {other.gameObject.name} on layer {other.gameObject.layer}");

            // Check for collision with Ground
            if (((1 << other.gameObject.layer) & groundLayer) != 0)
            {
                Debug.Log($"ProjectileController: Hit ground layer: {other.gameObject.name}");
                DestroyProjectile(transform.position, true); // Trigger explosion on ground hit
            }
            // Check for collision with Player (direct hit)
            else if (((1 << other.gameObject.layer) & playerLayer) != 0)
            {
                Debug.Log($"ProjectileController: Hit player layer: {other.gameObject.name}");
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage, knockbackForce, knockbackDirection);
                    Debug.Log($"PlayerHealth found. Applied {damage} damage with knockback force {knockbackForce} and direction {knockbackDirection}.");
                }
                else
                {
                    Debug.LogWarning("PlayerHealth component not found on player object. Make sure the player has a PlayerHealth script attached.");
                }
                DestroyProjectile(transform.position, false); // No explosion on direct player hit, as damage is already applied
            }
        }

        public void DestroyProjectile(Vector2 explosionPosition, bool triggerExplosion)
        {
            if (hasBeenDestroyed) return; // Prevent multiple destructions
            hasBeenDestroyed = true;

            Debug.Log("ProjectileController: Destroying projectile and playing explosion effect.");

            if (triggerExplosion)
            {
                // Perform explosion damage in an area
                Collider2D[] hitColliders = Physics2D.OverlapCircleAll(explosionPosition, explosionRadius, playerLayer);
                foreach (Collider2D hitCollider in hitColliders)
                {
                    PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(explosionDamage, knockbackForce, knockbackDirection); // Use explosionDamage
                        Debug.Log($"Explosion hit {hitCollider.gameObject.name}. Applied {explosionDamage} damage with knockback force {knockbackForce} and direction {knockbackDirection}.");
                    }
                }
            }

            // Play explosion effect
            if (explosionEffect != null)
            {
                GameObject explosionInstance = Instantiate(explosionEffect, explosionPosition, Quaternion.identity);
                ParticleSystem ps = explosionInstance.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                    // Destroy the particle system GameObject after its duration
                    Destroy(explosionInstance, ps.main.duration + ps.main.startLifetime.constantMax + 0.1f);
                }
                else
                {
                    Debug.LogWarning("ExplosionEffect does not have a ParticleSystem component. Destroying after a default time.");
                    Destroy(explosionInstance, 3f); // Default destroy if no PS found
                }
            }
            else
            {
                Debug.LogWarning("ExplosionEffect is null. No explosion effect will play.");
            }
            // Destroy the projectile GameObject itself
            Destroy(gameObject);
        }

        // OnDestroy is called when the GameObject is destroyed
        void OnDestroy()
        {
            // Ensure the projectile is marked as destroyed if it\"s destroyed by other means (e.g., scene unload)
            hasBeenDestroyed = true;
        }
    }
}

