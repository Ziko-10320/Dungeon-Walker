using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using FirstGearGames.SmoothCameraShaker;

public class RobustLauncherSystem : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private GameObject Gun;
    [SerializeField] private GameObject Arm;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private Transform launcherAimPoint; // Specific point on the launcher for aiming
    [SerializeField] private Transform minDistancePoint; // Transform point for minimum distance visualization
    [SerializeField] private Transform trajectoryVisualPoint; // Transform point for trajectory visualization (controls green line)

    [Header("Ball Prefabs")]
    [SerializeField] private GameObject orangeBallPrefab;
    [Tooltip("Blue ball prefab")]
    [SerializeField] private GameObject blueBallPrefab;
    [Tooltip("Green ball prefab")]
    [SerializeField] private GameObject greenBallPrefab;

    [Header("Explosion Effects")]
    [Tooltip("Orange ball main explosion particle system prefab")]
    [SerializeField] private GameObject orangeExplosionPrefab;
    [Tooltip("Orange ball additional explosion particle system 1 prefab")]
    [SerializeField] private GameObject orangeExplosionPrefab2;
    [Tooltip("Orange ball additional explosion particle system 2 prefab")]
    [SerializeField] private GameObject orangeExplosionPrefab3;
    [Tooltip("Orange ball additional explosion particle system 3 prefab")]
    [SerializeField] private GameObject orangeExplosionPrefab4;

    [Tooltip("Blue ball main explosion particle system prefab")]
    [SerializeField] private GameObject blueExplosionPrefab;
    [Tooltip("Blue ball additional explosion particle system 1 prefab")]
    [SerializeField] private GameObject blueExplosionPrefab2;
    [Tooltip("Blue ball additional explosion particle system 2 prefab")]
    [SerializeField] private GameObject blueExplosionPrefab3;
    [Tooltip("Blue ball additional explosion particle system 3 prefab")]
    [SerializeField] private GameObject blueExplosionPrefab4;

    [Tooltip("Green ball main explosion particle system prefab")]
    [SerializeField] private GameObject greenExplosionPrefab;
    [Tooltip("Green ball additional explosion particle system 1 prefab")]
    [SerializeField] private GameObject greenExplosionPrefab2;
    [Tooltip("Green ball additional explosion particle system 2 prefab")]
    [SerializeField] private GameObject greenExplosionPrefab3;
    [Tooltip("Green ball additional explosion particle system 3 prefab")]
    [SerializeField] private GameObject greenExplosionPrefab4;

    [Tooltip("Scale multiplier for explosion effects")]
    public float explosionScale = 1f;
    [Tooltip("Scale multiplier for additional explosion effects")]
    public float additionalExplosionsScale = 0.7f;
    [Tooltip("Delay between main and additional explosions")]
    public float explosionDelay = 0.05f;
    [Tooltip("Random offset for additional explosions")]
    public float explosionRandomOffset = 0.5f;
    [Tooltip("Enable explosion effects")]
    public bool enableExplosionEffects = true;
    [Tooltip("Show explosion debug info")]
    public bool showExplosionDebug = false;

    [Header("Ball Preview System")]
    [Tooltip("Transform where the next ball preview will be positioned")]
    [SerializeField] private Transform nextBallPreviewPoint;
    [Tooltip("Scale of the preview ball (0.5 = half size)")]
    public float previewBallScale = 0.7f;
    [Tooltip("Show next ball preview")]
    public bool showNextBallPreview = true;
    [Tooltip("Preview ball follows launcher rotation")]
    public bool previewFollowsLauncher = true;

    [Header("Ball Destruction System")]
    [Tooltip("Layer mask for collision destruction")]
    [SerializeField] private LayerMask destructionLayers = -1;
    [Tooltip("Time before ball auto-destructs (seconds)")]
    public float ballLifetime = 3f;
    [Tooltip("Enable collision-based destruction")]
    public bool enableCollisionDestruction = true;
    [Tooltip("Enable time-based destruction")]
    public bool enableTimeDestruction = true;
    [Tooltip("Instant destruction (no fade-out)")]
    public bool instantDestruction = true;
    [Tooltip("Show destruction debug info")]
    public bool showDestructionDebug = false;

    [Header("Ball Damage System")]
    [Tooltip("Damage dealt by balls to enemies")]
    public float ballDamage = 25f;
    [Tooltip("Layer mask for enemies that can take damage")]
    [SerializeField] private LayerMask enemyLayers = -1;
    [Tooltip("Enable damage system")]
    public bool enableDamageSystem = true;
    [Tooltip("Damage enemies on collision")]
    public bool damageOnCollision = true;
    [Tooltip("Damage enemies on explosion")]
    public bool damageOnExplosion = true;
    [Tooltip("Explosion damage radius")]
    public float explosionDamageRadius = 2f;
    [Tooltip("Explosion damage multiplier")]
    public float explosionDamageMultiplier = 1.5f;
    [Tooltip("Show damage debug info")]
    public bool showDamageDebug = false;

    [Header("Sound Effects")]
    [Tooltip("Main explosion sound effect")]
    [SerializeField] private AudioClip explosionSound;
    [Tooltip("Additional explosion sound effects (played with delay)")]
    [SerializeField] private AudioClip[] additionalExplosionSounds;
    [Tooltip("Volume for explosion sounds")]
    [Range(0f, 1f)]
    public float explosionVolume = 1f;
    [Tooltip("Delay between main and additional explosion sounds")]
    public float soundExplosionDelay = 0.1f;
    [Tooltip("Enable sound effects")]
    public bool enableSoundEffects = true;



    [Header("Curved Trajectory Settings")]
    [Tooltip("Enable curved trajectory aiming")]
    public bool useCurvedTrajectory = true;
    [Tooltip("Gravity value for trajectory calculation (use Physics2D.gravity.y or custom)")]
    public float gravityForce = -9.81f;
    [Tooltip("Number of points to draw in trajectory curve")]
    public int trajectoryPointsCount = 20;
    [Tooltip("Time step between trajectory points")]
    public float trajectoryTimeStep = 0.1f;
    [Tooltip("Maximum trajectory distance")]
    public float maxTrajectoryDistance = 10f;
    [Tooltip("Show curved trajectory debug")]
    public bool showTrajectoryDebug = false;

    [Header("Dynamic Launch Force Settings")]
    [Tooltip("Enable dynamic launch force based on aiming distance")]
    public bool useDynamicForce = true;
    [Tooltip("Minimum launch force (when aiming close)")]
    public float minLaunchForce = 3f;
    [Tooltip("Maximum launch force (when aiming far)")]
    public float maxLaunchForce = 15f;
    [Tooltip("Distance for minimum force")]
    public float minForceDistance = 1f;
    [Tooltip("Distance for maximum force")]
    public float maxForceDistance = 8f;
    [Tooltip("Force calculation curve (0=linear, 1=exponential)")]
    [Range(0f, 2f)]
    public float forceCurve = 1f;
    [Tooltip("Show dynamic force debug info")]
    public bool showForceDebug = false;

    [Header("Ball Launch Settings")]
    [Tooltip("Force applied to launched balls (used when dynamic force is disabled)")]
    public float launchForce = 10f;
    [Tooltip("Minimum time between shots (in seconds)")]
    public float shootCooldown = 0.1f;
    [Tooltip("Add random spread to ball direction")]
    public float randomSpread = 0f;
    [Tooltip("Show ball spawn debug info")]
    public bool showBallDebug = false;

    [Header("Aiming Settings")]
    [Tooltip("Maximum angle (in degrees) the gun/arm can rotate upward")]
    public float maxUpwardAngle = 80f;
    [Tooltip("Maximum angle (in degrees) the gun/arm can rotate downward")]
    public float maxDownwardAngle = 20f;
    [Tooltip("Reference to the player\"s transform for flip detection")]
    public Transform playerTransform;
    [Tooltip("Minimum distance required to rotate gun/arm")]
    public float minDistanceToAim = 0.5f;
    [Tooltip("Enable aiming stabilization during player movement")]
    public bool enableAimStabilization = true;

    [Header("Launcher Calibration")]
    [Tooltip("Manual rotation offset for the launcher when player faces RIGHT (in degrees)")]
    public float launcherRotationOffsetRight = 0f;
    [Tooltip("Manual rotation offset for the launcher when player faces LEFT (in degrees)")]
    public float launcherRotationOffsetLeft = 0f;
    [Tooltip("Should the launcher rotate independently from the arm?")]
    public bool independentLauncherRotation = true;
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
    [Tooltip("Automatically calibrate launcher alignment on start")]
    public bool autoCalibrate = true;
    [Tooltip("Show calibration info in console")]
    public bool showCalibrationDebug = true;

    // Core aiming variables
    private Vector2 aimDirection; // The direction we want to aim
    private Vector2 mouseScreenPosition; // Mouse screen position
    private Vector2 mouseWorldPosition; // Mouse world position
    private Vector2 stabilizedMouseWorldPosition; // Stabilized mouse world position
    private bool isPlayerFacingRight = true; // Track player facing direction
    private Vector2 aimFromPosition; // Position we\"re aiming from

    // World space rotation tracking (independent of player flip)
    private float worldArmRotation = 0f;
    private float worldLauncherRotation = 0f;
    private float worldTrajectoryRotation = 0f; // Controls the green line (actual projectile direction)

    // Ball spawning variables
    private GameObject[] ballPrefabs; // Array for optimized random selection
    private float lastShootTime = 0f; // For cooldown tracking

    // Ball preview system
    private GameObject currentPreviewBall; // Current preview ball instance
    private int nextBallIndex = 0; // Index of next ball to spawn

    // Curved trajectory system
    private List<Vector3> trajectoryPoints = new List<Vector3>(); // Calculated trajectory points
    private Vector2 currentLaunchVelocity; // Current calculated launch velocity

    // Dynamic force system
    private float currentCalculatedForce = 10f; // Current calculated launch force

    // Ball destruction and damage variables
    private List<GameObject> activeProjectiles = new List<GameObject>(); // Changed to a list to handle multiple balls in flight

    // Performance optimization variables
    private float lastTrajectoryUpdate = 0f;
    private float trajectoryUpdateInterval = 0.02f; // Update trajectory 50 times per second

    public ShakeData CameraShakeExplosion;

    void Start()
    {
        // Initialize ball prefabs array for optimized random selection
        InitializeBallPrefabs();

        // Initialize next ball preview
        InitializeNextBallPreview();

        if (autoCalibrate)
        {
            CalibrateAiming();
        }

        UpdateMinDistancePointPosition();
        UpdateTrajectoryVisualPoint();
    }

    void Update()
    {
        // Core updates every frame - ALWAYS allow rotation regardless of player movement
        HandleAiming();
        HandleShooting();
        ApplyWorldSpaceRotations();

        // Optimized updates with intervals
        if (Time.time - lastTrajectoryUpdate >= trajectoryUpdateInterval)
        {
            UpdateMinDistancePointPosition();

            if (autoUpdateTrajectoryVisual)
            {
                UpdateTrajectoryVisualPoint();
            }

            // Update dynamic force calculation
            if (useDynamicForce)
            {
                CalculateDynamicForce();
            }

            // Update curved trajectory calculation
            if (useCurvedTrajectory)
            {
                CalculateCurvedTrajectory();
            }

            // Update next ball preview
            UpdateNextBallPreview();

            lastTrajectoryUpdate = Time.time;
        }

        // Handle destruction for all active projectiles based on lifetime
        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            GameObject projectile = activeProjectiles[i];
            ProjectileLifecycleController lifecycleController = projectile.GetComponent<ProjectileLifecycleController>();
            if (lifecycleController != null && enableTimeDestruction && !lifecycleController.hasBeenDestroyed && Time.time - lifecycleController.spawnTime >= ballLifetime)
            {
                if (showDestructionDebug)
                {
                    Debug.Log($"Projectile auto-destructed after {ballLifetime}s");
                }
                DestroyBall(projectile, projectile.transform.position); // Pass the specific projectile to destroy
            }
        }
    }

    private void CalculateDynamicForce()
    {
        if (!IsAimingValid())
        {
            currentCalculatedForce = minLaunchForce;
            return;
        }

        float distanceToMouse = GetDistanceToMouse();

        // Clamp distance to our force range
        float clampedDistance = Mathf.Clamp(distanceToMouse, minForceDistance, maxForceDistance);

        // Calculate normalized distance (0 to 1)
        float normalizedDistance = (clampedDistance - minForceDistance) / (maxForceDistance - minForceDistance);

        // Apply curve to the normalized distance
        float curvedDistance = Mathf.Pow(normalizedDistance, forceCurve);

        // Calculate final force
        currentCalculatedForce = Mathf.Lerp(minLaunchForce, maxLaunchForce, curvedDistance);

        if (showForceDebug)
        {
            Debug.Log($"Distance: {distanceToMouse:F2}, Normalized: {normalizedDistance:F2}, Curved: {curvedDistance:F2}, Force: {currentCalculatedForce:F2}");
        }
    }

    private void InitializeBallPrefabs()
    {
        // Create array with non-null prefabs for optimized random selection
        var validPrefabs = new System.Collections.Generic.List<GameObject>();

        if (orangeBallPrefab != null) validPrefabs.Add(orangeBallPrefab);
        if (blueBallPrefab != null) validPrefabs.Add(blueBallPrefab);
        if (greenBallPrefab != null) validPrefabs.Add(greenBallPrefab);

        ballPrefabs = validPrefabs.ToArray();

        // Initialize next ball index
        if (ballPrefabs.Length > 0)
        {
            nextBallIndex = Random.Range(0, ballPrefabs.Length);
        }

        if (showBallDebug)
        {
            Debug.Log($"Initialized {ballPrefabs.Length} ball prefabs for spawning");
        }
    }

    private void InitializeNextBallPreview()
    {
        if (!showNextBallPreview || ballPrefabs == null || ballPrefabs.Length == 0 || nextBallPreviewPoint == null)
        {
            return;
        }

        CreatePreviewBall();
    }

    private void CreatePreviewBall()
    {
        if (ballPrefabs == null || ballPrefabs.Length == 0 || nextBallPreviewPoint == null)
        {
            return;
        }

        // Destroy existing preview ball
        if (currentPreviewBall != null)
        {
            DestroyImmediate(currentPreviewBall);
        }

        // Create new preview ball
        GameObject nextBallPrefab = ballPrefabs[nextBallIndex];
        currentPreviewBall = Instantiate(nextBallPrefab, nextBallPreviewPoint.position, nextBallPreviewPoint.rotation);

        // Scale down the preview ball
        currentPreviewBall.transform.localScale = Vector3.one * previewBallScale;

        // Remove physics components from preview ball
        Rigidbody2D previewRb = currentPreviewBall.GetComponent<Rigidbody2D>();
        if (previewRb != null)
        {
            DestroyImmediate(previewRb);
        }

        // Disable colliders on preview ball
        Collider2D[] colliders = currentPreviewBall.GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }

        // Make preview ball slightly transparent (if it has a SpriteRenderer)
        SpriteRenderer spriteRenderer = currentPreviewBall.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 0.7f; // Make it 70% opaque
            spriteRenderer.color = color;
        }

        if (showBallDebug)
        {
            Debug.Log($"Created preview ball: {nextBallPrefab.name}");
        }
    }

    private void UpdateNextBallPreview()
    {
        if (!showNextBallPreview || currentPreviewBall == null || nextBallPreviewPoint == null)
        {
            return;
        }

        // Update preview ball position
        currentPreviewBall.transform.position = nextBallPreviewPoint.position;

        // Update preview ball rotation if it should follow launcher
        if (previewFollowsLauncher)
        {
            currentPreviewBall.transform.rotation = nextBallPreviewPoint.rotation;
        }
    }

    private void CalculateCurvedTrajectory()
    {
        if (!IsAimingValid() || projectileSpawnPoint == null)
        {
            trajectoryPoints.Clear();
            return;
        }

        // Calculate launch velocity based on current aim direction and force
        Vector2 launchDirection = GetLauncherDirection();
        float forceToUse = useDynamicForce ? currentCalculatedForce : launchForce;
        currentLaunchVelocity = launchDirection * forceToUse;

        // Calculate trajectory points
        trajectoryPoints.Clear();
        Vector3 startPosition = projectileSpawnPoint.position;
        Vector2 velocity = currentLaunchVelocity;

        for (int i = 0; i < trajectoryPointsCount; i++)
        {
            float time = i * trajectoryTimeStep;

            // Calculate position using kinematic equations
            Vector3 point = startPosition + (Vector3)(velocity * time) + 0.5f * Vector3.up * gravityForce * time * time;

            // Stop if trajectory goes too far or hits ground level
            if (Vector2.Distance(startPosition, point) > maxTrajectoryDistance || point.y < startPosition.y - 5f)
            {
                break;
            }

            trajectoryPoints.Add(point);
        }

        if (showTrajectoryDebug && trajectoryPoints.Count > 0)
        {
            Debug.Log($"Calculated {trajectoryPoints.Count} trajectory points with force {forceToUse:F2}");
        }
    }

    private void HandleShooting()
    {
        // Check for left mouse click and cooldown
        if (Mouse.current.leftButton.wasPressedThisFrame && Time.time >= lastShootTime + shootCooldown)
        {
            // Only shoot if aiming is valid (not in dead zone)
            if (IsAimingValid())
            {
                SpawnNextBall();
                lastShootTime = Time.time;
            }
            else if (showBallDebug)
            {
                Debug.Log("Cannot shoot - mouse in dead zone");
            }
        }
    }

    private void SpawnNextBall()
    {
        // Check if we have valid ball prefabs
        if (ballPrefabs == null || ballPrefabs.Length == 0)
        {
            if (showBallDebug)
            {
                Debug.LogWarning("No ball prefabs assigned! Cannot spawn ball.");
            }
            return;
        }

        // Check if projectile spawn point is assigned
        if (projectileSpawnPoint == null)
        {
            if (showBallDebug)
            {
                Debug.LogWarning("No projectile spawn point assigned! Cannot spawn ball.");
            }
            return;
        }

        // Use the predetermined next ball
        GameObject selectedBallPrefab = ballPrefabs[nextBallIndex];

        // Spawn ball at projectile spawn point
        GameObject newProjectile = Instantiate(selectedBallPrefab, projectileSpawnPoint.position, Quaternion.identity);
        activeProjectiles.Add(newProjectile); // Add to the list of active projectiles

        // Add ProjectileLifecycleController to manage its state
        ProjectileLifecycleController lifecycleController = newProjectile.AddComponent<ProjectileLifecycleController>();
        lifecycleController.launcherSystem = this;
        lifecycleController.spawnTime = Time.time;
        lifecycleController.hasBeenDestroyed = false; // Reset for new projectile

        // Get launch direction - Use direct aim direction for accurate shooting
        Vector2 launchDirection = GetLauncherDirection();

        // Apply random spread if enabled
        if (randomSpread > 0f)
        {
            float spreadAngle = Random.Range(-randomSpread, randomSpread);
            float currentAngle = Mathf.Atan2(launchDirection.y, launchDirection.x) * Mathf.Rad2Deg;
            float newAngle = (currentAngle + spreadAngle) * Mathf.Deg2Rad;
            launchDirection = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle));
        }

        // Calculate force to use
        float forceToUse = useDynamicForce ? currentCalculatedForce : launchForce;

        // Apply force to ball (check for Rigidbody2D first)
        Rigidbody2D projectileRb = newProjectile.GetComponent<Rigidbody2D>();
        if (projectileRb != null)
        {
            projectileRb.AddForce(launchDirection * forceToUse, ForceMode2D.Impulse);
        }
        else if (showBallDebug)
        {
            Debug.LogWarning($"Ball prefab \"{selectedBallPrefab.name}\" doesn\"t have Rigidbody2D component!");
        }

        if (showBallDebug)
        {
            Debug.Log($"Spawned {selectedBallPrefab.name} with force {forceToUse:F2} in direction {launchDirection}");
        }

        // Add a component to handle collisions for destruction/damage
        BallCollisionHandler collisionHandler = newProjectile.AddComponent<BallCollisionHandler>();
        collisionHandler.launcherSystem = this;

        // Prepare next ball
        PrepareNextBall();
    }


    private void PrepareNextBall()
    {
        if (ballPrefabs == null || ballPrefabs.Length == 0)
        {
            return;
        }

        // Select next ball randomly
        nextBallIndex = Random.Range(0, ballPrefabs.Length);

        // Create new preview ball if preview is enabled
        if (showNextBallPreview)
        {
            CreatePreviewBall();
        }

        if (showBallDebug)
        {
            Debug.Log($"Next ball prepared: {ballPrefabs[nextBallIndex].name}");
        }
    }

    private void HandleAiming()
    {
        // Get raw mouse screen position
        mouseScreenPosition = Mouse.current.position.ReadValue();

        // Convert mouse screen position to world position
        mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);

        // Stabilize mouse world position if enabled
        if (enableAimStabilization)
        {
            // To stabilize aiming, we need to calculate the aim direction relative to the launcher\"s current position,
            // but the mouse position itself should not be directly affected by player movement.
            // The `stabilizedMouseWorldPosition` should be the mouse\"s world position relative to the camera\"s view,
            // not relative to the player\"s changing position.
            // By simply using `mouseWorldPosition` (which is already relative to the camera\"s view),
            // the aim will remain stable even if the player moves.
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
            KritinaMovement playerMovement = playerTransform.GetComponent<KritinaMovement>();
            if (playerMovement != null)
            {
                isPlayerFacingRight = playerMovement.isFacingRight;
            }
            else
            {
                isPlayerFacingRight = playerTransform.localScale.x > 0;
            }
        }
    }

    private void CalculateAimDirection()
    {
        aimFromPosition = launcherAimPoint != null ? launcherAimPoint.position : Gun.transform.position;
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

        // Apply launcher offsets
        float currentLauncherOffset = isPlayerFacingRight ? launcherRotationOffsetRight : launcherRotationOffsetLeft;
        worldLauncherRotation = clampedWorldAngle + currentLauncherOffset;

        // Apply trajectory offsets (this controls the green line - where projectiles actually go)
        float currentTrajectoryOffset = isPlayerFacingRight ? trajectoryRotationOffsetRight : trajectoryRotationOffsetLeft;
        worldTrajectoryRotation = clampedWorldAngle + currentTrajectoryOffset;

        aimDirection = directionToMouse.normalized;
    }

    private float ClampWorldAngle(float worldAngle)
    {
        while (worldAngle > 180f) worldAngle -= 360f;
        while (worldAngle < -180f) worldAngle += 360f;

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
        Quaternion launcherWorldRotation = Quaternion.Euler(0, 0, worldLauncherRotation);
        Quaternion trajectoryWorldRotation = Quaternion.Euler(0, 0, worldTrajectoryRotation);

        if (useInstantRotation)
        {
            Arm.transform.rotation = armWorldRotation;
            if (independentLauncherRotation)
            {
                Gun.transform.rotation = launcherWorldRotation;
            }
            else
            {
                Gun.transform.rotation = armWorldRotation;
            }

            // Apply trajectory visual rotation (controls green line direction)
            if (trajectoryVisualPoint != null)
            {
                trajectoryVisualPoint.rotation = trajectoryWorldRotation;
            }
        }
        else
        {
            Arm.transform.rotation = Quaternion.Lerp(Arm.transform.rotation, armWorldRotation, rotationSpeed * Time.deltaTime);
            if (independentLauncherRotation)
            {
                Gun.transform.rotation = Quaternion.Lerp(Gun.transform.rotation, launcherWorldRotation, rotationSpeed * Time.deltaTime);
            }
            else
            {
                Gun.transform.rotation = Quaternion.Lerp(Gun.transform.rotation, armWorldRotation, rotationSpeed * Time.deltaTime);
            }

            if (trajectoryVisualPoint != null)
            {
                trajectoryVisualPoint.rotation = Quaternion.Lerp(trajectoryVisualPoint.rotation, trajectoryWorldRotation, rotationSpeed * Time.deltaTime);
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

        // Position at projectile spawn point for accurate trajectory visualization
        if (projectileSpawnPoint != null)
        {
            trajectoryVisualPoint.position = projectileSpawnPoint.position;
        }
        else if (launcherAimPoint != null)
        {
            trajectoryVisualPoint.position = launcherAimPoint.position;
        }
        else
        {
            trajectoryVisualPoint.position = Gun.transform.position;
        }
    }

    private void CalibrateAiming()
    {
        if (showCalibrationDebug)
        {
            Debug.Log("=== Robust Launcher System Calibration ===");
            Debug.Log($"Player facing right: {isPlayerFacingRight}");
            Debug.Log($"Ball prefabs assigned: {(ballPrefabs != null ? ballPrefabs.Length : 0)}");
            Debug.Log($"Curved trajectory enabled: {useCurvedTrajectory}");
            Debug.Log($"Dynamic force enabled: {useDynamicForce}");
            Debug.Log($"Damage system enabled: {enableDamageSystem}");
            Debug.Log($"Next ball preview enabled: {showNextBallPreview}");
            Debug.Log($"Ball destruction enabled: Collision={enableCollisionDestruction}, Time={enableTimeDestruction}");
            Debug.Log($"Force range: {minLaunchForce}-{maxLaunchForce}, Distance range: {minForceDistance}-{maxForceDistance}");
            Debug.Log($"Ball lifetime: {ballLifetime}s, Ball damage: {ballDamage}");
        }

        worldArmRotation = 0f;
        float currentLauncherOffset = isPlayerFacingRight ? launcherRotationOffsetRight : launcherRotationOffsetLeft;
        float currentTrajectoryOffset = isPlayerFacingRight ? trajectoryRotationOffsetRight : trajectoryRotationOffsetLeft;
        worldLauncherRotation = currentLauncherOffset;
        worldTrajectoryRotation = currentTrajectoryOffset;
        aimDirection = Vector2.right;

        ApplyWorldSpaceRotations();
        UpdateMinDistancePointPosition();
        UpdateTrajectoryVisualPoint();

        if (showCalibrationDebug)
        {
            Debug.Log($"Calibration complete - Ready for robust launcher system!");
        }
    }

    [ContextMenu("Calibrate Aiming")]
    public void ManualCalibrate()
    {
        CalibrateAiming();
    }

    [ContextMenu("Test Ball Spawn")]
    public void TestBallSpawn()
    {
        if (IsAimingValid())
        {
            SpawnNextBall();
        }
        else
        {
            Debug.Log("Cannot test spawn - mouse in dead zone or aiming invalid");
        }
    }

    [ContextMenu("Refresh Next Ball Preview")]
    public void RefreshNextBallPreview()
    {
        if (showNextBallPreview)
        {
            CreatePreviewBall();
        }
    }

    // Public methods for runtime control
    public void SetUseDynamicForce(bool enabled)
    {
        useDynamicForce = enabled;
        if (showForceDebug)
        {
            Debug.Log($"Dynamic force set to: {enabled}");
        }
    }

    public void SetUseCurvedTrajectory(bool enabled)
    {
        useCurvedTrajectory = enabled;
        if (showTrajectoryDebug)
        {
            Debug.Log($"Curved trajectory set to: {enabled}");
        }
    }

    public void SetShowNextBallPreview(bool enabled)
    {
        showNextBallPreview = enabled;
        if (enabled)
        {
            CreatePreviewBall();
        }
        else if (currentPreviewBall != null)
        {
            DestroyImmediate(currentPreviewBall);
        }
    }

    public void SetBallDamage(float damage)
    {
        ballDamage = damage;
        if (showDamageDebug)
        {
            Debug.Log($"Ball damage set to: {damage}");
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
        if (showTrajectoryDebug)
        {
            Debug.Log($"Gravity force set to: {gravity}");
        }
    }

    public void SetLaunchForce(float force)
    {
        launchForce = force;
        if (showBallDebug)
        {
            Debug.Log($"Launch force set to: {force}");
        }
    }

    public void SetMinLaunchForce(float force)
    {
        minLaunchForce = force;
        if (showForceDebug)
        {
            Debug.Log($"Min launch force set to: {force}");
        }
    }

    public void SetMaxLaunchForce(float force)
    {
        maxLaunchForce = force;
        if (showForceDebug)
        {
            Debug.Log($"Max launch force set to: {force}");
        }
    }

    public void SetBallLifetime(float lifetime)
    {
        ballLifetime = lifetime;
        if (showDestructionDebug)
        {
            Debug.Log($"Ball lifetime set to: {lifetime}s");
        }
    }

    public void SetShootCooldown(float cooldown)
    {
        shootCooldown = cooldown;
        if (showBallDebug)
        {
            Debug.Log($"Shoot cooldown set to: {cooldown}s");
        }
    }

    public void SetTrajectoryPointsCount(int count)
    {
        trajectoryPointsCount = Mathf.Clamp(count, 5, 50);
        if (showTrajectoryDebug)
        {
            Debug.Log($"Trajectory points count set to: {trajectoryPointsCount}");
        }
    }

    // This now returns the trajectory direction (green line) - where projectiles actually go
    public Vector2 GetLauncherDirection()
    {
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

    public float GetDistanceToMouse()
    {
        return (stabilizedMouseWorldPosition - aimFromPosition).magnitude;
    }

    public bool IsMouseInDeadZone()
    {
        return GetDistanceToMouse() < minDistanceToAim;
    }

    public bool CanShoot()
    {
        return Time.time >= lastShootTime + shootCooldown && IsAimingValid();
    }

    public GameObject GetNextBallPrefab()
    {
        if (ballPrefabs != null && ballPrefabs.Length > 0)
        {
            return ballPrefabs[nextBallIndex];
        }
        return null;
    }

    public List<Vector3> GetTrajectoryPoints()
    {
        return new List<Vector3>(trajectoryPoints);
    }

    public Vector2 GetCurrentLaunchVelocity()
    {
        return currentLaunchVelocity;
    }

    public float GetCurrentCalculatedForce()
    {
        return useDynamicForce ? currentCalculatedForce : launchForce;
    }

    // Methods integrated from StableDamageExplodingBall
    public void DestroyBall(GameObject projectileToDestroy, Vector2 explosionPosition)
    {
        if (projectileToDestroy == null) return; // Ensure the projectile object exists

        ProjectileLifecycleController lifecycleController = projectileToDestroy.GetComponent<ProjectileLifecycleController>();
        if (lifecycleController != null && lifecycleController.hasBeenDestroyed) return; // Already marked for destruction

        if (lifecycleController != null)
        {
            lifecycleController.hasBeenDestroyed = true; // Mark as destroyed
        }

        if (showDestructionDebug)
        {
            Debug.Log($"Destroying projectile at {explosionPosition}");
        }

        // Handle explosion damage first
        HandleExplosionDamage(explosionPosition);

        // Trigger camera shake if assigned
        if (CameraShakeExplosion != null)
        {
            CameraShakerHandler.Shake(CameraShakeExplosion);
        }

        // Create explosion effects
        if (enableExplosionEffects)
        {
            string ballType = GetBallType(projectileToDestroy);
            CreateExplosionEffects(explosionPosition, ballType);
        }

        // Play explosion sounds
        if (enableSoundEffects)
        {
            PlayExplosionSounds(explosionPosition);
        }

        // Remove from active projectiles list
        activeProjectiles.Remove(projectileToDestroy);

        // Destroy the projectile GameObject itself
        // This is crucial: the ball GameObject must be destroyed AFTER its effects are triggered.
        Destroy(projectileToDestroy);
    }



    private string GetBallType(GameObject ballPrefab)
    {
        if (ballPrefab.name.Contains("Orange")) return "OrangeBall";
        if (ballPrefab.name.Contains("Blue")) return "BlueBall";
        if (ballPrefab.name.Contains("Green")) return "GreenBall";
        // Add more ball types as needed based on their prefab names
        return "GenericBall";
    }

    private void CreateExplosionEffects(Vector2 explosionPosition, string ballType)
    {
        if (!enableExplosionEffects) return;

        GameObject mainExplosionPrefab = null;
        GameObject additionalExplosionPrefab1 = null;
        GameObject additionalExplosionPrefab2 = null;
        GameObject additionalExplosionPrefab3 = null;

        switch (ballType)
        {
            case "OrangeBall":
                mainExplosionPrefab = orangeExplosionPrefab;
                additionalExplosionPrefab1 = orangeExplosionPrefab2;
                additionalExplosionPrefab2 = orangeExplosionPrefab3;
                additionalExplosionPrefab3 = orangeExplosionPrefab4;
                break;
            case "BlueBall":
                mainExplosionPrefab = blueExplosionPrefab;
                additionalExplosionPrefab1 = blueExplosionPrefab2;
                additionalExplosionPrefab2 = blueExplosionPrefab3;
                additionalExplosionPrefab3 = blueExplosionPrefab4;
                break;
            case "GreenBall":
                mainExplosionPrefab = greenExplosionPrefab;
                additionalExplosionPrefab1 = greenExplosionPrefab2;
                additionalExplosionPrefab2 = greenExplosionPrefab3;
                additionalExplosionPrefab3 = greenExplosionPrefab4;
                break;
            default:
                if (showExplosionDebug)
                {
                    Debug.LogWarning($"Unknown ball type: {ballType}. No specific explosion prefabs found.");
                }
                break;
        }

        // Create main explosion
        if (mainExplosionPrefab != null)
        {
            if (showExplosionDebug) Debug.Log($"Attempting to create main explosion from prefab: {mainExplosionPrefab.name}");
            CreateSingleExplosion(mainExplosionPrefab, explosionPosition, explosionScale, 0f);
        }
        else if (showExplosionDebug)
        {
            Debug.LogWarning($"Main explosion prefab is null for {ballType}");
        }

        // Create additional explosions with delays
        if (additionalExplosionPrefab1 != null)
        {
            if (showExplosionDebug) Debug.Log($"Attempting to create additional explosion 1 from prefab: {additionalExplosionPrefab1.name}");
            StartCoroutine(CreateDelayedExplosion(additionalExplosionPrefab1, explosionPosition, additionalExplosionsScale, explosionDelay));
        }
        else if (showExplosionDebug)
        {
            Debug.LogWarning($"Additional explosion prefab 1 is null for {ballType}");
        }

        if (additionalExplosionPrefab2 != null)
        {
            if (showExplosionDebug) Debug.Log($"Attempting to create additional explosion 2 from prefab: {additionalExplosionPrefab2.name}");
            StartCoroutine(CreateDelayedExplosion(additionalExplosionPrefab2, explosionPosition, additionalExplosionsScale, explosionDelay * 2f));
        }
        else if (showExplosionDebug)
        {
            Debug.LogWarning($"Additional explosion prefab 2 is null for {ballType}");
        }

        if (additionalExplosionPrefab3 != null)
        {
            if (showExplosionDebug) Debug.Log($"Attempting to create additional explosion 3 from prefab: {additionalExplosionPrefab3.name}");
            StartCoroutine(CreateDelayedExplosion(additionalExplosionPrefab3, explosionPosition, additionalExplosionsScale, explosionDelay * 3f));
        }
        else if (showExplosionDebug)
        {
            Debug.LogWarning($"Additional explosion prefab 3 is null for {ballType}");
        }

        if (showExplosionDebug)
        {
            Debug.Log($"Created explosion effects at {explosionPosition} for {ballType}");
        }
    }

    private void CreateSingleExplosion(GameObject explosionPrefab, Vector2 position, float scale, float delay)
    {
        if (explosionPrefab == null)
        {
            Debug.LogError("explosionPrefab is null in CreateSingleExplosion!");
            return;
        }

        // Add random offset for additional explosions
        Vector2 finalPosition = position;
        if (delay > 0f && explosionRandomOffset > 0f)
        {
            Vector2 randomOffset = Random.insideUnitCircle * explosionRandomOffset;
            finalPosition += randomOffset;
        }

        // Instantiate explosion
        GameObject explosionInstance = Instantiate(explosionPrefab, finalPosition, Quaternion.identity);
        explosionInstance.name = explosionPrefab.name + "_PS_Instance"; // Give it a distinct name
        if (showExplosionDebug) Debug.Log($"Instantiated explosion: {explosionInstance.name} at {finalPosition}");

        // Get particle system and play it
        ParticleSystem particles = explosionInstance.GetComponent<ParticleSystem>();
        if (particles != null)
        {
            if (showExplosionDebug) Debug.Log($"Found ParticleSystem on {explosionInstance.name}. Playing...");
            // Ensure Play On Awake is false for the prefab, as we control playing here
            if (!particles.isPlaying)
            {
                particles.Play();
            }
            // Destroy the particle system GameObject after its duration
            Destroy(explosionInstance, particles.main.duration + particles.main.startLifetime.constantMax); // Add startLifetime to duration
        }
        else
        {
            if (showExplosionDebug) Debug.LogWarning($"No ParticleSystem found on {explosionInstance.name}. Destroying after default duration.");
            // If it\"s not a particle system, destroy it after a default duration
            Destroy(explosionInstance, 3f); // Use a default duration if no particle system is found
        }
    }

    private IEnumerator CreateDelayedExplosion(GameObject explosionPrefab, Vector2 position, float scale, float delay)
    {
        yield return new WaitForSeconds(delay);
        CreateSingleExplosion(explosionPrefab, position, scale, delay); // Pass delay to CreateSingleExplosion for offset calculation
    }

    private void PlayExplosionSounds(Vector2 explosionPosition)
    {
        // Play main explosion sound
        if (explosionSound != null)
        {
            PlaySoundAtPosition(explosionSound, explosionPosition, 0f);
        }

        // Play additional explosion sounds with delays
        if (additionalExplosionSounds != null && additionalExplosionSounds.Length > 0)
        {
            for (int i = 0; i < additionalExplosionSounds.Length; i++)
            {
                if (additionalExplosionSounds[i] != null)
                {
                    float soundDelay = soundExplosionDelay * (i + 1);
                    StartCoroutine(PlayDelayedSound(additionalExplosionSounds[i], explosionPosition, soundDelay));
                }
            }
        }
    }

    private void PlaySoundAtPosition(AudioClip clip, Vector2 position, float delay)
    {
        if (clip == null)
        {
            return;
        }

        // Create temporary audio source
        GameObject audioObject = new GameObject("ExplosionAudio");
        audioObject.transform.position = position;

        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = explosionVolume;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.Play();

        // Destroy audio object after clip finishes
        Destroy(audioObject, clip.length + 0.1f);

        if (showDestructionDebug)
        {
            Debug.Log($"Playing explosion sound at {position}");
        }
    }

    private IEnumerator PlayDelayedSound(AudioClip clip, Vector2 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlaySoundAtPosition(clip, position, delay);
    }

    // Damage handling methods
    public void HandleCollision(GameObject collidedObject, Vector2 contactPoint, GameObject projectileGameObject)
    {
        ProjectileLifecycleController lifecycleController = projectileGameObject.GetComponent<ProjectileLifecycleController>();
        if (lifecycleController != null && lifecycleController.hasBeenDestroyed) return; // Already marked for destruction

        bool shouldDestroy = false;

        if (enableCollisionDestruction)
        {
            if (((1 << collidedObject.layer) & destructionLayers) != 0)
            {
                shouldDestroy = true;
                if (showDestructionDebug)
                {
                    Debug.Log($"Projectile collided with destruction layer: {LayerMask.LayerToName(collidedObject.layer)}");
                }
            }
        }

        if (enableDamageSystem && damageOnCollision)
        {
            HandleCollisionDamage(collidedObject, contactPoint, projectileGameObject);
        }

        if (shouldDestroy)
        {
            DestroyBall(projectileGameObject, contactPoint);
        }
    }

    public void HandleTrigger(GameObject triggeredObject, GameObject projectileGameObject)
    {
        ProjectileLifecycleController lifecycleController = projectileGameObject.GetComponent<ProjectileLifecycleController>();
        if (lifecycleController != null && lifecycleController.hasBeenDestroyed) return; // Already marked for destruction

        bool shouldDestroy = false;

        if (enableCollisionDestruction)
        {
            if (((1 << triggeredObject.layer) & destructionLayers) != 0)
            {
                shouldDestroy = true;
                if (showDestructionDebug)
                {
                    Debug.Log($"Projectile triggered with destruction layer: {LayerMask.LayerToName(triggeredObject.layer)}");
                }
            }
        }

        if (enableDamageSystem && damageOnCollision)
        {
            HandleTriggerDamage(triggeredObject, projectileGameObject.transform.position, projectileGameObject);
        }

        if (shouldDestroy)
        {
            DestroyBall(projectileGameObject, projectileGameObject.transform.position);
        }
    }

    private void HandleCollisionDamage(GameObject target, Vector2 impactPoint, GameObject projectileGameObject)
    {
        if (((1 << target.layer) & enemyLayers) != 0)
        {
            FleaHealth fleaHealth = target.GetComponent<FleaHealth>();
            if (fleaHealth != null)
            {
                Vector2 attackDirection = (target.transform.position - projectileGameObject.transform.position).normalized;
                fleaHealth.TakeDamage((int)ballDamage, attackDirection);
                if (showDamageDebug)
                {
                    Debug.Log($"Projectile dealt {ballDamage} damage to Flea {target.name} at {impactPoint}");
                }
                return;
            }

            SprayerHealth sprayerHealth = target.GetComponent<SprayerHealth>();
            if (sprayerHealth != null)
            {
                Vector2 attackDirection = (target.transform.position - projectileGameObject.transform.position).normalized;
                sprayerHealth.TakeDamage((int)ballDamage, attackDirection);
                if (showDamageDebug)
                {
                    Debug.Log($"Projectile dealt {ballDamage} damage to Sprayer {target.name} at {impactPoint}");
                }
                return;
            }

            FlyHealth flyHealth = target.GetComponent<FlyHealth>();
            if (flyHealth != null)
            {
                Vector2 attackDirection = (target.transform.position - projectileGameObject.transform.position).normalized;
                flyHealth.TakeDamage((int)ballDamage, attackDirection);
                if (showDamageDebug)
                {
                    Debug.Log($"Projectile dealt {ballDamage} damage to Fly {target.name} at {impactPoint}");
                }
                return;
            }

            if (fleaHealth == null && sprayerHealth == null && flyHealth == null && showDamageDebug)
            {
                Debug.LogWarning($"Enemy {target.name} doesn\"t have FleaHealth, SprayerHealth, or FlyHealth component!");
            }
        }
    }

    private void HandleTriggerDamage(GameObject target, Vector2 impactPoint, GameObject projectileGameObject)
    {
        if (((1 << target.layer) & enemyLayers) != 0)
        {
            FleaHealth fleaHealth = target.GetComponent<FleaHealth>();
            if (fleaHealth != null)
            {
                Vector2 attackDirection = (target.transform.position - projectileGameObject.transform.position).normalized;
                fleaHealth.TakeDamage((int)ballDamage, attackDirection);
                if (showDamageDebug)
                {
                    Debug.Log($"Projectile dealt {ballDamage} trigger damage to Flea {target.name} at {impactPoint}");
                }
                return;
            }

            SprayerHealth sprayerHealth = target.GetComponent<SprayerHealth>();
            if (sprayerHealth != null)
            {
                Vector2 attackDirection = (target.transform.position - projectileGameObject.transform.position).normalized;
                sprayerHealth.TakeDamage((int)ballDamage, attackDirection);
                if (showDamageDebug)
                {
                    Debug.Log($"Projectile dealt {ballDamage} trigger damage to Sprayer {target.name} at {impactPoint}");
                }
                return;
            }

            FlyHealth flyHealth = target.GetComponent<FlyHealth>();
            if (flyHealth != null)
            {
                Vector2 attackDirection = (target.transform.position - projectileGameObject.transform.position).normalized;
                flyHealth.TakeDamage((int)ballDamage, attackDirection);
                if (showDamageDebug)
                {
                    Debug.Log($"Projectile dealt {ballDamage} trigger damage to Fly {target.name} at {impactPoint}");
                }
                return;
            }

            if (fleaHealth == null && sprayerHealth == null && flyHealth == null && showDamageDebug)
            {
                Debug.LogWarning($"Enemy {target.name} doesn\"t have FleaHealth, SprayerHealth, or FlyHealth component!");
            }
        }
    }

    private void HandleExplosionDamage(Vector2 explosionCenter)
    {
        if (!enableDamageSystem || !damageOnExplosion)
        {
            return;
        }

        Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(explosionCenter, explosionDamageRadius, enemyLayers);

        foreach (Collider2D enemyCollider in enemiesInRange)
        {
            FleaHealth fleaHealth = enemyCollider.GetComponent<FleaHealth>();
            if (fleaHealth != null)
            {
                float distance = Vector2.Distance(explosionCenter, enemyCollider.transform.position);
                float damageMultiplier = 1f - (distance / explosionDamageRadius);
                float explosionDamage = ballDamage * explosionDamageMultiplier * damageMultiplier;
                Vector2 attackDirection = (enemyCollider.transform.position - (Vector3)explosionCenter).normalized;
                fleaHealth.TakeDamage((int)explosionDamage, attackDirection);
                if (showDamageDebug)
                {
                    Debug.Log($"Explosion dealt {explosionDamage:F1} damage to Flea {enemyCollider.name} (distance: {distance:F2})");
                }
                continue;
            }

            SprayerHealth sprayerHealth = enemyCollider.GetComponent<SprayerHealth>();
            if (sprayerHealth != null)
            {
                float distance = Vector2.Distance(explosionCenter, enemyCollider.transform.position);
                float damageMultiplier = 1f - (distance / explosionDamageRadius);
                float explosionDamage = ballDamage * explosionDamageMultiplier * damageMultiplier;
                Vector2 attackDirection = (enemyCollider.transform.position - (Vector3)explosionCenter).normalized;
                sprayerHealth.TakeDamage((int)explosionDamage, attackDirection);
                if (showDamageDebug)
                {
                    Debug.Log($"Explosion dealt {explosionDamage:F1} damage to Sprayer {enemyCollider.name} (distance: {distance:F2})");
                }
                continue;
            }

            FlyHealth flyHealth = enemyCollider.GetComponent<FlyHealth>();
            if (flyHealth != null)
            {
                float distance = Vector2.Distance(explosionCenter, enemyCollider.transform.position);
                float damageMultiplier = 1f - (distance / explosionDamageRadius);
                float explosionDamage = ballDamage * explosionDamageMultiplier * damageMultiplier;
                Vector2 attackDirection = (enemyCollider.transform.position - (Vector3)explosionCenter).normalized;
                flyHealth.TakeDamage((int)explosionDamage, attackDirection);
                if (showDamageDebug)
                {
                    Debug.Log($"Explosion dealt {explosionDamage:F1} damage to Fly {enemyCollider.name} (distance: {distance:F2})");
                }
                continue;
            }

            if (fleaHealth == null && sprayerHealth == null && flyHealth == null && showDamageDebug)
            {
                Debug.LogWarning($"Enemy {enemyCollider.name} doesn\"t have FleaHealth, SprayerHealth, or FlyHealth component!");
            }
        }

        if (showDamageDebug)
        {
            Debug.Log($"Explosion damaged {enemiesInRange.Length} enemies within {explosionDamageRadius} units");
        }
    }

    // Optimized debug visualization
    private void OnDrawGizmos()
    {
        if (Gun != null)
        {
            Vector2 gunPos = Gun.transform.position;
            Vector2 aimFromPos = launcherAimPoint != null ? launcherAimPoint.position : gunPos;

            // Draw launcher aim point if assigned
            if (launcherAimPoint != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(launcherAimPoint.position, 0.15f);
                Gizmos.DrawLine(gunPos, launcherAimPoint.position);
            }

            // Draw trajectory visual point if assigned
            if (trajectoryVisualPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(trajectoryVisualPoint.position, 0.12f);
            }

            // Draw projectile spawn point if assigned
            if (projectileSpawnPoint != null)
            {
                Gizmos.color = CanShoot() ? Color.green : Color.red;
                Gizmos.DrawWireSphere(projectileSpawnPoint.position, 0.1f);
            }

            // Draw next ball preview point if assigned
            if (nextBallPreviewPoint != null)
            {
                Gizmos.color = showNextBallPreview ? Color.yellow : Color.gray;
                Gizmos.DrawWireSphere(nextBallPreviewPoint.position, 0.08f);
            }

            // Draw aim direction (yellow)
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(aimFromPos, aimDirection * 3f);

            // Draw GREEN LINE - actual projectile direction (controlled by trajectory offsets)
            Gizmos.color = Color.green;
            Vector2 trajectoryGizmoOrigin = trajectoryVisualPoint != null ? trajectoryVisualPoint.position : projectileSpawnPoint.position;
            Gizmos.DrawRay(trajectoryGizmoOrigin, GetLauncherDirection() * 4f);

            // Draw curved trajectory if enabled and calculated
            if (useCurvedTrajectory && trajectoryPoints.Count > 1)
            {
                Gizmos.color = Color.red;
                for (int i = 0; i < trajectoryPoints.Count - 1; i++)
                {
                    Gizmos.DrawLine(trajectoryPoints[i], trajectoryPoints[i + 1]);
                }

                // Draw trajectory end point
                if (trajectoryPoints.Count > 0)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(trajectoryPoints[trajectoryPoints.Count - 1], 0.2f);
                }
            }

            // Draw dynamic force visualization
            if (useDynamicForce && IsAimingValid())
            {
                float distanceToMouse = GetDistanceToMouse();
                float normalizedForce = (currentCalculatedForce - minLaunchForce) / (maxLaunchForce - minLaunchForce);

                // Draw force indicator circle (size based on force)
                Gizmos.color = Color.Lerp(Color.blue, Color.red, normalizedForce);
                Gizmos.DrawWireSphere(aimFromPos, 0.3f + normalizedForce * 0.3f);

                // Draw force range indicators
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(aimFromPos, minForceDistance);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(aimFromPos, maxForceDistance);
            }

            // Draw minimum aim distance circle
            Gizmos.color = IsMouseInDeadZone() ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(aimFromPos, minDistanceToAim);

            // Draw line from aim point to mouse
            Gizmos.color = IsAimingValid() ? Color.white : Color.red;
            Gizmos.DrawLine(aimFromPos, stabilizedMouseWorldPosition);

            // Draw MinDistancePoint position if assigned
            if (minDistancePoint != null)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawWireSphere(minDistancePoint.position, 0.1f);
                Gizmos.DrawLine(aimFromPos, minDistancePoint.position);
            }
        }

        // Gizmos for explosion damage radius and random offset
        if (enableDamageSystem && damageOnExplosion)
        {
            // Iterate through active projectiles to draw their explosion radius
            foreach (GameObject projectile in activeProjectiles)
            {
                if (projectile != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(projectile.transform.position, explosionDamageRadius);

                    // Note: explosionRandomOffset is no longer used for visual debug here
                    // as it\"s handled by individual ball explosion scripts.
                }
            }
        }
    }
}

// New helper script to handle ball collisions and pass them back to RobustLauncherSystem
public class BallCollisionHandler : MonoBehaviour
{
    public RobustLauncherSystem launcherSystem;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (launcherSystem != null)
        {
            launcherSystem.HandleCollision(collision.gameObject, collision.contacts[0].point, gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (launcherSystem != null)
        {
            launcherSystem.HandleTrigger(other.gameObject, gameObject);
        }
    }
}

// New script to manage projectile lifecycle and state
public class ProjectileLifecycleController : MonoBehaviour
{
    public RobustLauncherSystem launcherSystem;
    public float spawnTime;
    public bool hasBeenDestroyed = false;
}

// Simplified ParticleSystemLifecycle script (can be removed if not needed, or kept for generic particle systems)
public class ParticleSystemLifecycle : MonoBehaviour
{
    void Start()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps == null)
        {
            Debug.LogWarning($"ParticleSystemLifecycle: No ParticleSystem found on {gameObject.name}. Destroying this component.");
            Destroy(this); // Destroy this component if no ParticleSystem is found
            return;
        }

        // Ensure the particle system plays if it\"s not already playing
        if (!ps.isPlaying)
        {
            ps.Play();
        }

        // Destroy the GameObject after the particle system has finished playing
        // We add a small buffer to ensure all particles have faded out.
        float totalDuration = ps.main.duration + ps.main.startLifetime.constantMax + 0.1f;
        Destroy(gameObject, totalDuration);
    }
}


