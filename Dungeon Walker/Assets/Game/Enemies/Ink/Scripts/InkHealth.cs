using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class InkHealth : MonoBehaviour
{
    // Public variables for health and effects
    public int maxHealth = 100; // Maximum health of the ink enemy
    public GameObject deathEffect; // Optional: Effect to play when the ink enemy dies
    public float knockbackDistance = 1f; // Distance the ink enemy moves during knockback
    public float knockbackDuration = 0.2f; // Duration of the knockback effect
    public Transform bloodSpawnPoint; // Spawn point for blood particles
    public ParticleSystem bloodParticle; // Blood particle system

    public ParticleSystem DeathInkParticules;
    public ParticleSystem DeathInkParticules2;
    public ParticleSystem DeathInkParticules3;
    public ParticleSystem DeathInkParticules4;
    public ParticleSystem DeathInkParticules5;

    public Transform DeathInkSpawn;
    public Transform DeathInkSpawn2;
    public Transform DeathInkSpawn3;
    public Transform DeathInkSpawn4;
    public Transform DeathInkSpawn5;

    // Flash Damage Variables
    public Material flashMaterial; // Material with the flash shader
    public string flashAmountProperty = "_FlashAmount"; // Name of the Flash Amount property in the shader
    public float flashDuration = 0.2f; // Duration of the flash effect

    // Array of SpriteRenderers for the parts of the ink enemy
    public SpriteRenderer[] spriteRenderers;
    private Material[] originalMaterials;
    // Damage Sound Variables
    public AudioClip damageSoundClip; // Audio clip to play when taking damage
    [Range(0f, 1f)]
    public float damageSoundVolume = 1f; // Volume slider for damage sound

    [Header("Invincibility System")]
    [Tooltip("Enable the invincibility system")]
    public bool enableInvincibilitySystem = true;

    [Tooltip("Minimum time between invincibility activations (seconds)")]
    public float minInvincibilityInterval = 2f;

    [Tooltip("Maximum time between invincibility activations (seconds)")]
    public float maxInvincibilityInterval = 5f;

    [Tooltip("Duration of invincibility state (seconds)")]
    public float invincibilityDuration = 3f;

    [Tooltip("Chance of activating invincibility when interval is reached (0-1)")]
    [Range(0f, 1f)]
    public float invincibilityActivationChance = 0.8f;

    [Tooltip("Should invincibility start automatically on game start?")]
    public bool startWithInvincibility = false;

    [Tooltip("Delay before first invincibility check (seconds)")]
    public float initialInvincibilityDelay = 1f;

    [Header("Invincibility Animation Control")]
    [Tooltip("Animator component for controlling hide/show animations")]
    public Animator inkAnimator;

    [Tooltip("Animation trigger name for hiding (entering invincibility)")]
    public string hideAnimationTrigger = "Hide";

    [Tooltip("Animation trigger name for showing (exiting invincibility)")]
    public string showAnimationTrigger = "Show";

    [Tooltip("Animation state name for the hide animation")]
    public string hideAnimationStateName = "InkHide";

    [Tooltip("Animation state name for the show animation")]
    public string showAnimationStateName = "InkShow";

    [Tooltip("Should the enemy stay hidden until invincibility ends?")]
    public bool stayHiddenDuringInvincibility = true;

    [Tooltip("Time to wait for hide animation to complete before becoming invincible")]
    public float hideAnimationWaitTime = 0.5f;

    [Tooltip("Time to wait for show animation to complete")]
    public float showAnimationWaitTime = 0.5f;

    [Header("Invincibility Collision Control")]
    [Tooltip("Colliders to disable during invincibility")]
    public Collider2D[] collidersToDisable;

    [Tooltip("Should all colliders be automatically found and disabled?")]
    public bool autoFindColliders = true;

    [Tooltip("Rigidbody2D to make kinematic during invincibility")]
    public Rigidbody2D inkRigidbody;

    [Tooltip("Should rigidbody be automatically found?")]
    public bool autoFindRigidbody = true;

    [Header("Invincibility Visual Effects")]
    [Tooltip("Should sprite renderers be hidden during invincibility?")]
    public bool hideSpritesDuringInvincibility = true;

    [Tooltip("Alpha value for sprites during invincibility (0 = invisible, 1 = visible)")]
    [Range(0f, 1f)]
    public float invincibilityAlpha = 0f;

    [Tooltip("Particle system for ink dripping effect (for animation events) - plays once")]
    public ParticleSystem InkDripping;

    [Header("Invincibility Audio")]
    [Tooltip("Sound to play when entering invincibility")]
    public AudioClip invincibilityStartSound;

    [Tooltip("Sound to play when exiting invincibility")]
    public AudioClip invincibilityEndSound;

    [Tooltip("Volume for invincibility sounds")]
    [Range(0f, 1f)]
    public float invincibilitySoundVolume = 1f;

    [Header("Invincibility Debug")]
    [Tooltip("Show debug messages for invincibility system")]
    public bool showInvincibilityDebug = false;

    public WeaponSwitchManager weaponSwitchManager;
    public UnityEvent<GameObject> OnDeath;
    [Tooltip("Show invincibility state in inspector (read-only)")]
    [SerializeField] private bool isCurrentlyInvincible = false;

    [Tooltip("Time until next invincibility check (read-only)")]
    [SerializeField] private float timeUntilNextInvincibilityCheck = 0f;

    [Tooltip("Current invincibility timer (read-only)")]
    [SerializeField] private float currentInvincibilityTimer = 0f;

    // Private variables
    [HideInInspector]
    public int currentHealth;
    private bool isKnockedBack = false; // Is the ink enemy currently being knocked back?
    private bool isFlashing = false; // Added to prevent multiple flash coroutines
    private AudioSource audioSource; // Reference to the AudioSource component

    // Invincibility private variables
    private bool isInvincible = false;
    private bool isInInvincibilityTransition = false; // Prevents multiple invincibility coroutines
    private Coroutine invincibilityCoroutine;
    private Coroutine invincibilityTimerCoroutine;
    private float nextInvincibilityCheckTime;
    private Color[] originalSpriteColors; // Store original sprite colors

    //CameraShake
    public ShakeData CameraShakeDeath;
    public bool isStunned = false;

    [Header("Ground Check Settings")]
    [Tooltip("The transform representing the point to check for ground from.")]
    public Transform groundCheck;
    [Tooltip("The radius of the circle used to check for ground.")]
    public float groundCheckRadius = 0.2f;
    [Tooltip("Which layers should be considered 'ground'.")]
    public LayerMask groundLayer;

    [Header("V2 Puddle Attack")]
    [Tooltip("If true, this Ink enemy will create a damage puddle when it lands.")]
    public bool isV2PuddleAttack = false;

    [Tooltip("The Animator that controls the ink dripping animation.")]
    public Animator puddleAnimator; // Use a separate animator for the puddle effect
    public GameObject puddlePrefab;
    public Transform puddleSpawnPoint;
    [Tooltip("The name of the animation state for the ink dripping.")]
    public string inkDripAnimationName = "InkDrip";

    [Tooltip("The GameObject holding the looping smoke particle effects.")]
    public GameObject smokeParticles;

    [Tooltip("The transform where the damage area will be centered.")]
    public Transform damagePoint;
    public LayerMask playerLayer;
    [Tooltip("The size of the damage area (Width, Height).")]
    public Vector2 damageAreaSize = new Vector2(2f, 0.5f);

    [Tooltip("How much damage the puddle deals per tick.")]
    public int damagePerTick = 5;

    [Tooltip("How often the damage tick occurs (in seconds).")]
    public float damageInterval = 0.5f;

    public GameObject deathSplatterEffectPrefab;
    public Transform splatterSpawnPoint;
    private bool hasLanded = false;
    private GameObject activePuddleInstance = null;

    [Header("V2 Power-Up Drop Settings")]
    [Tooltip("If true, this enemy is considered 'mutated' and can drop power-ups.")]
    public bool isMutated = true;
    [Tooltip("The loot table to use for this enemy's drops.")]
    public PowerUpDropTable dropTable;
    [Tooltip("The chance (0.0 to 1.0) for this enemy to drop a power-up on death.")]
    [Range(0f, 1f)] public float dropChance = 0.05f; // 5% chance
    [Tooltip("The prefab for the physical power-up pickup item.")]
    public GameObject powerUpPickupPrefab;
    public Transform powerUpSpawnPoint;
    void Awake()
    {
        // Get or add the AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false; // Ensure it doesn\"t play automatically

        // Auto-find components if enabled
        if (autoFindRigidbody && inkRigidbody == null)
        {
            inkRigidbody = GetComponent<Rigidbody2D>();
        }

        if (autoFindColliders && (collidersToDisable == null || collidersToDisable.Length == 0))
        {
            collidersToDisable = GetComponents<Collider2D>();
        }

        // Store original sprite colors
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalSpriteColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    originalSpriteColors[i] = spriteRenderers[i].color;
                }
            }
        }

       
    }
    void OnEnable()
    {
        ResetEnemyState();
        var InkAttack = GetComponent<InkAttack>();
        if (InkAttack != null)
        {
            InkAttack.enabled = true;
        }
    }
    public void ResetEnemyState()
    {
        // --- HEALTH & STATE RESET ---
        currentHealth = maxHealth;
        isKnockedBack = false;
        isFlashing = false;
        isStunned = false;
        hasLanded = false; // <-- CRITICAL: This must be reset to false.
        isInvincible = false;
        isInInvincibilityTransition = false;

        // --- V2 PUDDLE ATTACK RESET ---
        if (isV2PuddleAttack)
        {
            // Stop the damage coroutine.
            StopAllCoroutines();

            // Disable the smoke particles.
            if (smokeParticles != null)
            {
                smokeParticles.SetActive(false);
            }

            // Reset the puddle animator.
            if (puddleAnimator != null)
            {
                puddleAnimator.speed = 0; // Stop it from playing
                puddleAnimator.Rebind(); // Rewind the animation to its default state
                puddleAnimator.Update(0f);
            }
        }
        // --- END OF V2 RESET ---

        // --- RIGIDBODY & COLLIDER RESET ---
        if (inkRigidbody != null)
        {
            inkRigidbody.bodyType = RigidbodyType2D.Dynamic;
            inkRigidbody.velocity = Vector2.zero;
        }
        if (collidersToDisable != null)
        {
            foreach (Collider2D col in collidersToDisable)
            {
                if (col != null) col.enabled = true;
            }
        }

        // --- MATERIAL & VISUALS RESET ---
        if (spriteRenderers != null && originalMaterials != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null && originalMaterials[i] != null)
                {
                    spriteRenderers[i].material = originalMaterials[i];
                }
            }
        }
        if (spriteRenderers != null && originalSpriteColors != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null && i < originalSpriteColors.Length)
                {
                    spriteRenderers[i].color = originalSpriteColors[i];
                }
            }
        }

        // --- INVINCIBILITY & AI RESET ---
        // We already stopped all coroutines, so now we just re-initialize the system.
        if (enableInvincibilitySystem)
        {
            InitializeInvincibilitySystem();
        }

        // --- MAIN ANIMATOR RESET ---
        if (inkAnimator != null)
        {
            inkAnimator.ResetTrigger(hideAnimationTrigger);
            inkAnimator.SetTrigger(showAnimationTrigger);
        }
    }


    void Start()
    {

        if (weaponSwitchManager == null)
        {
            weaponSwitchManager = FindObjectOfType<WeaponSwitchManager>();
            if (weaponSwitchManager == null)
            {
                Debug.LogError("WeaponSwitchManager not found in the scene. Please assign it or ensure it exists.");
            }
        }
        // Initialize health
        currentHealth = maxHealth;

        // Initialize invincibility system
        if (enableInvincibilitySystem)
        {
            InitializeInvincibilitySystem();
        }
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalMaterials = new Material[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    originalMaterials[i] = spriteRenderers[i].sharedMaterial;
                }
            }
        }
    }

    void Update()
    {
        // If the enemy has already landed, there is nothing for this method to do.
        if (hasLanded)
        {
            return;
        }

        // This block only runs if the enemy has NOT landed yet.
        if (inkRigidbody != null && inkRigidbody.bodyType == RigidbodyType2D.Dynamic)
        {
            // Check if it's touching the ground.
            if (IsGrounded())
            {
                // It has landed!
                hasLanded = true;
                inkRigidbody.bodyType = RigidbodyType2D.Static;

                // --- THIS IS THE GUARANTEED FIX ---
                // If this is a V2 enemy, start the puddle attack sequence,
                // but do it in a coroutine that waits one frame.
                if (isV2PuddleAttack)
                {
                    StartCoroutine(StartPuddleAttackAfterFrame());
                }
                // --- END OF FIX ---
            }
        }

        // Your invincibility debug logic is fine here.
        if (showInvincibilityDebug)
        {
            isCurrentlyInvincible = isInvincible;
            timeUntilNextInvincibilityCheck = Mathf.Max(0f, nextInvincibilityCheckTime - Time.time);
        }
    } 
private IEnumerator StartPuddleAttackAfterFrame()
{
    yield return new WaitForEndOfFrame();

    if (puddlePrefab != null && puddleSpawnPoint != null && ObjectPoolManager.Instance != null)
    {
        // --- THIS IS THE GUARANTEED FIX ---
        // 1. Spawn the puddle from the pool.
        GameObject spawnedPuddle = ObjectPoolManager.Instance.SpawnFromPool(puddlePrefab, puddleSpawnPoint.position, puddleSpawnPoint.rotation);

        // 2. Store a reference to this specific puddle instance.
        activePuddleInstance = spawnedPuddle;
        // --- END OF FIX ---
        if (smokeParticles != null)
        {
            smokeParticles.SetActive(true);
        }
        // Your existing animation logic is preserved.
        if (spawnedPuddle != null)
        {
            Animator puddleAnimator = spawnedPuddle.GetComponent<Animator>();
            if (puddleAnimator != null)
            {
                puddleAnimator.Play(inkDripAnimationName, 0, 0f);
                puddleAnimator.speed = 1;
            }
        }
    }

    StartCoroutine(DamageOverTimeRoutine());
}


private IEnumerator DamageOverTimeRoutine()
    {
        while (true)
        {
            if (damagePoint == null) yield break;

            // --- THIS IS THE GUARANTEED FIX ---
            // We are now using the correct 'playerLayer' variable to find the player.
            Collider2D[] playersToDamage = Physics2D.OverlapBoxAll(damagePoint.position, damageAreaSize, 0f, playerLayer);
            // --- END OF FIX ---

            foreach (var playerCollider in playersToDamage)
            {
                PlayerHealth playerHealth = playerCollider.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damagePerTick, 0f, Vector2.zero);
                }
                L3antixHealth l3antixHealth = playerCollider.GetComponent<L3antixHealth>();
                if (l3antixHealth != null)
                {
                    l3antixHealth.TakeDamage(damagePerTick, 0f, Vector2.zero);
                }
            }

            yield return new WaitForSeconds(damageInterval);
        }
    }


    // Initialize the invincibility system
    private void InitializeInvincibilitySystem()
    {
        if (startWithInvincibility)
        {
            // Start with invincibility immediately
            StartCoroutine(ActivateInvincibilityWithDelay(0f));
        }
        else
        {
            // Schedule first invincibility check
            ScheduleNextInvincibilityCheck();
        }

        if (showInvincibilityDebug)
        {
            Debug.Log($"InkHealth: Invincibility system initialized. First check in {initialInvincibilityDelay}s");
        }
    }

    // Schedule the next invincibility check
    private void ScheduleNextInvincibilityCheck()
    {
        float randomInterval = Random.Range(minInvincibilityInterval, maxInvincibilityInterval);
        nextInvincibilityCheckTime = Time.time + randomInterval;

        if (invincibilityTimerCoroutine != null)
        {
            StopCoroutine(invincibilityTimerCoroutine);
        }
        invincibilityTimerCoroutine = StartCoroutine(InvincibilityTimer(randomInterval));

        if (showInvincibilityDebug)
        {
            Debug.Log($"InkHealth: Next invincibility check scheduled in {randomInterval:F2}s");
        }
    }

    // Timer coroutine for invincibility checks
    private IEnumerator InvincibilityTimer(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        // Check if we should activate invincibility
        if (enableInvincibilitySystem && !isInvincible && !isInInvincibilityTransition)
        {
            float randomChance = Random.Range(0f, 1f);
            if (randomChance <= invincibilityActivationChance)
            {
                if (showInvincibilityDebug)
                {
                    Debug.Log($"InkHealth: Activating invincibility (chance: {randomChance:F2} <= {invincibilityActivationChance:F2})");
                }
                StartCoroutine(ActivateInvincibility());
            }
            else
            {
                if (showInvincibilityDebug)
                {
                    Debug.Log($"InkHealth: Skipping invincibility (chance: {randomChance:F2} > {invincibilityActivationChance:F2})");
                }
                ScheduleNextInvincibilityCheck();
            }
        }
    }

    // Activate invincibility with delay
    private IEnumerator ActivateInvincibilityWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(ActivateInvincibility());
    }

    // Main invincibility activation coroutine
    private IEnumerator ActivateInvincibility()
    {
        if (isInInvincibilityTransition || isInvincible)
        {
            yield break;
        }

        isInInvincibilityTransition = true;
        // Before doing anything else, check if the Rigidbody is dynamic AND if the enemy is in the air.
        if (inkRigidbody != null && inkRigidbody.bodyType == RigidbodyType2D.Dynamic && !IsGrounded())
        {
            if (showInvincibilityDebug)
            {
                Debug.Log("InkHealth: Aborting invincibility attempt because the enemy is airborne.");
            }

            // Abort the invincibility sequence.
            isInInvincibilityTransition = false; // Reset the flag
            ScheduleNextInvincibilityCheck();    // Immediately schedule the next attempt
            yield break;                         // Exit the coroutine
        }
        if (showInvincibilityDebug)
        {
            Debug.Log("InkHealth: Starting invincibility sequence");
        }

        // Play invincibility start sound
        if (invincibilityStartSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(invincibilityStartSound, invincibilitySoundVolume);
        }

        // Trigger hide animation
        if (inkAnimator != null && !string.IsNullOrEmpty(hideAnimationTrigger))
        {
            inkAnimator.SetTrigger(hideAnimationTrigger);
            if (showInvincibilityDebug)
            {
                Debug.Log($"InkHealth: Triggered hide animation: {hideAnimationTrigger}");
            }
        }

        // Wait for hide animation to complete
        yield return new WaitForSeconds(hideAnimationWaitTime);

        // Activate invincibility state
        isInvincible = true;
        currentInvincibilityTimer = invincibilityDuration;

        // Disable colliders
        if (collidersToDisable != null)
        {
            foreach (Collider2D col in collidersToDisable)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }
        }

      

        // Hide sprites if enabled
        if (hideSpritesDuringInvincibility && spriteRenderers != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    Color color = spriteRenderers[i].color;
                    color.a = invincibilityAlpha;
                    spriteRenderers[i].color = color;
                }
            }
        }

        if (showInvincibilityDebug)
        {
            Debug.Log($"InkHealth: Invincibility activated for {invincibilityDuration}s");
        }

        isInInvincibilityTransition = false;

        // Wait for invincibility duration while keeping enemy hidden
        float timer = 0f;
        while (timer < invincibilityDuration)
        {
            timer += Time.deltaTime;
            currentInvincibilityTimer = invincibilityDuration - timer;

            // Keep the enemy in hide state if stayHiddenDuringInvincibility is true
            if (stayHiddenDuringInvincibility && inkAnimator != null && !string.IsNullOrEmpty(hideAnimationStateName))
            {
                AnimatorStateInfo stateInfo = inkAnimator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsName(hideAnimationStateName))
                {
                    // Force back to hide state if it somehow exited
                    inkAnimator.SetTrigger(hideAnimationTrigger);
                }
            }

            yield return null;
        }

        // Deactivate invincibility
        StartCoroutine(DeactivateInvincibility());
    }

    // Deactivate invincibility coroutine
    private IEnumerator DeactivateInvincibility()
    {
        if (!isInvincible)
        {
            yield break;
        }

        isInInvincibilityTransition = true;
        isInvincible = false;
        currentInvincibilityTimer = 0f;

        if (showInvincibilityDebug)
        {
            Debug.Log("InkHealth: Deactivating invincibility");
        }

        // Play invincibility end sound
        if (invincibilityEndSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(invincibilityEndSound, invincibilitySoundVolume);
        }

        // Trigger show animation
        if (inkAnimator != null && !string.IsNullOrEmpty(showAnimationTrigger))
        {
            inkAnimator.SetTrigger(showAnimationTrigger);
            if (showInvincibilityDebug)
            {
                Debug.Log($"InkHealth: Triggered show animation: {showAnimationTrigger}");
            }
        }

        // Restore sprites
        if (hideSpritesDuringInvincibility && spriteRenderers != null && originalSpriteColors != null)
        {
            for (int i = 0; i < spriteRenderers.Length && i < originalSpriteColors.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].color = originalSpriteColors[i];
                }
            }
        }

        // Wait for show animation to complete
        yield return new WaitForSeconds(showAnimationWaitTime);

        // Re-enable colliders
        if (collidersToDisable != null)
        {
            foreach (Collider2D col in collidersToDisable)
            {
                if (col != null)
                {
                    col.enabled = true;
                }
            }
        }

      
        if (showInvincibilityDebug)
        {
            Debug.Log("InkHealth: Invincibility deactivated, enemy is now vulnerable");
        }

        isInInvincibilityTransition = false;

        // Schedule next invincibility check
        ScheduleNextInvincibilityCheck();
    }

    // Public method to play ink dripping particles once (for animation events)
    public void PlayInkDripping()
    {
        if (InkDripping != null)
        {
            InkDripping.Play();
            if (showInvincibilityDebug)
            {
                Debug.Log("InkHealth: Ink dripping played once via animation event");
            }
        }
    }

    // Public method to manually activate invincibility
    public void ManuallyActivateInvincibility()
    {
        if (!isInvincible && !isInInvincibilityTransition)
        {
            StartCoroutine(ActivateInvincibility());
        }
    }

    // Public method to manually deactivate invincibility
    public void ManuallyDeactivateInvincibility()
    {
        if (isInvincible && !isInInvincibilityTransition)
        {
            if (invincibilityCoroutine != null)
            {
                StopCoroutine(invincibilityCoroutine);
            }
            StartCoroutine(DeactivateInvincibility());
        }
    }

    // Public method to check if currently invincible
    public bool IsInvincible()
    {
        return isInvincible;
    }

    // Method to take damage (modified to check invincibility and accept knockback force)
    public void TakeDamage(float damage, Vector2 attackDirection, float knockbackForce = 1f) // Added knockbackForce parameter
    {
        // Check if invincible
        if (isInvincible)
        {
            if (showInvincibilityDebug)
            {
                Debug.Log($"InkHealth: Damage blocked due to invincibility (damage: {damage})");
            }
            return; // No damage taken, no effects played
        }

        // Reduce health
        currentHealth -= (int)damage;

        // Play damage sound if assigned
        if (damageSoundClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSoundClip, damageSoundVolume);
        }

        // Apply knockback
        if (!isKnockedBack)
        {
            StartCoroutine(ApplyKnockback(attackDirection, knockbackForce)); // Pass knockbackForce
        }

        // Play blood particle effect
        if (bloodSpawnPoint != null && bloodParticle != null)
        {
            InstantiateAndPlayParticleSystem(bloodParticle, bloodSpawnPoint.position);
        }

        // Trigger flash damage effect
        if (!isFlashing) // Only start new flash if not already flashing
        {
            StartCoroutine(FlashDamage());
        }
        if (currentHealth > 0)
        {
            SoulLinkEnemy soul = GetComponent<SoulLinkEnemy>();
            if (soul != null)
            {
                soul.TryStartLink();
            }
        }
        // Check if the ink enemy is dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Coroutine to apply knockback using transform movement
    private IEnumerator ApplyKnockback(Vector2 attackDirection, float force) // Added force parameter
    {
        isKnockedBack = true;

        // Use the attack direction directly for knockback
        float knockbackDirection = Mathf.Sign(attackDirection.x); // Same as the attack direction

        // Calculate the target position for knockback, scaled by force
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + new Vector3(knockbackDirection * knockbackDistance * force, 0, 0); // Apply force

        // Track elapsed time
        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            // Move the ink enemy toward the end position
            transform.position = Vector3.Lerp(startPosition, endPosition, elapsed / knockbackDuration);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure the final position is exact
        transform.position = endPosition;

        isKnockedBack = false;
    }

    private void TryDropPowerUp()
    {
        // If this isn't a mutated enemy or has no drop table, do nothing.
        if (!isMutated || dropTable == null || powerUpPickupPrefab == null) return;

        // Determine the final drop chance. For bosses, you can set this to 1.0 in the Inspector.
        float roll = Random.Range(0f, 1f);
        if (roll > dropChance) return; // The roll failed.

        // The roll succeeded! Get a random power-up from the table.
        PowerUpData powerUpToDrop = dropTable.GetRandomDrop();

        // If the table gave us a valid power-up...
        if (powerUpToDrop != null)
        {
            // Determine the spawn position.
            // It will use the powerUpSpawnPoint's position if you have assigned one,
            // otherwise it will default to the enemy's main transform position.
            Vector3 spawnPosition = (powerUpSpawnPoint != null) ? powerUpSpawnPoint.position : transform.position;

            // Spawn the pickup prefab at the chosen position.
            GameObject pickupObject = Instantiate(powerUpPickupPrefab, spawnPosition, Quaternion.identity);

            // Get the PowerUpPickup script from the new object and tell it what it is.
            PowerUpPickup pickupScript = pickupObject.GetComponent<PowerUpPickup>();
            if (pickupScript != null)
            {
                pickupScript.Initialize(powerUpToDrop);
            }
        }
    }


    // Method to handle death
    public void Die()
    {
        if (activePuddleInstance != null && activePuddleInstance.activeInHierarchy)
        {
            // If it does, disable it immediately. This returns it to the object pool.
            activePuddleInstance.SetActive(false);
            activePuddleInstance = null; // Clear the reference for the next life.
        }
        // Stop invincibility system when dying
        if (invincibilityCoroutine != null)
        {
            StopCoroutine(invincibilityCoroutine);
        }
        if (invincibilityTimerCoroutine != null)
        {
            StopCoroutine(invincibilityTimerCoroutine);
        }

        if (DeathInkSpawn != null && DeathInkParticules != null)
        {
            InstantiateAndPlayParticleSystem(DeathInkParticules, DeathInkSpawn.position);
        }

        if (DeathInkSpawn2 != null && DeathInkParticules2 != null)
        {
            InstantiateAndPlayParticleSystem(DeathInkParticules2, DeathInkSpawn2.position);
        }

        if (DeathInkSpawn3 != null && DeathInkParticules3 != null)
        {
            InstantiateAndPlayParticleSystem(DeathInkParticules3, DeathInkSpawn3.position);
        }

        if (DeathInkSpawn4 != null && DeathInkParticules4 != null)
        {
            InstantiateAndPlayParticleSystem(DeathInkParticules4, DeathInkSpawn4.position);
        }

        if (DeathInkSpawn5 != null && DeathInkParticules5 != null)
        {
            InstantiateAndPlayParticleSystem(DeathInkParticules5, DeathInkSpawn5.position);
        }
        // Trigger camera shake
        CameraShakerHandler.Shake(CameraShakeDeath);

        OnDeath?.Invoke(gameObject);

        if (weaponSwitchManager != null)
        {
            weaponSwitchManager.OnEnemyKilled();
            Debug.Log("Enemy died, notifying WeaponSwitchManager.");
        }

        SoulLinkEnemy soul = GetComponent<SoulLinkEnemy>();
        if (soul != null && soul.inChain)
        {
            soul.NotifyDied();
        }
        if (deathSplatterEffectPrefab != null && ObjectPoolManager.Instance != null)
        {
            Vector3 spawnPosition = (splatterSpawnPoint != null) ? splatterSpawnPoint.position : transform.position;

            // Tell the pool manager to spawn the effect at that position
            ObjectPoolManager.Instance.SpawnFromPool(deathSplatterEffectPrefab, spawnPosition, Quaternion.identity);
        }
        TryDropPowerUp();
        // Destroy the ink enemy
        gameObject.SetActive(false);
    }

    // Helper method to instantiate and play a particle system
    private void InstantiateAndPlayParticleSystem(ParticleSystem particleSystem, Vector3 position)
    {
        // Instantiate the particle system at the given position
        ParticleSystem instance = Instantiate(particleSystem, position, Quaternion.identity);

        // Play the particle system
        instance.Play();
    }
    public void ForceCleanup()
    {
        // This is the same cleanup logic from your Die() method.
        if (activePuddleInstance != null && activePuddleInstance.activeInHierarchy)
        {
            activePuddleInstance.SetActive(false);
            activePuddleInstance = null;
        }
    }
    // Coroutine to handle the flash damage effect
    private IEnumerator FlashDamage()
    {
        isFlashing = true;

        // --- MATERIAL SWAPPING LOGIC ---
        // 1. Swap to the Flash Material
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].material = flashMaterial;
            }
        }

        // 2. Wait for the flash duration.
        // We wait for the full duration now, as we are not animating a shader property.
        yield return new WaitForSeconds(flashDuration);

        // 3. Swap back to the Original Materials
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && originalMaterials[i] != null)
            {
                spriteRenderers[i].material = originalMaterials[i];
            }
        }
        // --- END OF SWAPPING LOGIC ---

        isFlashing = false;
    }
    private bool IsGrounded()
    {
        // Failsafe: If the groundCheck transform isn't assigned, assume it's not grounded to be safe.
        if (groundCheck == null)
        {
            Debug.LogWarning("Ground Check transform is not assigned on " + gameObject.name);
            return false;
        }

        // Draw a small circle at the groundCheck position. If that circle overlaps with anything
        // on the 'groundLayer', the method returns true.
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void OnDrawGizmosSelected()
    {
        // Only draw the gizmo if this is a V2 enemy and the damage point is assigned.
        if (isV2PuddleAttack && damagePoint != null)
        {
            Gizmos.color = Color.red;

            // --- THIS IS THE GUARANTEED FIX ---
            // We need to calculate the TRUE world position of the damage area.
            // We start with the damagePoint's LOCAL position relative to the Ink enemy.
            Vector3 localCenter = damagePoint.localPosition;

            // Then, we use transform.TransformPoint() to convert that local position
            // into the correct world space position. This works everywhere.
            Vector3 worldCenter = transform.TransformPoint(localCenter);

            // Now, we draw the cube at the correct world position.
            Gizmos.DrawWireCube(worldCenter, damageAreaSize);
            // --- END OF FIX ---
        }
    }


}


