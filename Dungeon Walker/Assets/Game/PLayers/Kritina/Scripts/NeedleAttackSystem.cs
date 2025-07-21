using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatAttackSystem : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Animator playerAnimator; // Reference to the player's Animator
    [SerializeField] private string anticipationTriggerName = "Anticipation"; // Name of the Anticipation Trigger in the Animator
    [SerializeField] private string attackTriggerName = "BatAttack"; // Name of the Attack Trigger in the Animator
    [SerializeField] private string upwardAttackTriggerName = "UpwardAttack"; // Name of the Upward Attack Trigger in the Animator
    [SerializeField] private string throwBatTriggerName = "ThrowBat"; // Name of the Throw Bat Trigger in the Animator
    [SerializeField] private float anticipationDuration = 1.0f; // Duration of the anticipation animation
    [SerializeField] private float attackCooldown = 1.0f; // Cooldown duration for the attack (in seconds)
    [SerializeField] private int damage = 20; // Damage amount dealt by the attack

    [Header("Bat Throwing Settings")]
    [SerializeField] private GameObject batPrefab; // Prefab of the bat projectile
    [SerializeField] private Transform batSpawnPoint; // Point where the bat is spawned when thrown
    [SerializeField] private float batThrowSpeed = 15f; // Speed of the thrown bat
    [SerializeField] private float batPickupRange = 1.5f; // Range within which player can pick up the bat
    [SerializeField] private LayerMask groundLayer = 1; // Layer mask for ground collision

    [Header("Damage Area Settings")]
    [SerializeField] private Transform attackPoint; // Origin point of the normal attack (usually in front of the player)
    [SerializeField] private Transform upwardAttackPoint; // Origin point of the upward attack (usually above the player)
    [SerializeField] private float attackRange = 0.5f; // Radius of the attack area (circle)
    [SerializeField] private LayerMask enemyLayers; // Layers of enemies that can receive damage

    [Header("Direction Detection Settings")]
    [SerializeField] private float upwardZoneMinY = 0.5f; // Minimum Y-coordinate for upward attack zone (relative to player)
    [SerializeField] private float upwardZoneMaxY = 5.0f; // Maximum Y-coordinate for upward attack zone (relative to player)
    [SerializeField] private float normalZoneMaxY = 0.4f; // Maximum Y-coordinate for normal attack zone (relative to player)
    [SerializeField] private bool showDirectionDebug = false; // Show debug information for mouse direction

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource; // Reference to the AudioSource component
    [SerializeField] private AudioClip attackSound; // Sound played when performing normal attack
    [SerializeField] private AudioClip upwardAttackSound; // Sound played when performing upward attack
    [SerializeField] private AudioClip throwBatSound; // Sound played when throwing the bat

    [Header("Visual Settings")]
    [SerializeField] private GameObject playerBatVisual; // Visual representation of the bat on the player (to hide when thrown)

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

    // Private variables
    private float nextAttackTime = 0f; // Time when the next attack is allowed
    private bool canDealDamage = false; // Flag to control damage application once per attack
    private bool isAnticipating = false; // Flag to check if anticipation is active
    private Coroutine ghostEffectCoroutine; // Reference to the ghost effect coroutine
    private Camera playerCamera; // Reference to the main camera
    private bool isUpwardAttack = false; // Flag to determine which attack type is being performed
    private bool hasBat = true; // Flag to track if player has the bat
    private GameObject thrownBat; // Reference to the currently thrown bat

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

        // Get reference to the main camera for mouse direction calculation
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogWarning("Main camera not found. Mouse direction detection may not work properly.");
        }

        // Auto-assign upward attack point if not set
        if (upwardAttackPoint == null && attackPoint != null)
        {
            Debug.LogWarning("Upward attack point not assigned. Please assign it in the inspector for proper upward attack functionality.");
        }

        // Auto-assign AudioSource if not set
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogWarning("AudioSource component not found on this GameObject. Attack sounds will not play.");
            }
        }

        // Auto-assign bat spawn point if not set
        if (batSpawnPoint == null)
        {
            batSpawnPoint = transform; // Use player transform as default
            Debug.LogWarning("Bat spawn point not assigned. Using player transform as default.");
        }

        // Ensure player bat visual is visible at start
        if (playerBatVisual != null)
        {
            playerBatVisual.SetActive(true);
        }
    }

    void Update()
    {
        // Check for Right Mouse Button press for attacks (only if player has bat)
        if (Input.GetMouseButtonDown(1) && Time.time >= nextAttackTime && !isAnticipating && hasBat)
        {
            // Determine attack type based on mouse direction
            bool shouldPerformUpwardAttack = ShouldPerformUpwardAttack();

            if (showDirectionDebug)
            {
                Debug.Log($"Attack Type: {(shouldPerformUpwardAttack ? "Upward" : "Normal")}");
            }

            StartAnticipationAttack(shouldPerformUpwardAttack);
        }

        // Check for Left Mouse Button press for throwing bat (only if player has bat)
        if (Input.GetMouseButtonDown(0) && hasBat && !isAnticipating)
        {
            ThrowBat();
        }

        // Check for bat pickup if player doesn't have bat
        if (!hasBat && thrownBat != null)
        {
            float distanceToBat = Vector2.Distance(transform.position, thrownBat.transform.position);
            if (distanceToBat <= batPickupRange)
            {
                PickUpBat();
            }
        }
    }

    /// <summary>
    /// Determines if the player should perform an upward attack based on mouse direction
    /// </summary>
    /// <returns>True if upward attack should be performed, false for normal attack</returns>
    private bool ShouldPerformUpwardAttack()
    {
        if (playerCamera == null) return false;

        // Get mouse position in world space
        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z; // Ensure same Z coordinate

        // Calculate relative Y position of mouse to player
        float relativeMouseY = mouseWorldPos.y - transform.position.y;

        // Check if mouse is within the upward attack zone
        if (relativeMouseY >= upwardZoneMinY && relativeMouseY <= upwardZoneMaxY)
        {
            return true;
        }
        // Check if mouse is within the normal attack zone (forward or down)
        else if (relativeMouseY <= normalZoneMaxY)
        {
            return false;
        }

        // Default to normal attack if outside defined zones or in an ambiguous area
        return false;
    }

    void ThrowBat()
    {
        if (batPrefab == null)
        {
            Debug.LogWarning("Bat prefab not assigned! Cannot throw bat.");
            return;
        }

        // Get mouse position in world space
        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;

        // Calculate direction from bat spawn point to mouse
        Vector2 throwDirection = (mouseWorldPos - batSpawnPoint.position).normalized;

        // Instantiate the bat projectile
        GameObject batInstance = Instantiate(batPrefab, batSpawnPoint.position, Quaternion.identity);
        thrownBat = batInstance;

        // Initialize the bat projectile
        BatProjectile batProjectile = batInstance.GetComponent<BatProjectile>();
        if (batProjectile != null)
        {
            batProjectile.Initialize(throwDirection, batThrowSpeed, damage, enemyLayers, groundLayer);
        }

        // Trigger throw animation
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(throwBatTriggerName);
        }

        // Play throw sound
        if (audioSource != null && throwBatSound != null)
        {
            audioSource.PlayOneShot(throwBatSound);
        }

        // Hide player bat visual
        if (playerBatVisual != null)
        {
            playerBatVisual.SetActive(false);
        }

        // Player no longer has bat
        hasBat = false;

        if (showDirectionDebug)
        {
            Debug.Log($"Threw bat towards: {throwDirection}");
        }
    }

    void PickUpBat()
    {
        if (thrownBat != null)
        {
            // Destroy the thrown bat
            BatProjectile batProjectile = thrownBat.GetComponent<BatProjectile>();
            if (batProjectile != null)
            {
                batProjectile.PickUp();
            }
            else
            {
                Destroy(thrownBat);
            }

            thrownBat = null;
        }

        // Show player bat visual
        if (playerBatVisual != null)
        {
            playerBatVisual.SetActive(true);
        }

        // Player now has bat
        hasBat = true;

        if (showDirectionDebug)
        {
            Debug.Log("Picked up bat!");
        }
    }

    void StartAnticipationAttack(bool performUpwardAttack = false)
    {
        isAnticipating = true;
        isUpwardAttack = performUpwardAttack;

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

        // Perform the appropriate attack based on the flag
        if (isUpwardAttack)
        {
            PerformUpwardAttack();
        }
        else
        {
            PerformBatAttack();
        }

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

        // Copy the transform's properties directly
        ghostObject.transform.position = originalRenderer.transform.position;
        ghostObject.transform.rotation = originalRenderer.transform.rotation;
        ghostObject.transform.localScale = originalRenderer.transform.lossyScale; // Use lossyScale to get the true global scale

        // Copy sprite and sorting properties
        ghostRenderer.sprite = originalRenderer.sprite;
        ghostRenderer.sortingLayerID = originalRenderer.sortingLayerID;
        ghostRenderer.sortingOrder = originalRenderer.sortingOrder - 1; // Render behind the original

        // Directly copy the flip properties
        ghostRenderer.flipX = originalRenderer.flipX;
        ghostRenderer.flipY = originalRenderer.flipY;

        // Apply ghost material or create a transparent one
        if (ghostMaterial != null)
        {
            ghostRenderer.material = ghostMaterial;
        }
        else
        {
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

    void PerformBatAttack()
    {
        // Reset the next attack time
        nextAttackTime = Time.time + attackCooldown;

        // Trigger the normal attack animation
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(attackTriggerName);
        }

        // Play attack sound
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        // Enable damage application. ApplyDamage() will be called via Animation Event
        canDealDamage = true;

        // Add a small camera shake on attack
        if (enableCameraEffects && cameraEffects != null)
        {
            cameraEffects.StartShakeEffect();
        }

        if (showDirectionDebug)
        {
            Debug.Log("Performing Normal Bat Attack");
        }
    }

    void PerformUpwardAttack()
    {
        // Reset the next attack time
        nextAttackTime = Time.time + attackCooldown;

        // Trigger the upward attack animation
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(upwardAttackTriggerName);
        }

        // Play upward attack sound
        if (audioSource != null && upwardAttackSound != null)
        {
            audioSource.PlayOneShot(upwardAttackSound);
        }

        // Enable damage application. ApplyUpwardDamage() will be called via Animation Event
        canDealDamage = true;

        // Add a small camera shake on attack
        if (enableCameraEffects && cameraEffects != null)
        {
            cameraEffects.StartShakeEffect();
        }

        if (showDirectionDebug)
        {
            Debug.Log("Performing Upward Attack");
        }
    }

    // This function will be called as an Animation Event at the specified frame for normal attacks
    public void ApplyDamage()
    {
        if (!canDealDamage || !hasBat) return; // Ensure we haven't already applied damage and player has bat

        ApplyDamageAtPoint(attackPoint);
    }

    // This function will be called as an Animation Event at the specified frame for upward attacks
    public void ApplyUpwardDamage()
    {
        if (!canDealDamage || !hasBat) return; // Ensure we haven't already applied damage and player has bat

        // Use upward attack point if available, otherwise fallback to normal attack point
        Transform damagePoint = upwardAttackPoint != null ? upwardAttackPoint : attackPoint;
        ApplyDamageAtPoint(damagePoint);
    }

    /// <summary>
    /// Generic damage application method that can be used for both attack types
    /// </summary>
    /// <param name="damagePoint">The transform point where damage should be applied</param>
    private void ApplyDamageAtPoint(Transform damagePoint)
    {
        if (damagePoint == null)
        {
            Debug.LogWarning("Damage point is null! Cannot apply damage.");
            canDealDamage = false;
            return;
        }

        // Detect enemies in the attack range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(damagePoint.position, attackRange, enemyLayers);

        // Apply damage to each detected enemy
        foreach (Collider2D enemy in hitEnemies)
        {
            // Calculate knockback direction from damage point to enemy
            Vector2 knockbackDirection = ((Vector2)(enemy.transform.position - damagePoint.position)).normalized;

            // Use a common interface or base class for health components if possible
            if (enemy.TryGetComponent<FleaHealth>(out var fleaHealth))
            {
                fleaHealth.TakeDamage(damage, knockbackDirection, fleaKnockbackForce);
            }
            else if (enemy.TryGetComponent<InkHealth>(out var inkHealth))
            {
                inkHealth.TakeDamage(damage, knockbackDirection, inkKnockbackForce);
            }
            else if (enemy.TryGetComponent<FlyHealth>(out var flyHealth))
            {
                flyHealth.TakeDamage(damage, knockbackDirection, flyKnockbackForce);
            }
            else if (enemy.TryGetComponent<SprayerHealth>(out var sprayerHealth))
            {
                sprayerHealth.TakeDamage(damage, knockbackDirection, sprayerKnockbackForce);
            }
            else
            {
                // Fallback for other types or if no specific health script is found
                Debug.LogWarning($"No recognized health script found on {enemy.name}. Damage applied without specific knockback.");
            }

            Debug.Log($"Hit {enemy.name} for {damage} damage with {(isUpwardAttack ? "Upward" : "Normal")} attack!");
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

    public bool IsPerformingUpwardAttack()
    {
        return isUpwardAttack;
    }

    public bool HasBat()
    {
        return hasBat;
    }

    public bool CanAttack()
    {
        return hasBat && !isAnticipating && Time.time >= nextAttackTime;
    }

    // To visualize the attack range in the Scene View (for debugging only)
    void OnDrawGizmosSelected()
    {
        // Draw normal attack range
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
            Gizmos.DrawLine(transform.position, attackPoint.position);
        }

        // Draw upward attack range
        if (upwardAttackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(upwardAttackPoint.position, attackRange);
            Gizmos.DrawLine(transform.position, upwardAttackPoint.position);
        }

        // Draw bat pickup range
        if (!hasBat)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, batPickupRange);
        }

        // Draw ghost target indicators
        Gizmos.color = Color.cyan;
        foreach (SpriteRenderer targetRenderer in ghostTargets)
        {
            if (targetRenderer != null)
            {
                Gizmos.DrawWireCube(targetRenderer.bounds.center, targetRenderer.bounds.size);
            }
        }

        // Draw mouse direction indicator (only in play mode)
        if (Application.isPlaying && playerCamera != null)
        {
            Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = transform.position.z;
            Vector2 directionToMouse = (mouseWorldPos - transform.position).normalized;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)directionToMouse * 2f);
        }

        // Draw Upward Attack Zone
        Gizmos.color = Color.green;
        Vector3 playerPos = transform.position;
        Vector3 upwardZoneMin = new Vector3(playerPos.x - 1f, playerPos.y + upwardZoneMinY, playerPos.z);
        Vector3 upwardZoneMax = new Vector3(playerPos.x + 1f, playerPos.y + upwardZoneMaxY, playerPos.z);
        Gizmos.DrawWireCube(Vector3.Lerp(upwardZoneMin, upwardZoneMax, 0.5f), upwardZoneMax - upwardZoneMin);

        // Draw Normal Attack Zone
        Gizmos.color = Color.magenta;
        Vector3 normalZoneMin = new Vector3(playerPos.x - 1f, playerPos.y - 1f, playerPos.z);
        Vector3 normalZoneMax = new Vector3(playerPos.x + 1f, playerPos.y + normalZoneMaxY, playerPos.z);
        Gizmos.DrawWireCube(Vector3.Lerp(normalZoneMin, normalZoneMax, 0.5f), normalZoneMax - normalZoneMin);
    }
}

