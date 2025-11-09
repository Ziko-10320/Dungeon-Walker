using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RatKingHealth : MonoBehaviour, IDamageable
{

    public int baseMaxHealth = 100; // Renamed from maxHealth
    public int maxHealth;
    public GameObject deathEffect; // Optional: Effect to play when the Rat King dies
    public Transform bloodSpawnPoint; // Spawn point for blood particles
    public ParticleSystem bloodParticle; // Blood particle system

    public List<string> deathEffectNames;

    [Header("Death Audio")]
    public AudioClip deathSound; // A single clip for the death sound
    [Range(0f, 1f)] public float deathSoundVolume = 1.0f;
    public Transform DeathMushroomSpawn;
    public Transform DeathMushroomSpawn2;
   
    // Flash Damage Variables
    public Material flashMaterial; // Material with the flash shader
    public string flashAmountProperty = "_FlashAmount"; // Name of the Flash Amount property in the shader
    public float flashDuration = 0.2f; // Duration of the flash effect

    // Array of SpriteRenderers for the parts of the Rat King
    public SpriteRenderer[] spriteRenderers;
    private Material[] originalMaterials;
    // Damage Sound Variables
    public AudioClip damageSoundClip; // Audio clip to play when taking damage
    [Range(0f, 1f)] public float damageSoundVolume = 0.7f; // Volume slider added here

    public WeaponSwitchManager weaponSwitchManager;
    public UnityEvent<GameObject> OnDeath;
    // Private variables
    private int currentHealth;
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
    [Header("Scaling Per Wave")]
    [Tooltip("How much extra health the Rat King gets for each wave it survives after its first appearance.")]
    public int healthIncreasePerWave = 100;
    [Tooltip("How much extra damage the Rat King's attacks get per wave.")]
    public int damageIncreasePerWave = 10;
    [Tooltip("How much extra chase speed the Rat King gets per wave.")]
    public float chaseSpeedIncreasePerWave = 0.5f;

    // Internal memory for this specific Rat King instance
    private int firstSpawnWave = -1;
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
        // --- 1. THE SCALING LOGIC ---
        if (firstSpawnWave == -1)
        {
            // This is the first time this Rat King has ever spawned.
            firstSpawnWave = ScoreDisplay.CurrentWaveNumber;
        }

        // Calculate how many waves this boss has "survived".
        int wavesSurvived = ScoreDisplay.CurrentWaveNumber - firstSpawnWave;
        if (wavesSurvived < 0) wavesSurvived = 0;

        // --- 2. CALCULATE AND APPLY NEW STATS ---
        // Health (handled by this script)
        maxHealth = baseMaxHealth + (wavesSurvived * healthIncreasePerWave);

        // Get references to the other scripts
        var bossScript = GetComponent<RatKingBoss>();
        var attackScript = GetComponent<RatKingAttack>();

        // Speed (tell the boss script its new speed)
        if (bossScript != null)
        {
            float baseChaseSpeed = Random.Range(bossScript.chaseSpeedRange.x, bossScript.chaseSpeedRange.y);
            bossScript.chaseSpeed = baseChaseSpeed + (wavesSurvived * chaseSpeedIncreasePerWave);
        }

        // Damage (tell the attack script its new damage values)
        if (attackScript != null)
        {
            attackScript.damageAmount = attackScript.baseDamageAmount + (wavesSurvived * damageIncreasePerWave);
            attackScript.cheeseDamageAmount = attackScript.baseCheeseDamageAmount + (wavesSurvived * damageIncreasePerWave);
        }

        // --- 3. YOUR EXISTING RESET LOGIC ---
        ResetState(); // This correctly sets currentHealth = maxHealth

        Transform player = null;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            gameObject.SetActive(false);
            return;
        }

        if (bossScript != null)
        {
            bossScript.enabled = true;
            bossScript.Initialize(player);
        }
        if (attackScript != null)
        {
            attackScript.enabled = true;
            attackScript.Initialize(player);
        }
    }
    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || Camera.main == null) return;

        // Create a clean, independent object for the sound
        GameObject soundPlayerObject = new GameObject("RatKing_FORCE_PLAY_DEATH_SOUND");

        // Position it directly on the camera to guarantee it's heard
        soundPlayerObject.transform.position = Camera.main.transform.position;

        // Add and aggressively configure the AudioSource
        AudioSource tempAudioSource = soundPlayerObject.AddComponent<AudioSource>();

        tempAudioSource.clip = clip;

        // --- CRITICAL OVERRIDES ---
        tempAudioSource.volume = volume;
        tempAudioSource.spatialBlend = 0.0f;              // Force 2D sound
        tempAudioSource.priority = 0;                     // Highest priority
        tempAudioSource.bypassEffects = true;             // Ignore mixers
        tempAudioSource.bypassListenerEffects = true;     // Ignore listener effects
        tempAudioSource.bypassReverbZones = true;         // Ignore reverb zones

        // Play the sound and schedule its destruction
        tempAudioSource.Play();
        Destroy(soundPlayerObject, clip.length);
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
                    originalMaterials[i] = spriteRenderers[i].material;
                }
            }
        }
    }
    public void ResetState()
    {
        // --- HEALTH & STATE RESET ---
        currentHealth = maxHealth;
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
    public void TakeDamage(float damage, Vector2 attackDirection, float knockbackForce = 0f)
    {
        // Reduce health
        currentHealth -= (int)damage;

        // Play damage sound if assigned
        if (damageSoundClip != null && audioSource != null)
        {
            audioSource.volume = damageSoundVolume; // Apply volume
            audioSource.PlayOneShot(damageSoundClip);
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

        // Check if the Rat King is dead
        if (currentHealth <= 0)
        {
            Die();
        }
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
    private void Die()
    {
        PlaySound(deathSound, deathSoundVolume);
        if (VFX_Director.Instance != null && deathEffectNames.Count > 0)
        {
            // We will use the existing spawn point transforms to position the effects.
            Transform[] spawns = { DeathMushroomSpawn, DeathMushroomSpawn2 };

            // Loop through the effect names and spawn points.
            for (int i = 0; i < deathEffectNames.Count; i++)
            {
                // Make sure we have a valid name and a valid spawn point.
                if (!string.IsNullOrEmpty(deathEffectNames[i]) && i < spawns.Length && spawns[i] != null)
                {
                    // Tell the Director to play the effect at the correct spawn point.
                    VFX_Director.Instance.PlayEffect(deathEffectNames[i], spawns[i].position);
                }
            }
        }

        // Trigger camera shake
        CameraShakerHandler.Shake(CameraShakeDeath);

        OnDeath?.Invoke(gameObject);

        if (weaponSwitchManager != null)
        {
            weaponSwitchManager.OnEnemyKilled();
            Debug.Log("Enemy died, notifying WeaponSwitchManager.");
        }
        if (deathSplatterEffectPrefab != null && ObjectPoolManager.Instance != null)
        {
            Vector3 spawnPosition = (splatterSpawnPoint != null) ? splatterSpawnPoint.position : transform.position;

            // Tell the pool manager to spawn the effect at that position
            ObjectPoolManager.Instance.SpawnFromPool(deathSplatterEffectPrefab, spawnPosition, Quaternion.identity);
        }
        TryDropPowerUp();
        // Destroy the Rat King
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