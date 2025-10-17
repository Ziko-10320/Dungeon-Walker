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

    [Header("V2 Charged Attack")]
    [Tooltip("If true, this fly can perform the charged projectile attack.")]
    public bool canPerformChargedAttack = false;

    [Tooltip("How many normal attacks to perform before a charged attack.")]
    public int attacksBeforeCharge = 2;

    [Header("Charged Projectile Settings")]
    public GameObject chargedProjectilePrefab; // Assign your big projectile prefab here
    public float chargedProjectileSpeed = 8f;
    public int chargedProjectileDamage = 30;
    public float chargedProjectileLifetime = 4f;
    public float chargedExplosionRadius = 3f;
    public GameObject chargedExplosionEffect; // Assign the big explosion effect here

    [Header("Charged Attack Effects")]
    public float anticipationDuration = 1.0f;
    public ParticleSystem anticipationParticles; // Assign the charging particles here
    public AudioClip chargeUpSound;
    public AudioClip chargedAttackSound;
    private int attackCounter = 0;
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
    private Queue<GameObject> chargedProjectilePool;
    private Queue<GameObject> chargedExplosionPool;
    private Queue<GameObject> anticipationParticlesPool;
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
        if (canPerformChargedAttack)
        {
            // Pool for Charged Projectiles
            if (chargedProjectilePrefab != null)
            {
                chargedProjectilePool = new Queue<GameObject>();
                for (int i = 0; i < 5; i++) // Create a small pool of 5
                {
                    GameObject obj = Instantiate(chargedProjectilePrefab, projectilePoolParent.transform);
                    obj.SetActive(false);
                    chargedProjectilePool.Enqueue(obj);
                }
            }

            // Pool for Charged Explosions
            if (chargedExplosionEffect != null)
            {
                chargedExplosionPool = new Queue<GameObject>();
                for (int i = 0; i < 5; i++)
                {
                    GameObject obj = Instantiate(chargedExplosionEffect, projectilePoolParent.transform);
                    obj.SetActive(false);
                    chargedExplosionPool.Enqueue(obj);
                }
            }

            // Pool for Anticipation Particles
            if (anticipationParticles != null)
            {
                anticipationParticlesPool = new Queue<GameObject>();
                for (int i = 0; i < 2; i++)
                {
                    GameObject obj = Instantiate(anticipationParticles.gameObject, projectilePoolParent.transform);
                    obj.SetActive(false);
                    anticipationParticlesPool.Enqueue(obj);
                }
            }
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
            if (projectile == null || !projectile.activeInHierarchy)
            {
                activeProjectiles.RemoveAt(i);
                continue;
            }

            // --- THE CRITICAL FIX: HOW WE IDENTIFY A CHARGED PROJECTILE ---
            // We check if the projectile's NAME contains the name of the charged prefab.
            // This is a reliable way to know which type it is.
            bool isCharged = false;
            if (canPerformChargedAttack && chargedProjectilePrefab != null)
            {
                isCharged = projectile.name.Contains(chargedProjectilePrefab.name);
            }
            // --- END OF CRITICAL FIX ---

            // Use the collider's bounds to check for collision. This respects the scale.
            Collider2D projCollider = projectile.GetComponent<Collider2D>();
            if (projCollider == null) continue;
            Collider2D playerHit = Physics2D.OverlapBox(projCollider.bounds.center, projCollider.bounds.size, 0f, playerLayer);
            Collider2D groundHit = Physics2D.OverlapBox(projCollider.bounds.center, projCollider.bounds.size, 0f, groundLayer);

            bool hasCollided = (playerHit != null) || (groundHit != null);

            if (hasCollided)
            {
                // --- APPLY THE CORRECT STATS BASED ON 'isCharged' ---
                int directDamage = isCharged ? chargedProjectileDamage : projectileDamage;
                int explosionDmg = isCharged ? chargedProjectileDamage : explosionDamage; // Use charged damage for AoE
                float explosionRad = isCharged ? chargedExplosionRadius : explosionRadius;
                if (isCharged)
                {
                    // Use the LOCAL charged explosion pool
                    if (chargedExplosionPool != null && chargedExplosionPool.Count > 0)
                    {
                        GameObject explosion = chargedExplosionPool.Dequeue();
                        chargedExplosionPool.Enqueue(explosion);
                        explosion.transform.position = projectile.transform.position;
                        explosion.SetActive(true);
                    }
                }
                else
                {
                    // Use the GLOBAL normal explosion pool (your original logic)
                    if (dustExplosionEffect != null)
                    {
                        ObjectPoolManager.Instance.SpawnFromPool(dustExplosionEffect, projectile.transform.position, Quaternion.identity);
                    }
                }

                // --- 1. Apply DIRECT damage if we hit a player ---
                if (playerHit != null)
                {
                    PlayerHealth ph = playerHit.GetComponent<PlayerHealth>();
                    if (ph != null) ph.TakeDamage(directDamage, playerKnockbackForce, playerKnockbackDirection);
                    L3antixHealth lh = playerHit.GetComponent<L3antixHealth>();
                    if (lh != null) lh.TakeDamage(directDamage, playerKnockbackForce, playerKnockbackDirection);
                }

           

                // --- 3. Apply AREA damage with the CORRECT radius and damage ---
                Collider2D[] playersInExplosion = Physics2D.OverlapCircleAll(projectile.transform.position, explosionRad, playerLayer);
                foreach (var player in playersInExplosion)
                {
                    if (player == playerHit) continue; // Don't double-damage
                    PlayerHealth pHealth = player.GetComponent<PlayerHealth>();
                    if (pHealth != null) pHealth.TakeDamage(explosionDmg, playerKnockbackForce, playerKnockbackDirection);
                    L3antixHealth lHealth = player.GetComponent<L3antixHealth>();
                    if (lHealth != null) lHealth.TakeDamage(explosionDmg, playerKnockbackForce, playerKnockbackDirection);
                }

                // --- 4. Return the projectile to the pool ---
                projectile.SetActive(false);
                activeProjectiles.RemoveAt(i);
            }
        }

    }

    public void ThrowDust()
    {
        if (playerTransform == null) return;

        PlayerInvisibility invis = playerTransform.GetComponent<PlayerInvisibility>();
        if (invis != null && invis.IsInvisible()) return;
        PlayerInvisibility3antix invis3antix = playerTransform.GetComponent<PlayerInvisibility3antix>();
        if (invis3antix != null && invis3antix.IsInvisible()) return;

        // --- THIS IS THE NEW "BRAIN" LOGIC ---
        // Check if this is a V2 fly and if it's time for a charged shot.
        if (canPerformChargedAttack && attackCounter >= attacksBeforeCharge)
        {
            // It's time for a big one!
            StartCoroutine(PerformChargedAttackRoutine());
            attackCounter = 0; // Reset the counter.
        }
        else
        {
            // Just a normal attack. This calls your original, working coroutine.
            if (attackSoundClip != null) audioSource.PlayOneShot(attackSoundClip, attackSoundVolume);
            StartCoroutine(ThrowDustCoroutine());
            attackCounter++; // Increment the counter.
        }
        // --- END OF NEW LOGIC ---
    }
    private IEnumerator PerformChargedAttackRoutine()
    {
        // --- 1. PRE-SPAWN THE PROJECTILE AND START ANTICIPATION ---
        GameObject anticipationInstance = null;
        if (anticipationParticlesPool != null && anticipationParticlesPool.Count > 0)
        {
            anticipationInstance = anticipationParticlesPool.Dequeue();
            anticipationParticlesPool.Enqueue(anticipationInstance);
            anticipationInstance.transform.position = projectileSpawnPoint.position;
            anticipationInstance.transform.rotation = projectileSpawnPoint.rotation;
            anticipationInstance.transform.SetParent(projectileSpawnPoint);
            anticipationInstance.SetActive(true);
        }

        if (chargedProjectilePool == null || chargedProjectilePool.Count == 0) yield break;
        GameObject projectileToScale = chargedProjectilePool.Dequeue();
        chargedProjectilePool.Enqueue(projectileToScale);

        // --- THIS IS THE FIX FOR THE SCALING BUG ---
        // Store the projectile's original scale from its prefab.
        Vector3 originalScale = projectileToScale.transform.localScale;
        // -------------------------------------------

        projectileToScale.transform.position = projectileSpawnPoint.position;
        projectileToScale.transform.rotation = projectileSpawnPoint.rotation;
        projectileToScale.transform.localScale = originalScale * 0.05f; // Start at 5% of its original scale
        projectileToScale.SetActive(true);

        // --- THIS IS THE FIX FOR THE FALLING BUG ---
        Rigidbody2D projRb = projectileToScale.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            // Make the Rigidbody kinematic. It will ignore gravity and all forces.
            // It will be completely frozen in place.
            projRb.isKinematic = true;
        }
        // -----------------------------------------

        if (chargeUpSound != null) audioSource.PlayOneShot(chargeUpSound, attackSoundVolume);

        // --- 2. THE SCALING ANIMATION LOOP ---
        float scaleTimer = 0f;
        float scaleDuration = 1.0f; // The scaling will now ALWAYS take 1 second.
        while (scaleTimer < scaleDuration)
        {
            float progress = scaleTimer / scaleDuration;
            // Lerp from 5% of original scale to 100% of original scale.
            projectileToScale.transform.localScale = Vector3.Lerp(originalScale * 0.05f, originalScale, progress);
            scaleTimer += Time.deltaTime;
            yield return null;
        }

        // Ensure it ends at exactly its original scale.
        projectileToScale.transform.localScale = originalScale;

        // If anticipation is longer than scaling, wait for the remaining time.
        if (anticipationDuration > scaleDuration)
        {
            yield return new WaitForSeconds(anticipationDuration - scaleDuration);
        }

        // --- 3. CLEANUP AND FIRE ---
        if (anticipationInstance != null)
        {
            anticipationInstance.transform.SetParent(null);
            anticipationInstance.SetActive(false);
        }

        if (chargedAttackSound != null) audioSource.PlayOneShot(chargedAttackSound, attackSoundVolume);

        if (projRb != null)
        {
            // --- RE-ENABLE PHYSICS BEFORE FIRING ---
            // Switch it back to a dynamic Rigidbody so it can be moved by physics again.
            projRb.isKinematic = false;
            // ------------------------------------

            Vector2 direction = (lastKnownPlayerPosition - (Vector2)projectileToScale.transform.position).normalized;
            projRb.velocity = direction * chargedProjectileSpeed;
            projRb.AddTorque(Random.Range(-200f, 200f));
        }

        activeProjectiles.Add(projectileToScale);
        StartCoroutine(LifetimeCountdown(projectileToScale, chargedProjectileLifetime));
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
        // This method now ONLY handles the normal projectile.
        if (projectilePool == null || projectilePool.Count == 0) return;
        GameObject projectileToSpawn = projectilePool.Dequeue();
        projectilePool.Enqueue(projectileToSpawn);

        projectileToSpawn.transform.position = projectileSpawnPoint.position;
        projectileToSpawn.SetActive(true);

        activeProjectiles.Add(projectileToSpawn);

        Rigidbody2D projRb = projectileToSpawn.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            Vector2 direction = (targetPlayerPosition - (Vector2)projectileToSpawn.transform.position).normalized;
            projRb.velocity = direction * projectileSpeed;
        }

        StartCoroutine(LifetimeCountdown(projectileToSpawn, projectileLifetime));
    }


    // We need a single, clear LifetimeCountdown method.
    private IEnumerator LifetimeCountdown(GameObject projectile, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        if (projectile != null && projectile.activeInHierarchy)
        {
            // When lifetime expires, just disable it. The Update loop will handle the explosion.
            projectile.SetActive(false);
        }
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