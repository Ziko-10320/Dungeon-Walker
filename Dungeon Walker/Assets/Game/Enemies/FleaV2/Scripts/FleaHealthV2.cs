using FirstGearGames.SmoothCameraShaker;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class FleaHealthV2 : MonoBehaviour, IPunObservable, IDamageable
{
    // Public variables for health and effects
    public int baseMaxHealth = 100;
    public int maxHealth = 100; // Maximum health of the mushroom
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

    [Header("V2 Death Explosion")]
    [Tooltip("The material used to signal the upcoming explosion.")]
    public Material flashSignalMaterial;
    [Tooltip("The duration of the pre-explosion warning signal.")]
    public float signalDuration = 1f;
    [Tooltip("How fast the signal flashes. Higher is faster.")]
    public float blinkInterval = 0.1f;
    [Tooltip("The radius of the explosion damage area.")]
    public float explosionRadius = 2.5f;
    [Tooltip("The damage dealt by the explosion.")]
    public int explosionDamage = 50;
    [Tooltip("The layer the player is on, for explosion detection.")]
    public LayerMask playerLayer;
    private static MaterialPropertyBlock propertyBlock;

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
    [Tooltip("How much extra health the flea gets for each wave it survives after its first appearance.")]
    public int healthIncreasePerWave = 10;
    [Tooltip("How much extra damage the flea's attack gets per wave.")]
    public int damageIncreasePerWave = 5;
    [Tooltip("How much extra chase speed the flea gets per wave.")]
    public float chaseSpeedIncreasePerWave = 0.5f;

    // Internal memory for this specific flea instance
    private int firstSpawnWave = -1;
    void OnEnable()
    {
        // --- 1. THE SCALING LOGIC ---
        if (firstSpawnWave == -1)
        {
            // This is the first time this flea has ever spawned.
            // Record the current wave as its "birth" wave.
            firstSpawnWave = ScoreDisplay.CurrentWaveNumber;
        }

        // Calculate how many waves this flea has "survived" since its first appearance.
        int wavesSurvived = ScoreDisplay.CurrentWaveNumber - firstSpawnWave;
        if (wavesSurvived < 0) wavesSurvived = 0; // Safety check

        // --- 2. CALCULATE AND APPLY NEW STATS ---
        // Health (handled by this script)
        maxHealth = baseMaxHealth + (wavesSurvived * healthIncreasePerWave);
        currentHealth = maxHealth;

        // Get references to the other scripts on this enemy
        var followScript = GetComponent<FleaFollow>();
        var attackScript = GetComponent<FleaChargeAttack>();

        // Speed (tell the follow script its new speed)
        if (followScript != null)
        {
            // We get the base speed from the script's range, then add the bonus.
            float baseChaseSpeed = Random.Range(followScript.chaseSpeedRange.x, followScript.chaseSpeedRange.y);
            followScript.chaseSpeed = baseChaseSpeed + (wavesSurvived * chaseSpeedIncreasePerWave);
        }

        // Damage (tell the attack script its new damage)
        if (attackScript != null)
        {
            attackScript.attackDamage = attackScript.baseAttackDamage + (wavesSurvived * damageIncreasePerWave);
        }

        // This is the guaranteed reset for pooled enemies.
        currentHealth = maxHealth;
        isKnockedBack = false;
        isFlashing = false;
        isStunned = false;
        if (spriteRenderers != null && originalMaterials != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null && i < originalMaterials.Length && originalMaterials[i] != null)
                {
                    spriteRenderers[i].sharedMaterial = originalMaterials[i];
                }
            }
        }
        // --- END OF FIX ---

        // Re-enable colliders
        foreach (var col in GetComponents<Collider2D>())
        {
            col.enabled = true;
        }

        // Your existing logic for finding the player and initializing other scripts is good.
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            gameObject.SetActive(false); // Disable if no player exists.
            return;
        }
        Transform player = playerObject.transform;

        if (followScript != null)
        {
            followScript.enabled = true;
            followScript.InitializeAndReset(player);
        }
        if (attackScript != null)
        {
            attackScript.enabled = true;
            attackScript.InitializeAndReset(player);
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
        GameObject soundPlayerObject = new GameObject("V2_FORCE_PLAY_DEATH_SOUND");

        // Position it directly on the camera to guarantee it's heard
        soundPlayerObject.transform.position = Camera.main.transform.position;

        // Add and aggressively configure the AudioSource
        AudioSource tempAudioSource = soundPlayerObject.AddComponent<AudioSource>();

        tempAudioSource.clip = clipToPlay;

        // --- CRITICAL OVERRIDES ---
        tempAudioSource.volume = this.deathSoundVolume;  // Use the new public variable for control
        tempAudioSource.spatialBlend = 0.0f;              // Force 2D sound
        tempAudioSource.priority = 0;                     // Highest priority
        tempAudioSource.bypassEffects = true;             // Ignore mixers
        tempAudioSource.bypassListenerEffects = true;     // Ignore listener effects
        tempAudioSource.bypassReverbZones = true;         // Ignore reverb zones

        // Play the sound and schedule its destruction
        tempAudioSource.Play();
        Destroy(soundPlayerObject, clipToPlay.length);
    }
    void Awake()
    {
        // Get or add the AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = damageSoundVolume;
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

    void Start()
    {
        // Start can now be much simpler.
        view = GetComponent<PhotonView>();
        if (weaponSwitchManager == null)
        {
            weaponSwitchManager = FindObjectOfType<WeaponSwitchManager>();
        }
        // Initialize health
        currentHealth = maxHealth;
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
        if (currentHealth <= 0) return;
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
        if (currentHealth <= 0)
        {
            // If it has, immediately start the death sequence.
            Die(null);
        }
        else
        {
            // If the enemy is STILL ALIVE, then it's safe to play the normal damage flash.
            if (!isFlashing)
            {
                StartCoroutine(FlashDamage());
            }
        }
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
        if (currentHealth <= 0) return;
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
    public void ForceDieByChain()
    {
        // If the death sequence is already running, do nothing.
        // This check is important to prevent multiple explosions.
        if (currentHealth <= 0) return;

        // --- THIS IS THE CRITICAL FIX ---
        // Force health to 0 to mark it as "in the death process".
        currentHealth = 0;

        // Find the SoulLink component and notify it, just like the normal Die() method.
        // This is for consistency, though the chain already knows.
        SoulLinkEnemy soul = GetComponent<SoulLinkEnemy>();
        if (soul != null && soul.inChain)
        {
            soul.NotifyDied();
        }

        // Immediately start the death sequence.
        StartCoroutine(DeathSequenceRoutine());
    }
    // Method to handle death
    public void Die(GameObject attacker = null)
    {
        // If health is already <= 0, the death sequence has already started.
        // This check prevents the Die() method from being called multiple times.
        if (currentHealth > 0) return;
       
        // The only job of Die() now is to start the death sequence coroutine.
        StartCoroutine(DeathSequenceRoutine());
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

    private IEnumerator DeathSequenceRoutine()
    {
        // --- 1. PRE-DEATH SETUP (Disable AI and Colliders) ---
        // Stop all other logic (like movement and attacks).
        var followScript = GetComponent<FleaFollow>(); // Assuming V2 script
        if (followScript != null) followScript.enabled = false;

        var attackScript = GetComponent<FleaChargeAttack>(); // Assuming V2 script
        if (attackScript != null) attackScript.enabled = false;

        // Disable colliders so it can't be hit or block things anymore.
        foreach (var col in GetComponents<Collider2D>())
        {
            col.enabled = false;
        }

        float timer = 0f;
        while (timer < signalDuration)
        {
            // --- SWAP TO SIGNAL MATERIAL ---
            if (flashSignalMaterial != null)
            {
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] != null)
                    {
                        spriteRenderers[i].sharedMaterial = flashSignalMaterial;
                    }
                }
            }
            yield return new WaitForSeconds(blinkInterval / 2); // Wait for half the interval

            // --- SWAP BACK TO ORIGINAL MATERIAL ---
            if (originalMaterials != null)
            {
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] != null && i < originalMaterials.Length && originalMaterials[i] != null)
                    {
                        spriteRenderers[i].sharedMaterial = originalMaterials[i];
                    }
                }
            }
            yield return new WaitForSeconds(blinkInterval / 2); // Wait for the other half

            timer += blinkInterval; // Advance the main timer by one full blink cycle
        }

        // --- 3. THE NEW EXPLOSION ---
        Collider2D[] playersToDamage = Physics2D.OverlapCircleAll(bloodSpawnPoint.position, explosionRadius, playerLayer);
        foreach (var playerCollider in playersToDamage)
        {
           PlayerHealth playerHealth = playerCollider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Vector2 knockbackDirection = (playerCollider.transform.position - bloodSpawnPoint.position).normalized;
                playerHealth.TakeDamage(explosionDamage, 15f, knockbackDirection);
            }
            L3antixHealth L3antixHealth = playerCollider.GetComponent<L3antixHealth>();
            if (L3antixHealth != null)
            {
                Vector2 knockbackDirection = (playerCollider.transform.position - bloodSpawnPoint.position).normalized;
                L3antixHealth.TakeDamage(explosionDamage, 15f, knockbackDirection);
            }
        }

        // --- 4. YOUR ORIGINAL DEATH LOGIC (PRESERVED) ---
        // This block is taken directly from your original Die() method.
        SoulLinkEnemy soul = GetComponent<SoulLinkEnemy>();
        if (soul != null && soul.inChain)
        {
            soul.NotifyDied();
        }

        if (PhotonNetwork.IsMasterClient || !PhotonNetwork.IsConnected)
        {
            if (view != null && PhotonNetwork.IsConnected)
            {
                view.RPC("RPC_PlayDeathEffects", RpcTarget.All, transform.position);
            }
            else
            {
                PlayDeathEffects(transform.position);
            }

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
                    ObjectPoolManager.Instance.SpawnFromPool(deathSplatterEffectPrefab, spawnPosition, Quaternion.identity);
                }
                gameObject.SetActive(false); // Return to pool
            }
        }
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
        ParticleSystem instance = Instantiate(particleSystem, position, Quaternion.identity);
        instance.Play();
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
        if (currentHealth <= 0)
        {
            isFlashing = false; // Still reset the flag
            yield break;        // Exit the coroutine RIGHT NOW. Do not execute any more code.
        }
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
    void OnDrawGizmosSelected()
    {
        // Set the color for the gizmo. Red is good for damage areas.
        Gizmos.color = Color.red;

        // Draw a wireframe sphere at the enemy's position with the explosionRadius.
        // This will visually represent the area of effect for the death explosion.
        Gizmos.DrawWireSphere(bloodSpawnPoint.position, explosionRadius);
    }

}