using FirstGearGames.SmoothCameraShaker;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FleaHealth : MonoBehaviour, IPunObservable, IDamageable
{
    // Public variables for health and effects
    public int baseMaxHealth = 100; // Maximum health of the mushroom
    public int maxHealth;
    public GameObject deathEffect; // Optional: Effect to play when the mushroom dies
    public float knockbackDistance = 1f; // Distance the mushroom moves during knockback
    public float knockbackDuration = 0.2f; // Duration of the knockback effect
    public Transform bloodSpawnPoint; // Spawn point for blood particles
    public ParticleSystem bloodParticle; // Blood particle system
    public List<string> deathEffectNames;
    private TutorialGameManager tutorialManager;
    public AudioClip[] deathSounds;
    public Transform DeathMushroomSpawn;
    public Transform DeathMushroomSpawn2;
   
    // Flash Damage Variables
    public Material flashMaterial; // Material with the flash shader
    public string flashAmountProperty = "_FlashAmount"; // Name of the Flash Amount property in the shader
    public float flashDuration = 0.2f; // Duration of the flash effect

    // Array of SpriteRenderers for the parts of the mushroom
    public SpriteRenderer[] spriteRenderers;


    // Audio Variables
    public AudioClip damageSound; // Sound to play when taking damage
    [Range(0f, 1f)] public float damageSoundVolume = 0.7f; // Volume slider added here

    public WeaponSwitchManager weaponSwitchManager;
    public UnityEvent<GameObject> OnDeath;

    private AudioSource audioSource; // Reference to the AudioSource component

    private Material[] originalMaterials;
    // Private variables
    [HideInInspector]
    public int currentHealth;
    private bool isKnockedBack = false; // Is the mushroom currently being knocked back?
    private bool isFlashing = false; // Added to prevent multiple flash coroutines

    //CameraShake
    public ShakeData CameraShakeDeath;
    public bool isStunned = false;
    private PhotonView view;
    public GameObject deathSplatterEffectPrefab;
    public Transform splatterSpawnPoint;

    [Header("Scaling Per Wave")]
    [Tooltip("How much extra health the flea gets for each wave it survives after its first appearance.")]
    public int healthIncreasePerWave = 10;
    [Tooltip("How much extra damage the flea's attack gets per wave.")]
    public int damageIncreasePerWave = 5;
    [Tooltip("How much extra chase speed the flea gets per wave.")]
    public float chaseSpeedIncreasePerWave = 0.5f;

    // Internal memory for this specific flea instance
    public int firstSpawnWave { get; private set; } = -1;
    void OnEnable()
    {
        // --- 1. GET THE TIER MULTIPLIER ---
        float tierMultiplier = 1.0f;
        if (StatMultiplierManager.Instance != null)
        {
            tierMultiplier = StatMultiplierManager.Instance.FleaMultiplier;
        }

        // --- 2. WAVE SCALING LOGIC ---
        if (firstSpawnWave == -1)
        {
            firstSpawnWave = ScoreDisplay.CurrentWaveNumber;
        }
        int wavesSurvived = ScoreDisplay.CurrentWaveNumber - firstSpawnWave;
        if (wavesSurvived < 0) wavesSurvived = 0;

        // --- 3. APPLY ALL SCALING ---
        // Apply TIER multiplier to BASE health, then add the WAVE bonus.
        maxHealth = Mathf.RoundToInt(baseMaxHealth * tierMultiplier) + (wavesSurvived * healthIncreasePerWave);
        currentHealth = maxHealth;

        // --- 4. YOUR EXISTING RESET LOGIC (UNCHANGED) ---
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

        var followScript = GetComponent<FleaFollow>();
        if (followScript != null)
        {
            followScript.enabled = true;
            followScript.InitializeAndReset(player);
        }
        var attackScript = GetComponent<FleaChargeAttack>();
        if (attackScript != null)
        {
            attackScript.enabled = true;
            // The attack script will now set its own damage in its OnEnable.
        }

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
    public void SetTutorialManager(TutorialGameManager manager)
    {
        this.tutorialManager = manager;
    }

    private void PlayRandomSound(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;

        int randomIndex = Random.Range(0, clips.Length);
        AudioClip clipToPlay = clips[randomIndex];
        if (clipToPlay == null) return;

        // --- STEP 1: Create a clean, independent object for the sound ---
        // We name it to make it easy to find in the Hierarchy if we need to debug it.
        GameObject soundPlayerObject = new GameObject("FORCE_PLAY_DEATH_SOUND");

        // --- STEP 2: Position it directly on the camera ---
        // This completely eliminates 3D audio falloff. A sound playing from the listener's exact position
        // will always be at maximum 3D volume.
        soundPlayerObject.transform.position = Camera.main.transform.position;

        // --- STEP 3: Add and aggressively configure the AudioSource ---
        AudioSource tempAudioSource = soundPlayerObject.AddComponent<AudioSource>();

        tempAudioSource.clip = clipToPlay;

        // --- CRITICAL OVERRIDES ---
        tempAudioSource.volume = 1.0f;                  // Force volume to 100%. We ignore the script's public variable for this test.
        tempAudioSource.spatialBlend = 0.0f;              // Force it to be a 2D sound. This is the most important setting.
        tempAudioSource.priority = 0;                     // Set to highest priority so Unity doesn't deprioritize or cull it.
        tempAudioSource.bypassEffects = true;             // Ignore any Audio Mixer effects that might be reducing volume.
        tempAudioSource.bypassListenerEffects = true;     // Ignore effects on the camera's Audio Listener.
        tempAudioSource.bypassReverbZones = true;         // Ignore any reverb zones.

        // --- STEP 4: Play the sound and schedule its destruction ---
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

        // Get or add the AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure AudioSource
        audioSource.playOnAwake = false; // Don't play sound on start
        audioSource.spatialBlend = 1.0f; // 3D sound
        audioSource.volume = damageSoundVolume; // Set initial volume

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

    // Method to handle death
    public void Die(GameObject attacker = null)
    {
        SoulLinkEnemy soul = GetComponent<SoulLinkEnemy>();
        if (soul != null && soul.inChain)
        {
            // Notify the chain BEFORE we destroy the GameObject so the chain can capture position/linePoint.
            soul.NotifyDied();
        }
        if (tutorialManager != null)
        {
            tutorialManager.OnEnemyDefeated();
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

            if (attacker != null && attacker.CompareTag("Projectile"))
            {
                OnDeath?.Invoke(attacker); // Invoque l'événement uniquement pour les projectiles
            }

            if (weaponSwitchManager != null)
            {
                weaponSwitchManager.OnEnemyKilled();
                Debug.Log("Enemy died, notifying WeaponSwitchManager.");
            }

           

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
        // Check if the manager and the specific flea blood prefab exist
        if (ObjectPoolManager.Instance != null && ObjectPoolManager.Instance.fleaBloodHitEffectPrefab != null)
        {
            // Ask the manager for a blood effect from the specific Flea blood pool.
            ObjectPoolManager.Instance.SpawnFromPool(ObjectPoolManager.Instance.fleaBloodHitEffectPrefab, position, Quaternion.identity);
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