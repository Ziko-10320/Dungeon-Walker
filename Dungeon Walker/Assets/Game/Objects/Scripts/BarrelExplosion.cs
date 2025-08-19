using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FirstGearGames.SmoothCameraShaker;

public class BarrelExplosion : MonoBehaviour
{
    [Header("Barrel Health Settings")]
    [Tooltip("Maximum health of the barrel.")]
    public int maxHealth = 50;
    [Tooltip("Current health of the barrel.")]
    private int currentHealth;

    [Header("Explosion Settings")]
    [Tooltip("Radius of the explosion effect.")]
    public float explosionRadius = 3f;
    [Tooltip("Damage dealt to players within the explosion radius.")]
    public int explosionDamage = 30;
    [Tooltip("LayerMask to identify objects that can be damaged by the explosion (e.g., Player layer).")]
    public LayerMask damageableLayers;

    [Header("Particle System Settings")]
    [Tooltip("Array of Particle Systems to be triggered on explosion.")]
    public ParticleSystem[] explosionParticles;
    [Tooltip("Array of Transforms representing spawn points for explosion particles. Must match the size of Explosion Particles array.")]
    public Transform[] particleSpawnPoints;

    [Header("Visual Feedback")]
    [Tooltip("Material with the flash shader for damage feedback.")]
    public Material flashMaterial;
    [Tooltip("Name of the Flash Amount property in the shader.")]
    public string flashAmountProperty = "_FlashAmount";
    [Tooltip("Duration of the flash effect when taking damage.")]
    public float flashDuration = 0.1f;
    [Tooltip("Array of SpriteRenderers for the barrel parts to apply flash effect.")]
    public SpriteRenderer[] spriteRenderers;

    private Material[] originalMaterials; // To store original materials
    private bool isFlashing = false; // Added to prevent multiple flash coroutines
    public ShakeData CameraShakeDeath;
    void Start()
    {
        currentHealth = maxHealth;

        // Store original materials for all sprite renderers
        originalMaterials = new Material[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                originalMaterials[i] = spriteRenderers[i].material;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Barrel took {damage} damage. Current health: {currentHealth}");

        if (!isFlashing)
        {
            StartCoroutine(FlashDamage());
        }

        if (currentHealth <= 0)
        {
            Explode();
        }
    }

    private void Explode()
    {
        Debug.Log("Barrel exploded!");

        // Trigger explosion particles
        if (explosionParticles != null && particleSpawnPoints != null)
        {
            for (int i = 0; i < explosionParticles.Length; i++)
            {
                if (explosionParticles[i] != null && i < particleSpawnPoints.Length && particleSpawnPoints[i] != null)
                {
                    InstantiateAndPlayParticleSystem(explosionParticles[i], particleSpawnPoints[i].position);
                }
                else if (explosionParticles[i] != null)
                {
                    // Fallback to barrel's position if no specific spawn point is assigned for this particle system
                    InstantiateAndPlayParticleSystem(explosionParticles[i], transform.position);
                }
            }
        }

        // Deal damage to players within range
        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(transform.position, explosionRadius, damageableLayers);
        foreach (Collider2D hit in hitObjects)
        {
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Assuming PlayerHealth has a TakeDamage method that accepts int damage, float knockbackForce, Vector2 knockbackDirection
                // For simplicity, we'll just pass damage here. You might want to add knockback for the explosion.
                playerHealth.TakeDamage(explosionDamage, 0f, Vector2.zero); // No knockback for now
                Debug.Log($"Player {hit.name} took {explosionDamage} damage from barrel explosion.");
            }
        }
        CameraShakerHandler.Shake(CameraShakeDeath);
        Destroy(gameObject);
    }

    // Helper method to instantiate and play a particle system
    private void InstantiateAndPlayParticleSystem(ParticleSystem particleSystem, Vector3 position)
    {
        ParticleSystem instance = Instantiate(particleSystem, position, Quaternion.identity);
        instance.Play();
    }

    private IEnumerator FlashDamage()
    {
        isFlashing = true;

        if (flashMaterial == null || spriteRenderers.Length == 0)
        {
            Debug.LogError("Flash material or SpriteRenderers are not assigned on BarrelExplosionSystem.");
            isFlashing = false;
            yield break;
        }

        Material[] flashMaterialInstances = new Material[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
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
                if (material != null) material.SetFloat(flashAmountProperty, flashAmount);
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
                if (material != null) material.SetFloat(flashAmountProperty, flashAmount);
            }
            elapsed += Time.deltaTime;
            yield return null;
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

    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
