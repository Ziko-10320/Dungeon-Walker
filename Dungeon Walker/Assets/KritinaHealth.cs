using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("Component References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private KritinaMovement movementScript;
    [SerializeField] private PlayerDash dashScript;

    [Header("Flash Damage Effect")]
    [SerializeField] private Material flashMaterial; // Material with the flash shader
    [SerializeField] private string flashAmountProperty = "_FlashAmount"; // Name of the Flash Amount property in the shader
    [SerializeField] private float flashDuration = 0.2f; // Duration of the flash effect
    [SerializeField] private SpriteRenderer[] spriteRenderers; // Array of all player part sprites

    private Material[] originalMaterials; // To store original materials
    private bool isInvincible = false;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (movementScript == null) movementScript = GetComponent<KritinaMovement>();
        if (dashScript == null) dashScript = GetComponent<PlayerDash>();

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

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage, float knockbackForce, Vector2 knockbackDirection)
    {
        if (isInvincible) return;

        currentHealth -= damage;
        Debug.Log("Player took " + damage + " damage. Current health: " + currentHealth);

        StartCoroutine(HandleHit(knockbackForce, knockbackDirection));

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator HandleHit(float knockbackForce, Vector2 knockbackDirection)
    {
        isInvincible = true;

        rb.velocity = Vector2.zero;
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

        // Start the new flash material effect
        StartCoroutine(FlashDamageEffect());

        if (movementScript != null) movementScript.enabled = false;
        if (dashScript != null) dashScript.enabled = false;

        yield return new WaitForSeconds(0.3f);

        if (movementScript != null) movementScript.enabled = true;
        if (dashScript != null) dashScript.enabled = true;

        yield return new WaitForSeconds(0.2f);

        isInvincible = false;
    }

    // NEW: Flash material logic, adapted from your FleaHealth script
    private IEnumerator FlashDamageEffect()
    {
        if (flashMaterial == null || spriteRenderers.Length == 0)
        {
            Debug.LogError("Flash material or SpriteRenderers are not assigned on PlayerHealth.");
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
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        gameObject.SetActive(false);
    }
}
