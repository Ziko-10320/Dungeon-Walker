using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D.Animation;
using FirstGearGames.SmoothCameraShaker;
using UnityEngine.UI;
public class L3antixHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] public int maxHealth = 100;
    [HideInInspector] public int currentHealth;
    private Animator animator;
    [SerializeField] private AudioClip deathAnimationSound;
    [Header("Component References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private L3antixMovement L3antixMovement;
    [SerializeField] private GameUIManager gameUIManager; // ---- UPDATED ----: Reference to the new GameUIManager

    [Header("Health Regeneration")]
    [SerializeField] private int healthPerSecond = 10;
    [SerializeField] private float delayBeforeHeal = 3f;
    [Header("Bee PowerUp Synergy")]
    [SerializeField] private int beeHealthPerSecond = 20;
    [SerializeField] private float beeDelayBeforeHeal = 1.5f;
    private Coroutine healingCoroutine;
    private float lastDamageTime;
    [SerializeField] private ParticleSystem[] healingParticles;

    [Header("Flash Damage Effect")]
    [SerializeField] private Material flashMaterial;
    [SerializeField] private string flashAmountProperty = "_FlashAmount";
    [SerializeField] private string mainTextureProperty = "_MainTex";
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
    public int upgradedShieldMaxHealth = 100;
    private int shieldCurrentHealth = 0;

    private bool usingUpgradedShield = false;
    public bool HasShield => shieldCurrentHealth > 0;

    [Header("Shield Stacking & Explosion")]
    [Tooltip("The exact point where the shield explosion will originate.")]
    [SerializeField] private Transform shieldExplosionPoint;
    [Tooltip("The damage dealt by the normal shield's explosion when it breaks.")]
    [SerializeField] private int shieldExplosionDamage = 50;
    [Tooltip("The radius of the normal shield's explosion.")]
    [SerializeField] private float shieldExplosionRadius = 3f;
    private List<PowerUpType> shieldQueue = new List<PowerUpType>();

    private static MaterialPropertyBlock propertyBlock;
    [Header("UI References")]
    [SerializeField] private UnityEngine.UI.Slider shieldSlider;

    [HideInInspector] public bool isSuperActive = false;
    [SerializeField] private PlayerInvisibility3antix playerInvisibility;
    public ShakeData CameraShakeDeath;
    private PowerUpManagerL3antix PowerUpManagerL3antix;

    [Header("Revive Settings")]
    [Tooltip("The UI panel that asks the player if they want to watch an ad to revive.")]
    [SerializeField] private GameObject reviveRequestPanel;
    [Tooltip("The maximum number of times the player can revive per life/run.")]
    [SerializeField] private int maxRevives = 2;
    [Tooltip("Particle effects to play when the player is revived.")]
    [SerializeField] private ParticleSystem[] reviveParticles;
    private int revivesUsed = 0;
    private bool isDead = false;

    [Header("Death Sequence Settings")]
    [Tooltip("A black UI Image that covers the screen and can be faded in.")]
    [SerializeField] private Image deathFadeImage;
    [Tooltip("The name of the Sorting Layer to put the player on during death (e.g., 'OnTop').")]
    [SerializeField] private string onTopSortingLayerName = "OnTop";
    [Tooltip("How long the screen takes to fade to black.")]
    [SerializeField] private float fadeDuration = 0.5f;
    [Tooltip("The delay after the screen is black before showing revive options.")]
    [SerializeField] private float postFadeDelay = 1.0f;
    private Dictionary<SpriteRenderer, int> originalSortingLayers = new Dictionary<SpriteRenderer, int>();
    [Header("Death Camera Zoom")]
    [Tooltip("The main camera to control for the zoom effect.")]
    [SerializeField] private Camera mainCamera;
    [Tooltip("The target orthographic size for the zoom-in.")]
    [SerializeField] private float zoomInSize = 3f;
    [Tooltip("How long the zoom-in effect should take.")]
    [SerializeField] private float zoomDuration = 1.0f;
    private float originalCameraSize;
    private bool isReviving = false;
    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (L3antixMovement == null) L3antixMovement = GetComponent<L3antixMovement>();
        if (playerInvisibility == null) playerInvisibility = GetComponent<PlayerInvisibility3antix>();
        originalMaterials = new Material[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                originalMaterials[i] = spriteRenderers[i].sharedMaterial;
            }
        }
        PowerUpManagerL3antix = FindObjectOfType<PowerUpManagerL3antix>();
        if (PowerUpManagerL3antix == null)
        {
            Debug.LogWarning("PlayerHealth could not find PowerUpManager on Awake.");
        }
        foreach (var renderer in spriteRenderers)
        {
            if (renderer != null)
            {
                // THIS IS THE FIX: We store the ID of the layer itself.
                originalSortingLayers[renderer] = renderer.sortingLayerID;
            }
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        if (mainCamera != null)
        {
            originalCameraSize = mainCamera.orthographicSize;
        }
    }

    public void AdRevive()
    {
        Debug.Log("[PlayerHealth] Ad Revive requested. Hiding panel and telling ReviveSystem to start.");
        if (reviveRequestPanel != null)
        {
            reviveRequestPanel.SetActive(false);
        }
        revivesUsed++;
        // Find the ReviveSystem and tell it to do its job.
        ReviveSystemL3antix reviveSystem = GetComponent<ReviveSystemL3antix>();
        if (reviveSystem != null)
        {
            reviveSystem.DoAdRevive();
        }
    }

    public void OnReviveComplete()
    {
        Debug.Log("[PlayerHealth] A revive power-up has completed. Finalizing player state.");
        if (animator != null) animator.SetTrigger("Revive");
        // 1. Restore physics and movement.
        rb.isKinematic = false;
        if (L3antixMovement != null) L3antixMovement.enabled = true;

        FullHeal(); // Use your existing FullHeal method.
        isDead = false;
        isReviving = false;
        isInvincible = false;
    }
    private IEnumerator FadeScreen(float targetAlpha)
    {
        if (deathFadeImage == null) yield break;
        float startAlpha = deathFadeImage.color.a;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            deathFadeImage.color = new Color(0, 0, 0, newAlpha);
            yield return null;
        }
        deathFadeImage.color = new Color(0, 0, 0, targetAlpha);
    }

    private IEnumerator ZoomOnPlayer(float targetSize)
    {
        if (mainCamera == null) yield break;
        float startSize = mainCamera.orthographicSize;
        float timer = 0f;
        while (timer < zoomDuration)
        {
            timer += Time.unscaledDeltaTime;
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, timer / zoomDuration);
            yield return null;
        }
        mainCamera.orthographicSize = targetSize;
    }
    private void ResetPostProcessingOnDeath()
    {
        // This method cleans up the low-health screen effects.
        if (vignette != null) vignette.active = false;
        if (chromaticAberration != null) chromaticAberration.active = false;
    }
    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
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
        // First, check if we even need to heal.
        if (currentHealth >= maxHealth || healingCoroutine != null)
        {
            return;
        }

        // --- BEE HEALING LOGIC ---
        // Check if the player has the Bee Power-Up active.
        bool hasBeePowerUp = (PowerUpManagerL3antix != null && PowerUpManagerL3antix.HasPowerUp(PowerUpType.BeePowerUp));
        // Determine which healing values to use.
        int activeHealthPerSecond;
        float activeDelayBeforeHeal;

        if (hasBeePowerUp)
        {
            // If the bee power-up is active, use the enhanced values.
            activeHealthPerSecond = beeHealthPerSecond;
            activeDelayBeforeHeal = beeDelayBeforeHeal;
        }
        else
        {
            // Otherwise, use the default values.
            activeHealthPerSecond = healthPerSecond;
            activeDelayBeforeHeal = delayBeforeHeal;
        }
        // --- END OF BEE HEALING LOGIC ---

        // Now, check if enough time has passed using the ACTIVE delay.
        if (Time.time > lastDamageTime + activeDelayBeforeHeal)
        {
            // Start the healing coroutine, but pass it the ACTIVE healing rate.
            healingCoroutine = StartCoroutine(HealOverTime(activeHealthPerSecond));
        }
    }


    public void TakeDamage(int damage, float knockbackForce, Vector2 knockbackDirection)
    {
       
        
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

        ExplosiveCoinsPowerUpL3antix explosive = GetComponent<ExplosiveCoinsPowerUpL3antix>();
        PowerUpManagerL3antix powerUpManager = FindObjectOfType<PowerUpManagerL3antix>();
        if (explosive != null && powerUpManager != null && powerUpManager.HasPowerUp(PowerUpType.ExplosiveCoins))
        {
            explosive.TrySpawnCoin();
        }
        PlayDamageSound();

    }

    private void Die()
    {
        // 1. First, check if we are already in the process of dying.
        if (isDead) return;
        isDead = true;
        Debug.Log("Player health reached zero. Starting death sequence...");

        // Start the new, dramatic death sequence coroutine.
        StartCoroutine(DeathSequence());
    }
    // --- END OF NEW Die() METHOD ---

    // --- THIS IS THE NEW COROUTINE THAT CONTAINS ALL THE DEATH LOGIC ---
    private IEnumerator DeathSequence(bool skipToFinalDeath = false)
    {
        if (deathAnimationSound != null && mainCamera != null)
        {
            AudioSource.PlayClipAtPoint(deathAnimationSound, mainCamera.transform.position, 1f); // Volume is 1f (full)
        }
        animator.SetTrigger("Death");
        
        if (!skipToFinalDeath)
        {
            // ... (The dramatic intro part is the same: set invincible, freeze player, zoom, fade in)
            isInvincible = true;
            if (L3antixMovement != null) L3antixMovement.enabled = false;
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
            int onTopLayerID = SortingLayer.NameToID(onTopSortingLayerName);
            foreach (var renderer in spriteRenderers) { if (renderer != null) renderer.sortingLayerID = onTopLayerID; }
            if (mainCamera != null) StartCoroutine(ZoomOnPlayer(zoomInSize));
            CameraShakerHandler.Shake(CameraShakeDeath);
            if (deathFadeImage != null)
            {
                deathFadeImage.gameObject.SetActive(true);
                yield return StartCoroutine(FadeScreen(1f));
            }
            yield return new WaitForSecondsRealtime(postFadeDelay);
        }

        // --- PART 2: The Revive Logic ---
        if (!skipToFinalDeath)
        {
            // ... (The logic for checking for revive power-ups is the same)
            ReviveUpgradedSystemL3antix reviveUp = GetComponent<ReviveUpgradedSystemL3antix>();
            if (reviveUp != null && reviveUp.hasReviveUpgradedPowerUp && !reviveUp.HasUsedRevive)
            {
                isReviving = true; // Set the master lock.
                reviveUp.TryRevive(); // Let the script handle itself.
                yield break;
            }
            ReviveSystemL3antix revive = GetComponent<ReviveSystemL3antix>();
            if (revive != null && revive.hasRevivePowerUp && !revive.hasUsedRevive)
            {
                isReviving = true;
                revive.TryRevive();
                yield break;
            }

            // Ad Revive Logic
#if UNITY_WEBGL
            // --- CRAZY GAMES (WEBGL) PATH ---
            bool canUseAdRevive_Crazy = (revivesUsed < maxRevives) && (CrazyGamesManager.Instance != null && CrazyGamesManager.Instance.IsSDKInitialized);
            if (canUseAdRevive_Crazy && reviveRequestPanel != null)
            {
                yield return StartCoroutine(FadeOutAndResetVisuals());
                reviveRequestPanel.SetActive(true);

                // Find the "Yes" button on the panel and set it up for CrazyGames
                Button yesButton = reviveRequestPanel.transform.Find("YesButton").GetComponent<Button>(); // Make sure your button is named "YesButton"
                if (yesButton != null)
                {
                    yesButton.onClick.RemoveAllListeners(); // Clear old listeners
                    yesButton.onClick.AddListener(() => {
                        // When clicked, show a CrazyGames ad. If successful, call AdRevive().
                        CrazyGamesManager.Instance.ShowRewardedAd(AdRevive);
                    });
                }
                yield break; // Stop the coroutine here and wait for player input.
            }

#else
            // --- UNITY ADS (ANDROID) PATH ---
            AdManager_New adManager = FindObjectOfType<AdManager_New>();
            bool isAdReady_Unity = (adManager != null && adManager.IsAdReady("Rewarded_Android"));
            bool canUseAdRevive_Unity = (revivesUsed < maxRevives) && isAdReady_Unity;

            if (canUseAdRevive_Unity && reviveRequestPanel != null)
            {
                yield return StartCoroutine(FadeOutAndResetVisuals());
                reviveRequestPanel.SetActive(true);

                // --- THIS IS THE GUARANTEED FIX ---
                // DELETE THE ENTIRE BLOCK OF CODE THAT FINDS THE BUTTON AND ADDS A LISTENER.
                // Your RewardedAdButton script is already handling this. This code is causing the conflict.
                /*
                Button yesButton = reviveRequestPanel.transform.Find("YesButton").GetComponent<Button>();
                if (yesButton != null)
                {
                    yesButton.onClick.RemoveAllListeners();
                    yesButton.onClick.AddListener(() => {
                        AdRevive();
                    });
                }
                */
                // --- END OF FIX ---

                yield break; // Stop the coroutine here and wait for the player to click the button.
            }
#endif
        }

        // --- PART 3: Final Death ---
        Debug.Log("No revives available. Showing final death panel.");
        // THIS IS THE FIX for the dim screen on final death.
        yield return StartCoroutine(FadeOutAndResetVisuals()); // Use the helper method here too.

        ResetPostProcessingOnDeath();
        if (checkpointManager != null && PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.SetFinalScore(checkpointManager.TotalScore);
        }

        // 2. Tell the UI Manager to show the screen. It will now have the correct score.
        if (gameUIManager != null)
        {
            gameUIManager.ShowDeathScreen();
        }

        // 3. NOW, disable the camera follow and the player object.
        CameraFollowMouseHorizontal cameraFollow = mainCamera.GetComponent<CameraFollowMouseHorizontal>();
        if (cameraFollow != null)
        {
            cameraFollow.enabled = false;
        }
        gameObject.SetActive(false);
    }
    public void ResetDeathEffects()
    {
        Debug.Log("[PlayerHealth] Resetting all death effects (fade, zoom, sorting layers).");

        // Start a coroutine to handle the fade out and reset.
        StartCoroutine(FadeOutAndResetVisuals());
    }

    // This is the helper coroutine for the method above.
    private IEnumerator FadeOutAndResetVisuals()
    {
        // 1. Fade the black screen OUT.
        if (deathFadeImage != null)
        {
            yield return StartCoroutine(FadeScreen(0f));
            deathFadeImage.gameObject.SetActive(false);
        }

        // 2. Reset the camera zoom.
        if (mainCamera != null)
        {
            yield return StartCoroutine(ZoomOnPlayer(originalCameraSize));
        }

        // 3. Restore the player's original sorting layers. THIS IS THE FIX.
        foreach (var pair in originalSortingLayers)
        {
            // pair.Key is the SpriteRenderer, pair.Value is the original sortingLayerID
            if (pair.Key != null)
            {
                pair.Key.sortingLayerID = pair.Value;
            }
        }
    }
    public void DeclineRevive()
    {
        Debug.Log("Player declined ad revive. Proceeding to final death.");
        reviveRequestPanel.SetActive(false);

        // Reset visual effects instantly.
        if (deathFadeImage != null) deathFadeImage.gameObject.SetActive(false);
        if (mainCamera != null) mainCamera.orthographicSize = originalCameraSize;
        ResetPostProcessingOnDeath();

        // --- THIS IS THE FIX ---
        // 1. Find the CheckpointManager and save the score.
        if (checkpointManager == null) checkpointManager = FindObjectOfType<CheckpointManager>();
        if (checkpointManager != null && PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.SetFinalScore(checkpointManager.TotalScore);
        }

        // 2. Tell the UI Manager to show the screen.
        if (gameUIManager != null)
        {
            gameUIManager.ShowDeathScreen();
        }

        // 3. NOW, disable the camera follow and the player object.
        CameraFollowMouseHorizontal cameraFollow = mainCamera.GetComponent<CameraFollowMouseHorizontal>();
        if (cameraFollow != null)
        {
            cameraFollow.enabled = false;
        }
        gameObject.SetActive(false);
        // --- END OF FIX ---
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
    private IEnumerator HealOverTime(int healRate) // It now takes the healRate as an argument
    {
        Debug.Log($"Health regeneration started at a rate of {healRate}/sec.");
        while (currentHealth < maxHealth)
        {
            StartHealingParticles();
            currentHealth += healRate; // Use the new healRate variable
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            UpdateHealthEffects();
            Debug.Log($"Player healed. Current health: {currentHealth}");
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
        // --- YOUR WORKING CODE (PRESERVED) ---
        if (flashMaterial == null || spriteRenderers.Length == 0)
        {
            yield break;
        }
        SpriteLibrary spriteLibrary = GetComponent<SpriteLibrary>();
        if (spriteLibrary == null || spriteLibrary.spriteLibraryAsset == null)
        {
            Debug.LogError("FlashEffect Error: SpriteLibrary or its asset is missing!", this);
            yield break;
        }
        L3antixSkinController skinController = GetComponent<L3antixSkinController>();
        string currentSkinName = (skinController != null) ? skinController.GetCurrentSkinName() : "Default";
        Sprite skinSprite = spriteLibrary.spriteLibraryAsset.GetSprite("Body", currentSkinName);
        if (skinSprite == null)
        {
            Debug.LogError($"FlashEffect Error: Could not find a sprite in the library with Category 'Head' and Label '{currentSkinName}'. Check your Sprite Library Asset!", this);
            yield break;
        }
        Texture2D skinTexture = skinSprite.texture;
        Material flashInstance = new Material(flashMaterial);
        flashInstance.SetTexture(mainTextureProperty, skinTexture);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].material = flashInstance;
            }
        }
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            float flashAmount = Mathf.Lerp(1.0f, 0.0f, elapsed / flashDuration);
            flashInstance.SetFloat(flashAmountProperty, flashAmount);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // --- END OF YOUR WORKING CODE ---


        // --- THIS IS THE GUARANTEED FIX ---
        // Before we restore materials, we ask the PlayerInvisibility script for its status.
        bool shouldBeInvisible = playerInvisibility != null && playerInvisibility.IsInvisible();

        if (shouldBeInvisible)
        {
            // If the player IS supposed to be invisible, restore the INVISIBLE material.
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].material = playerInvisibility.invisibleMaterial;
                }
            }
        }
        else
        {
            // If the player is NOT supposed to be invisible, restore the ORIGINAL materials.
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null && originalMaterials[i] != null)
                {
                    spriteRenderers[i].material = originalMaterials[i];
                }
            }
        }
        // --- END OF FIX ---

        // Clean up the instance.
        Destroy(flashInstance);
    }



    public void FullHeal()
    {
        currentHealth = maxHealth;
        UpdateHealthEffects();

        // Optional: trigger healing particles instantly
        StartHealingParticles();
        StopHealingParticles();
    }
    public void ForceRemoveShield(PowerUpType shieldTypeToRemove)
    {
        Debug.Log($"[PlayerHealth] Force removing shield of type: {shieldTypeToRemove}");

        // If the shield to remove is the one currently active...
        if (HasShield && shieldQueue.Count > 0 && shieldQueue[0] == shieldTypeToRemove)
        {
            // ...forcefully break it and activate the next one.
            Debug.Log("Force removing the currently active shield.");
            // We call DamageShield with a huge number to guarantee it breaks.
            DamageShield(99999);
        }
        else if (shieldQueue.Contains(shieldTypeToRemove))
        {
            // If the shield is in the queue but not active, just remove it silently.
            Debug.Log("Force removing a queued (non-active) shield.");
            shieldQueue.Remove(shieldTypeToRemove);
        }
    }
    public void AddShieldToQueue(PowerUpType shieldType)
    {
        if (shieldQueue.Contains(shieldType)) return; // Don't add duplicates

        shieldQueue.Add(shieldType);
        // Sort so the normal shield is always first.
        shieldQueue = shieldQueue.OrderBy(s => s == PowerUpType.ShieldUpgraded).ToList();

        // If no shield is currently active, activate the next one.
        if (!HasShield)
        {
            ActivateNextShield();
        }
    }

    // METHOD 2: Activates the next shield in the queue.
    private void ActivateNextShield()
    {
        if (shieldQueue.Count == 0)
        {
            // No shields left, ensure everything is off.
            shieldCurrentHealth = 0;
            if (shieldSlider != null) shieldSlider.gameObject.SetActive(false);
            return;
        }

        // Determine which shield is next and set its health.
        PowerUpType nextShieldType = shieldQueue[0];
        usingUpgradedShield = (nextShieldType == PowerUpType.ShieldUpgraded);
        shieldCurrentHealth = usingUpgradedShield ? upgradedShieldMaxHealth : shieldMaxHealth;

        // Use YOUR working RestoreShieldToMax logic to handle the visuals.
        RestoreShieldToMax();
    }

    // METHOD 3: The new, corrected DamageShield.
    public void DamageShield(int damage)
    {
        if (!HasShield) return;

        shieldCurrentHealth -= damage;
        if (shieldCurrentHealth < 0) shieldCurrentHealth = 0;

        if (shieldSlider != null) shieldSlider.value = shieldCurrentHealth;

        if (shieldCurrentHealth <= 0)
        {
            PowerUpType brokenShieldType = shieldQueue[0];
            Debug.Log($"Shield broke! Type: {brokenShieldType}");

            // --- THIS IS THE FIX ---
            // We will call your OLD, WORKING destruction logic from here.
            TriggerShieldDestructionVisuals(brokenShieldType);

            // If the broken shield was the normal one, trigger the explosion.
            if (brokenShieldType == PowerUpType.Shield)
            {
                TriggerShieldExplosion();
            }
            // --- END OF FIX ---

            shieldQueue.RemoveAt(0); // Remove the broken shield from the queue
            ActivateNextShield();    // Activate the next one
        }
        else
        {
            Debug.Log($"Shield took {damage} damage. Remaining HP: {shieldCurrentHealth}");
        }
    }

    // METHOD 4: A new helper method containing YOUR destruction logic.
    private void TriggerShieldDestructionVisuals(PowerUpType brokenShieldType)
    {
        if (PowerUpManagerL3antix != null)
        {
            if (brokenShieldType == PowerUpType.ShieldUpgraded)
            {
                // If the upgraded shield broke, play its sound.
                PowerUpManagerL3antix.PlayBoxShieldDestroySound();
            }
            else // It was a normal shield
            {
                // If the normal shield broke, play its sound.
                PowerUpManagerL3antix.PlayBubbleWrapDestroySound();
            }
        }
        PowerUpManagerL3antix pm = FindObjectOfType<PowerUpManagerL3antix>();
        if (pm == null) return;

        if (brokenShieldType == PowerUpType.ShieldUpgraded)
        {
            Debug.Log("🛡️ Playing Upgraded Shield destruction visuals.");
            pm.upgradedShieldAnimator?.SetTrigger("EndShield");
            pm.upgradedShieldObject.SetActive(false);
            // pm.upgradedShieldObject.SetActive(false); // The animation should handle this.
            if (pm.destructionShieldPrefab != null && pm.destructionSpawnPoints != null)
            {
                foreach (Transform spawn in pm.destructionSpawnPoints)
                {
                    if (spawn != null) Instantiate(pm.destructionShieldPrefab, spawn.position, spawn.rotation);
                }
            }
        }
        else // It was a normal shield
        {
            Debug.Log("🛡️ Playing Normal Shield destruction visuals.");
            // --- THE MISSING LINE ---
            pm.shieldAnimator?.SetTrigger("EndShield");
            // pm.shieldObject.SetActive(false); // The animation should handle this.
        }

        if (shieldSlider != null)
            shieldSlider.gameObject.SetActive(false);
    }

    // NEW METHOD 4: The explosion logic.
    private void TriggerShieldExplosion()
    {
        CameraShakerHandler.Shake(CameraShakeDeath);
        Vector3 explosionOrigin = (shieldExplosionPoint != null) ? shieldExplosionPoint.position : transform.position;
        Debug.Log($"Normal shield explosion at {explosionOrigin}!");
        Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(explosionOrigin, shieldExplosionRadius, LayerMask.GetMask("Enemy"));
        foreach (var enemyCollider in enemiesInRange)
        {
            if (enemyCollider.TryGetComponent(out FleaHealth flea)) flea.TakeDamage(shieldExplosionDamage, Vector2.zero);
            if (enemyCollider.TryGetComponent(out FleaHealthV2 fleaV2)) fleaV2.TakeDamage(shieldExplosionDamage, Vector2.zero);
            if (enemyCollider.TryGetComponent(out FlyHealth fly)) fly.TakeDamage(shieldExplosionDamage, Vector2.zero);
            if (enemyCollider.TryGetComponent(out SprayerHealth sprayer)) sprayer.TakeDamage(shieldExplosionDamage, Vector2.zero);
            if (enemyCollider.TryGetComponent(out InkHealth ink)) ink.TakeDamage(shieldExplosionDamage, Vector2.zero);
            if (enemyCollider.TryGetComponent(out RatKingHealth rat)) rat.TakeDamage(shieldExplosionDamage, Vector2.zero, 0f);
        }
    }

    // NEW METHOD 5: The gizmo drawer.
    private void OnDrawGizmosSelected()
    {
        Vector3 explosionOrigin = (shieldExplosionPoint != null) ? shieldExplosionPoint.position : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(explosionOrigin, shieldExplosionRadius);
    }



    public void RestoreShieldToMax()
    {
        if (!HasShield) return;

        if (usingUpgradedShield)
            shieldCurrentHealth = upgradedShieldMaxHealth;
        else
            shieldCurrentHealth = shieldMaxHealth;

        if (shieldSlider != null)
        {
            shieldSlider.maxValue = shieldCurrentHealth;
            shieldSlider.value = shieldCurrentHealth;
            shieldSlider.gameObject.SetActive(true);
        }

        PowerUpManagerL3antix pm = FindObjectOfType<PowerUpManagerL3antix>();
        if (pm != null)
        {
            if (usingUpgradedShield)
            {
                if (pm.upgradedShieldAnimator != null)
                {
                    pm.upgradedShieldAnimator.Rebind();
                    pm.upgradedShieldAnimator.Update(0f);
                    pm.upgradedShieldAnimator.SetTrigger("StartShield");
                }

                if (pm.upgradedShieldObject != null)
                    pm.upgradedShieldObject.SetActive(true);
            }
            else
            {
                if (pm.shieldAnimator != null)
                {
                    pm.shieldAnimator.Rebind();
                    pm.shieldAnimator.Update(0f);
                    pm.shieldAnimator.SetTrigger("StartShield");
                }

                if (pm.shieldObject != null)
                    pm.shieldObject.SetActive(true);
            }
        }

        Debug.Log(usingUpgradedShield
            ? "🛡️ Upgraded Shield fully restored!"
            : "🛡️ Normal Shield fully restored!");
    }
    public void RestoreShieldAtCheckpoint()
    {
        PowerUpManagerL3antix pm = GetComponent<PowerUpManagerL3antix>();
        if (pm == null) return;

        // Clear the old queue completely.
        shieldQueue.Clear();
        shieldCurrentHealth = 0;

        // Check which shields are permanently equipped and add them back to the queue.
        if (pm.HasPowerUp(PowerUpType.Shield))
        {
            AddShieldToQueue(PowerUpType.Shield);
        }
        if (pm.HasPowerUp(PowerUpType.ShieldUpgraded))
        {
            AddShieldToQueue(PowerUpType.ShieldUpgraded);
        }

        // The AddShieldToQueue method will automatically sort the list and call
        // ActivateNextShield if the queue is not empty, which in turn calls
        // your working RestoreShieldToMax logic.
        Debug.Log("Shields restored at checkpoint.");
    }

}