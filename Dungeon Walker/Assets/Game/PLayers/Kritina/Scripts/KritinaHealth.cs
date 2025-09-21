using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] public int maxHealth = 100;
    [HideInInspector] public int currentHealth;
   

    [Header("Component References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private KritinaMovement movementScript;
    [SerializeField] private GameUIManager gameUIManager; // ---- UPDATED ----: Reference to the new GameUIManager

    [Header("Health Regeneration")]
    [SerializeField] private int healthPerSecond = 10;
    [SerializeField] private float delayBeforeHeal = 3f;
    private Coroutine healingCoroutine;
    private float lastDamageTime;
    [SerializeField] private ParticleSystem[] healingParticles;

    [Header("Flash Damage Effect")]
    [SerializeField] private Material flashMaterial;
    [SerializeField] private string flashAmountProperty = "_FlashAmount";
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    [Header("Post-Processing Health Effects")]
    [SerializeField] private Volume postProcessVolume;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip damageSound;
    [Range(0f, 1f)]
    [SerializeField] private float damageSoundVolume = 1f;
    [SerializeField] private CheckpointManager checkpointManager;
    private Material[] originalMaterials;
    public bool isInvincible = false;
    [Header("Shield Settings")]
    [SerializeField] private int shieldMaxHealth = 0; // 👈 this is what you see in the inspector
    private int shieldCurrentHealth = 0;

    public bool HasShield => shieldCurrentHealth > 0;

    [Header("UI References")]
    [SerializeField] private UnityEngine.UI.Slider shieldSlider;

    [HideInInspector] public bool isSuperActive = false;
  
    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (movementScript == null) movementScript = GetComponent<KritinaMovement>();

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

        // ---- UPDATED ----: Automatically find the GameUIManager if it's not assigned in the Inspector.
        if (gameUIManager == null)
        {
            gameUIManager = FindObjectOfType<GameUIManager>();
        }

        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGet(out vignette);
            postProcessVolume.profile.TryGet(out chromaticAberration);

            if (vignette != null) vignette.active = false;
            if (chromaticAberration != null) chromaticAberration.active = false;
        }
        else
        {
            Debug.LogError("Post Process Volume ou son Profile ne sont pas assignés !");
        }

        if (shieldSlider != null)
        {
            shieldSlider.gameObject.SetActive(false); // hidden at the start
        }
    }

    void Update()
    {
        if (currentHealth < maxHealth && healingCoroutine == null && Time.time > lastDamageTime + delayBeforeHeal)
        {
            healingCoroutine = StartCoroutine(HealOverTime());
        }
    }

    public void TakeDamage(int damage, float knockbackForce, Vector2 knockbackDirection)
    {
        PlayerInvisibility invis = GetComponent<PlayerInvisibility>();
        if (invis != null && invis.IsInvisible())
        {
            invis.ForceVisible();
        }
        if (isSuperActive || isInvincible)
        {
            Debug.Log("Player is invincible – no damage taken.");
            return;
        }

        // 🔹 Second: check shield
        if (HasShield)
        {
            DamageShield(damage);
            return; // damage absorbed by shield, no health loss
        }

        currentHealth -= damage;
        UpdateHealthEffects();
        Debug.Log("Player took " + damage + " damage. Current health: " + currentHealth);

        lastDamageTime = Time.time;
        if (healingCoroutine != null)
        {
            StopCoroutine(healingCoroutine);
            healingCoroutine = null;
            StopHealingParticles();
        }
        StartCoroutine(HandleHit(knockbackForce, knockbackDirection));

        if (currentHealth <= 0)
        {
            Die();
        }

        ExplosiveCoinsPowerUp explosive = GetComponent<ExplosiveCoinsPowerUp>();
        PowerUpManager powerUpManager = FindObjectOfType<PowerUpManager>();
        if (explosive != null && powerUpManager != null && powerUpManager.HasPowerUp(PowerUpType.ExplosiveCoins))
        {
            explosive.TrySpawnCoin();
        }
        PlayDamageSound();
       
    }

    private void Die()
    {
        Debug.Log("Player has died!");

        // --- Try Revive Upgraded first ---
        ReviveUpgradedSystem reviveUp = GetComponent<ReviveUpgradedSystem>();
        if (reviveUp != null && reviveUp.hasReviveUpgradedPowerUp && !reviveUp.HasUsedRevive)
        {
            Debug.Log("Revive Upgraded available — starting revive sequence.");
            reviveUp.TryRevive();
            return; // cancel death, revive instead
        }

        // --- Then try normal Revive ---
        ReviveSystem revive = GetComponent<ReviveSystem>();
        if (revive != null && revive.hasRevivePowerUp && !revive.hasUsedRevive)
        {
            Debug.Log("Revive available — starting revive sequence.");
            revive.TryRevive();
            return;
        }

        // --- Normal (final) death flow (no revive) ---
        if (checkpointManager == null)
        {
            checkpointManager = FindObjectOfType<CheckpointManager>();
        }

        if (checkpointManager != null && PlayerStatsManager.Instance != null)
        {
            int finalScore = checkpointManager.TotalScore;
            PlayerStatsManager.Instance.SetFinalScore(finalScore);
            Debug.Log("Final score of " + finalScore + " sent to PlayerStatsManager.");
        }
        else
        {
            Debug.LogWarning("Could not set final score. CheckpointManager or PlayerStatsManager not found.");
        }

        if (gameUIManager != null)
        {
            gameUIManager.ShowDeathScreen();
        }
        else
        {
            Debug.LogError("GameUIManager not found! Cannot show death screen.");
        }

        gameObject.SetActive(false); // final death
    }

    // ... The rest of your script (HealOverTime, UpdateHealthEffects, HandleHit, etc.) is unchanged ...
    // ... as it is all correct and does not need modification. I've included it below for completeness.
    public void CancelDeathState()
    {
        if (currentHealth <= 0)
        {
            // force lock health to at least 1 so death can't trigger
            currentHealth = 1;
        }
    }
    private IEnumerator HealOverTime()
    {
        Debug.Log("Health regeneration started.");
        while (currentHealth < maxHealth)
        {
            StartHealingParticles();
            currentHealth += healthPerSecond;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            UpdateHealthEffects();
            Debug.Log("Player healed. Current health: " + currentHealth);
            yield return new WaitForSeconds(1f);
        }
        Debug.Log("Health is full.");
        StopHealingParticles();
        healingCoroutine = null;
    }

    private void UpdateHealthEffects()
    {
        if (vignette == null || chromaticAberration == null) return;
        float healthPercent = (float)currentHealth / maxHealth;
        if (healthPercent <= 0.6f)
        {
            vignette.active = true;
            chromaticAberration.active = true;
        }
        else if (healthPercent >= 0.7f)
        {
            vignette.active = false;
            chromaticAberration.active = false;
        }
        if (vignette.active)
        {
            float dangerFactor = Mathf.InverseLerp(0.6f, 0.15f, healthPercent);
            dangerFactor = Mathf.Clamp01(dangerFactor);
            vignette.intensity.value = Mathf.Lerp(0, 0.5f, dangerFactor);
            chromaticAberration.intensity.value = Mathf.Lerp(0, 1.0f, dangerFactor);
        }
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
        if (isSuperActive) yield break; // ignore hits during super

        isInvincible = true;
        rb.velocity = Vector2.zero;
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        StartCoroutine(FlashDamageEffect());
        yield return new WaitForSeconds(0.3f);
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
                var main = ps.main;
                main.loop = true;
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
                var main = ps.main;
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
    public void FullHeal()
    {
        currentHealth = maxHealth;
        UpdateHealthEffects();

        // Optional: trigger healing particles instantly
        StartHealingParticles();
        StopHealingParticles();
    }

    public void ActivateShield(int health)
    {
        if (health <= 0) health = shieldMaxHealth;

        shieldMaxHealth = health;
        shieldCurrentHealth = health;

        if (shieldSlider != null)
        {
            shieldSlider.maxValue = shieldMaxHealth;
            shieldSlider.value = shieldCurrentHealth;
            shieldSlider.gameObject.SetActive(true); // show when active
        }

        Debug.Log($"Shield activated with {shieldCurrentHealth} HP.");
    }

    private void DamageShield(int damage)
    {
        shieldCurrentHealth -= damage;
        if (shieldCurrentHealth < 0) shieldCurrentHealth = 0;

        if (shieldSlider != null)
        {
            shieldSlider.value = shieldCurrentHealth;
        }

        if (shieldCurrentHealth <= 0)
        {
            Debug.Log("Shield is broken! Player is now vulnerable.");

            if (shieldSlider != null)
                shieldSlider.gameObject.SetActive(false); // hide UI when broken

            PowerUpManager powerUpManager = FindObjectOfType<PowerUpManager>();
            if (powerUpManager != null && powerUpManager.shieldAnimator != null)
            {
                powerUpManager.shieldAnimator.SetTrigger("EndShield");
                StartCoroutine(DisableShieldAfterAnim(powerUpManager));
            }
        }
        else
        {
            Debug.Log($"Shield took {damage} damage. Remaining shield HP: {shieldCurrentHealth}");
        }
    }

    private IEnumerator DisableShieldAfterAnim(PowerUpManager powerUpManager)
    {
        // Wait one frame so animator applies the trigger
        yield return null;

        // Wait until EndShield animation finishes (assuming it has length)
        AnimatorStateInfo stateInfo = powerUpManager.shieldAnimator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(stateInfo.length);

        // Disable shield object
        if (powerUpManager.shieldObject != null)
            powerUpManager.shieldObject.SetActive(false);

        // Reset animator to default (so next time it works fine)
        powerUpManager.shieldAnimator.Rebind();
        powerUpManager.shieldAnimator.Update(0f);
    }


    public void RestoreShieldToMax()
    {
        if (shieldMaxHealth <= 0) return; // No shield power-up equipped

        shieldCurrentHealth = shieldMaxHealth;

        if (shieldSlider != null)
        {
            shieldSlider.maxValue = shieldMaxHealth;
            shieldSlider.value = shieldCurrentHealth;
            shieldSlider.gameObject.SetActive(true); // ✅ Always re-enable UI
        }

        PowerUpManager powerUpManager = FindObjectOfType<PowerUpManager>();
        if (powerUpManager != null && powerUpManager.shieldObject != null)
        {
            // ✅ Reset animator BEFORE enabling
            if (powerUpManager.shieldAnimator != null)
            {
                powerUpManager.shieldAnimator.Rebind();
                powerUpManager.shieldAnimator.Update(0f);
            }

            // Enable shield object
            powerUpManager.shieldObject.SetActive(true);

            // ✅ Delay 0.2s before playing StartShield
            StartCoroutine(PlayStartShieldAnimWithDelay(powerUpManager, 0.5f));
        }

        Debug.Log("Shield fully restored!");
    }

    private IEnumerator PlayStartShieldAnimWithDelay(PowerUpManager powerUpManager, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (powerUpManager.shieldAnimator != null)
        {
            powerUpManager.shieldAnimator.SetTrigger("StartShield");
        }
    }


    public void RestoreShieldAtCheckpoint()
    {
        PowerUpManager powerUpManager = GetComponent<PowerUpManager>();

        if (powerUpManager != null && powerUpManager.HasPowerUp(PowerUpType.Shield))
        {
            shieldCurrentHealth = shieldMaxHealth;

            if (shieldSlider != null)
            {
                shieldSlider.maxValue = shieldMaxHealth;
                shieldSlider.value = shieldCurrentHealth;
                shieldSlider.gameObject.SetActive(true); // 🟢 Always bring UI back
            }

            if (powerUpManager.shieldObject != null)
            {
                powerUpManager.shieldObject.SetActive(true); // 🟢 Bring shield back

                if (powerUpManager.shieldAnimator != null)
                {
                    powerUpManager.shieldAnimator.Rebind();
                    powerUpManager.shieldAnimator.Update(0f);
                    powerUpManager.shieldAnimator.SetTrigger("StartShield");
                }
            }

            Debug.Log("Shield restored at checkpoint! 🛡️");
        }
    }



}
