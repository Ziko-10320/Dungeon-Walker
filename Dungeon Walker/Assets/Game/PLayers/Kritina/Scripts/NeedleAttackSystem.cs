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
    [SerializeField] private Transform throwSlashSpawnPointRight; // Point where the ThrowSlash is spawned when facing right
    [SerializeField] private Transform throwSlashSpawnPointLeft; // Point where the ThrowSlash is spawned when facing left
    [SerializeField] private float throwSlashSpeed = 15f; // Speed of the thrown ThrowSlash
    [SerializeField] private int throwSlashDamage = 20; // Damage dealt by ThrowSlash
    [SerializeField] private GameObject bat2Prefab; // Prefab of the Bat2 to spawn on ground hit
    [SerializeField] public float aimVerticalOffsetRightCursor = 0f; // Vertical offset for aim when cursor is to the right of the player
    [SerializeField] public float aimVerticalOffsetLeftCursor = 0f; // Vertical offset for aim when cursor is to the left of the player

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

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheck; // Point for ground detection
    [SerializeField] private float groundCheckRadius = 0.2f; // Radius of the ground check circle
    [SerializeField] private LayerMask whatIsGround; // Layer mask for ground

    // Private variables
    private float nextAttackTime = 0f;
    private bool canDealDamage = false;
    private bool isAnticipating = false;
    private Coroutine ghostEffectCoroutine;
    private Camera playerCamera;
    private bool isUpwardAttack = false;
    private bool hasBat = true;
    private GameObject spawnedBat2; // Reference to the currently spawned Bat2 on the ground
    private Vector3 lastMousePosition;
    private bool isGrounded;

    // Static list to track all active Bat2 objects for cleanup
    private static List<GameObject> activeBat2Objects = new List<GameObject>();

    // Static list to track all active ghost objects for cleanup
    private static List<GameObject> activeGhostObjects = new List<GameObject>();

    void OnDisable()
    {
        Debug.Log("BatAttackSystem OnDisable called - performing comprehensive cleanup");

        // Clean up all active Bat2 objects
        CleanupAllBat2Objects();

        // Clean up all ghost objects
        CleanupAllGhostObjects();

        // Stop all coroutines to prevent MissingReferenceException
        StopAllCoroutines();

        // Ensure the player\"s bat visual is active when the script is disabled
        // This prepares it for when the bat weapon is re-enabled or picked up
        if (playerBatVisual != null)
        {
            playerBatVisual.SetActive(true);
            Debug.Log("playerBatVisual set to active in OnDisable (preparing for re-enable).");
        }

        // Reset other states
        isAnticipating = false;
        Time.timeScale = 1.0f; // Ensure time scale is reset
        Debug.Log("BatAttackSystem state reset in OnDisable.");
    }

    void OnEnable()
    {
        Debug.Log("BatAttackSystem OnEnable called - resetting to fresh state");
        ResetBatSystemState();
    }

    void Start()
    {
        InitializeComponents();
        ResetBatSystemState();
    }

    void Update()
    {
        CheckGround();

        if (Input.GetMouseButtonDown(1) && Time.time >= nextAttackTime && !isAnticipating && hasBat)
        {
            bool shouldPerformUpwardAttack = ShouldPerformUpwardAttack();
            StartAnticipationAttack(shouldPerformUpwardAttack);
        }

        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime && !isAnticipating && hasBat)
        {
            lastMousePosition = Input.mousePosition;
            StartAnticipationAndThrowSlash();
        }

        if (!hasBat && spawnedBat2 != null)
        {
            // Check if spawnedBat2 is still valid before accessing its transform
            if (spawnedBat2 != null)
            {
                float distanceToBat2 = Vector2.Distance(transform.position, spawnedBat2.transform.position);
                if (distanceToBat2 <= batPickupRange)
                {
                    PickUpBat2();
                }
                CheckAndAdjustBat2Position();
            }
        }
    }

    private void InitializeComponents()
    {
        if (cameraEffects == null)
        {
            cameraEffects = Camera.main?.GetComponent<CameraEffects>();
            if (cameraEffects == null)
            {
                enableCameraEffects = false;
            }
        }

        playerCamera = Camera.main;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // The original throwSlashSpawnPoint is no longer needed as we have specific left/right ones.
        // If it was used for other purposes, it should be kept and its usage clarified.
        // For now, assuming it's solely for ThrowSlash spawning and can be removed.
        // if (throwSlashSpawnPoint == null)
        // {
        //     throwSlashSpawnPoint = transform;
        // }

        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Animator>();
        }

        if (groundCheck == null)
        {
            groundCheck = transform;
        }
    }

    private void ResetBatSystemState()
    {
        hasBat = true;
        isAnticipating = false;
        spawnedBat2 = null; // Clear reference to Bat2
        Time.timeScale = 1.0f;

        if (playerBatVisual != null)
        {
            playerBatVisual.SetActive(true);
        }
        Debug.Log("BatAttackSystem state fully reset.");
    }

    private void CleanupAllBat2Objects()
    {
        // Clean up the current spawned Bat2 if it exists and is not already destroyed
        if (spawnedBat2 != null)
        {
            Destroy(spawnedBat2);
            spawnedBat2 = null;
        }

        // Clean up all tracked Bat2 objects
        for (int i = activeBat2Objects.Count - 1; i >= 0; i--)
        {
            if (activeBat2Objects[i] != null)
            {
                Destroy(activeBat2Objects[i]);
            }
        }
        activeBat2Objects.Clear();

        // Find and destroy any remaining Bat2 objects in the scene by name pattern
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            // Check if the object\"s name contains "Bat2" and it\"s not the player\"s visual bat
            if (obj != playerBatVisual && obj.name.Contains("Bat2") && obj.GetComponent<Rigidbody2D>() != null)
            {
                Destroy(obj);
            }
        }

        Debug.Log("All Bat2 objects cleaned up");
    }

    private void CleanupAllGhostObjects()
    {
        // Stop ghost effect coroutine if it\"s running
        if (ghostEffectCoroutine != null)
        {
            StopCoroutine(ghostEffectCoroutine);
            ghostEffectCoroutine = null;
        }

        // Clean up all tracked ghost objects
        for (int i = activeGhostObjects.Count - 1; i >= 0; i--)
        {
            if (activeGhostObjects[i] != null)
            {
                Destroy(activeGhostObjects[i]);
            }
        }
        activeGhostObjects.Clear();

        // Find and destroy any remaining ghost objects by tag and name pattern
        GameObject[] remainingGhostsByTag = GameObject.FindGameObjectsWithTag("Ghost");
        foreach (GameObject ghost in remainingGhostsByTag)
        {
            Destroy(ghost);
        }

        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("Ghost_"))
            {
                Destroy(obj);
            }
        }

        Debug.Log("All ghost objects cleaned up");
    }

    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
    }

    private void CheckAndAdjustBat2Position()
    {
        if (spawnedBat2 == null) return;

        Rigidbody2D bat2Rb = spawnedBat2.GetComponent<Rigidbody2D>();
        if (bat2Rb == null) return;

        // Use a small raycast distance to detect ground directly below the bat
        float raycastDistance = 0.2f; // Adjusted for more precise ground detection
        // Offset the bat slightly above the ground to prevent it from sinking
        float verticalOffset = 0.05f; // Adjusted for more precise positioning

        // Perform a raycast downwards from the bat\"s position
        RaycastHit2D hit = Physics2D.Raycast(spawnedBat2.transform.position, Vector2.down, raycastDistance, groundLayer);

        // If the raycast hits the ground and the bat is below the intended vertical offset
        if (hit.collider != null && spawnedBat2.transform.position.y < hit.point.y + verticalOffset)
        {
            Vector3 newPosition = spawnedBat2.transform.position;
            newPosition.y = hit.point.y + verticalOffset; // Set the bat\"s Y position to be slightly above the ground hit point
            spawnedBat2.transform.position = newPosition;
            // If the bat is moving downwards, stop its vertical velocity to prevent bouncing or sinking
            if (bat2Rb.velocity.y < 0)
            {
                bat2Rb.velocity = new Vector2(bat2Rb.velocity.x, 0);
            }
        }
    }

    private bool ShouldPerformUpwardAttack()
    {
        if (playerCamera == null) return false;

        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;
        float relativeMouseY = mouseWorldPos.y - transform.position.y;

        if (relativeMouseY >= upwardZoneMinY && relativeMouseY <= upwardZoneMaxY)
        {
            return true;
        }
        else if (relativeMouseY <= normalZoneMaxY)
        {
            return false;
        }

        return false;
    }

    void StartAnticipationAttack(bool performUpwardAttack = false)
    {
        isAnticipating = true;
        isUpwardAttack = performUpwardAttack;
        nextAttackTime = Time.time + anticipationDuration + attackCooldown;

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(anticipationTriggerName);
        }

        if (enableCameraEffects && cameraEffects != null)
        {
            cameraEffects.StartHoldAndReleaseEffect();
        }

        if (ghostTargets.Count > 0)
        {
            ghostEffectCoroutine = StartCoroutine(GhostEffectRoutine());
        }

        StartCoroutine(AnticipationRoutine());
    }

    IEnumerator AnticipationRoutine()
    {
        Time.timeScale = 0.8f;
        yield return new WaitForSecondsRealtime(anticipationDuration);
        Time.timeScale = 1.0f;

        if (ghostEffectCoroutine != null)
        {
            StopCoroutine(ghostEffectCoroutine);
            ghostEffectCoroutine = null;
        }

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
        nextAttackTime = Time.time + anticipationDuration + attackCooldown;

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(anticipationTriggerName);
        }

        if (enableCameraEffects && cameraEffects != null)
        {
            cameraEffects.StartHoldAndReleaseEffect();
        }

        if (ghostTargets.Count > 0)
        {
            ghostEffectCoroutine = StartCoroutine(GhostEffectRoutine());
        }

        StartCoroutine(AnticipationRoutineForThrowSlash());
    }

    IEnumerator AnticipationRoutineForThrowSlash()
    {
        Time.timeScale = 0.8f;
        yield return new WaitForSecondsRealtime(anticipationDuration);
        Time.timeScale = 1.0f;

        if (ghostEffectCoroutine != null)
        {
            StopCoroutine(ghostEffectCoroutine);
            ghostEffectCoroutine = null;
        }

        ThrowSlash();
        isAnticipating = false;
    }

    void ThrowSlash()
    {
        if (throwSlashPrefab == null || playerCamera == null)
        {
            Debug.LogWarning("Cannot throw - missing prefab or camera");
            return;
        }

        Transform currentSpawnPoint = null;
        if (transform.localScale.x > 0)
        { // Player is facing right
            currentSpawnPoint = throwSlashSpawnPointRight;
        }
        else
        { // Player is facing left
            currentSpawnPoint = throwSlashSpawnPointLeft;
        }

        if (currentSpawnPoint == null)
        {
            Debug.LogWarning("ThrowSlash spawn point is not assigned for the current direction.");
            return;
        }

        Vector3 targetWorldPoint = playerCamera.ScreenToWorldPoint(new Vector3(lastMousePosition.x, lastMousePosition.y, playerCamera.nearClipPlane));
        targetWorldPoint.z = currentSpawnPoint.position.z;

        // Determine if the cursor is to the right or left of the player
        float playerScreenX = playerCamera.WorldToScreenPoint(transform.position).x;
        float cursorScreenX = lastMousePosition.x;

        if (cursorScreenX > playerScreenX) // Cursor is to the right of the player
        {
            targetWorldPoint.y += aimVerticalOffsetRightCursor;
        }
        else // Cursor is to the left of the player
        {
            targetWorldPoint.y += aimVerticalOffsetLeftCursor;
        }

        Vector2 throwDirection = (targetWorldPoint - currentSpawnPoint.position).normalized;

        if (throwDirection.magnitude < 0.1f)
        {
            throwDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        }

        GameObject slashInstance = Instantiate(throwSlashPrefab, currentSpawnPoint.position, Quaternion.identity);

        Rigidbody2D slashRb = slashInstance.GetComponent<Rigidbody2D>();
        if (slashRb == null)
        {
            slashRb = slashInstance.AddComponent<Rigidbody2D>();
            slashRb.gravityScale = 0;
        }
        slashRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Collider2D slashCollider = slashInstance.GetComponent<Collider2D>();
        if (slashCollider == null)
        {
            slashCollider = slashInstance.AddComponent<BoxCollider2D>();
            slashCollider.isTrigger = true;
        }

        slashRb.velocity = throwDirection * throwSlashSpeed;

        float angle = Mathf.Atan2(throwDirection.y, throwDirection.x) * Mathf.Rad2Deg;
        slashInstance.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        ThrowSlashHandler slashHandler = slashInstance.AddComponent<ThrowSlashHandler>();
        slashHandler.Initialize(throwSlashDamage, enemyLayers, groundLayer, bat2Prefab, this, audioSource, throwSlashHitEnemySound,
                                 fleaKnockbackForce, inkKnockbackForce, flyKnockbackForce, sprayerKnockbackForce);

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(throwBatTriggerName);
        }

        if (audioSource != null && throwBatSound != null)
        {
            audioSource.PlayOneShot(throwBatSound);
        }

        if (playerBatVisual != null)
        {
            playerBatVisual.SetActive(false);
        }

        hasBat = false;
    }

    public void SetSpawnedBat2(GameObject bat2)
    {
        spawnedBat2 = bat2;
        if (bat2 != null && !activeBat2Objects.Contains(bat2))
        {
            activeBat2Objects.Add(bat2);
        }
    }

    void PickUpBat2()
    {
        if (spawnedBat2 != null)
        {
            activeBat2Objects.Remove(spawnedBat2);
            Destroy(spawnedBat2);
            spawnedBat2 = null;
        }

        // When picking up the bat, reset the system state as if nothing happened
        ResetBatSystemState();
        Debug.Log("Picked up Bat2! Bat visual enabled and system state reset.");
    }

    IEnumerator GhostEffectRoutine()
    {
        float timer = 0f;

        while (timer < anticipationDuration)
        {
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

        GameObject ghostObject = new GameObject("Ghost_" + originalRenderer.name);
        ghostObject.tag = "Ghost";
        activeGhostObjects.Add(ghostObject);

        SpriteRenderer ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();

        ghostObject.transform.position = originalRenderer.transform.position;
        ghostObject.transform.rotation = originalRenderer.transform.rotation;
        ghostObject.transform.localScale = originalRenderer.transform.lossyScale;

        ghostRenderer.sprite = originalRenderer.sprite;
        ghostRenderer.sortingLayerID = originalRenderer.sortingLayerID;
        ghostRenderer.sortingOrder = originalRenderer.sortingOrder - 1;
        ghostRenderer.flipX = originalRenderer.flipX;
        ghostRenderer.flipY = originalRenderer.flipY;

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

        StartCoroutine(FadeOutGhost(ghostRenderer));
    }

    IEnumerator FadeOutGhost(SpriteRenderer ghostRenderer)
    {
        // Add null check at the start of the coroutine
        if (ghostRenderer == null) yield break;

        Color startColor = ghostRenderer.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        float timer = 0f;

        while (timer < ghostDuration)
        {
            // Add null check inside the loop as well
            if (ghostRenderer == null) yield break;

            timer += Time.unscaledDeltaTime;
            float progress = timer / ghostDuration;
            ghostRenderer.color = Color.Lerp(startColor, endColor, progress);
            yield return null;
        }

        if (ghostRenderer != null && ghostRenderer.gameObject != null)
        {
            activeGhostObjects.Remove(ghostRenderer.gameObject);
            Destroy(ghostRenderer.gameObject);
        }
    }

    void PerformBatAttack()
    {
        nextAttackTime = Time.time + attackCooldown;

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(attackTriggerName);
        }

        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        canDealDamage = true;

        if (enableCameraEffects && cameraEffects != null)
        {
            cameraEffects.StartShakeEffect();
        }
    }

    void PerformUpwardAttack()
    {
        nextAttackTime = Time.time + attackCooldown;

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(upwardAttackTriggerName);
        }

        if (audioSource != null && upwardAttackSound != null)
        {
            audioSource.PlayOneShot(upwardAttackSound);
        }

        canDealDamage = true;

        if (enableCameraEffects && cameraEffects != null)
        {
            cameraEffects.StartShakeEffect();
        }
    }

    public void ApplyDamage()
    {
        if (!canDealDamage || !hasBat) return;
        ApplyDamageAtPoint(attackPoint);
    }

    public void ApplyUpwardDamage()
    {
        if (!canDealDamage || !hasBat) return;
        Transform damagePoint = upwardAttackPoint != null ? upwardAttackPoint : attackPoint;
        ApplyDamageAtPoint(damagePoint);
    }

    private void ApplyDamageAtPoint(Transform damagePoint)
    {
        if (damagePoint == null)
        {
            canDealDamage = false;
            return;
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(damagePoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            // Add null check for enemy GameObject before accessing its transform
            if (enemy == null || enemy.gameObject == null) continue;

            Vector2 knockbackDirection = ((Vector2)(enemy.transform.position - damagePoint.position)).normalized;

            // Use a common interface or base class for health components if possible
            // Add null checks before calling TryGetComponent
            if (enemy.TryGetComponent<FleaHealth>(out var fleaHealth) && fleaHealth != null)
            {
                fleaHealth.TakeDamage(damage, knockbackDirection, fleaKnockbackForce);
            }
            else if (enemy.TryGetComponent<InkHealth>(out var inkHealth) && inkHealth != null)
            {
                inkHealth.TakeDamage(damage, knockbackDirection, inkKnockbackForce);
            }
            else if (enemy.TryGetComponent<FlyHealth>(out var flyHealth) && flyHealth != null)
            {
                flyHealth.TakeDamage(damage, knockbackDirection, flyKnockbackForce);
            }
            else if (enemy.TryGetComponent<SprayerHealth>(out var sprayerHealth) && sprayerHealth != null)
            {
                sprayerHealth.TakeDamage(damage, knockbackDirection, sprayerKnockbackForce);
            }
            else if (enemy.TryGetComponent<RatKingHealth>(out var RatKingHealth) && RatKingHealth != null)
            {
                RatKingHealth.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning($"No recognized health script found on {enemy.name}. Damage applied without specific knockback.");
            }
        }

        canDealDamage = false;
    }

    // Public utility methods
    public bool IsAnticipating() => isAnticipating;
    public bool IsPerformingUpwardAttack() => isUpwardAttack;
    public bool HasBat() => hasBat;
    public bool CanAttack() => hasBat && !isAnticipating && Time.time >= nextAttackTime;

    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
            Gizmos.DrawLine(transform.position, attackPoint.position);
        }

        if (upwardAttackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(upwardAttackPoint.position, attackRange);
            Gizmos.DrawLine(transform.position, upwardAttackPoint.position);
        }

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

            // Apply aim offset based on cursor position relative to player
            float playerScreenX = playerCamera.WorldToScreenPoint(transform.position).x;
            float cursorScreenX = Input.mousePosition.x;

            if (cursorScreenX > playerScreenX) // Cursor is to the right of the player
            {
                mouseWorldPos.y += aimVerticalOffsetRightCursor;
            }
            else // Cursor is to the left of the player
            {
                mouseWorldPos.y += aimVerticalOffsetLeftCursor;
            }

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

        // Draw Ground Check
        Gizmos.color = Color.white;
        if (groundCheck != null)
        {
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    public class ThrowSlashHandler : MonoBehaviour
    {
        private int damage;
        private LayerMask enemyLayers;
        private LayerMask groundLayer;
        private GameObject bat2Prefab;
        private BatAttackSystem parentSystem;
        private bool hasHit = false;
        private AudioSource audioSource;
        private AudioClip hitEnemySound;

        private float _fleaKnockbackForce;
        private float _inkKnockbackForce;
        private float _flyKnockbackForce;
        private float _sprayerKnockbackForce;

        public void Initialize(int dmg, LayerMask enemies, LayerMask ground, GameObject bat2, BatAttackSystem system, AudioSource src, AudioClip hitSound,
                               float fleaKB, float inkKB, float flyKB, float sprayerKB)
        {
            damage = dmg;
            enemyLayers = enemies;
            groundLayer = ground;
            bat2Prefab = bat2;
            parentSystem = system;
            audioSource = src;
            hitEnemySound = hitSound;

            _fleaKnockbackForce = fleaKB;
            _inkKnockbackForce = inkKB;
            _flyKnockbackForce = flyKB;
            _sprayerKnockbackForce = sprayerKB;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (hasHit) return;

            // Check for enemy collision
            if (((1 << other.gameObject.layer) & enemyLayers) != 0)
            {
                // Check if the enemy has a health script and apply damage
                Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;

                if (other.TryGetComponent<FleaHealth>(out var fleaHealth) && fleaHealth != null)
                {
                    fleaHealth.TakeDamage(damage, knockbackDirection, _fleaKnockbackForce);
                }
                else if (other.TryGetComponent<InkHealth>(out var inkHealth) && inkHealth != null)
                {
                    inkHealth.TakeDamage(damage, knockbackDirection, _inkKnockbackForce);
                }
                else if (other.TryGetComponent<FlyHealth>(out var flyHealth) && flyHealth != null)
                {
                    flyHealth.TakeDamage(damage, knockbackDirection, _flyKnockbackForce);
                }
                else if (other.TryGetComponent<SprayerHealth>(out var sprayerHealth) && sprayerHealth != null)
                {
                    sprayerHealth.TakeDamage(damage, knockbackDirection, _sprayerKnockbackForce);
                }
                else if (other.TryGetComponent<RatKingHealth>(out var RatKingHealth) && RatKingHealth != null)
                {
                    RatKingHealth.TakeDamage(damage);
                }
                else
                {
                    Debug.LogWarning($"No recognized health script found on {other.name}. Damage applied without specific knockback.");
                }

                if (audioSource != null && hitEnemySound != null)
                {
                    audioSource.PlayOneShot(hitEnemySound);
                }

                hasHit = true;
                Destroy(gameObject); // Destroy the slash after hitting an enemy
            }
            // Check for ground collision
            else if (((1 << other.gameObject.layer) & groundLayer) != 0)
            {
                hasHit = true;
                // Spawn Bat2 at the collision point
                if (bat2Prefab != null)
                {
                    GameObject bat2Instance = Instantiate(bat2Prefab, transform.position, Quaternion.identity);
                    parentSystem.SetSpawnedBat2(bat2Instance);
                }
                Destroy(gameObject); // Destroy the slash after hitting the ground
            }
        }
    }
}
