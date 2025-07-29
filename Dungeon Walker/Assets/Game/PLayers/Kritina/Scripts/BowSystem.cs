using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using FirstGearGames.SmoothCameraShaker;

public class BowSystems : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private GameObject BowGameObject;
    [SerializeField] private GameObject Arm;
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private Transform bowAimPoint; // Specific point on the bow for aiming
    [SerializeField] private Transform minDistancePoint; // Transform point for minimum distance visualization
    [SerializeField] private Transform trajectoryVisualPoint; // Transform point for trajectory visualization (controls green line)

    [Header("Arrow Prefab")]
    [Tooltip("The arrow prefab to be launched")]
    [SerializeField] private GameObject arrowPrefab;

    [Header("Arrow Preview System")]
    [Tooltip("Transform where the next arrow preview will be positioned")]
    [SerializeField] private Transform nextArrowPreviewPoint;
    [Tooltip("Scale of the preview arrow (0.5 = half size)")]
    public float previewArrowScale = 0.7f;
    [Tooltip("Show next arrow preview")]
    public bool showNextArrowPreview = true;
    [Tooltip("Preview arrow follows bow rotation")]
    public bool previewFollowsBow = true;

    [Header("Arrow Destruction System")]
    [Tooltip("Layer mask for collision destruction")]
    [SerializeField] public LayerMask destructionLayers = -1;
    [Tooltip("Time before arrow auto-destructs (seconds)")]
    public float arrowLifetime = 3f;
    [Tooltip("Enable collision-based destruction")]
    public bool enableCollisionDestruction = true;
    [Tooltip("Enable time-based destruction")]
    public bool enableTimeDestruction = true;
    [Tooltip("Instant destruction (no fade-out)")]
    public bool instantDestruction = true;
    [Tooltip("Show destruction debug info")]
    public bool showDestructionDebug = false;
    [Tooltip("Particle system to play when arrow is destroyed")]
    [SerializeField] private ParticleSystem arrowDestroyParticleSystem;

    [Header("Arrow Damage System")]
    [Tooltip("Damage dealt by arrows to enemies (min value)")]
    public float minArrowDamage = 10f;
    [Tooltip("Damage dealt by arrows to enemies (max value)")]
    public float maxArrowDamage = 100f;
    [Tooltip("Layer mask for enemies that can take damage")]
    [SerializeField] public LayerMask enemyLayers = -1;
    [Tooltip("Enable damage system")]
    public bool enableDamageSystem = true;
    [Tooltip("Damage enemies on collision")]
    public bool damageOnCollision = true;
    public bool showDamageDebug = false;

    [Header("Charge Settings")]
    [Tooltip("Maximum time to hold for full charge")]
    public float maxChargeTime = 3.0f;
    [Tooltip("Minimum arrow speed when barely charged")]
    public float minArrowSpeed = 10f;
    [Tooltip("Maximum arrow speed when fully charged")]
    public float maxArrowSpeed = 50f;
    [Tooltip("Gravity scale when barely charged (more fall-off)")]
    public float minGravityScale = 1.0f;
    [Tooltip("Gravity scale when fully charged (less fall-off)")]
    public float maxGravityScale = 0.1f;

    [Header("Sound Effects")]
    public bool enableSoundEffects = true;
    [Tooltip("Sound effect for shooting the arrow")]
    [SerializeField] private AudioClip shootSound;
    [Tooltip("Volume for shoot sound")]
    [Range(0f, 1f)]
    public float shootSoundVolume = 1f;
    [Tooltip("Sound effect for arrow impact with walls")]
    [SerializeField] public AudioClip wallImpactSound;
    [Tooltip("Volume for wall impact sound")]
    [Range(0f, 1f)]
    public float wallImpactSoundVolume = 1f;
    [Tooltip("Sound effect for arrow impact with enemies")]
    [SerializeField] private AudioClip enemyImpactSound;
    [Tooltip("Volume for enemy impact sound")]
    [Range(0f, 1f)]
    public float enemyImpactSoundVolume = 1f;

    [Header("Trajectory Settings")]
    [Tooltip("Gravity value for arrow fall-off (use Physics2D.gravity.y or custom)")]
    public float gravityForce = -9.81f;

    [Header("Arrow Launch Settings")]
    [Tooltip("Minimum time between shots (in seconds)")]
    public float shootCooldown = 0.1f;
    [Tooltip("Add random spread to arrow direction")]
    public float randomSpread = 0f;
    [Tooltip("Show arrow spawn debug info")]
    public bool showArrowDebug = false;

    [Header("Aiming Settings")]
    [Tooltip("Maximum angle (in degrees) the bow/arm can rotate upward")]
    public float maxUpwardAngle = 80f;
    [Tooltip("Maximum angle (in degrees) the bow/arm can rotate downward")]
    public float maxDownwardAngle = 20f;
    [Tooltip("Reference to the player\"s transform for flip detection")]
    public Transform playerTransform;
    [Tooltip("Minimum distance required to rotate bow/arm")]
    public float minDistanceToAim = 0.5f;
    [Tooltip("Enable aiming stabilization during player movement")]
    public bool enableAimStabilization = true;

    [Header("Bow Calibration")]
    [Tooltip("Manual rotation offset for the bow when player faces RIGHT (in degrees)")]
    public float bowRotationOffsetRight = 0f;
    [Tooltip("Manual rotation offset for the bow when player faces LEFT (in degrees)")]
    public float bowRotationOffsetLeft = 0f;
    [Tooltip("Should the bow rotate independently from the arm?")]
    public bool independentBowRotation = true;
    [Tooltip("Rotation speed for smooth movement")]
    public float rotationSpeed = 15f;
    [Tooltip("Use instant rotation for immediate response")]
    public bool useInstantRotation = true;

    [Header("Trajectory Visual Calibration")]
    [Tooltip("Trajectory rotation offset when player faces RIGHT (in degrees) - Controls green line direction")]
    public float trajectoryRotationOffsetRight = 0f;
    [Tooltip("Trajectory rotation offset when player faces LEFT (in degrees) - Controls green line direction")]
    public float trajectoryRotationOffsetLeft = 0f;
    [Tooltip("Update trajectory visual point automatically")]
    public bool autoUpdateTrajectoryVisual = true;

    [Header("Auto-Calibration")]
    [Tooltip("Automatically calibrate bow alignment on start")]
    public bool autoCalibrate = true;
    [Tooltip("Show calibration info in console")]
    public bool showCalibrationDebug = true;

    // Core aiming variables
    private Vector2 aimDirection;
    private Vector2 mouseScreenPosition;
    private Vector2 mouseWorldPosition;
    private Vector2 stabilizedMouseWorldPosition;
    private bool isPlayerFacingRight = true;
    private Vector2 aimFromPosition;

    // World space rotation tracking (independent of player flip)
    private float worldArmRotation = 0f;
    private float worldBowRotation = 0f;
    private float worldTrajectoryRotation = 0f; // Controls the green line (actual projectile direction)

    // Arrow spawning variables
    private float lastShootTime = 0f;
    private float currentChargeTime = 0f;
    private bool isCharging = false;

    // Arrow preview system
    private GameObject currentPreviewArrow;

    // Dynamic force system (adapted for charge)
    private float currentCalculatedSpeed = 10f;

    // Arrow destruction and damage variables
    private List<GameObject> activeArrows = new List<GameObject>(); // Changed to a list to handle multiple arrows in flight
    private Dictionary<GameObject, List<GameObject>> arrowHitEnemies = new Dictionary<GameObject, List<GameObject>>(); // Track enemies hit by each arrow

    // Performance optimization variables
    private float lastAimUpdate = 0f;
    private float aimUpdateInterval = 0.02f;

    public ShakeData CameraShakeImpact;

    void Start()
    {
        InitializeArrowPreview();

        if (autoCalibrate)
        {
            CalibrateAiming();
        }

        UpdateMinDistancePointPosition();
        UpdateTrajectoryVisualPoint();
    }

    void Update()
    {
        HandleAiming();
        HandleShootingInput();
        ApplyWorldSpaceRotations();

        if (Time.time - lastAimUpdate >= aimUpdateInterval)
        {
            UpdateMinDistancePointPosition();
            CalculateDynamicSpeed();
            UpdateArrowPreview();
            if (autoUpdateTrajectoryVisual)
            {
                UpdateTrajectoryVisualPoint();
            }
            lastAimUpdate = Time.time;
        }

        // Handle destruction for all active arrows based on lifetime
        for (int i = activeArrows.Count - 1; i >= 0; i--)
        {
            GameObject arrow = activeArrows[i];
            ArrowLifecycleController lifecycleController = arrow.GetComponent<ArrowLifecycleController>();
            if (lifecycleController != null && enableTimeDestruction && !lifecycleController.hasBeenDestroyed && Time.time - lifecycleController.spawnTime >= arrowLifetime)
            {
                if (showDestructionDebug)
                {
                    Debug.Log($"Arrow auto-destructed after {arrowLifetime}s");
                }
                DestroyArrow(arrow, arrow.transform.position); // Pass the specific arrow to destroy
            }
        }
    }

    // New methods for handling arrow collisions and triggers directly within BowSystems
    void OnCollisionEnter2D(Collision2D collision)
    {
        // This method will be called for the BowSystems script itself, not the arrow.
        // We need to handle collisions on the arrow's own GameObject.
        // The arrow will have its own script (or this script will be on the arrow prefab).
        // Since the user wants everything in BowSystems, we will manage the collision logic
        // by checking the collided object's properties and if it's an arrow spawned by this system.
        // This approach is generally not recommended for performance and clean architecture,
        // but adheres to the user's strict requirement of a single script.

        // This part is tricky because BowSystems is on the player, not the arrow.
        // We need to ensure the arrow's Rigidbody2D has its 'isKinematic' set to false
        // and its Collider2D has 'isTrigger' set to true for piercing behavior.
        // The actual collision handling will be done by the arrow itself, which will call back to BowSystems.
        // For now, this method will remain empty as the logic will be in the ShootArrow and HandleDamage methods.
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Similar to OnCollisionEnter2D, this method is for the BowSystems GameObject.
        // The actual piercing logic will be handled by the arrow's own components/logic.
    }

    private void CalculateDynamicSpeed()
    {
        float chargePercentage = currentChargeTime / maxChargeTime;
        currentCalculatedSpeed = Mathf.Lerp(minArrowSpeed, maxArrowSpeed, chargePercentage);

        if (showArrowDebug)
        {
            Debug.Log($"Charge: {chargePercentage:F2}, Calculated Speed: {currentCalculatedSpeed:F2}");
        }
    }

    private void InitializeArrowPreview()
    {
        if (!showNextArrowPreview || arrowPrefab == null || nextArrowPreviewPoint == null)
        {
            return;
        }

        CreatePreviewArrow();
    }

    private void CreatePreviewArrow()
    {
        if (arrowPrefab == null || nextArrowPreviewPoint == null)
        {
            return;
        }

        if (currentPreviewArrow != null)
        {
            DestroyImmediate(currentPreviewArrow);
        }

        currentPreviewArrow = Instantiate(arrowPrefab, nextArrowPreviewPoint.position, nextArrowPreviewPoint.rotation);
        currentPreviewArrow.transform.localScale = Vector3.one * previewArrowScale;

        Rigidbody2D previewRb = currentPreviewArrow.GetComponent<Rigidbody2D>();
        if (previewRb != null)
        {
            DestroyImmediate(previewRb);
        }

        Collider2D[] colliders = currentPreviewArrow.GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }

        SpriteRenderer spriteRenderer = currentPreviewArrow.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 0.7f;
            spriteRenderer.color = color;
        }

        if (showArrowDebug)
        {
            Debug.Log($"Created preview arrow: {arrowPrefab.name}");
        }
    }

    private void UpdateArrowPreview()
    {
        if (!showNextArrowPreview || currentPreviewArrow == null || nextArrowPreviewPoint == null)
        {
            return;
        }

        currentPreviewArrow.transform.position = nextArrowPreviewPoint.position;

        if (previewFollowsBow)
        {
            currentPreviewArrow.transform.rotation = nextArrowPreviewPoint.rotation;
        }
    }

    private void HandleShootingInput()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame && Time.time >= lastShootTime + shootCooldown)
        {
            isCharging = true;
            currentChargeTime = 0f;
            if (currentPreviewArrow != null) currentPreviewArrow.SetActive(false);
        }

        if (Mouse.current.leftButton.isPressed && isCharging)
        {
            currentChargeTime += Time.deltaTime;
            currentChargeTime = Mathf.Min(currentChargeTime, maxChargeTime);
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isCharging)
        {
            isCharging = false;
            ShootArrow();
            lastShootTime = Time.time;
            if (currentPreviewArrow != null) currentPreviewArrow.SetActive(true);
        }
    }

    private void ShootArrow()
    {
        if (arrowPrefab == null || arrowSpawnPoint == null)
        {
            if (showArrowDebug)
            {
                Debug.LogWarning("Arrow Prefab or Arrow Spawn Point not assigned! Cannot spawn arrow.");
            }
            return;
        }

        float chargePercentage = currentChargeTime / maxChargeTime;
        float arrowSpeed = Mathf.Lerp(minArrowSpeed, maxArrowSpeed, chargePercentage);
        float calculatedDamage = Mathf.Lerp(minArrowDamage, maxArrowDamage, chargePercentage);
        float calculatedGravityScale = Mathf.Lerp(minGravityScale, maxGravityScale, chargePercentage);

        GameObject newArrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, BowGameObject.transform.rotation);
        activeArrows.Add(newArrow); // Add to the list of active arrows

        // Add ArrowLifecycleController to manage its state
        ArrowLifecycleController lifecycleController = newArrow.AddComponent<ArrowLifecycleController>();
        lifecycleController.bowSystem = this;
        lifecycleController.spawnTime = Time.time;
        lifecycleController.hasBeenDestroyed = false; // Reset for new arrow
        lifecycleController.chargePercentage = chargePercentage; // Pass charge percentage to the lifecycle controller

        Rigidbody2D arrowRb = newArrow.GetComponent<Rigidbody2D>();

        Vector2 launchDirection = GetBowDirection(); // Use GetBowDirection which is now based on worldTrajectoryRotation

        if (randomSpread > 0f)
        {
            float spreadAngle = Random.Range(-randomSpread, randomSpread);
            float currentAngle = Mathf.Atan2(launchDirection.y, launchDirection.x) * Mathf.Rad2Deg;
            float newAngle = (currentAngle + spreadAngle) * Mathf.Rad2Deg;
            launchDirection = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle));
        }

        if (arrowRb != null)
        {
            arrowRb.velocity = launchDirection * arrowSpeed;
            arrowRb.gravityScale = calculatedGravityScale;
        }
        else if (showArrowDebug)
        {
            Debug.LogWarning($"Arrow prefab \"{arrowPrefab.name}\" doesn\"t have Rigidbody2D component!");
        }

        // Set the arrow's collider to be a trigger to allow piercing
        Collider2D arrowCollider = newArrow.GetComponent<Collider2D>();
        if (arrowCollider != null)
        {
            arrowCollider.isTrigger = true; // Make it a trigger for piercing
        }
        else if (showArrowDebug)
        {
            Debug.LogWarning($"Arrow prefab \"{arrowPrefab.name}\" doesn\"t have a Collider2D component!");
        }

        if (showArrowDebug)
        {
            Debug.Log($"Spawned {arrowPrefab.name} with speed {arrowSpeed:F2}, damage {calculatedDamage:F2}, gravityScale {calculatedGravityScale:F2} in direction {launchDirection}");
        }

        if (enableSoundEffects && shootSound != null)
        {
            PlaySoundAtPosition(shootSound, arrowSpawnPoint.position, shootSoundVolume);
        }

        // Add ArrowRotationController to the spawned arrow
        ArrowRotationController arrowRotController = newArrow.AddComponent<ArrowRotationController>();
        if (arrowRotController != null)
        {
            arrowRotController.rb = arrowRb;
        }

       
    }

    private void HandleAiming()
    {
        mouseScreenPosition = Mouse.current.position.ReadValue();
        mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);

        if (enableAimStabilization)
        {
            stabilizedMouseWorldPosition = mouseWorldPosition;
        }
        else
        {
            stabilizedMouseWorldPosition = mouseWorldPosition;
        }

        UpdatePlayerFacingDirection();
        CalculateAimDirection();
    }

    private void UpdatePlayerFacingDirection()
    {
        if (playerTransform != null)
        {
            // Assuming KritinaMovement is on the playerTransform or a parent
            KritinaMovement playerMovement = playerTransform.GetComponentInParent<KritinaMovement>();
            if (playerMovement != null)
            {
                isPlayerFacingRight = playerMovement.isFacingRight;
            }
            else
            {
                // Fallback if KritinaMovement is not found
                isPlayerFacingRight = playerTransform.localScale.x > 0;
            }
        }
    }

    private void CalculateAimDirection()
    {
        aimFromPosition = bowAimPoint != null ? bowAimPoint.position : BowGameObject.transform.position;
        Vector2 directionToMouse = (stabilizedMouseWorldPosition - aimFromPosition);
        float distanceToMouse = directionToMouse.magnitude;

        if (distanceToMouse < minDistanceToAim)
        {
            return;
        }

        float worldAngleToMouse = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
        float clampedWorldAngle = ClampWorldAngle(worldAngleToMouse);

        // Set world space rotations
        worldArmRotation = clampedWorldAngle;

        // Apply bow offsets
        float currentBowOffset = isPlayerFacingRight ? bowRotationOffsetRight : bowRotationOffsetLeft;
        worldBowRotation = clampedWorldAngle + currentBowOffset;

        // Apply trajectory offsets (this controls the green line - where projectiles actually go)
        float currentTrajectoryOffset = isPlayerFacingRight ? trajectoryRotationOffsetRight : trajectoryRotationOffsetLeft;
        worldTrajectoryRotation = clampedWorldAngle + currentTrajectoryOffset;

        aimDirection = directionToMouse.normalized;
    }

    private float ClampWorldAngle(float worldAngle)
    {
        while (worldAngle > 180f) worldAngle -= 360f;
        while (worldAngle < -180f) worldAngle += 360f;

        // This clamping logic is directly from your provided RobustLauncherSystem
        if (worldAngle >= -maxDownwardAngle && worldAngle <= maxUpwardAngle)
        {
            return Mathf.Clamp(worldAngle, -maxDownwardAngle, maxUpwardAngle);
        }
        else if (worldAngle > 90f && worldAngle < 270f)
        {
            float leftUpLimit = 180f - maxDownwardAngle;
            float leftDownLimit = 180f + maxDownwardAngle;
            return Mathf.Clamp(worldAngle, leftUpLimit, leftDownLimit);
        }

        return worldAngle;
    }

    private void ApplyWorldSpaceRotations()
    {
        Quaternion armWorldRotation = Quaternion.Euler(0, 0, worldArmRotation);
        Quaternion bowWorldRotation = Quaternion.Euler(0, 0, worldBowRotation);

        if (useInstantRotation)
        {
            if (Arm != null) Arm.transform.rotation = armWorldRotation;
            if (BowGameObject != null)
            {
                if (independentBowRotation)
                {
                    BowGameObject.transform.rotation = bowWorldRotation;
                }
                else
                {
                    BowGameObject.transform.rotation = armWorldRotation;
                }
            }

            // Apply trajectory visual rotation (controls green line direction)
            if (trajectoryVisualPoint != null)
            {
                trajectoryVisualPoint.rotation = Quaternion.Euler(0, 0, worldTrajectoryRotation);
            }
        }
        else
        {
            if (Arm != null) Arm.transform.rotation = Quaternion.Lerp(Arm.transform.rotation, armWorldRotation, rotationSpeed * Time.deltaTime);
            if (BowGameObject != null)
            {
                if (independentBowRotation)
                {
                    BowGameObject.transform.rotation = Quaternion.Lerp(BowGameObject.transform.rotation, bowWorldRotation, rotationSpeed * Time.deltaTime);
                }
                else
                {
                    BowGameObject.transform.rotation = Quaternion.Lerp(BowGameObject.transform.rotation, armWorldRotation, rotationSpeed * Time.deltaTime);
                }
            }

            if (trajectoryVisualPoint != null)
            {
                trajectoryVisualPoint.rotation = Quaternion.Lerp(trajectoryVisualPoint.rotation, Quaternion.Euler(0, 0, worldTrajectoryRotation), rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void UpdateMinDistancePointPosition()
    {
        if (minDistancePoint == null) return;

        Vector2 minDistancePosition = aimFromPosition + (aimDirection.normalized * minDistanceToAim);
        minDistancePoint.position = minDistancePosition;
    }

    private void UpdateTrajectoryVisualPoint()
    {
        if (trajectoryVisualPoint == null) return;

        // Position at arrow spawn point for accurate trajectory visualization
        if (arrowSpawnPoint != null)
        {
            trajectoryVisualPoint.position = arrowSpawnPoint.position;
        }
        else if (bowAimPoint != null)
        {
            trajectoryVisualPoint.position = bowAimPoint.position;
        }
        else
        {
            trajectoryVisualPoint.position = BowGameObject.transform.position;
        }
    }

    private void CalibrateAiming()
    {
        if (showCalibrationDebug)
        {
            Debug.Log("=== Bow System Calibration ===");
            Debug.Log($"Player facing right: {isPlayerFacingRight}");
            Debug.Log($"Arrow prefab assigned: {(arrowPrefab != null ? arrowPrefab.name : "None")}");
            Debug.Log($"Show arrow preview: {showNextArrowPreview}");
            Debug.Log($"Arrow destruction enabled: Collision={enableCollisionDestruction}, Time={enableTimeDestruction}");
            Debug.Log($"Charge time: {maxChargeTime}s, Min/Max Speed: {minArrowSpeed}-{maxArrowSpeed}, Min/Max Damage: {minArrowDamage}-{maxArrowDamage}");
            Debug.Log($"Arrow lifetime: {arrowLifetime}s");
        }

        worldArmRotation = 0f;
        float currentBowOffset = isPlayerFacingRight ? bowRotationOffsetRight : bowRotationOffsetLeft;
        float currentTrajectoryOffset = isPlayerFacingRight ? trajectoryRotationOffsetRight : trajectoryRotationOffsetLeft;
        worldBowRotation = currentBowOffset;
        worldTrajectoryRotation = currentTrajectoryOffset;
        aimDirection = Vector2.right;

        ApplyWorldSpaceRotations();
        UpdateMinDistancePointPosition();
        UpdateTrajectoryVisualPoint();

        if (showCalibrationDebug)
        {
            Debug.Log($"Calibration complete - Ready for bow system!");
        }
    }

    [ContextMenu("Calibrate Aiming")]
    public void ManualCalibrate()
    {
        CalibrateAiming();
    }

    [ContextMenu("Test Arrow Spawn")]
    public void TestArrowSpawn()
    {
        ShootArrow();
    }

    [ContextMenu("Refresh Arrow Preview")]
    public void RefreshArrowPreview()
    {
        if (showNextArrowPreview)
        {
            CreatePreviewArrow();
        }
    }

    public void SetShowArrowPreview(bool enabled)
    {
        showNextArrowPreview = enabled;
        if (enabled)
        {
            CreatePreviewArrow();
        }
        else if (currentPreviewArrow != null)
        {
            DestroyImmediate(currentPreviewArrow);
        }
    }

    public void SetArrowDamage(float damage)
    {
        minArrowDamage = damage;
        maxArrowDamage = damage;
        if (showDamageDebug)
        {
            Debug.Log($"Arrow damage set to: {damage}");
        }
    }

    public void SetEnableDamageSystem(bool enabled)
    {
        enableDamageSystem = enabled;
        if (showDamageDebug)
        {
            Debug.Log($"Damage system set to: {enabled}");
        }
    }

    public void SetGravityForce(float gravity)
    {
        gravityForce = gravity;
    }

    public void SetMinArrowSpeed(float speed)
    {
        minArrowSpeed = speed;
        if (showArrowDebug)
        {
            Debug.Log($"Min arrow speed set to: {speed}");
        }
    }

    public void SetMaxArrowSpeed(float speed)
    {
        maxArrowSpeed = speed;
        if (showArrowDebug)
        {
            Debug.Log($"Max arrow speed set to: {speed}");
        }
    }

    public void SetArrowLifetime(float lifetime)
    {
        arrowLifetime = lifetime;
        if (showDestructionDebug)
        {
            Debug.Log($"Arrow lifetime set to: {lifetime}s");
        }
    }

    public void SetShootCooldown(float cooldown)
    {
        shootCooldown = cooldown;
        if (showArrowDebug)
        {
            Debug.Log($"Shoot cooldown set to: {cooldown}s");
        }
    }

    public Vector2 GetBowDirection()
    {
        // This now returns the trajectory direction (green line) - where projectiles actually go
        float trajectoryAngleRad = worldTrajectoryRotation * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(trajectoryAngleRad), Mathf.Sin(trajectoryAngleRad));
    }

    public Vector2 GetAimDirection()
    {
        return aimDirection;
    }

    public bool IsAimingValid()
    {
        float distanceToMouse = (stabilizedMouseWorldPosition - aimFromPosition).magnitude;
        return distanceToMouse >= minDistanceToAim;
    }

    public bool IsMouseInDeadZone()
    {
        return GetDistanceToMouse() < minDistanceToAim;
    }

    private float GetDistanceToMouse()
    {
        return (stabilizedMouseWorldPosition - aimFromPosition).magnitude;
    }

    public bool CanShoot()
    {
        return Time.time >= lastShootTime + shootCooldown && IsAimingValid();
    }

    public void DestroyArrow(GameObject arrowToDestroy, Vector2 impactPosition)
    {
        if (arrowToDestroy == null) return; // Ensure the arrow object exists

        ArrowLifecycleController lifecycleController = arrowToDestroy.GetComponent<ArrowLifecycleController>();
        if (lifecycleController != null && lifecycleController.hasBeenDestroyed) return; // Already marked for destruction

        if (lifecycleController != null)
        {
            lifecycleController.hasBeenDestroyed = true; // Mark as destroyed
        }

        if (showDestructionDebug)
        {
            Debug.Log($"Destroying arrow at {impactPosition}");
        }

        HandleImpactDamage(impactPosition);

        // Play particle system at impact position
        if (arrowDestroyParticleSystem != null)
        {
            // Instantiate the particle system and immediately play it
            ParticleSystem newParticleSystem = Instantiate(arrowDestroyParticleSystem, impactPosition, Quaternion.identity);
            newParticleSystem.Play();

            // Destroy the particle system GameObject after its duration
            Destroy(newParticleSystem.gameObject, newParticleSystem.main.duration);
        }


        // Remove from active arrows list
        activeArrows.Remove(arrowToDestroy);

        // Use object pooling or simply disable/destroy based on settings
        if (instantDestruction)
        {
            Destroy(arrowToDestroy);
        }
        else
        {
            // For fade-out or other effects, you would trigger them here
            Destroy(arrowToDestroy, 0.1f); // Small delay for potential effects
        }
    }





    public void HandleDamage(GameObject target, Vector2 impactPoint, float chargePercentage, GameObject arrowGameObject)
    {
        if (enableDamageSystem)
        {
            if (((1 << target.layer) & enemyLayers) != 0)
            {
                float damageToDeal = Mathf.Lerp(minArrowDamage, maxArrowDamage, chargePercentage);
                Vector2 attackDirection = (target.transform.position - arrowGameObject.transform.position).normalized;

                FleaHealth fleaHealth = target.GetComponent<FleaHealth>();
                if (fleaHealth != null)
                {
                    fleaHealth.TakeDamage((int)damageToDeal, attackDirection);
                    if (showDamageDebug)
                    {
                        Debug.Log($"Arrow dealt {damageToDeal} damage to Flea {target.name} at {impactPoint}");
                    }
                    if (enableSoundEffects && enemyImpactSound != null)
                    {
                        PlaySoundAtPosition(enemyImpactSound, impactPoint, enemyImpactSoundVolume);
                    }
                    return;
                }

                SprayerHealth sprayerHealth = target.GetComponent<SprayerHealth>();
                if (sprayerHealth != null)
                {
                    sprayerHealth.TakeDamage((int)damageToDeal, attackDirection);
                    if (showDamageDebug)
                    {
                        Debug.Log($"Arrow dealt {damageToDeal} damage to Sprayer {target.name} at {impactPoint}");
                    }
                    if (enableSoundEffects && enemyImpactSound != null)
                    {
                        PlaySoundAtPosition(enemyImpactSound, impactPoint, enemyImpactSoundVolume);
                    }
                    return;
                }

                FlyHealth flyHealth = target.GetComponent<FlyHealth>();
                if (flyHealth != null)
                {
                    flyHealth.TakeDamage((int)damageToDeal, attackDirection);
                    if (showDamageDebug)
                    {
                        Debug.Log($"Arrow dealt {damageToDeal} damage to Fly {target.name} at {impactPoint}");
                    }
                    if (enableSoundEffects && enemyImpactSound != null)
                    {
                        PlaySoundAtPosition(enemyImpactSound, impactPoint, enemyImpactSoundVolume);
                    }
                    return;
                }

                InkHealth inkHealth = target.GetComponent<InkHealth>();
                if (inkHealth != null)
                {
                    inkHealth.TakeDamage(Mathf.RoundToInt(damageToDeal), attackDirection, 1f);
                    if (showDamageDebug)
                    {
                        Debug.Log($"Arrow dealt {damageToDeal} damage to Ink {target.name} at {impactPoint}");
                    }
                    if (enableSoundEffects && enemyImpactSound != null)
                    {
                        PlaySoundAtPosition(enemyImpactSound, impactPoint, enemyImpactSoundVolume);
                    }
                    return;
                }

                RatKingHealth RatKingHealth = target.GetComponent<RatKingHealth>();
                if (RatKingHealth != null)
                {
                    RatKingHealth.TakeDamage(Mathf.RoundToInt(damageToDeal));
                    if (showDamageDebug)
                    {
                        Debug.Log($"Arrow dealt {damageToDeal} damage to RatKing {target.name} at {impactPoint}");
                    }
                    if (enableSoundEffects && enemyImpactSound != null)
                    {
                        PlaySoundAtPosition(enemyImpactSound, impactPoint, enemyImpactSoundVolume);
                    }
                    return;
                }

                if (showDamageDebug)
                {
                    Debug.LogWarning($"Enemy {target.name} doesn\"t have a recognized health component (FleaHealth, SprayerHealth, FlyHealth, InkHealth, or RatKingHealth)!");
                }
            }
        }
    }

    private void HandleImpactDamage(Vector2 impactCenter)
    {
        if (showDamageDebug)
        {
            Debug.Log($"Arrow impacted at {impactCenter}");
        }
    }

    private void OnDrawGizmos()
    {
        if (BowGameObject != null)
        {
            Vector2 bowPos = BowGameObject.transform.position;
            Vector2 aimFromPos = bowAimPoint != null ? bowAimPoint.position : bowPos;

            if (bowAimPoint != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(bowAimPoint.position, 0.15f);
                Gizmos.DrawLine(bowPos, bowAimPoint.position);
            }

            // Draw trajectory visual point if assigned
            if (trajectoryVisualPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(trajectoryVisualPoint.position, 0.12f);
            }

            if (arrowSpawnPoint != null)
            {
                Gizmos.color = CanShoot() ? Color.green : Color.red;
                Gizmos.DrawWireSphere(arrowSpawnPoint.position, 0.1f);
            }

            if (nextArrowPreviewPoint != null)
            {
                Gizmos.color = showNextArrowPreview ? Color.yellow : Color.gray;
                Gizmos.DrawWireSphere(nextArrowPreviewPoint.position, 0.08f);
            }

            // Draw aim direction (yellow)
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(aimFromPos, aimFromPos + aimDirection * 2f);

            // Draw min distance point (red)
            if (minDistancePoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(minDistancePoint.position, 0.05f);
            }
        }
    }

    // Helper to play sound at a given position
    public void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }
    }
}

// ArrowLifecycleController and ArrowRotationController (if they are separate files, they should remain separate)
// If they were nested, they should be extracted to their own files.

// Example of ArrowLifecycleController (if it was nested, extract it to a new file named ArrowLifecycleController.cs)
public class ArrowLifecycleController : MonoBehaviour
{
    public BowSystems bowSystem;
    public float spawnTime;
    public bool hasBeenDestroyed = false;
    public float chargePercentage; // To store the charge percentage for damage calculation
    private List<GameObject> hitEnemies = new List<GameObject>(); // Track enemies hit by this specific arrow

    void OnTriggerEnter2D(Collider2D other)
    {
        if (bowSystem == null || hasBeenDestroyed) return;

        // Check if the triggered object is an enemy
        bool isEnemy = ((1 << other.gameObject.layer) & bowSystem.enemyLayers) != 0;

        if (isEnemy)
        {
            // If it\'s an enemy and this specific arrow hasn\'t hit it before
            if (!hitEnemies.Contains(other.gameObject))
            {
                bowSystem.HandleDamage(other.gameObject, other.transform.position, chargePercentage, gameObject);
                hitEnemies.Add(other.gameObject);

                if (bowSystem.showDamageDebug)
                {
                    Debug.Log($"Arrow pierced and damaged enemy (trigger): {other.gameObject.name}");
                }
            }
        }
        else // If it\'s not an enemy, or if it\'s an object on a destruction layer
        {
            // Check if the triggered object is on a destruction layer
            if (((1 << other.gameObject.layer) & bowSystem.destructionLayers) != 0)
            {
                if (bowSystem.enableCollisionDestruction)
                {
                    bowSystem.DestroyArrow(gameObject, gameObject.transform.position);
                }
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (bowSystem == null || hasBeenDestroyed) return;

        // Check if the collided object is an enemy
        bool isEnemy = ((1 << collision.gameObject.layer) & bowSystem.enemyLayers) != 0;

        if (isEnemy)
        {
            // If it\'s an enemy and this specific arrow hasn\'t hit it before
            if (!hitEnemies.Contains(collision.gameObject))
            {
                bowSystem.HandleDamage(collision.gameObject, collision.contacts[0].point, chargePercentage, gameObject);
                hitEnemies.Add(collision.gameObject);

                if (bowSystem.showDamageDebug)
                {
                    Debug.Log($"Arrow pierced and damaged enemy: {collision.gameObject.name}");
                }
            }
        }
        else // If it\'s not an enemy, or if it\'s an object on a destruction layer
        {
            // Check if the collided object is on a destruction layer
            if (((1 << collision.gameObject.layer) & bowSystem.destructionLayers) != 0)
            {
                if (bowSystem.enableCollisionDestruction)
                {
                    if (bowSystem.enableSoundEffects && bowSystem.wallImpactSound != null)
                    {
                        bowSystem.PlaySoundAtPosition(bowSystem.wallImpactSound, collision.contacts[0].point, bowSystem.wallImpactSoundVolume);
                    }
                    bowSystem.DestroyArrow(gameObject, collision.contacts[0].point);
                }
            }
        }
    }
}

// Example of ArrowRotationController (if it was nested, extract it to a new file named ArrowRotationController.cs)
public class ArrowRotationController : MonoBehaviour
{
    public Rigidbody2D rb;

    void Update()
    {
        if (rb != null && rb.velocity.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}

