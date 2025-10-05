using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;
public class SprayerHealth : MonoBehaviour, IPunObservable
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
        // This is the guaranteed reset for pooled enemies.
        currentHealth = maxHealth;
        isKnockedBack = false;
        isFlashing = false;
        isStunned = false; // Assuming you have this variable

        // If the enemy has a movement script, re-enable it.
        var SprayerFollow = GetComponent<SprayerFollow>();
        if (SprayerFollow != null)
        {
            SprayerFollow.enabled = true;
        }
        var SprayerAttack = GetComponent<SprayerAttack>();
        if (SprayerAttack != null)
        {
            SprayerAttack.enabled = true;
        }
        if (spriteRenderers != null && originalMaterials != null && spriteRenderers.Length == originalMaterials.Length)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null && originalMaterials[i] != null)
                {
                    // --- CHANGE THIS LINE ---
                    spriteRenderers[i].sharedMaterial = originalMaterials[i];
                }
            }
        }
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
    public void TakeDamage(float damage, Vector2 attackDirection, float knockbackForce = 1f, GameObject attacker = null)
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
          
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
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