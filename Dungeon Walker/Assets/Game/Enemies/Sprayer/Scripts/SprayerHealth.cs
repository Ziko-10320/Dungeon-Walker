using FirstGearGames.SmoothCameraShaker;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SprayerHealth : MonoBehaviour, IPunObservable
{
    // Public variables for health and effects
    public int baseMaxHealth = 100; // Renamed from maxHealth
    public int maxHealth;
    public GameObject deathEffect; // Optional: Effect to play when the mushroom dies
    public float knockbackDistance = 1f; // Distance the mushroom moves during knockback
    public float knockbackDuration = 0.2f; // Duration of the knockback effect
    public Transform bloodSpawnPoint; // Spawn point for blood particles
    public ParticleSystem bloodParticle; // Blood particle system
    [Header("Death Audio")]
    public AudioClip[] deathSounds; // Array for randomized death sounds
    [Range(0f, 1f)] public float deathSoundVolume = 1.0f;
    public List<string> deathEffectNames;

    public Transform DeathMushroomSpawn;
    public Transform DeathMushroomSpawn2;
   

    // Flash Damage Variables
    public Material flashMaterial; // Material with the flash shader
    public string flashAmountProperty = "_FlashAmount"; // Name of the Flash Amount property in the shader
    public float flashDuration = 0.2f; // Duration of the flash effect

    // Array of SpriteRenderers for the parts of the mushroom
    public SpriteRenderer[] spriteRenderers;
    private Material[] originalMaterials;
    // Damage Sound Variables
    public AudioClip damageSound; // Audio clip to play when taking damage
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
    private PhotonView view;
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
    [Tooltip("How much extra health the sprayer gets for each wave it survives after its first appearance.")]
    public int healthIncreasePerWave = 15;
    [Tooltip("How much extra damage the sprayer's attack gets per wave.")]
    public float damageIncreasePerWave = 2f; // Use float for damagePerSecond
    [Tooltip("How much extra chase speed the sprayer gets per wave.")]
    public float chaseSpeedIncreasePerWave = 0.4f;

    // Internal memory for this specific sprayer instance
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
            // This is the first time this sprayer has ever spawned.
            firstSpawnWave = ScoreDisplay.CurrentWaveNumber;
        }

        // Calculate how many waves this sprayer has "survived".
        int wavesSurvived = ScoreDisplay.CurrentWaveNumber - firstSpawnWave;
        if (wavesSurvived < 0) wavesSurvived = 0;

        // --- 2. CALCULATE AND APPLY NEW STATS ---
        // Health (handled by this script)
        maxHealth = baseMaxHealth + (wavesSurvived * healthIncreasePerWave);
        currentHealth = maxHealth;

        // Get references to the other scripts
        var followScript = GetComponent<SprayerFollow>();
        var attackScript = GetComponent<SprayerAttack>();

        // Speed (tell the follow script its new speed)
        if (followScript != null)
        {
            float baseChaseSpeed = Random.Range(followScript.chaseSpeedRange.x, followScript.chaseSpeedRange.y);
            followScript.chaseSpeed = baseChaseSpeed + (wavesSurvived * chaseSpeedIncreasePerWave);
        }

        // Damage (tell the attack script its new damage)
        if (attackScript != null)
        {
            attackScript.damagePerSecond = attackScript.baseDamagePerSecond + (wavesSurvived * damageIncreasePerWave);
        }

        // --- 3. YOUR EXISTING RESET LOGIC ---
        isKnockedBack = false;
        isFlashing = false;
        isStunned = false;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            gameObject.SetActive(false);
            return;
        }
        Transform player = playerObject.transform;

        if (followScript != null)
        {
            followScript.InitializeAndReset(player);
        }
        if (attackScript != null)
        {
            attackScript.InitializeAndReset(player);
        }

        // Material reset logic
        if (spriteRenderers != null && originalMaterials != null && spriteRenderers.Length == originalMaterials.Length)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null && originalMaterials[i] != null)
                {
                    spriteRenderers[i].material = originalMaterials[i];
                }
            }
        }
        EnemyStun stun = GetComponent<EnemyStun>();
        if (stun != null)
        {
            stun.ResetStunState();
        }
    }
    private void PlayRandomSound(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || Camera.main == null) return;

        int randomIndex = Random.Range(0, clips.Length);
        AudioClip clipToPlay = clips[randomIndex];
        if (clipToPlay == null) return;

        // Create a clean, independent object for the sound
        GameObject soundPlayerObject = new GameObject("Sprayer_FORCE_PLAY_DEATH_SOUND");

        // Position it directly on the camera to guarantee it's heard
        soundPlayerObject.transform.position = Camera.main.transform.position;

        // Add and aggressively configure the AudioSource
        AudioSource tempAudioSource = soundPlayerObject.AddComponent<AudioSource>();

        tempAudioSource.clip = clipToPlay;

        // --- CRITICAL OVERRIDES ---
        tempAudioSource.volume = this.deathSoundVolume;  // Use the public variable for control
        tempAudioSource.spatialBlend = 0.0f;              // Force 2D sound
        tempAudioSource.priority = 0;                     // Highest priority
        tempAudioSource.bypassEffects = true;             // Ignore mixers
        tempAudioSource.bypassListenerEffects = true;     // Ignore listener effects
        tempAudioSource.bypassReverbZones = true;         // Ignore reverb zones

        // Play the sound and schedule its destruction
        tempAudioSource.Play();
        Destroy(soundPlayerObject, clipToPlay.length);
    }
    void Start()
    {
        view = GetComponent<PhotonView>();
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

    // Method to take damage
    public void TakeDamage(float damage, Vector2 attackDirection, float knockbackForce = 1f)
    {
        // If we are online, send a request to the Master Client to deal the damage.
        if (view != null && PhotonNetwork.IsConnected)
        {
            // We send the damage amount and the attacker's direction for the effects.
            view.RPC("RPC_TakeDamage", RpcTarget.MasterClient, (int)damage, attackDirection, knockbackForce);
        }
        else
        {
            // OFFLINE MODE: We are the authority, so we apply damage and effects directly.
            ApplyDamageAndEffects((int)damage, attackDirection, knockbackForce);
        }
        if (currentHealth > 0)
        {
            SoulLinkEnemy soul = GetComponent<SoulLinkEnemy>();
            if (soul != null)
            {
                soul.TryStartLink();
            }
        }
    }
    [PunRPC]
    private void RPC_TakeDamage(int damage, Vector2 attackDirection, float knockbackForce)
    {
        // This code ONLY runs on the Master Client's machine.
        // It calls the local function to apply the damage and effects.
        ApplyDamageAndEffects(damage, attackDirection, knockbackForce);
    }

    // This function contains your original TakeDamage logic.
    private void ApplyDamageAndEffects(int damage, Vector2 attackDirection, float knockbackForce)
    {
        if (view != null && PhotonNetwork.IsConnected)
        {
            // ONLINE: Send an RPC to EVERYONE to trigger the visual effects.
            view.RPC("RPC_PlayDamageEffects", RpcTarget.All, attackDirection, knockbackForce);
        }
        else
        {
            // OFFLINE: Just run the effects locally.
            PlayDamageEffects(attackDirection, knockbackForce);
        }

        // Reduce health
        currentHealth -= damage;

        // Play damage sound if assigned
        if (damageSound != null && audioSource != null)
        {
            audioSource.volume = damageSoundVolume;
            audioSource.PlayOneShot(damageSound);
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
        if (!isFlashing)
        {
            StartCoroutine(FlashDamage());
        }

        // Check if the mushroom is dead
        if (currentHealth <= 0)
        {
            // The Master Client or offline player handles the death.
            Die(null); // We pass null because the attacker info is less critical here.
        }
    }
    [PunRPC]
    private void RPC_PlayDamageEffects(Vector2 attackDirection, float knockbackForce)
    {
        // This runs on EVERYONE'S machine.
        // It calls the local function to play all the visual candy.
        PlayDamageEffects(attackDirection, knockbackForce);
    }

    // This function contains your original visual effect logic.
    private void PlayDamageEffects(Vector2 attackDirection, float knockbackForce)
    {
        // Play damage sound if assigned
        if (damageSound != null && audioSource != null)
        {
            audioSource.volume = damageSoundVolume;
            audioSource.PlayOneShot(damageSound);
        }

        // Apply knockback locally on each client for responsiveness
        if (!isKnockedBack)
        {
            StartCoroutine(ApplyKnockback(attackDirection, knockbackForce));
        }

        // Play blood particle effect locally
        if (bloodSpawnPoint != null && bloodParticle != null)
        {
            InstantiateAndPlayParticleSystem(bloodParticle, bloodSpawnPoint.position);
        }

        // Trigger flash damage effect locally
        if (!isFlashing)
        {
            StartCoroutine(FlashDamage());
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
    public void Die(GameObject attacker = null)
    {
        SoulLinkEnemy soul = GetComponent<SoulLinkEnemy>();
        if (soul != null && soul.inChain)
        {
            // Notify the chain BEFORE we destroy the GameObject so the chain can capture position/linePoint.
            soul.NotifyDied();
        }
        if (PhotonNetwork.IsMasterClient || !PhotonNetwork.IsConnected)
        {
            if (view != null && PhotonNetwork.IsConnected)
            {
                // ONLINE: Send an RPC with the position where the effects should play.
                view.RPC("RPC_PlayDeathEffects", RpcTarget.All, transform.position);
            }
            else
            {
                // OFFLINE: Just play the effects locally.
                PlayDeathEffects(transform.position);
            }
           

        // Trigger camera shake
        CameraShakerHandler.Shake(CameraShakeDeath);

        OnDeath?.Invoke(gameObject);

        if (weaponSwitchManager != null)
        {
            weaponSwitchManager.OnEnemyKilled();
            Debug.Log("Enemy died, notifying WeaponSwitchManager.");
        }
            TryDropPowerUp();
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                if (deathSplatterEffectPrefab != null && ObjectPoolManager.Instance != null)
                {
                    Vector3 spawnPosition = (splatterSpawnPoint != null) ? splatterSpawnPoint.position : transform.position;

                    // Tell the pool manager to spawn the effect at that position
                    ObjectPoolManager.Instance.SpawnFromPool(deathSplatterEffectPrefab, spawnPosition, Quaternion.identity);
                }
                // In offline mode, just destroy it locally.
                gameObject.SetActive(false);
            }
        }
       
    }
    [PunRPC]
    private void RPC_PlayDeathEffects(Vector3 deathPosition)
    {
        // This runs on EVERYONE'S machine.
        PlayDeathEffects(deathPosition);
    }

    // This function contains your original death effect logic.
    private void PlayDeathEffects(Vector3 deathPosition)
    {
        PlayRandomSound(deathSounds);
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
        // Camera shake should only happen on the local player's camera.
        // A simple way is to check the distance to the main camera.
        if (Camera.main != null && Vector3.Distance(Camera.main.transform.position, deathPosition) < 50f) // 50f is a large range
        {
            CameraShakerHandler.Shake(CameraShakeDeath);
        }
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // The Master Client is the only one who writes the official health value.
            if (PhotonNetwork.IsMasterClient)
            {
                stream.SendNext(currentHealth);
            }
        }
        else
        {
            // All other clients receive the health value and update their local copy.
            this.currentHealth = (int)stream.ReceiveNext();
            // You could update a health bar UI here if you have one.
        }
    }
    // Helper method to instantiate and play a particle system

    private void InstantiateAndPlayParticleSystem(ParticleSystem particleSystem, Vector3 position)
    {
        // Check if the manager and the specific sprayer blood prefab exist
        if (ObjectPoolManager.Instance != null && ObjectPoolManager.Instance.sprayerBloodHitEffectPrefab != null)
        {
            // Ask the manager for a blood effect from the specific Sprayer blood pool.
            ObjectPoolManager.Instance.SpawnFromPool(ObjectPoolManager.Instance.sprayerBloodHitEffectPrefab, position, Quaternion.identity);
        }
        else
        {
            // Fallback to the old Instantiate method if the pool system isn't ready.
            ParticleSystem instance = Instantiate(particleSystem, position, Quaternion.identity);
            instance.Play();
        }
    }

    // Coroutine to handle the flash damage effect
    private IEnumerator FlashDamage()
    {
        isFlashing = true;

        // 1. Swap to the Flash Material (using .sharedMaterial)
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].sharedMaterial = flashMaterial;
            }
        }

        yield return new WaitForSeconds(flashDuration);

        // 3. Swap back to the Original Materials (using .sharedMaterial)
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && originalMaterials[i] != null)
            {
                spriteRenderers[i].sharedMaterial = originalMaterials[i];
            }
        }

        isFlashing = false;
    }


}