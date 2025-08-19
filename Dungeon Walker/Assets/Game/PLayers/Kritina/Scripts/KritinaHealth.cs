using System.Collections;
using UnityEngine;
using UnityEngine.Rendering; // Requis pour accéder aux Volumes
using UnityEngine.Rendering.Universal; // Requis pour les effets spécifiques de l'URP (si tu utilises l'URP)

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("Component References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private KritinaMovement movementScript;

    [Header("Health Regeneration")]
    [SerializeField] private int healthPerSecond = 10; // Points de vie régénérés par seconde
    [SerializeField] private float delayBeforeHeal = 3f; // Temps à attendre sans dégâts avant de commencer à soigner
    private Coroutine healingCoroutine; // Pour garder une référence à notre processus de soin
    private float lastDamageTime; // Pour savoir quand le joueur a pris des dégâts pour la dernière fois
    [SerializeField] private ParticleSystem[] healingParticles;
    [Header("Flash Damage Effect")]
    [SerializeField] private Material flashMaterial; // Material with the flash shader
    [SerializeField] private string flashAmountProperty = "_FlashAmount"; // Name of the Flash Amount property in the shader
    [SerializeField] private float flashDuration = 0.2f; // Duration of the flash effect
    [SerializeField] private SpriteRenderer[] spriteRenderers; // Array of all player part sprites

    [Header("Post-Processing Health Effects")]
    [SerializeField] private Volume postProcessVolume; // Fais glisser ton objet Global Volume ici
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip damageSound; // Sound played when taking damage
    [Range(0f, 1f)]
    [SerializeField] private float damageSoundVolume = 1f;

    private Material[] originalMaterials; // To store original materials
    private bool isInvincible = false;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (movementScript == null) movementScript = GetComponent<KritinaMovement>();


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
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            // Tente de récupérer les effets depuis le profile.
            postProcessVolume.profile.TryGet(out vignette);
            postProcessVolume.profile.TryGet(out chromaticAberration);

            // Désactive les effets au cas où ils seraient restés actifs dans l'éditeur.
            if (vignette != null) vignette.active = false;
            if (chromaticAberration != null) chromaticAberration.active = false;
        }
        else
        {
            Debug.LogError("Post Process Volume ou son Profile ne sont pas assignés !");
        }
    }
    void Update()
    {
        // Si le joueur n'a pas toute sa vie, qu'il n'est pas déjà en train de se soigner,
        // et que le délai depuis le dernier dégât est écoulé...
        if (currentHealth < maxHealth && healingCoroutine == null && Time.time > lastDamageTime + delayBeforeHeal)
        {
            // ...alors on commence la régénération.
            healingCoroutine = StartCoroutine(HealOverTime());
        }
    }
    private IEnumerator HealOverTime()
    {
        Debug.Log("Health regeneration started.");

        // Tant que la vie n'est pas au maximum...
        while (currentHealth < maxHealth)
        {
            StartHealingParticles();
            // ...on ajoute de la vie et on attend une seconde.
            currentHealth += healthPerSecond;

            // On s'assure de ne pas dépasser la vie maximale.
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            UpdateHealthEffects();
            Debug.Log("Player healed. Current health: " + currentHealth);
            yield return new WaitForSeconds(1f); // Attend 1 seconde avant la prochaine régénération
        }

        Debug.Log("Health is full.");
        StopHealingParticles();
        healingCoroutine = null; // Réinitialise la référence une fois la vie pleine.
    }
    private void UpdateHealthEffects()
    {
        if (vignette == null || chromaticAberration == null) return;

        // 1. Calculer le pourcentage de vie actuel (de 1.0 à 0.0)
        float healthPercent = (float)currentHealth / maxHealth;

        // 2. Gérer l'activation/désactivation des effets
        if (healthPercent <= 0.6f) // En dessous de 60% de vie
        {
            vignette.active = true;
            chromaticAberration.active = true;
        }
        else if (healthPercent >= 0.7f) // Au-dessus de 70% de vie
        {
            vignette.active = false;
            chromaticAberration.active = false;
        }

        // 3. Calculer et appliquer l'intensité si les effets sont actifs
        if (vignette.active) // ou chromaticAberration.active, les deux sont liés
        {
            // On calcule un "facteur de danger" qui va de 0 (à 60% de vie) à 1 (à 15% de vie)
            // La fonction InverseLerp est parfaite pour ça !
            float dangerFactor = Mathf.InverseLerp(0.6f, 0.15f, healthPercent);
            dangerFactor = Mathf.Clamp01(dangerFactor); // On s'assure que la valeur reste entre 0 et 1

            // Appliquer l'intensité en fonction du facteur de danger
            vignette.intensity.value = Mathf.Lerp(0, 0.5f, dangerFactor); // 0 -> 0.5
            chromaticAberration.intensity.value = Mathf.Lerp(0, 1.0f, dangerFactor); // 0 -> 1.0
        }
    }

    public void TakeDamage(int damage, float knockbackForce, Vector2 knockbackDirection)
    {
        if (isInvincible) return;

        currentHealth -= damage;
        UpdateHealthEffects();
        Debug.Log("Player took " + damage + " damage. Current health: " + currentHealth);

        lastDamageTime = Time.time; // Enregistre le moment du dégât
        if (healingCoroutine != null)
        {
            StopCoroutine(healingCoroutine); // Arrête la régénération en cours
            healingCoroutine = null;
            StopHealingParticles();
        }
        StartCoroutine(HandleHit(knockbackForce, knockbackDirection));

        if (currentHealth <= 0)
        {
            Die();
        }

        PlayDamageSound();
    }

    private void PlayDamageSound()
    {
        if (damageSound != null)
        {
            AudioSource.PlayClipAtPoint(damageSound, transform.position, damageSoundVolume);
        }
    }

    private IEnumerator HandleHit(float knockbackForce, Vector2 knockbackDirection)
    {
        isInvincible = true;

        rb.velocity = Vector2.zero;
        // Apply knockback force in the calculated direction (including X and Y components)
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

        // Start the new flash material effect
        StartCoroutine(FlashDamageEffect());

        // if (movementScript != null) movementScript.enabled = false;
        // if (dashScript != null) dashScript.enabled = false;

        yield return new WaitForSeconds(0.3f);

        // if (movementScript != null) movementScript.enabled = true;
        // if (dashScript != null) dashScript.enabled = true;

        yield return new WaitForSeconds(0.2f);

        isInvincible = false;
    }

    private void StartHealingParticles()
    {
        if (healingParticles == null) return;

        foreach (ParticleSystem ps in healingParticles)
        {
            if (ps != null)
            {
                // 1. On accède au module 'main' pour changer ses propriétés
                var main = ps.main;

                // 2. On active la boucle
                main.loop = true;

                // 3. On s'assure que le système joue (s'il ne jouait pas déjà)
                if (!ps.isPlaying)
                {
                    ps.Play();
                }
            }
        }
    }

    private void StopHealingParticles()
    {
        if (healingParticles == null) return;

        foreach (ParticleSystem ps in healingParticles)
        {
            if (ps != null)
            {
                // 1. On accède au module 'main'
                var main = ps.main;

                // 2. On désactive la boucle. 
                // Le système de particules terminera son cycle actuel et s'arrêtera naturellement.
                main.loop = false;
            }
        }
    }


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

