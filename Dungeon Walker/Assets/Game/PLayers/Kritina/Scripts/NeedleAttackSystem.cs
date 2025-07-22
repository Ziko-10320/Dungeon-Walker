using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatAttackSystem : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Animator playerAnimator; // Reference to the player\"s Animator
    [SerializeField] private string anticipationTriggerName = "Anticipation"; // Name of the Anticipation Trigger in the Animator
    [SerializeField] private string attackTriggerName = "BatAttack"; // Name of the Attack Trigger in the Animator
    [SerializeField] private string upwardAttackTriggerName = "UpwardAttack"; // Name of the Upward Attack Trigger in the Animator
    [SerializeField] private string throwBatTriggerName = "ThrowBat"; // Name of the Throw Bat Trigger in the Animator
    [SerializeField] private float anticipationDuration = 1.0f; // Duration of the anticipation animation
    [SerializeField] private float attackCooldown = 1.0f; // Cooldown duration for the attack (in seconds)
    [SerializeField] private int damage = 20; // Damage amount dealt by the attack

    [Header("Throw Slash Settings")]
    [SerializeField] private GameObject throwSlashPrefab; // Prefab of the ThrowSlash projectile
    [SerializeField] private Transform throwSlashSpawnPoint; // Point where the ThrowSlash is spawned when thrown
    [SerializeField] private float throwSlashSpeed = 15f; // Speed of the thrown ThrowSlash
    [SerializeField] private int throwSlashDamage = 20; // Damage dealt by ThrowSlash
    [SerializeField] private GameObject bat2Prefab; // Prefab of the Bat2 to spawn on ground hit
    [SerializeField] private float throwSlashGroundDetectionDelay = 0.5f; // Delay before ThrowSlash can detect ground


    [Header("Bat Pickup Settings")]
    [SerializeField] private float batPickupRange = 1.5f; // Range within which player can pick up the Bat2

    [Header("Damage Area Settings")]
    [SerializeField] private Transform attackPoint; // Origin point of the normal attack (usually in front of the player)
    [SerializeField] private Transform upwardAttackPoint; // Origin point of the upward attack (usually above the player)
    [SerializeField] private float attackRange = 0.5f; // Radius of the attack area (circle)
    [SerializeField] private LayerMask enemyLayers; // Layers of enemies that can receive damage
    [SerializeField] private LayerMask groundLayer; // Layer mask for ground collision

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
    [SerializeField] private AudioClip throwSlashHitEnemySound; // Sound played when ThrowSlash hits an enemy

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
    private GameObject spawnedBat2; // Reference to the currently spawned Bat2 on the ground
    private Vector3 lastMousePosition; // Store the mouse position at the moment of input

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

        // Auto-assign throw slash spawn point if not set
        if (throwSlashSpawnPoint == null)
        {
            throwSlashSpawnPoint = transform; // Use player transform as default
            Debug.LogWarning("Throw Slash spawn point not assigned. Using player transform as default.");
        }

        // Ensure player bat visual is visible at start
        if (playerBatVisual != null)
        {
            playerBatVisual.SetActive(true);
        }

        // Auto-assign player animator if not set
        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Animator>();
            if (playerAnimator == null)
            {
                Debug.LogWarning("Player Animator not found. Animations will not work.");
            }
        }
    }

    void Update()
    {
        // Right Mouse Button for Normal/Upward Attacks
        if (Input.GetMouseButtonDown(1) && Time.time >= nextAttackTime && !isAnticipating && hasBat)
        {
            bool shouldPerformUpwardAttack = ShouldPerformUpwardAttack();
            StartAnticipationAttack(shouldPerformUpwardAttack);
        }

        // Left Mouse Button for Throwing ThrowSlash
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime && !isAnticipating && hasBat)
        {
            // Store mouse position at the moment of input
            lastMousePosition = Input.mousePosition;
            StartAnticipationAndThrowSlash();
        }

        // Check for Bat2 pickup if player doesn\"t have bat
        if (!hasBat && spawnedBat2 != null)
        {
            float distanceToBat2 = Vector2.Distance(transform.position, spawnedBat2.transform.position);
            if (distanceToBat2 <= batPickupRange)
            {
                PickUpBat2();
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

    void StartAnticipationAttack(bool performUpwardAttack = false)
    {
        isAnticipating = true;
        isUpwardAttack = performUpwardAttack;
        nextAttackTime = Time.time + anticipationDuration + attackCooldown; // Set cooldown for after anticipation

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

        // Start the anticipation routine for normal/upward attack
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

    void StartAnticipationAndThrowSlash()
    {
        isAnticipating = true;
        nextAttackTime = Time.time + anticipationDuration + attackCooldown; // Set cooldown for after anticipation

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

        // Start the anticipation routine which will lead to ThrowSlash
        StartCoroutine(AnticipationRoutineForThrowSlash());
    }

    IEnumerator AnticipationRoutineForThrowSlash()
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

        // Trigger ThrowBat animation and throw ThrowSlash
        ThrowSlash();

        isAnticipating = false;
    }

    void ThrowSlash()
    {
        if (throwSlashPrefab == null)
        {
            Debug.LogWarning("Throw Slash prefab not assigned! Cannot throw.");
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("Player camera not found! Cannot determine throw direction.");
            return;
        }

        // Use the stored mouse position to get the target world point
        Vector3 targetWorldPoint = playerCamera.ScreenToWorldPoint(new Vector3(lastMousePosition.x, lastMousePosition.y, playerCamera.nearClipPlane));
        targetWorldPoint.z = throwSlashSpawnPoint.position.z; // Ensure the Z coordinate matches the spawn point

        // Calculate the direction from the spawn point to the target world point
        Vector2 throwDirection = (targetWorldPoint - throwSlashSpawnPoint.position).normalized;

        // If the target is too close to the spawn point, provide a default direction
        if (throwDirection.magnitude < 0.1f)
        {
            throwDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        }

        GameObject slashInstance = Instantiate(throwSlashPrefab, throwSlashSpawnPoint.position, Quaternion.identity);

        // Initialize ThrowSlash properties
        Rigidbody2D slashRb = slashInstance.GetComponent<Rigidbody2D>();
        if (slashRb == null)
        {
            Debug.LogWarning("ThrowSlash prefab is missing Rigidbody2D component! Adding one.");
            slashRb = slashInstance.AddComponent<Rigidbody2D>();
            slashRb.gravityScale = 0; // Disable gravity for projectile
        }

        // Ensure the ThrowSlash has a collider
        Collider2D slashCollider = slashInstance.GetComponent<Collider2D>();
        if (slashCollider == null)
        {
            Debug.LogWarning("ThrowSlash prefab is missing Collider2D component! Adding one.");
            slashCollider = slashInstance.AddComponent<BoxCollider2D>(); // Default to BoxCollider2D
            slashCollider.isTrigger = true;
        }

        // Set velocity for movement
        slashRb.velocity = throwDirection * throwSlashSpeed;

        // Calculate rotation for the ThrowSlash based on throwDirection
        float angle = Mathf.Atan2(throwDirection.y, throwDirection.x) * Mathf.Rad2Deg;
        slashInstance.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        // Add a component to handle collision and Bat2 spawning
        ThrowSlashHandler slashHandler = slashInstance.AddComponent<ThrowSlashHandler>();
        slashHandler.Initialize(throwSlashDamage, enemyLayers, groundLayer, bat2Prefab, this, throwSlashGroundDetectionDelay, audioSource, throwSlashHitEnemySound);

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(throwBatTriggerName);
        }

        // Play throw sound
        if (audioSource != null && throwBatSound != null)
        {
            audioSource.PlayOneShot(throwBatSound);
        }

        // Disable the Bat spriteRenderer
        if (playerBatVisual != null)
        {
            playerBatVisual.SetActive(false);
        }

        hasBat = false;

        if (showDirectionDebug)
        {
            Debug.Log($"Threw ThrowSlash towards: {throwDirection} with speed: {throwSlashSpeed}");
        }
    }

    public void SetSpawnedBat2(GameObject bat2)
    {
        spawnedBat2 = bat2;
    }

    void PickUpBat2()
    {
        if (spawnedBat2 != null)
        {
            Destroy(spawnedBat2);
            spawnedBat2 = null;
        }

        // Enable the Bat spriteRenderer again
        if (playerBatVisual != null)
        {
            playerBatVisual.SetActive(true);
        }

        hasBat = true;
        Debug.Log("Picked up Bat2! Bat spriteRenderer enabled.");
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

        // Copy the transform\"s properties directly
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
        if (!canDealDamage || !hasBat) return; // Ensure we haven\"t already applied damage and player has bat

        ApplyDamageAtPoint(attackPoint);
    }

    // This function will be called as an Animation Event at the specified frame for upward attacks
    public void ApplyUpwardDamage()
    {
        if (!canDealDamage || !hasBat) return; // Ensure we haven\"t already applied damage and player has bat

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
                Debug.LogWarning($"No recognized health script found on {enemy.name}. Damage applied without specific knockback.");
            }


        }

        canDealDamage = false; // Disable damage application after it\"s been dealt
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

    // Inner class to handle ThrowSlash collision and Bat2 spawning
    public class ThrowSlashHandler : MonoBehaviour
    {
        private int damage;
        private LayerMask enemyLayers;
        private LayerMask groundLayer;
        private GameObject bat2Prefab;
        private BatAttackSystem parentSystem;
        private bool hasHit = false;
        private float canDetectGroundTime; // New variable for ground detection delay
        private AudioSource audioSource; // AudioSource for playing sounds
        private AudioClip hitEnemySound; // Sound for hitting enemy

        public void Initialize(int dmg, LayerMask enemies, LayerMask ground, GameObject bat2, BatAttackSystem system, float groundDetectionDelay, AudioSource src, AudioClip hitSound)
        {
            damage = dmg;
            enemyLayers = enemies;
            groundLayer = ground;
            bat2Prefab = bat2;
            parentSystem = system;
            canDetectGroundTime = Time.time + groundDetectionDelay; // Set the time when ground detection becomes active
            audioSource = src;
            hitEnemySound = hitSound;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (hasHit) return;

            // Check if it hit an enemy
            if (((1 << other.gameObject.layer) & enemyLayers) != 0)
            {
                Debug.Log($"ThrowSlash hit enemy: {other.name}");

                // Play hit enemy sound
                if (audioSource != null && hitEnemySound != null)
                {
                    audioSource.PlayOneShot(hitEnemySound);
                }

                // Apply damage using the same logic as the main attack system
                ApplyDamageToEnemy(other);

                // Spawn Bat2 at the enemy hit position
                SpawnBat2(transform.position);

                Destroy(gameObject); // Destroy ThrowSlash on enemy hit
            }
            // Check if it hit the ground, only if enough time has passed
            else if (((1 << other.gameObject.layer) & groundLayer) != 0 && Time.time >= canDetectGroundTime)
            {
                Debug.Log("ThrowSlash hit ground.");
                hasHit = true;
                SpawnBat2(transform.position);
                Destroy(gameObject); // Destroy ThrowSlash after spawning Bat2
            }
        }

        void ApplyDamageToEnemy(Collider2D enemy)
        {
            // Calculate knockback direction from ThrowSlash to enemy
            Vector2 knockbackDirection = ((Vector2)(enemy.transform.position - transform.position)).normalized;

            // Use the same damage application logic as the main system
            if (enemy.TryGetComponent<FleaHealth>(out var fleaHealth))
            {
                fleaHealth.TakeDamage(damage, knockbackDirection, parentSystem.fleaKnockbackForce);
            }
            else if (enemy.TryGetComponent<InkHealth>(out var inkHealth))
            {
                inkHealth.TakeDamage(damage, knockbackDirection, parentSystem.inkKnockbackForce);
            }
            else if (enemy.TryGetComponent<FlyHealth>(out var flyHealth))
            {
                flyHealth.TakeDamage(damage, knockbackDirection, parentSystem.flyKnockbackForce);
            }
            else if (enemy.TryGetComponent<SprayerHealth>(out var sprayerHealth))
            {
                sprayerHealth.TakeDamage(damage, knockbackDirection, parentSystem.sprayerKnockbackForce);
            }
            else
            {
                Debug.LogWarning($"No recognized health script found on {enemy.name}. ThrowSlash damage applied without specific knockback.");
            }
        }

        void SpawnBat2(Vector3 position)
        {
            if (bat2Prefab != null)
            {
                GameObject bat2Instance = Instantiate(bat2Prefab, position, Quaternion.identity);
                // Bat2 is now dynamic, so we don\"t set isKinematic or velocity to zero here.
                // It should have its own Rigidbody2D with gravity enabled in its prefab.
                parentSystem.SetSpawnedBat2(bat2Instance);
                Debug.Log("Bat2 spawned at ground hit position.");
            }
            else
            {
                Debug.LogWarning("Bat2 Prefab is not assigned in BatAttackSystem. Cannot spawn Bat2.");
            }
        }
    }
}
