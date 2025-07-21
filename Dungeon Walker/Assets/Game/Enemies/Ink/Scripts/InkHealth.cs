using System.Collections;
using UnityEngine;
using FirstGearGames.SmoothCameraShaker;

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

    [Tooltip("Show invincibility state in inspector (read-only)")]
    [SerializeField] private bool isCurrentlyInvincible = false;

    [Tooltip("Time until next invincibility check (read-only)")]
    [SerializeField] private float timeUntilNextInvincibilityCheck = 0f;

    [Tooltip("Current invincibility timer (read-only)")]
    [SerializeField] private float currentInvincibilityTimer = 0f;

    // Private variables
    private int currentHealth;
    private bool isKnockedBack = false; // Is the ink enemy currently being knocked back?
    private bool isFlashing = false; // Added to prevent multiple flash coroutines
    private AudioSource audioSource; // Reference to the AudioSource component

    // Invincibility private variables
    private bool isInvincible = false;
    private bool isInInvincibilityTransition = false; // Prevents multiple invincibility coroutines
    private Coroutine invincibilityCoroutine;
    private Coroutine invincibilityTimerCoroutine;
    private float nextInvincibilityCheckTime;
    private bool wasRigidbodyKinematic; // Store original kinematic state
    private Color[] originalSpriteColors; // Store original sprite colors

    //CameraShake
    public ShakeData CameraShakeDeath;

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

        // Store original rigidbody kinematic state
        if (inkRigidbody != null)
        {
            wasRigidbodyKinematic = inkRigidbody.isKinematic;
        }
    }

    void Start()
    {
        // Initialize health
        currentHealth = maxHealth;

        // Initialize invincibility system
        if (enableInvincibilitySystem)
        {
            InitializeInvincibilitySystem();
        }
    }

    void Update()
    {
        // Update invincibility debug info
        if (showInvincibilityDebug)
        {
            isCurrentlyInvincible = isInvincible;
            timeUntilNextInvincibilityCheck = Mathf.Max(0f, nextInvincibilityCheckTime - Time.time);
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

        // Make rigidbody kinematic
        if (inkRigidbody != null)
        {
            wasRigidbodyKinematic = inkRigidbody.isKinematic;
            inkRigidbody.isKinematic = true;
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

        // Restore rigidbody kinematic state
        if (inkRigidbody != null)
        {
            inkRigidbody.isKinematic = wasRigidbodyKinematic;
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
    public void TakeDamage(int damage, Vector2 attackDirection, float knockbackForce = 1f) // Added knockbackForce parameter
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

    // Method to handle death
    private void Die()
    {
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

        // Destroy the ink enemy
        Destroy(gameObject);

    }

    // Helper method to instantiate and play a particle system
    private void InstantiateAndPlayParticleSystem(ParticleSystem particleSystem, Vector3 position)
    {
        // Instantiate the particle system at the given position
        ParticleSystem instance = Instantiate(particleSystem, position, Quaternion.identity);

        // Play the particle system
        instance.Play();
    }

    // Coroutine to handle the flash damage effect
    private IEnumerator FlashDamage()
    {
        isFlashing = true;

        if (flashMaterial == null || spriteRenderers.Length == 0)
        {
            Debug.LogError("Flash material or SpriteRenderers are not assigned.");
            isFlashing = false;
            yield break;
        }

        // Create instances of the flash material for each sprite renderer
        Material[] originalMaterials = new Material[spriteRenderers.Length];
        Material[] flashMaterialInstances = new Material[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                // Store the original material
                originalMaterials[i] = spriteRenderers[i].material;

                // Create an instance of the flash material
                flashMaterialInstances[i] = new Material(flashMaterial);
                spriteRenderers[i].material = flashMaterialInstances[i];
            }
        }

        // Gradually increase the flash amount to 1
        float elapsed = 0f;
        while (elapsed < flashDuration / 2)
        {
            float flashAmount = Mathf.Lerp(0, 1, elapsed / (flashDuration / 2));

            // Update the flash amount for all material instances
            foreach (var material in flashMaterialInstances)
            {
                if (material != null)
                {
                    material.SetFloat(flashAmountProperty, flashAmount);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Gradually decrease the flash amount back to 0
        elapsed = 0f;
        while (elapsed < flashDuration / 2)
        {
            float flashAmount = Mathf.Lerp(1, 0, elapsed / (flashDuration / 2));

            // Update the flash amount for all material instances
            foreach (var material in flashMaterialInstances)
            {
                if (material != null)
                {
                    material.SetFloat(flashAmountProperty, flashAmount);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset the flash amount to 0 explicitly
        foreach (var material in flashMaterialInstances)
        {
            if (material != null)
            {
                material.SetFloat(flashAmountProperty, 0);
            }
        }

        // Restore the original materials and destroy the flash material instances
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].material = originalMaterials[i];
                Destroy(flashMaterialInstances[i]);
            }
        }
        isFlashing = false;
    }
}


