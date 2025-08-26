using FirstGearGames.SmoothCameraShaker;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
public class FleaHealth : MonoBehaviour
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

    // Audio Variables
    public AudioClip damageSound; // Sound to play when taking damage
    [Range(0f, 1f)] public float damageSoundVolume = 0.7f; // Volume slider added here

    public WeaponSwitchManager weaponSwitchManager;
    public UnityEvent<GameObject> OnDeath;

    private AudioSource audioSource; // Reference to the AudioSource component

    // Private variables
    private int currentHealth;
    private bool isKnockedBack = false; // Is the mushroom currently being knocked back?
    private bool isFlashing = false; // Added to prevent multiple flash coroutines

    //CameraShake
    public ShakeData CameraShakeDeath;

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
    }

    // Method to take damage
    public void TakeDamage(float damage, Vector2 attackDirection, float knockbackForce = 1f, GameObject attacker = null)
    {
        // Reduce health
        currentHealth -= (int)damage;

        // Play damage sound if assigned
        if (damageSound != null && audioSource != null)
        {
            audioSource.volume = damageSoundVolume; // Apply volume
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
            // Passe l'attaquant à la fonction Die
            Die(attacker);
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
    private void Die(GameObject attacker = null)
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

        if (attacker != null && attacker.CompareTag("Projectile"))
        {
            OnDeath?.Invoke(attacker); // Invoque l'événement uniquement pour les projectiles
        }

        if (weaponSwitchManager != null)
        {
            weaponSwitchManager.OnEnemyKilled();
            Debug.Log("Enemy died, notifying WeaponSwitchManager.");
        }

        // Destroy the mushroom
        Destroy(gameObject);
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

        if (flashMaterial == null || spriteRenderers.Length == 0)
        {
            Debug.LogError("Flash material or SpriteRenderers are not assigned.");
            isFlashing = false;
            yield break;
        }

        Material[] originalMaterials = new Material[spriteRenderers.Length];
        Material[] flashMaterialInstances = new Material[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                originalMaterials[i] = spriteRenderers[i].material;
                flashMaterialInstances[i] = new Material(flashMaterial);
                spriteRenderers[i].material = flashMaterialInstances[i];
            }
        }

        float elapsed = 0f;
        while (elapsed < flashDuration / 2)
        {
            float flashAmount = Mathf.Lerp(0, 1, elapsed / (flashDuration / 2));
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

        elapsed = 0f;
        while (elapsed < flashDuration / 2)
        {
            float flashAmount = Mathf.Lerp(1, 0, elapsed / (flashDuration / 2));
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

        foreach (var material in flashMaterialInstances)
        {
            if (material != null)
            {
                material.SetFloat(flashAmountProperty, 0);
            }
        }

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