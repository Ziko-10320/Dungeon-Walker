using System.Collections;
using UnityEngine;
using FirstGearGames.SmoothCameraShaker;
using UnityEngine.Events;

public class FlyHealth : MonoBehaviour
{
    // Public variables for health and effects
    public int maxHealth = 100; // Maximum health of the mushroom
    public GameObject deathEffect; // Optional: Effect to play when the mushroom dies
    public float knockbackDistance = 1f; // Distance the mushroom moves during knockback
    public float knockbackDuration = 0.2f; // Duration of the knockback effect
    public Transform bloodSpawnPoint; // Spawn point for blood particles
    public ParticleSystem bloodParticle; // Blood particle system

    public ParticleSystem DeathMushroomParticules;
    public ParticleSystem DeathMushroomParticules2;
    public ParticleSystem DeathMushroomParticules3;
    public ParticleSystem DeathMushroomParticules4;
    public ParticleSystem DeathMushroomParticules5;

    public Transform DeathMushroomSpawn;
    public Transform DeathMushroomSpawn2;
    public Transform DeathMushroomSpawn3;
    public Transform DeathMushroomSpawn4;
    public Transform DeathMushroomSpawn5;

    // Flash Damage Variables
    public Material flashMaterial; // Material with the flash shader
    public string flashAmountProperty = "_FlashAmount"; // Name of the Flash Amount property in the shader
    public float flashDuration = 0.2f; // Duration of the flash effect

    // Array of SpriteRenderers for the parts of the mushroom
    public SpriteRenderer[] spriteRenderers;
    private Material[] originalMaterials;
    // Damage Sound Variables
    public AudioClip damageSoundClip; // Audio clip to play when taking damage
    [Range(0f, 1f)] public float damageSoundVolume = 0.7f; // Volume slider added here

    public WeaponSwitchManager weaponSwitchManager;
    public UnityEvent<GameObject> OnDeath;
    // Private variables
    [HideInInspector]
    public int currentHealth;
    private bool isKnockedBack = false; // Is the mushroom currently being knocked back?
    private bool isFlashing = false; // Added to prevent multiple flash coroutines
    private AudioSource audioSource; // Reference to the AudioSource component

    //CameraShake
    public ShakeData CameraShakeDeath;
    public bool isStunned = false;
    public GameObject deathSplatterEffectPrefab;
    public Transform splatterSpawnPoint;
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
        audioSource.playOnAwake = false; // Ensure it doesn't play automatically
        audioSource.volume = damageSoundVolume; // Set initial volume
    }
    void OnEnable()
    {
        ResetState();
        // If the enemy has a movement script, re-enable it.
        var FlyFollow = GetComponent<FlyFollow>();
        if (FlyFollow != null)
        {
            FlyFollow.enabled = true;
        }
        var FlyAttack = GetComponent<FlyAttack>();
        if (FlyAttack != null)
        {
            FlyAttack.enabled = true;
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
    public void ResetState()
    {
        // --- HEALTH & STATE RESET ---
        currentHealth = maxHealth;
        isKnockedBack = false;
        isFlashing = false;
        isStunned = false;

        // --- MATERIAL & VISUALS RESET ---
        if (spriteRenderers != null && originalMaterials != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null && originalMaterials[i] != null)
                {
                    // This is the critical fix for the flash material bug
                    spriteRenderers[i].material = originalMaterials[i];
                }
            }
        }

        // --- STOP LEFTOVER COROUTINES ---
        StopAllCoroutines();
    }

    // Method to take damage
    public void TakeDamage(float damage, Vector2 attackDirection, float knockbackForce = 1f)
    {
        // Reduce health
        currentHealth -= (int)damage;

        // Play damage sound if assigned
        if (damageSoundClip != null && audioSource != null)
        {
            audioSource.volume = damageSoundVolume; // Apply volume
            audioSource.PlayOneShot(damageSoundClip);
        }

        // Apply knockback
        if (!isKnockedBack)
        {
            StartCoroutine(ApplyKnockback(attackDirection, knockbackForce));
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
        // Check if the mushroom is dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Coroutine to apply knockback using transform movement
    private IEnumerator ApplyKnockback(Vector2 attackDirection, float force)
    {
        isKnockedBack = true;

        float knockbackDirection = Mathf.Sign(attackDirection.x);
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + new Vector3(knockbackDirection * knockbackDistance * force, 0, 0);

        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, elapsed / knockbackDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPosition;
        isKnockedBack = false;
    }

    // Method to handle death
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

    public void Die()
    {
       
        if (DeathMushroomSpawn != null && DeathMushroomParticules != null)
        {
            InstantiateAndPlayParticleSystem(DeathMushroomParticules, DeathMushroomSpawn.position);
        }

        if (DeathMushroomSpawn2 != null && DeathMushroomParticules2 != null)
        {
            InstantiateAndPlayParticleSystem(DeathMushroomParticules2, DeathMushroomSpawn2.position);
        }

        if (DeathMushroomSpawn3 != null && DeathMushroomParticules3 != null)
        {
            InstantiateAndPlayParticleSystem(DeathMushroomParticules3, DeathMushroomSpawn3.position);
        }

        if (DeathMushroomSpawn4 != null && DeathMushroomParticules4 != null)
        {
            InstantiateAndPlayParticleSystem(DeathMushroomParticules4, DeathMushroomSpawn4.position);
        }

        if (DeathMushroomSpawn5 != null && DeathMushroomParticules5 != null)
        {
            InstantiateAndPlayParticleSystem(DeathMushroomParticules5, DeathMushroomSpawn5.position);
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
        // Destroy the mushroom
        gameObject.SetActive(false);

    }

    // Helper method to instantiate and play a particle system
    private void InstantiateAndPlayParticleSystem(ParticleSystem particleSystem, Vector3 position)
    {
        ParticleSystem instance = Instantiate(particleSystem, position, Quaternion.identity);
        instance.Play();
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
}