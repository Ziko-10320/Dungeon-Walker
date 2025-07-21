using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnhancedNeedleAttackSystem : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Animator playerAnimator; // Reference to the player's Animator
    [SerializeField] private string anticipationTriggerName = "Anticipation"; // Name of the Anticipation Trigger in the Animator
    [SerializeField] private string attackTriggerName = "NeedleAttack"; // Name of the Attack Trigger in the Animator
    [SerializeField] private float anticipationDuration = 1.0f; // Duration of the anticipation animation
    [SerializeField] private float attackCooldown = 1.0f; // Cooldown duration for the attack (in seconds)
    [SerializeField] private int damage = 20; // Damage amount dealt by the attack

    [Header("Audio Settings")]
    [SerializeField] private AudioClip swingMissSound; // Sound for when the swing doesn't hit an enemy
    [SerializeField, Range(0f, 1f)] private float swingMissSoundVolume = 1f; // Volume for swing miss sound
    [SerializeField] private AudioClip swingHitSound; // Sound for when the swing hits an enemy
    [SerializeField, Range(0f, 1f)] private float swingHitSoundVolume = 1f; // Volume for swing hit sound

    [Header("Damage Area Settings")]
    [SerializeField] private Transform attackPoint; // Origin point of the attack (usually in front of the player)
    [SerializeField] private float attackRange = 0.5f; // Radius of the attack area (circle)
    [SerializeField] private LayerMask enemyLayers; // Layers of enemies that can receive damage

    [Header("Ghost Effect Settings")]
    [SerializeField] private List<SpriteRenderer> ghostTargets = new List<SpriteRenderer>(); // SpriteRenderers that will have ghost effect
    [SerializeField] private float ghostInterval = 0.1f; // Time between ghost spawns during anticipation
    [SerializeField] private float ghostDuration = 0.3f; // How long each ghost copy lasts
    [SerializeField] private Color ghostColor = new Color(1f, 1f, 1f, 0.3f); // Color of the ghost effect
    [SerializeField] private Material ghostMaterial; // Optional custom material for ghosts

    [Header("Camera Effects Settings")]
    [SerializeField] private CameraEffects cameraEffects; // Reference to camera effects component
    [SerializeField] private bool enableCameraEffects = true; // Toggle camera effects on/off

    [Header("Knockback Settings")]
    [SerializeField] private float defaultKnockbackForce = 5f; // Default knockback force for enemies
    [SerializeField] private float fleaKnockbackForce = 8f; // Specific knockback for Flea
    [SerializeField] private float inkKnockbackForce = 5f; // Specific knockback for Ink
    [SerializeField] private float flyKnockbackForce = 7f; // Specific knockback for Fly
    [SerializeField] private float sprayerKnockbackForce = 6f; // Specific knockback for Sprayer

    private float nextAttackTime = 0f; // Time when the next attack is allowed
    private bool canDealDamage = false; // Flag to control damage application once per attack
    private bool isAnticipating = false; // Flag to check if anticipation is active
    private Coroutine ghostEffectCoroutine; // Reference to the ghost effect coroutine
    private AudioSource audioSource; // Reference to the AudioSource component

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        // Auto-find camera effects if not assigned
        if (cameraEffects == null)
        {
            cameraEffects = Camera.main?.GetComponent<CameraEffects>();
            if (cameraEffects == null)
            {
                Debug.LogWarning("CameraEffects component not found on main camera. Camera effects will be disabled.");
                enableCameraEffects = false;
            }
        }
    }

    void Update()
    {
        // Check for Right Mouse Button press and cooldown
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime && !isAnticipating)
        {
            StartAnticipationAttack();
        }
    }

    void StartAnticipationAttack()
    {
        isAnticipating = true;

        // Trigger the anticipation animation
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(anticipationTriggerName);
        }

        // Start camera hold effect
        if (enableCameraEffects && cameraEffects != null)
        {
            cameraEffects.StartHoldAndReleaseEffect();
        }

        // Start ghost effect for specified sprites
        if (ghostTargets.Count > 0)
        {
            ghostEffectCoroutine = StartCoroutine(GhostEffectRoutine());
        }

        // Start the anticipation routine
        StartCoroutine(AnticipationRoutine());
    }

    IEnumerator AnticipationRoutine()
    {
        // Slow down time slightly during anticipation for dramatic effect
        Time.timeScale = 0.8f;

        // Wait for the anticipation duration
        yield return new WaitForSecondsRealtime(anticipationDuration);

        // Reset time scale
        Time.timeScale = 1.0f;

        // Stop ghost effect
        if (ghostEffectCoroutine != null)
        {
            StopCoroutine(ghostEffectCoroutine);
        }

        // Perform the actual attack
        PerformNeedleAttack();
        isAnticipating = false;
    }

    IEnumerator GhostEffectRoutine()
    {
        float timer = 0f;

        while (timer < anticipationDuration)
        {
            // Create ghost copies for all specified targets
            foreach (SpriteRenderer targetRenderer in ghostTargets)
            {
                if (targetRenderer != null)
                {
                    CreateGhostCopy(targetRenderer);
                }
            }

            yield return new WaitForSecondsRealtime(ghostInterval);
            timer += ghostInterval;
        }
    }

    void CreateGhostCopy(SpriteRenderer originalRenderer)
    {
        if (originalRenderer == null) return;

        // Create ghost object
        GameObject ghostObject = new GameObject("Ghost_" + originalRenderer.name);
        SpriteRenderer ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();

        // *** FIX START ***
        // The core of the fix is here. We copy the transform's properties directly.
        // This correctly handles position, rotation, and scale, including any parent transformations.
        ghostObject.transform.position = originalRenderer.transform.position;
        ghostObject.transform.rotation = originalRenderer.transform.rotation;
        ghostObject.transform.localScale = originalRenderer.transform.lossyScale; // Use lossyScale to get the true global scale

        // Copy sprite and sorting properties
        ghostRenderer.sprite = originalRenderer.sprite;
        ghostRenderer.sortingLayerID = originalRenderer.sortingLayerID;
        ghostRenderer.sortingOrder = originalRenderer.sortingOrder - 1; // Render behind the original

        // Directly copy the flip properties. This is the simplest and most reliable way.
        ghostRenderer.flipX = originalRenderer.flipX;
        ghostRenderer.flipY = originalRenderer.flipY;
        // *** FIX END ***

        // Apply ghost material or create a transparent one
        if (ghostMaterial != null)
        {
            ghostRenderer.material = ghostMaterial;
        }
        else
        {
            // Using a shared material is better for performance than creating a new one each time.
            // However, for simplicity, your original approach is fine.
            Material tempMaterial = new Material(Shader.Find("Sprites/Default"));
            ghostRenderer.material = tempMaterial;
        }

        ghostRenderer.color = ghostColor;

        // Start fade out coroutine
        StartCoroutine(FadeOutGhost(ghostRenderer));
    }

    IEnumerator FadeOutGhost(SpriteRenderer ghostRenderer)
    {
        Color startColor = ghostRenderer.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        float timer = 0f;

        while (timer < ghostDuration)
        {
            // Use unscaledDeltaTime because the ghost effect might run when Time.timeScale is not 1
            timer += Time.unscaledDeltaTime;
            float progress = timer / ghostDuration;
            ghostRenderer.color = Color.Lerp(startColor, endColor, progress);
            yield return null;
        }

        // Destroy the ghost object
        if (ghostRenderer != null && ghostRenderer.gameObject != null)
        {
            Destroy(ghostRenderer.gameObject);
        }
    }

    void PerformNeedleAttack()
    {
        // Reset the next attack time
        nextAttackTime = Time.time + attackCooldown;

        // Trigger the attack animation
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(attackTriggerName);
        }

        // Enable damage application. ApplyDamage() will be called via Animation Event
        canDealDamage = true;

        // Add a small camera shake on attack
        if (enableCameraEffects && cameraEffects != null)
        {
            cameraEffects.StartShakeEffect();
        }
    }

    // This function will be called as an Animation Event at the specified frame
    public void ApplyDamage()
    {
        if (!canDealDamage) return; // Ensure we haven't already applied damage for this attack

        // Detect enemies in the attack range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        bool enemyWasHit = false;

        // Apply damage to each detected enemy
        foreach (Collider2D enemy in hitEnemies)
        {
            enemyWasHit = true;
            // Use a common interface or base class for health components if possible
            // For now, we'll check each type and cast
            if (enemy.TryGetComponent<FleaHealth>(out var fleaHealth))
            {
                fleaHealth.TakeDamage(damage, (Vector2)(enemy.transform.position - attackPoint.position).normalized, fleaKnockbackForce);
            }
            else if (enemy.TryGetComponent<InkHealth>(out var inkHealth))
            {
                inkHealth.TakeDamage(damage, (Vector2)(enemy.transform.position - attackPoint.position).normalized, inkKnockbackForce);
            }
            else if (enemy.TryGetComponent<FlyHealth>(out var flyHealth))
            {
                flyHealth.TakeDamage(damage, (Vector2)(enemy.transform.position - attackPoint.position).normalized, flyKnockbackForce);
            }
            else if (enemy.TryGetComponent<SprayerHealth>(out var sprayerHealth))
            {
                sprayerHealth.TakeDamage(damage, (Vector2)(enemy.transform.position - attackPoint.position).normalized, sprayerKnockbackForce);
            }
            else
            {
                // Fallback for other types or if no specific health script is found
                Debug.LogWarning($"No recognized health script found on {enemy.name}. Damage applied without specific knockback.");
            }

            Debug.Log("Hit " + enemy.name + " for " + damage + " damage!");
        }

        if (enemyWasHit)
        {
            if (swingHitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(swingHitSound, swingHitSoundVolume);
            }
        }
        else
        {
            if (swingMissSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(swingMissSound, swingMissSoundVolume);
            }
        }

        canDealDamage = false; // Disable damage application after it's been dealt
    }

    // Public methods to control the system from external scripts
    public void AddGhostTarget(SpriteRenderer targetRenderer)
    {
        if (targetRenderer != null && !ghostTargets.Contains(targetRenderer))
        {
            ghostTargets.Add(targetRenderer);
        }
    }

    public void RemoveGhostTarget(SpriteRenderer targetRenderer)
    {
        if (ghostTargets.Contains(targetRenderer))
        {
            ghostTargets.Remove(targetRenderer);
        }
    }

    public void SetAnticipationDuration(float duration)
    {
        anticipationDuration = Mathf.Max(0.1f, duration);
    }

    public void SetGhostInterval(float interval)
    {
        ghostInterval = Mathf.Max(0.05f, interval);
    }

    public bool IsAnticipating()
    {
        return isAnticipating;
    }

    // To visualize the attack range in the Scene View (for debugging only)
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);

        // Draw ghost target indicators
        Gizmos.color = Color.cyan;
        foreach (SpriteRenderer targetRenderer in ghostTargets)
        {
            if (targetRenderer != null)
            {
                Gizmos.DrawWireCube(targetRenderer.bounds.center, targetRenderer.bounds.size);
            }
        }
    }
}


