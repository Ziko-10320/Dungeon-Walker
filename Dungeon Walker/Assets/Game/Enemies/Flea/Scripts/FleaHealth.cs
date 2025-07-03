using System.Collections;
using UnityEngine;
using FirstGearGames.SmoothCameraShaker;
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

  

    // Private variables
    private int currentHealth;
    private bool isKnockedBack = false; // Is the mushroom currently being knocked back?
    //CameraShake
    public ShakeData CameraShakeDeath;
    void Start()
    {
        // Initialize health
        currentHealth = maxHealth;
    }

    // Method to take damage
    public void TakeDamage(int damage, Vector2 attackDirection)
    {
        // Reduce health
        currentHealth -= (int)damage;

        // Apply knockback
        if (!isKnockedBack)
        {
            StartCoroutine(ApplyKnockback(attackDirection));
        }

        // Play blood particle effect
        if (bloodSpawnPoint != null && bloodParticle != null)
        {
            InstantiateAndPlayParticleSystem(bloodParticle, bloodSpawnPoint.position);
        }

        // Trigger flash damage effect
        StartCoroutine(FlashDamage());

        // Check if the mushroom is dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Coroutine to apply knockback using transform movement
    private IEnumerator ApplyKnockback(Vector2 attackDirection)
    {
        isKnockedBack = true;

        // Use the attack direction directly for knockback
        float knockbackDirection = Mathf.Sign(attackDirection.x); // Same as the attack direction

        // Calculate the target position for knockback
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + new Vector3(knockbackDirection * knockbackDistance, 0, 0);

        // Track elapsed time
        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            // Move the mushroom toward the end position
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

        // Destroy the mushroom
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
        if (flashMaterial == null || spriteRenderers.Length == 0)
        {
            Debug.LogError("Flash material or SpriteRenderers are not assigned.");
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
    }
}