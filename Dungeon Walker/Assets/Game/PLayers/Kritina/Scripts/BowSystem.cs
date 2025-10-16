using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using FirstGearGames.SmoothCameraShaker;
using UnityEngine.UI;
using Photon.Pun;
public class BowSystems : MonoBehaviour, IPunObservable, IPoolable
{
    [Header("Component References")]
    [SerializeField] private GameObject BowGameObject;
    [SerializeField] private GameObject Arm;
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private Transform bowAimPoint; // Specific point on the bow for aiming
    [SerializeField] private Transform minDistancePoint; // Transform point for minimum distance visualization
    [SerializeField] private Transform trajectoryVisualPoint; // Transform point for trajectory visualization (controls green line)
    public Joystick aimJoystick;

    public bool isJoystickHeldDown = false;
    private bool isJoystickAiming = false;
    private PhotonView view;
    private bool isOnlineMode = false;
    private PlayerSyncManager syncManager;

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

    [Header("Arrow Launch Settings")]
    [Tooltip("The number of arrows to fire with each shot.")]
    [SerializeField] private int arrowsPerShot = 1; // **NEW**
    [Tooltip("The maximum radius of the random spread at the spawn point.")]
    [SerializeField] private float spawnRadius = 0f; // **NEW**
    [Tooltip("The maximum angle of directional spread for each arrow (in degrees).")]
    [SerializeField] private float spreadAngle = 5f; // **NEW** (replaces your old 'randomSpread')


    [Header("UI and Aiming Visuals")]
    [Tooltip("The UI Slider for the charge circle, set to Radial 360 fill.")]
    [SerializeField] private Slider chargeSlider;
    [Tooltip("The world-space transform the charge slider should follow (e.g., the player's head or torso).")]
    [SerializeField] private Transform chargeSliderFollowTarget; // **NEW VARIABLE**
    [SerializeField] private Image fullyChargedIndicator;
    [Tooltip("The Line Renderer component for the aim line.")]
    [SerializeField] private LineRenderer aimLineRenderer;
    [Tooltip("The length of the aim line.")]
    [SerializeField] private float aimLineLength = 15f;

    // Core aiming variables
    private Vector2 aimDirection;
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
    private List<GameObject> activeArrows = new List<GameObject>();

    // Performance optimization variables
    private float lastAimUpdate = 0f;
    private float aimUpdateInterval = 0.02f;

    // --- MODIFICATION: This new variable tracks if we are currently using the joystick --- 
    private bool isAimingWithJoystick = false;
    public ShakeData CameraShakeImpact;

    [Header("Input Settings")]
    [Tooltip("Activer les contrôles pour PC (souris).")]
    public bool enablePcInput = true;
    [Tooltip("Activer les contrôles pour Mobile (joystick).")]
    public bool enableMobileInput = true;
    [SerializeField] private int arrowPoolSize = 20;
    void OnEnable()
    {
        // Start listening for the broadcast from the joystick
        JoystickBroadcaster.OnJoystickTouchStateChanged += HandleJoystickTouchState;

    }
    public void CreatePools()
    {
        if (ObjectPoolManager.Instance != null && arrowPrefab != null)
        {
            ObjectPoolManager.Instance.CreatePool(arrowPrefab, arrowPoolSize);
        }
    }
    void OnDisable()
    {
        Debug.Log("BowSystems OnDisable called - cleaning up UI.");

        // --- THE FIX: Hide all bow-specific UI elements ---

        // Hide the charge slider
        if (chargeSlider != null)
        {
            chargeSlider.gameObject.SetActive(false);
        }

        // Hide the "fully charged" indicator
        if (fullyChargedIndicator != null)
        {
            fullyChargedIndicator.gameObject.SetActive(false);
        }

        // Hide the aim line
        if (aimLineRenderer != null)
        {
            aimLineRenderer.enabled = false;
        }
        JoystickBroadcaster.OnJoystickTouchStateChanged -= HandleJoystickTouchState;
        // Also, reset the charging state just in case
        isCharging = false;
        currentChargeTime = 0f;
    }
    private void HandleJoystickTouchState(bool isDown)
    {
        isJoystickHeldDown = isDown;
    }
    void Start()
    {
        view = GetComponentInParent<PhotonView>();
        syncManager = GetComponentInParent<PlayerSyncManager>();

        if (view != null && transform.root.CompareTag("OnlinePlayer"))
        {
            isOnlineMode = true;
            Debug.Log("BowSystems: Online Mode Detected.");
        }
        else
        {
            isOnlineMode = false;
            Debug.Log("BowSystems: Offline Mode Detected.");
        }

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
        if (isOnlineMode && !view.IsMine)
        {
            return;
        }

        // --- MODIFICATION: The entire Update loop is now simplified to call one master function --- 
        HandleInputAndShooting();

        // This part remains for optimization
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
        UpdateChargeSliderPosition();
    }

    public void OnJoystickPointerDown()
    {
        isJoystickHeldDown = true;
    }

    public void OnJoystickPointerUp()
    {
        isJoystickHeldDown = false;
    }
    // --- MODIFICATION: This is the new, unified input handling method --- 
    private void HandleInputAndShooting()
    {
        bool shootPressedThisFrame = false;
        bool shootHeldThisFrame = false;
        bool shootReleasedThisFrame = false;

        // --- START OF THE FINAL, CORRECT FIX ---

        // --- MOBILE JOYSTICK LOGIC ---
        // This logic is now driven by our 100% reliable isJoystickHeldDown boolean.
        if (enableMobileInput && aimJoystick != null)
        {
            // A "press" happens on the first frame the finger is down.
            if (isJoystickHeldDown && !isJoystickAiming)
            {
                shootPressedThisFrame = true;
                isJoystickAiming = true; // Enter the aiming state.
            }

            // A "hold" is true for every frame the finger is down.
            if (isJoystickHeldDown)
            {
                shootHeldThisFrame = true;
            }

            // A "release" happens on the first frame the finger is lifted.
            if (!isJoystickHeldDown && isJoystickAiming)
            {
                shootReleasedThisFrame = true;
                isJoystickAiming = false; // Exit the aiming state.
            }

            // AIMING DIRECTION: We still get the direction from the joystick handle.
            // If the handle is in the middle, the aim just holds its last position.
            if (aimJoystick.Direction.sqrMagnitude > 0.01f)
            {
                stabilizedMouseWorldPosition = bowAimPoint.position + new Vector3(aimJoystick.Direction.x, aimJoystick.Direction.y, 0) * 10f;
            }
        }

        // --- PC MOUSE LOGIC (Fallback) ---
        if (enablePcInput && !isJoystickAiming)
        {
            stabilizedMouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            shootPressedThisFrame = shootPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame;
            shootHeldThisFrame = shootHeldThisFrame || Mouse.current.leftButton.isPressed;
            shootReleasedThisFrame = shootReleasedThisFrame || Mouse.current.leftButton.wasReleasedThisFrame;
        }

        // --- END OF THE FINAL, CORRECT FIX ---


        // --- UNIFIED AIMING AND SHOOTING LOGIC (This part remains the same) ---
        UpdatePlayerFacingDirection();
        CalculateAimDirection();
        ApplyWorldSpaceRotations();

        if (shootPressedThisFrame && Time.time >= lastShootTime + shootCooldown)
        {
            isCharging = true;
            currentChargeTime = 0f;
            if (currentPreviewArrow != null) currentPreviewArrow.SetActive(false);
            if (chargeSlider != null) chargeSlider.gameObject.SetActive(true);
        }

        if (shootHeldThisFrame && isCharging)
        {
            currentChargeTime += Time.deltaTime;
            currentChargeTime = Mathf.Min(currentChargeTime, maxChargeTime);
            if (chargeSlider != null) chargeSlider.value = currentChargeTime / maxChargeTime;
            if (fullyChargedIndicator != null) fullyChargedIndicator.gameObject.SetActive(currentChargeTime >= maxChargeTime);
            if (aimLineRenderer != null && arrowSpawnPoint != null)
            {
                aimLineRenderer.enabled = true;
                Vector3 startPoint = arrowSpawnPoint.position;
                Vector3 direction = GetBowDirection();
                aimLineRenderer.SetPosition(0, startPoint);
                aimLineRenderer.SetPosition(1, startPoint + (direction * aimLineLength));
            }
        }

        if (shootReleasedThisFrame && isCharging)
        {
            isCharging = false;
            if (IsAimingValid()) ShootArrow();
            lastShootTime = Time.time;
            if (currentPreviewArrow != null) currentPreviewArrow.SetActive(true);
            if (chargeSlider != null) { chargeSlider.value = 0; chargeSlider.gameObject.SetActive(false); }
            if (fullyChargedIndicator != null) fullyChargedIndicator.gameObject.SetActive(false);
            if (aimLineRenderer != null) aimLineRenderer.enabled = false;
        }

        if (!shootHeldThisFrame && isCharging)
        {
            isCharging = false;
            if (currentPreviewArrow != null) currentPreviewArrow.SetActive(true);
            if (chargeSlider != null) { chargeSlider.value = 0; chargeSlider.gameObject.SetActive(false); }
            if (fullyChargedIndicator != null) fullyChargedIndicator.gameObject.SetActive(false);
            if (aimLineRenderer != null) aimLineRenderer.enabled = false;
        }
    }




    private void UpdateChargeSliderPosition()
    {
        // If the slider or target is missing, or if the slider is inactive, do nothing.
        if (chargeSlider == null || chargeSliderFollowTarget == null || !chargeSlider.gameObject.activeInHierarchy)
        {
            return;
        }

        // --- THE DEFINITIVE FIX FOR A "SCREEN SPACE - CAMERA" CANVAS ---

        // Step 1: Get the world position of our follow target.
        Vector3 worldPos = chargeSliderFollowTarget.position;

        // Step 2: Convert that world position into a screen position (pixels).
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPos);

        // Step 3: This is the crucial step you taught me. We must convert the screen point
        // into a local position within the canvas's coordinate system.
        Vector2 localPointerPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)chargeSlider.transform.parent, // The parent of the slider (the Canvas)
            screenPoint,          // The pixel position we calculated
            Camera.main,          // The camera associated with the canvas
            out localPointerPosition // The resulting local position
        );

        // Step 4: Apply the correctly converted local position to the slider's anchoredPosition.
        // This will work perfectly in Screen Space - Camera.
        chargeSlider.transform.localPosition = localPointerPosition;
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
        if (!showNextArrowPreview || arrowPrefab == null || nextArrowPreviewPoint == null) return;
        CreatePreviewArrow();
    }

    private void CreatePreviewArrow()
    {
        if (arrowPrefab == null || nextArrowPreviewPoint == null) return;
        if (currentPreviewArrow != null) DestroyImmediate(currentPreviewArrow);

        currentPreviewArrow = Instantiate(arrowPrefab, nextArrowPreviewPoint.position, nextArrowPreviewPoint.rotation);
        currentPreviewArrow.transform.localScale = Vector3.one * previewArrowScale;

        Rigidbody2D previewRb = currentPreviewArrow.GetComponent<Rigidbody2D>();
        if (previewRb != null) DestroyImmediate(previewRb);

        Collider2D[] colliders = currentPreviewArrow.GetComponents<Collider2D>();
        foreach (Collider2D col in colliders) col.enabled = false;

        SpriteRenderer spriteRenderer = currentPreviewArrow.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 0.7f;
            spriteRenderer.color = color;
        }

        if (showArrowDebug) Debug.Log($"Created preview arrow: {arrowPrefab.name}");
    }

    private void UpdateArrowPreview()
    {
        if (!showNextArrowPreview || currentPreviewArrow == null || nextArrowPreviewPoint == null) return;
        currentPreviewArrow.transform.position = nextArrowPreviewPoint.position;
        if (previewFollowsBow) currentPreviewArrow.transform.rotation = nextArrowPreviewPoint.rotation;
    }

    // --- START OF CHANGE 2: The New ShootArrow() Method ---
    private void ShootArrow()
    {
        if (arrowPrefab == null || arrowSpawnPoint == null) return;

        float chargePercentage = currentChargeTime / maxChargeTime;

        // Play the shoot sound ONCE per shot, not per arrow.
        if (enableSoundEffects && shootSound != null)
        {
            PlaySoundAtPosition(shootSound, arrowSpawnPoint.position, shootSoundVolume);
        }

        // --- THE SHOTGUN LOOP ---
        // This loop will run for the number of arrows you want to fire.
        for (int i = 0; i < arrowsPerShot; i++)
        {
            // 1. Calculate Directional Spread
            // Get the main aim direction.
            Vector2 baseDirection = GetBowDirection();
            // Create a random angle for spread.
            float angleOffset = Random.Range(-spreadAngle / 2, spreadAngle / 2);
            // Apply the random angle to the base direction.
            Vector2 launchDirection = Quaternion.Euler(0, 0, angleOffset) * baseDirection;

            // 2. Calculate Positional Spread
            // Get a random point within a circle.
            Vector2 randomSpawnOffset = Random.insideUnitCircle * spawnRadius;
            // Apply the offset to the main spawn point.
            Vector3 spawnPosition = arrowSpawnPoint.position + (Vector3)randomSpawnOffset;

            // 3. Spawn the Arrow (using your existing robust logic)
            if (isOnlineMode)
            {
                // Online: The shooter creates their own "real" arrow.
                GameObject myArrow = ObjectPoolManager.Instance.SpawnFromPool(arrowPrefab, spawnPosition, Quaternion.identity);
                InitializeArrow(myArrow, launchDirection, chargePercentage, true); // true = isReal

                // Tell everyone else to create a "visual" arrow.
                view.RPC("RPC_SpawnVisualArrow", RpcTarget.Others, spawnPosition, Quaternion.identity, launchDirection, chargePercentage);
            }
            else
            {
                // Offline: Just create one "real" arrow.
                GameObject myArrow = ObjectPoolManager.Instance.SpawnFromPool(arrowPrefab, spawnPosition, Quaternion.identity);
                InitializeArrow(myArrow, launchDirection, chargePercentage, true); // true = isReal
            }
        }
    }
   
    // --- ALSO, REPLACE YOUR OFFLINE/LOCAL VERSION OF THIS LOGIC ---
    public void TriggerArrowDestruction(Vector2 impactPosition)
    {
        if (isOnlineMode)
        {
            view.RPC("RPC_PlayArrowDestructionEffect", RpcTarget.All, (Vector3)impactPosition);
        }
        else
        {
            // OFFLINE: Use the same robust logic as the RPC.
            if (arrowDestroyParticleSystem != null)
            {
                GameObject effectInstance = Instantiate(arrowDestroyParticleSystem.gameObject, impactPosition, Quaternion.identity);
                ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                    Destroy(effectInstance, ps.main.duration);
                }
                else
                {
                    Destroy(effectInstance, 2f);
                }
            }
        }
    }

    // --- 7. ADD THIS NEW HELPER FUNCTION ---
    // This function sets up a newly created arrow.
    // --- REPLACE the old InitializeArrow method with this FINAL version ---
    private void InitializeArrow(GameObject arrow, Vector2 direction, float charge, bool isReal)
    {
        // --- HYBRID "GET OR ADD" LOGIC ---
        // This ensures the components exist without creating duplicates on pooled objects.
        ArrowLifecycleController lifecycle = arrow.GetComponent<ArrowLifecycleController>();
        if (lifecycle == null)
        {
            lifecycle = arrow.AddComponent<ArrowLifecycleController>();
        }

        ArrowRotationController rotationController = arrow.GetComponent<ArrowRotationController>();
        if (rotationController == null)
        {
            rotationController = arrow.AddComponent<ArrowRotationController>();
        }
        // --- END OF HYBRID LOGIC ---

        // --- THE CRITICAL RESET LOGIC ---
        // Now that we are GUARANTEED to have the components, we can safely set their state.
        lifecycle.bowSystem = this;
        lifecycle.chargePercentage = charge;
        lifecycle.isReal = isReal;
        lifecycle.spawnTime = Time.time;
        lifecycle.hasBeenDestroyed = false; // THIS IS THE FIX FOR THE SECOND SHOT.

        Rigidbody2D arrowRb = arrow.GetComponent<Rigidbody2D>();
        rotationController.rb = arrowRb;

        // --- PHYSICS RESET LOGIC ---
        if (arrowRb != null)
        {
            // Wake up the Rigidbody and ensure physics are running.
            arrowRb.simulated = true;
            // Clear any velocity from its previous life.
            arrowRb.velocity = Vector2.zero;
            arrowRb.angularVelocity = 0f;
            // Apply the new velocity and gravity.
            float arrowSpeed = Mathf.Lerp(minArrowSpeed, maxArrowSpeed, charge);
            arrowRb.velocity = direction * arrowSpeed;
            arrowRb.gravityScale = Mathf.Lerp(minGravityScale, maxGravityScale, charge);
        }
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
            // If the aim is within the dead zone, don't update the direction
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
        // Normalize angle to be within -180 to 180 range.
        worldAngle = (worldAngle + 540) % 360 - 180; // This handles negative angles correctly

        if (isPlayerFacingRight)
        {
            return Mathf.Clamp(worldAngle, -maxDownwardAngle, maxUpwardAngle);
        }
        else // Player is facing left
        {
            // This logic handles clamping when the player is flipped.
            // It correctly clamps the world-space angle based on the player's orientation.
            if (Mathf.Abs(worldAngle) > 90) // If aiming towards the left side of the screen
            {
                if (worldAngle > 0) // Top-left quadrant (e.g., 100 degrees)
                    return Mathf.Clamp(worldAngle, 180 - maxUpwardAngle, 180); // Clamp between (180-maxUp) and 180
                else // Bottom-left quadrant (e.g., -100 degrees)
                    return Mathf.Clamp(worldAngle, -180, -180 + maxDownwardAngle); // Clamp between -180 and (-180+maxDown)
            }
            else // If aiming towards the right side of the screen (even if player is flipped)
            {
                return Mathf.Clamp(worldAngle, -maxDownwardAngle, maxUpwardAngle); // Use normal clamping
            }
        }
    }

    private void ApplyWorldSpaceRotations()
    {
        Quaternion armWorldRotation = Quaternion.Euler(0, 0, worldArmRotation);
        Quaternion bowWorldRotation = Quaternion.Euler(0, 0, worldBowRotation);
        Quaternion trajectoryRotation = Quaternion.Euler(0, 0, worldTrajectoryRotation); // For the green line

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
                trajectoryVisualPoint.rotation = trajectoryRotation;
            }
        }
        else
        {
            float step = rotationSpeed * Time.deltaTime;
            if (Arm != null) Arm.transform.rotation = Quaternion.Lerp(Arm.transform.rotation, armWorldRotation, step);
            if (BowGameObject != null)
            {
                if (independentBowRotation)
                {
                    BowGameObject.transform.rotation = Quaternion.Lerp(BowGameObject.transform.rotation, bowWorldRotation, step);
                }
                else
                {
                    BowGameObject.transform.rotation = Quaternion.Lerp(BowGameObject.transform.rotation, armWorldRotation, step);
                }
            }

            if (trajectoryVisualPoint != null)
            {
                trajectoryVisualPoint.rotation = Quaternion.Lerp(trajectoryVisualPoint.rotation, trajectoryRotation, step);
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
        aimDirection = Vector2.right; // Default aim direction

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
        if (arrowToDestroy == null) return;

        ArrowLifecycleController lifecycle = arrowToDestroy.GetComponent<ArrowLifecycleController>();
        if (lifecycle != null && lifecycle.hasBeenDestroyed) return;
        if (lifecycle != null) lifecycle.hasBeenDestroyed = true;

        // --- THIS IS THE CRITICAL FIX ---

        // 1. We always trigger the visual effect first. This is safe.
        if (isOnlineMode)
        {
            view.RPC("RPC_PlayArrowDestructionEffect", RpcTarget.All, (Vector3)impactPosition);
        }
        else
        {
            if (arrowDestroyParticleSystem != null)
            {
                Instantiate(arrowDestroyParticleSystem.gameObject, impactPosition, Quaternion.identity);
            }
        }

        // 2. Now, we handle the destruction of the arrow object itself.
        if (isOnlineMode)
        {
            // --- ONLINE MODE ---
            if (lifecycle != null && lifecycle.isReal)
            {
                // If it's a "real" arrow, we MUST use PhotonNetwork.Destroy.
                // This tells everyone to remove this object and ensures all final RPCs are sent.
                PhotonNetwork.Destroy(arrowToDestroy);
            }
            else
            {
                // If it's just a "visual" arrow on a remote client, a simple local Destroy is fine.
                arrowToDestroy.SetActive(false);
            }
        }
        else
        {
            // --- OFFLINE MODE ---
            // A simple local Destroy is all that's needed.
            arrowToDestroy.SetActive(false);
        }
    }
  
    // --- 11. ADD THE ROTATION SYNC FUNCTION ---
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We are the owner. We send our arm and bow rotation.
            stream.SendNext(Arm.transform.rotation);
            stream.SendNext(BowGameObject.transform.rotation);
        }
        else
        {
            // We are a remote client. We receive and apply the rotation.
            this.Arm.transform.rotation = (Quaternion)stream.ReceiveNext();
            this.BowGameObject.transform.rotation = (Quaternion)stream.ReceiveNext();
        }
    }

// This method is called when an arrow impacts something (wall, enemy, etc.)
public void HandleImpactDamage(Vector2 impactPosition)
    {
        // This is a placeholder. In a real game, you might have an area-of-effect damage
        // or a camera shake specific to the impact, not just enemy damage.
        // For now, it's just a hook for future expansion.
        if (CameraShakeImpact != null)
        {
            CameraShakerHandler.Shake(CameraShakeImpact);
        }
    }

    public void HandleDamage(GameObject target, Vector2 impactPoint, float chargePercentage, GameObject arrowGameObject)
    {
        if (enableDamageSystem)
        {
            // Check if the target is on an enemy layer
            if (((1 << target.layer) & enemyLayers) != 0)
            {
                float damageToDeal = Mathf.Lerp(minArrowDamage, maxArrowDamage, chargePercentage);
                Vector2 attackDirection = (target.transform.position - arrowGameObject.transform.position).normalized;

                // Attempt to get various health components and apply damage
                // Using TryGetComponent is safer and more performant than GetComponent followed by a null check
                if (target.TryGetComponent<FleaHealth>(out var fleaHealth))
                {
                    fleaHealth.TakeDamage((int)damageToDeal, attackDirection);
                    if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                        L3antixSuperMeter.Instance.AddDamage((int)damageToDeal);

                    if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                        PlayerSuperMeter.Instance.AddDamage((int)damageToDeal);
                    if (showDamageDebug) Debug.Log($"Arrow dealt {damageToDeal} damage to Flea {target.name} at {impactPoint}");
                    if (enableSoundEffects && enemyImpactSound != null) PlaySoundAtPosition(enemyImpactSound, impactPoint, enemyImpactSoundVolume);
                    return;
                }

                if (target.TryGetComponent<SprayerHealth>(out var sprayerHealth))
                {
                    sprayerHealth.TakeDamage((int)damageToDeal, attackDirection);
                    if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                        L3antixSuperMeter.Instance.AddDamage((int)damageToDeal);

                    if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                        PlayerSuperMeter.Instance.AddDamage((int)damageToDeal);
                    if (showDamageDebug) Debug.Log($"Arrow dealt {damageToDeal} damage to Sprayer {target.name} at {impactPoint}");
                    if (enableSoundEffects && enemyImpactSound != null) PlaySoundAtPosition(enemyImpactSound, impactPoint, enemyImpactSoundVolume);
                    return;
                }

                if (target.TryGetComponent<FleaHealthV2>(out var FleaHealthV2))
                {
                    FleaHealthV2.TakeDamage((int)damageToDeal, attackDirection);
                    if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                        L3antixSuperMeter.Instance.AddDamage((int)damageToDeal);

                    if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                        PlayerSuperMeter.Instance.AddDamage((int)damageToDeal);
                    if (showDamageDebug) Debug.Log($"Arrow dealt {damageToDeal} damage to Sprayer {target.name} at {impactPoint}");
                    if (enableSoundEffects && enemyImpactSound != null) PlaySoundAtPosition(enemyImpactSound, impactPoint, enemyImpactSoundVolume);
                    return;
                }

                if (target.TryGetComponent<FlyHealth>(out var flyHealth))
                {
                    flyHealth.TakeDamage((int)damageToDeal, attackDirection);
                    if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                        L3antixSuperMeter.Instance.AddDamage((int)damageToDeal);

                    if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                        PlayerSuperMeter.Instance.AddDamage((int)damageToDeal);
                    if (showDamageDebug) Debug.Log($"Arrow dealt {damageToDeal} damage to Fly {target.name} at {impactPoint}");
                    if (enableSoundEffects && enemyImpactSound != null) PlaySoundAtPosition(enemyImpactSound, impactPoint, enemyImpactSoundVolume);
                    return;
                }

                if (target.TryGetComponent<InkHealth>(out var inkHealth))
                {
                    inkHealth.TakeDamage(Mathf.RoundToInt(damageToDeal), attackDirection, 1f);
                    if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                        L3antixSuperMeter.Instance.AddDamage((int)damageToDeal);

                    if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                        PlayerSuperMeter.Instance.AddDamage((int)damageToDeal);
                    if (showDamageDebug) Debug.Log($"Arrow dealt {damageToDeal} damage to Ink {target.name} at {impactPoint}");
                    if (enableSoundEffects && enemyImpactSound != null) PlaySoundAtPosition(enemyImpactSound, impactPoint, enemyImpactSoundVolume);
                    return;
                }

                if (target.TryGetComponent<RatKingHealth>(out var ratKingHealth))
                {
                    ratKingHealth.TakeDamage(Mathf.RoundToInt(damageToDeal));
                    if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                        L3antixSuperMeter.Instance.AddDamage((int)damageToDeal);

                    if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                        PlayerSuperMeter.Instance.AddDamage((int)damageToDeal);
                    if (showDamageDebug) Debug.Log($"Arrow dealt {damageToDeal} damage to RatKing {target.name} at {impactPoint}");
                    if (enableSoundEffects && enemyImpactSound != null) PlaySoundAtPosition(enemyImpactSound, impactPoint, enemyImpactSoundVolume);
                    return;
                }

                if (target.TryGetComponent<BarrelExplosion>(out var barrelExplosion))
                {
                    barrelExplosion.TakeDamage(Mathf.RoundToInt(damageToDeal));
                    if (showDamageDebug) Debug.Log($"Arrow dealt {damageToDeal} damage to Barrel {target.name} at {impactPoint}");
                    if (enableSoundEffects && enemyImpactSound != null) PlaySoundAtPosition(enemyImpactSound, impactPoint, enemyImpactSoundVolume);
                    return;
                }

                if (showDamageDebug)
                {
                    Debug.LogWarning($"Enemy {target.name} has no recognized health component! Damage not applied.");
                }
            }
            else
            {
                if (showDamageDebug)
                {
                    Debug.Log($"Collided with non-enemy object {target.name}. No damage applied.");
                }
            }
        }
    }

    public void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    private void OnDrawGizmos()
    {
        if (BowGameObject == null) return;
        Vector2 bowPos = BowGameObject.transform.position;
        Vector2 aimFromPos = bowAimPoint != null ? bowAimPoint.position : bowPos;

        // Draw aim point and trajectory visual point
        if (bowAimPoint != null) { Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(bowAimPoint.position, 0.15f); }
        if (trajectoryVisualPoint != null) { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(trajectoryVisualPoint.position, 0.12f); }
        if (arrowSpawnPoint != null) { Gizmos.color = CanShoot() ? Color.green : Color.red; Gizmos.DrawWireSphere(arrowSpawnPoint.position, 0.1f); }
        if (nextArrowPreviewPoint != null) { Gizmos.color = showNextArrowPreview ? Color.yellow : Color.gray; Gizmos.DrawWireSphere(nextArrowPreviewPoint.position, 0.08f); }

        // Draw aim direction line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(aimFromPos, aimFromPos + aimDirection * 2f);

        // Draw min distance point
        if (minDistancePoint != null) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(minDistancePoint.position, 0.05f); }
    }
}

// --- Helper Scripts (Ideally in their own files) ---

// File: ArrowLifecycleController.cs
public class ArrowLifecycleController : MonoBehaviour
{
    public BowSystems bowSystem;
    public float spawnTime;
    public bool hasBeenDestroyed = false;
    public float chargePercentage; // To store the charge percentage for damage calculation
    private List<GameObject> hitEnemies = new List<GameObject>(); // Track enemies hit by this specific arrow
    public bool isReal;
    void Update()
    {
        // Time-based destruction
        if (bowSystem != null && bowSystem.enableTimeDestruction && !hasBeenDestroyed)
        {
            if (Time.time > spawnTime + bowSystem.arrowLifetime)
            {
                bowSystem.DestroyArrow(gameObject, transform.position);
            }
        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (bowSystem == null || hasBeenDestroyed) return;

        // Check if the object we hit is an enemy
        bool isEnemy = ((1 << other.gameObject.layer) & bowSystem.enemyLayers) != 0;

        // Check if the object we hit is a wall/destructible surface
        bool isDestructible = ((1 << other.gameObject.layer) & bowSystem.destructionLayers) != 0;

        // --- PIERCING AND DAMAGE LOGIC ---
        if (isReal && isEnemy && !hitEnemies.Contains(other.gameObject))
        {
            // If it's a new enemy, deal damage and add it to the list.
            // The arrow does NOT get destroyed. This is the piercing logic.
            bowSystem.HandleDamage(other.gameObject, other.ClosestPoint(transform.position), chargePercentage, gameObject);
            hitEnemies.Add(other.gameObject);
        }

        // --- DESTRUCTION LOGIC ---
        // If the arrow hits a destructible surface (like a wall), it gets destroyed.
        // This check is now inside the same function, which is more reliable.
        if (isDestructible)
        {
            hasBeenDestroyed = true;

            // If this is the "real" arrow, it's responsible for telling everyone to play the effect.
            if (isReal)
            {
                bowSystem.TriggerArrowDestruction(transform.position);
            }

            // The arrow destroys itself locally.
            gameObject.SetActive(false);
        }
    }
}

// File: ArrowRotationController.cs
public class ArrowRotationController : MonoBehaviour
{
    public Rigidbody2D rb;

    void Update()
    {
        if (rb != null && rb.velocity.sqrMagnitude > 0.01f) // Check for significant velocity
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
}
