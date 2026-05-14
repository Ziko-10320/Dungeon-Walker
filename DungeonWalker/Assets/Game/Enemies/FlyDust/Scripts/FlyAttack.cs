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
    public int baseProjectileDamage = 10;
    public int projectileDamage;
    public float projectileLifetime = 3f;
    public LayerMask groundLayer;
    public LayerMask playerLayer;
    public float playerKnockbackForce = 5f;
    public Vector2 playerKnockbackDirection = Vector2.up;

    [Header("Attack Range")]
    [Tooltip("The maximum distance from the player at which the fly will attempt to attack.")]
    [SerializeField] private float attackRange = 15f;
    [Tooltip("An optional transform to define the center of the attack range. If empty, the fly's own position is used.")]
    [SerializeField] private Transform rangeOriginPoint;

    [Header("Explosion Settings")]
    public float explosionRadius = 2f;
    public int baseExplosionDamage = 15;
    public int explosionDamage;

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
    public int baseChargedProjectileDamage = 30;
    public int chargedProjectileDamage;
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


    private WaitForSeconds delayBetweenProjectilesWait;
    private WaitForSeconds projectileLifetimeWait;
  
    private PlayerInvisibility playerInvisibility;
    private PlayerInvisibility3antix playerInvisibility3antix;
    private int frameCounter = 0;
    private int updateRate = 1;

    [Header("Local Pooling Settings")]
    [Tooltip("How many normal projectiles to keep ready.")]
    public int normalProjectilePoolSize = 10;
    [Tooltip("How many normal destruction effects to keep ready.")]
    public int destructionEffectPoolSize = 10;
    [Tooltip("How many charged projectiles to keep ready (if V2).")]
    public int chargedProjectilePoolSize = 3;
    [Tooltip("How many charged destruction effects to keep ready (if V2).")]
    public int chargedEffectPoolSize = 3;

    // The queues for our local pools
    private Queue<GameObject> normalProjectilePool;
    private Queue<GameObject> destructionEffectPool;
    private Queue<GameObject> chargedProjectilePool;
    private Queue<GameObject> chargedEffectPool;
    private Queue<GameObject> anticipationParticlesPool;
    // A parent object to keep the hierarchy clean
    private Transform poolParent;

    void OnEnable()
    {
        // --- 1. GET THE TIER MULTIPLIER ---
        float tierMultiplier = 1.0f;
        if (StatMultiplierManager.Instance != null)
        {
            tierMultiplier = StatMultiplierManager.Instance.FlyMultiplier;
        }

        // --- 2. GET WAVE SCALING INFO FROM HEALTH SCRIPT ---
        int wavesSurvived = 0;
        int waveDamageBonus = 0;
        FlyHealth healthScript = GetComponent<FlyHealth>();
        if (healthScript != null)
        {
            if (healthScript.firstSpawnWave != -1)
            {
                wavesSurvived = ScoreDisplay.CurrentWaveNumber - healthScript.firstSpawnWave;
                if (wavesSurvived < 0) wavesSurvived = 0;
            }
            waveDamageBonus = wavesSurvived * healthScript.damageIncreasePerWave;
        }

        // --- 3. APPLY ALL SCALING ---
        projectileDamage = Mathf.RoundToInt(baseProjectileDamage * tierMultiplier) + waveDamageBonus;
        explosionDamage = Mathf.RoundToInt(baseExplosionDamage * tierMultiplier) + waveDamageBonus;
        // Also scale the V2 charged attack if it exists, giving it a bigger bonus
        chargedProjectileDamage = Mathf.RoundToInt(baseChargedProjectileDamage * tierMultiplier) + (waveDamageBonus * 2);
    }
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
        if (playerTransform != null)
        {
            playerInvisibility = playerTransform.GetComponent<PlayerInvisibility>();
            playerInvisibility3antix = playerTransform.GetComponent<PlayerInvisibility3antix>();
        }

        animator = GetComponent<Animator>();
        SetNextAttackTime();
     
        StopAllCoroutines();
        StartCoroutine(UpdateAI_LOD_Routine());
    }

    void Start()
    {

        poolParent = new GameObject(gameObject.name + "_Pool").transform;
        poolParent.SetParent(this.transform);

        // Initialize all the pools
        normalProjectilePool = CreatePool(dustProjectilePrefab, normalProjectilePoolSize);
        destructionEffectPool = CreatePool(dustExplosionEffect, destructionEffectPoolSize);

        if (canPerformChargedAttack)
        {
            chargedProjectilePool = CreatePool(chargedProjectilePrefab, chargedProjectilePoolSize);
            chargedEffectPool = CreatePool(chargedExplosionEffect, chargedEffectPoolSize);
            if (anticipationParticles != null)
            {
                anticipationParticlesPool = CreatePool(anticipationParticles.gameObject, 2); // A small pool of 2 is enough
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
    private Queue<GameObject> CreatePool(GameObject prefab, int size)
    {
        if (prefab == null) return null; // Safety check

        Queue<GameObject> newPool = new Queue<GameObject>();
        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab, poolParent);
            obj.SetActive(false);
            newPool.Enqueue(obj);
        }
        return newPool;
    }
    void Update()
    {
        frameCounter++;
        if (frameCounter < updateRate)
        {
            return; // SKIP THIS FRAME!
        }
        frameCounter = 0;

        bool playerInvisible = (playerInvisibility != null && playerInvisibility.IsInvisible()) ||
                              (playerInvisibility3antix != null && playerInvisibility3antix.IsInvisible());
        // Stop attack if player invisible
        if (playerInvisible)
        {
            animator.ResetTrigger("Attack"); // stop throw animation
            return;
        }

        lastKnownPlayerPosition = (Vector2)playerTransform.position + playerTargetOffset;

        if (Time.time >= nextAttackTime)
        {
            // --- THE FIX: Check if the player is in range BEFORE attacking ---
            if (IsPlayerInRange())
            {
                if (animator != null)
                {
                    animator.SetTrigger("Attack");
                }
                SetNextAttackTime();
            }
            // If player is not in range, we simply do nothing and wait for the next attack check.
        }

       

    }

    private IEnumerator UpdateAI_LOD_Routine()
    {
        // Create the wait object once to be efficient.
        WaitForSeconds wait = new WaitForSeconds(0.5f);

        // This loop will run as long as the component is active.
        while (true)
        {
            // First, check for all required components. If any are missing, wait and try again.
            if (playerTransform == null || AI_LOD_Manager.Instance == null)
            {
                Debug.LogWarning("FlyAttack LOD: Waiting for player or manager...");
                yield return wait;
                continue; // Skip the rest of this loop iteration and try again after the wait.
            }

            // If we get here, everything exists. Now we can do the logic.
            float dist = Vector2.Distance(transform.position, playerTransform.position);

            if (dist > AI_LOD_Manager.Instance.lowPriorityRange)
            {
                updateRate = AI_LOD_Manager.Instance.lowPriorityUpdateRate;
            }
            else if (dist > AI_LOD_Manager.Instance.midPriorityRange)
            {
                updateRate = AI_LOD_Manager.Instance.midPriorityUpdateRate;
            }
            else
            {
                updateRate = 1;
            }

            // --- THIS IS THE GUARANTEED FIX ---
            // The 'yield return' is now OUTSIDE of the if/else blocks.
            // This means the coroutine will ALWAYS pause here, preventing the infinite loop.
            yield return wait;
            // --- END OF FIX ---
        }
    }


    private bool IsPlayerInRange()
    {
        // If we don't have a reference to the player, they can't be in range.
        if (playerTransform == null)
        {
            return false;
        }

        // Determine the origin point for our range check.
        Vector3 origin = (rangeOriginPoint != null) ? rangeOriginPoint.position : transform.position;

        // Calculate the distance and check it against our attackRange.
        float distanceToPlayer = Vector2.Distance(origin, playerTransform.position);

        return distanceToPlayer <= attackRange;
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
    public void SetTutorialMode()
    {
        Debug.Log("FlyAttack has been set to Tutorial Mode. Damage will be 0.");
        this.projectileDamage = 0;
        this.explosionDamage = 0;
        this.chargedProjectileDamage = 0;
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

        SpawnProjectile(true, lastKnownPlayerPosition, projectileToScale);
    }


    private IEnumerator ThrowDustCoroutine()
    {
        // Spawn the first projectile
        SpawnProjectile(false, lastKnownPlayerPosition);

        // Check for a double attack
        if (Random.value < doubleAttackChance)
        {
            yield return delayBetweenProjectilesWait;
            SpawnProjectile(false, lastKnownPlayerPosition + secondProjectileOffset);
        }
    }


    private void SpawnProjectile(bool isCharged, Vector2 targetPosition, GameObject preSpawnedChargedProjectile = null)
    {
        GameObject projectile;
        Queue<GameObject> sourcePool;
        float speed;    // Declared once here
        float lifetime; // Declared once here

        if (isCharged)
        {
            projectile = preSpawnedChargedProjectile;
            sourcePool = chargedProjectilePool;

            // THE FIX: No "float" here. We are ASSIGNING to the variables from above.
            speed = this.chargedProjectileSpeed;
            lifetime = this.chargedProjectileLifetime;
        }
        else
        {
            sourcePool = normalProjectilePool;
            if (sourcePool == null || sourcePool.Count == 0) return;
            projectile = sourcePool.Dequeue();
            projectile.transform.position = projectileSpawnPoint.position;

            // THE FIX: No "float" here either.
            speed = this.projectileSpeed;
            lifetime = this.projectileLifetime;
        }

        if (projectile == null) return;

        projectile.SetActive(true);

        var controller = projectile.GetComponent<PooledProjectileController>() ?? projectile.AddComponent<PooledProjectileController>();

        // Now this line can see the 'speed' and 'lifetime' variables correctly.
        controller.Initialize(this, isCharged, targetPosition, speed, lifetime);

        sourcePool.Enqueue(projectile);
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

    public void HandleProjectileCollision(GameObject projectile, GameObject hitObject, bool isCharged)
    {
        // Stop the coroutine and disable the projectile immediately to return it to the pool
        projectile.GetComponent<PooledProjectileController>().StopAllCoroutines();
        projectile.SetActive(false);

        // Determine which explosion effect and stats to use
        Queue<GameObject> effectPool = isCharged ? chargedEffectPool : destructionEffectPool;
        int directDamage = isCharged ? chargedProjectileDamage : projectileDamage;
        int areaDamage = isCharged ? chargedProjectileDamage : explosionDamage;
        float explosionRadius = isCharged ? chargedExplosionRadius : this.explosionRadius;

        // Spawn the visual explosion from the correct pool
        if (effectPool != null && effectPool.Count > 0)
        {
            GameObject explosion = effectPool.Dequeue();
            explosion.transform.position = projectile.transform.position;
            explosion.SetActive(true);
            effectPool.Enqueue(explosion);
        }

        // Apply direct damage if we hit a player
        if (hitObject != null && ((1 << hitObject.layer) & playerLayer) != 0)
        {
            if (hitObject.TryGetComponent<PlayerHealth>(out var ph)) ph.TakeDamage(directDamage, playerKnockbackForce, playerKnockbackDirection);
            if (hitObject.TryGetComponent<L3antixHealth>(out var lh)) lh.TakeDamage(directDamage, playerKnockbackForce, playerKnockbackDirection);
        }

        // Apply area of effect (AoE) damage
        Collider2D[] playersInExplosion = Physics2D.OverlapCircleAll(projectile.transform.position, explosionRadius, playerLayer);
        foreach (var player in playersInExplosion)
        {
            // Don't apply AoE damage to the player who was directly hit
            if (player.gameObject == hitObject) continue;

            if (player.TryGetComponent<PlayerHealth>(out var pHealth)) pHealth.TakeDamage(areaDamage, playerKnockbackForce, playerKnockbackDirection);
            if (player.TryGetComponent<L3antixHealth>(out var lHealth)) lHealth.TakeDamage(areaDamage, playerKnockbackForce, playerKnockbackDirection);
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

    // This method is automatically called by Unity in the Scene view.
    private void OnDrawGizmosSelected()
    {
        // Determine the origin point for our gizmo.
        Vector3 origin = (rangeOriginPoint != null) ? rangeOriginPoint.position : transform.position;

        // Draw a yellow wireframe circle representing the attack range.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, attackRange);
    }

}