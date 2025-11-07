using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using FirstGearGames.SmoothCameraShaker;
using Photon.Pun;

public class RobustLauncherSystem : MonoBehaviour, IPunObservable, IPoolable 
{
    [Header("Component References")]
    [SerializeField] private GameObject Gun;
    [SerializeField] private GameObject Arm;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private Transform launcherAimPoint; // Specific point on the launcher for aiming
    [SerializeField] private Transform minDistancePoint; // Transform point for minimum distance visualization
    [SerializeField] private Transform trajectoryVisualPoint; // Transform point for trajectory visualization (controls green line)
    public Joystick aimJoystick;
    [Header("Ball Prefabs")]
    [SerializeField] private GameObject orangeBallPrefab;
    [Tooltip("Blue ball prefab")]
    [SerializeField] private GameObject blueBallPrefab;
    [Tooltip("Green ball prefab")]
    [SerializeField] private GameObject greenBallPrefab;
    private PhotonView view;
    private bool isOnlineMode = false;
    private PlayerSyncManager syncManager;
    private bool isJoystickHeldDown = false;
    [Header("Weapon Upgrade Data")]
    [SerializeField] private WeaponData weaponData;
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
    public int ballDamage = 100; // Changed to int for exact damage
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
    [Header("Input Settings")]
    public bool PcInput = true;
    public bool MobileInput = false;

    [Header("Sound Effects")]
    [Tooltip("Main explosion sound effect")]
    [SerializeField] private AudioClip explosionSound;
    [Tooltip("Additional explosion sound effects (played with delay)")]
    [SerializeField] private AudioClip[] additionalExplosionSounds;
    [Tooltip("Sound effect for shooting the launcher")]
    [SerializeField] private AudioClip shootSound;
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
    [Tooltip("Volume for the shoot sound")]
    [Range(0f, 1f)]
    public float shootVolume = 1f;
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
    private bool isAimingWithJoystick = false; // Tracks if the player is currently aiming with the joystick
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

    // --- ADD THESE LINES near your other Header variables ---
    [Header("Object Pooling Settings")]
    [Tooltip("How many of each ball type to create at the start.")]
    [SerializeField] private int ballPoolSize = 10;

    [Header("UI REFERENCES")]
    [Tooltip("The TextMeshPro UI element that will display the ammo count.")]
    [SerializeField] private TMPro.TextMeshProUGUI ammoText;
    [Tooltip("The empty Transform on the player that the ammo UI should follow.")]
    [SerializeField] private Transform ammoUiFollowPoint;
    [Tooltip("The main UI Canvas for positioning calculations.")]
    [SerializeField] private Canvas uiCanvas;

    void Start()
    {
        view = GetComponentInParent<PhotonView>();
        syncManager = GetComponentInParent<PlayerSyncManager>();

        if (view != null && transform.root.CompareTag("OnlinePlayer"))
        {
            isOnlineMode = true;
            Debug.Log("RobustLauncherSystem: Online Mode Detected.");
        }
        else
        {
            isOnlineMode = false;
            Debug.Log("RobustLauncherSystem: Offline Mode Detected.");
        }

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
        ApplyWeaponUpgrades();
    }
    private void UpdateAmmoUI()
    {
        if (ammoText == null) return;

        // Check if the weapon is currently on cooldown
        bool onCooldown = Time.time < lastShootTime + shootCooldown;

        if (onCooldown)
        {
            // If on cooldown, show the "reloading" text
            ammoText.text = "--/--";
        }
        else
        {
            // Otherwise, show "1/1"
            ammoText.text = "1/1";
        }
    }

    // METHOD 2: The position update logic
    private void UpdateAmmoUIPosition()
    {
        if (ammoText == null || !ammoText.gameObject.activeInHierarchy || ammoUiFollowPoint == null || uiCanvas == null)
        {
            return;
        }

        Vector2 screenPoint = Camera.main.WorldToScreenPoint(ammoUiFollowPoint.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)uiCanvas.transform,
            screenPoint,
            uiCanvas.worldCamera,
            out Vector2 localPosition
        );
        ammoText.transform.localPosition = localPosition;
    }

    // METHOD 3: A helper to find the canvas if it's not assigned
    private void FindCanvas()
    {
        if (uiCanvas == null)
        {
            uiCanvas = FindObjectOfType<Canvas>();
            if (uiCanvas == null)
            {
                Debug.LogError("RobustLauncherSystem: No UI Canvas found in the scene!");
            }
        }
    }
    private void ApplyWeaponUpgrades()
    {
        if (weaponData == null)
        {
            Debug.Log("No WeaponData assigned to RobustLauncherSystem. Using default stats.");
            return;
        }

        int currentLevel = InventoryManager.Instance.GetWeaponLevel(weaponData.name);

        if (currentLevel > 0)
        {
            Debug.Log("Applying upgrades for " + weaponData.weaponName + " at Level " + currentLevel);

            // Get the data for the correct level
            WeaponUpgradeData currentUpgrade = weaponData.upgradeLevels[currentLevel - 1];

            // --- OVERRIDE THE LAUNCHER'S STATS ---
            this.ballDamage = currentUpgrade.launcherStats.launcherDamage;
            this.shootCooldown = currentUpgrade.launcherStats.launcherFireRate;
            this.explosionDamageRadius = currentUpgrade.launcherStats.launcherRadius;

            Debug.Log("New Stats -> Damage: " + this.ballDamage + ", Fire Rate: " + this.shootCooldown + ", Radius: " + this.explosionDamageRadius);
        }
        else
        {
            Debug.Log(weaponData.weaponName + " is Level 0. Using default stats.");
        }
    }
    public void CreatePools()
    {
        if (ObjectPoolManager.Instance != null)
        {
            // Create pools for all ball types
            if (orangeBallPrefab != null) ObjectPoolManager.Instance.CreatePool(orangeBallPrefab, ballPoolSize);
            if (blueBallPrefab != null) ObjectPoolManager.Instance.CreatePool(blueBallPrefab, ballPoolSize);
            if (greenBallPrefab != null) ObjectPoolManager.Instance.CreatePool(greenBallPrefab, ballPoolSize);
            // Note: We are NOT pooling the explosions here, as you requested.
        }
    }
    void OnEnable()
    {
        FindCanvas(); // Make sure we have the canvas reference
        UpdateAmmoUI();
        if (ammoText != null) ammoText.gameObject.SetActive(true);
        // Start listening for the broadcast from the joystick
        JoystickBroadcaster.OnJoystickTouchStateChanged += HandleJoystickTouchState;
    }


    // This function is called when your script is disabled
    void OnDisable()
    {
        if (ammoText != null)
        {
            ammoText.gameObject.SetActive(false);
        }
        // Stop listening to prevent errors
        JoystickBroadcaster.OnJoystickTouchStateChanged -= HandleJoystickTouchState;
    }

    // This function is called automatically when the joystick sends a signal
    private void HandleJoystickTouchState(bool isDown)
    {
        isJoystickHeldDown = isDown;
    }
    void Update()
    {
        if (isOnlineMode && !view.IsMine)
        {
            // If we are online and this is not our character, we do nothing in Update.
            // The rotation will be handled by OnPhotonSerializeView.
            return;
        }
        // Core updates every frame - ALWAYS allow rotation regardless of player movement
        HandleInputAndShooting();
        ApplyWorldSpaceRotations();
        UpdateAmmoUI();
        UpdateAmmoUIPosition();
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
            Destroy(currentPreviewBall);

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
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // This code runs on our own character.
            // We are the owner, so we send the current rotation of our Arm and Gun to the network.
            stream.SendNext(Arm.transform.rotation);
            stream.SendNext(Gun.transform.rotation);
        }
        else
        {
            // This code runs on the remote clone of the character.
            // We receive the rotation data and apply it directly.
            // This will make the remote player's launcher aim correctly.
            this.Arm.transform.rotation = (Quaternion)stream.ReceiveNext();
            this.Gun.transform.rotation = (Quaternion)stream.ReceiveNext();
        }
    }

    private void HandleInputAndShooting()
    {
        if (view != null && !view.IsMine)
        {
            return; // If this is an online character that isn't mine, do nothing.
        }

        bool isAiming = false;
        bool shootAction = false; // This will be true only on release or click

        // --- START OF THE FIX ---

        // --- MOBILE JOYSTICK LOGIC ---
        // This logic is now driven by our 100% reliable isJoystickHeldDown boolean.
        if (MobileInput)
        {
            // A "press" happens on the first frame the finger is down.
            if (isJoystickHeldDown && !isAimingWithJoystick)
            {
                // We don't shoot on press for the launcher, but we enter the aiming state.
                isAimingWithJoystick = true; // MEMORIZE: We are now in joystick aiming mode.
            }

            // A "hold" is true for every frame the finger is down.
            if (isJoystickHeldDown)
            {
                isAiming = true; // We are actively aiming.
                                 // Update the aim position based on joystick direction
                if (aimJoystick.Direction.sqrMagnitude > 0.1f)
                {
                    Vector3 joystickDirection = new Vector3(aimJoystick.Direction.x, aimJoystick.Direction.y, 0);
                    mouseWorldPosition = launcherAimPoint.position + joystickDirection * 10f; // Simulate a world position to aim at
                }
            }

            // A "release" happens on the first frame the finger is lifted.
            if (!isJoystickHeldDown && isAimingWithJoystick)
            {
                isAiming = false; // No longer actively aiming
                isAimingWithJoystick = false; // FORGET: We are no longer aiming.
                shootAction = true; // Trigger the shot!
            }
        }

        // --- PC MOUSE LOGIC (Fallback or Primary) ---
        if (PcInput && !isAimingWithJoystick) // Only run PC input if it's enabled AND mobile isn't being used
        {
            isAiming = true; // Always allow aiming with the mouse
            mouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            // For PC, we shoot on click, not release
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                shootAction = true;
            }
        }

        // --- END OF THE FIX ---


        // --- AIMING LOGIC (runs if either input is active) ---
        if (isAiming)
        {
            stabilizedMouseWorldPosition = mouseWorldPosition;
            UpdatePlayerFacingDirection();
            CalculateAimDirection();
        }

        // --- SHOOTING LOGIC (runs when a shoot action is triggered) ---
        if (shootAction && Time.time >= lastShootTime + shootCooldown)
        {
            if (IsAimingValid())
            {
                SpawnNextBall();
                lastShootTime = Time.time;
            }
        }
    }



    private void SpawnNextBall()
    {
        if (ballPrefabs == null || ballPrefabs.Length == 0 || projectileSpawnPoint == null)
        {
            if (showBallDebug) Debug.LogWarning("Cannot spawn ball - missing prefabs or spawn point.");
            return;
        }

        GameObject selectedBallPrefab = ballPrefabs[nextBallIndex];
        Vector2 launchDirection = GetLauncherDirection();
        float forceToUse = useDynamicForce ? currentCalculatedForce : launchForce;

        // --- THE MODE-AWARE LOGIC ---
        if (isOnlineMode)
        {
            // --- ONLINE ---
            // 1. The shooter creates their own "real" ball locally. This is the authority.
            GameObject myBall = ObjectPoolManager.Instance.SpawnFromPool(selectedBallPrefab, projectileSpawnPoint.position, Quaternion.identity);

            InitializeBall(myBall, launchDirection, forceToUse, true); // true = isReal

            // 2. The shooter tells everyone else to create a "visual" ball.
            view.RPC("RPC_SpawnVisualBall", RpcTarget.Others, selectedBallPrefab.name, projectileSpawnPoint.position, launchDirection, forceToUse);
        }
        else
        {
            // --- OFFLINE ---
            // Just create a normal, real ball.
            GameObject myBall = ObjectPoolManager.Instance.SpawnFromPool(selectedBallPrefab, projectileSpawnPoint.position, Quaternion.identity);

            InitializeBall(myBall, launchDirection, forceToUse, true); // true = isReal
        }

        // Play sound locally for the shooter
        if (enableSoundEffects && shootSound != null)
        {
            PlaySound(shootSound, shootVolume);
        }

        PrepareNextBall();
    }

    // --- ADD THIS NEW HELPER FUNCTION ---
    // This function adds the necessary components and initializes a ball.
    // --- REPLACE your InitializeBall method with this FINAL version ---
    private void InitializeBall(GameObject ball, Vector2 direction, float force, bool isRealBall)
    {
        // --- HYBRID "GET OR ADD" LOGIC ---
        // Try to get the component. If it doesn't exist, add it.
        ProjectileLifecycleController lifecycle = ball.GetComponent<ProjectileLifecycleController>();
        if (lifecycle == null)
        {
            lifecycle = ball.AddComponent<ProjectileLifecycleController>();
        }

        BallCollisionHandler collisionHandler = ball.GetComponent<BallCollisionHandler>();
        if (collisionHandler == null)
        {
            collisionHandler = ball.AddComponent<BallCollisionHandler>();
        }
        // --- END OF HYBRID LOGIC ---

        // --- THE CRITICAL RESET LOGIC ---
        // Now that we are GUARANTEED to have a 'lifecycle' component, we can safely set its state.
        lifecycle.launcherSystem = this;
        lifecycle.spawnTime = Time.time;
        lifecycle.isReal = isRealBall;
        lifecycle.hasBeenDestroyed = false; // THIS IS THE FIX FOR THE SECOND SHOT.

        // Set the collision handler's owner.
        collisionHandler.launcherSystem = this;

        // --- PHYSICS RESET LOGIC ---
        // Apply the force and reset physics state.
        Rigidbody2D projectileRb = ball.GetComponent<Rigidbody2D>();
        if (projectileRb != null)
        {
            // Wake up the Rigidbody.
            projectileRb.simulated = true;
            // Clear old forces.
            projectileRb.velocity = Vector2.zero;
            projectileRb.angularVelocity = 0f;
            // Apply new force.
            projectileRb.AddForce(direction * force, ForceMode2D.Impulse);
        }
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

    public void SetBallDamage(int damage)
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
    public void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || Camera.main == null) return;

        // Create a clean, independent object for the sound
        GameObject soundPlayerObject = new GameObject("Launcher_FORCE_PLAY_SOUND");

        // --- THIS IS THE CRITICAL FIX for volume issues ---
        // Position it directly on the camera to guarantee it's heard at full volume
        soundPlayerObject.transform.position = Camera.main.transform.position;

        // Add and aggressively configure the AudioSource
        AudioSource tempAudioSource = soundPlayerObject.AddComponent<AudioSource>();

        tempAudioSource.clip = clip;

        // --- CRITICAL OVERRIDES ---
        tempAudioSource.volume = volume;
        tempAudioSource.spatialBlend = 0.0f;              // Force 2D sound
        tempAudioSource.priority = 0;                     // Highest priority
        tempAudioSource.bypassEffects = true;             // Ignore mixers
        tempAudioSource.bypassListenerEffects = true;     // Ignore listener effects
        tempAudioSource.bypassReverbZones = true;         // Ignore reverb zones

        // Play the sound and schedule its destruction
        tempAudioSource.Play();
        Destroy(soundPlayerObject, clip.length);
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
        if (projectileToDestroy == null) return;

        ProjectileLifecycleController lifecycleController = projectileToDestroy.GetComponent<ProjectileLifecycleController>();
        if (lifecycleController != null && lifecycleController.hasBeenDestroyed) return;

        if (lifecycleController != null)
        {
            lifecycleController.hasBeenDestroyed = true;
        }

        // 1. Get the type of the ball (e.g., "OrangeBall")
        string ballType = GetBallType(projectileToDestroy);
        // 2. Convert that type into a simple number (ID)
        int explosionId = GetExplosionIdByType(ballType);

        // 3. If the ID is valid, we proceed.
        if (explosionId != -1)
        {
            if (isOnlineMode)
            {
                // --- ONLINE ---
                // We send the ID and the position over the network. This matches the error message.
                view.RPC("RPC_PlayExplosionEffect", RpcTarget.All, explosionId, (Vector3)explosionPosition);
            }
            else
            {
                // --- OFFLINE ---
                // We just play the effect locally using the ID.
                PlayLocalExplosion(explosionId, explosionPosition);
            }
        }

        // The rest of the function continues as normal...
        HandleExplosionDamage(explosionPosition);
        if (CameraShakeExplosion != null) CameraShakerHandler.Shake(CameraShakeExplosion);
        if (enableSoundEffects) PlayExplosionSounds(explosionPosition);

        activeProjectiles.Remove(projectileToDestroy);
        projectileToDestroy.SetActive(false);

    }

    // --- ADD OR REPLACE THIS HELPER FUNCTION ---
    // This function converts the ball's name into a simple ID.
    private int GetExplosionIdByType(string ballType)
    {
        if (ballType.Contains("Orange")) return 0;
        if (ballType.Contains("Blue")) return 1;
        if (ballType.Contains("Green")) return 2;
        return -1; // Return -1 if no match is found
    }

    // --- ADD OR REPLACE THIS HELPER FUNCTION ---
    // This function takes an ID and plays the correct set of effects from your existing variables.
    private void PlayLocalExplosion(int id, Vector2 position)
    {
        switch (id)
        {
            case 0: // Orange
                if (orangeExplosionPrefab != null) CreateSingleExplosion(orangeExplosionPrefab, position, explosionScale, 0f);
                if (orangeExplosionPrefab2 != null) StartCoroutine(CreateDelayedExplosion(orangeExplosionPrefab2, position, additionalExplosionsScale, explosionDelay));
                if (orangeExplosionPrefab3 != null) StartCoroutine(CreateDelayedExplosion(orangeExplosionPrefab3, position, additionalExplosionsScale, explosionDelay * 2f));
                if (orangeExplosionPrefab4 != null) StartCoroutine(CreateDelayedExplosion(orangeExplosionPrefab4, position, additionalExplosionsScale, explosionDelay * 3f));
                break;
            case 1: // Blue
                if (blueExplosionPrefab != null) CreateSingleExplosion(blueExplosionPrefab, position, explosionScale, 0f);
                if (blueExplosionPrefab2 != null) StartCoroutine(CreateDelayedExplosion(blueExplosionPrefab2, position, additionalExplosionsScale, explosionDelay));
                if (blueExplosionPrefab3 != null) StartCoroutine(CreateDelayedExplosion(blueExplosionPrefab3, position, additionalExplosionsScale, explosionDelay * 2f));
                if (blueExplosionPrefab4 != null) StartCoroutine(CreateDelayedExplosion(blueExplosionPrefab4, position, additionalExplosionsScale, explosionDelay * 3f));
                break;
            case 2: // Green
                if (greenExplosionPrefab != null) CreateSingleExplosion(greenExplosionPrefab, position, explosionScale, 0f);
                if (greenExplosionPrefab2 != null) StartCoroutine(CreateDelayedExplosion(greenExplosionPrefab2, position, additionalExplosionsScale, explosionDelay));
                if (greenExplosionPrefab3 != null) StartCoroutine(CreateDelayedExplosion(greenExplosionPrefab3, position, additionalExplosionsScale, explosionDelay * 2f));
                if (greenExplosionPrefab4 != null) StartCoroutine(CreateDelayedExplosion(greenExplosionPrefab4, position, additionalExplosionsScale, explosionDelay * 3f));
                break;
        }
    }

    // --- FINALLY, THE RPC FUNCTION THAT MATCHES THE ERROR MESSAGE ---
    [PunRPC]
    private void RPC_PlayExplosionEffect(int explosionId, Vector3 position)
    {
        // This code runs on EVERYONE'S machine.
        // It simply calls our local "switchboard" function to play the correct effect.
        PlayLocalExplosion(explosionId, position);
    }
    private string GetBallType(GameObject ballPrefab)
    {
        if (ballPrefab.name.Contains("Orange")) return "OrangeBall";
        if (ballPrefab.name.Contains("Blue")) return "BlueBall";
        if (ballPrefab.name.Contains("Green")) return "GreenBall";
        // Add more ball types as needed based on their prefab names
        return "GenericBall";
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
            PlaySound(explosionSound, explosionVolume);
        }

        // Play additional explosion sounds with delays
        if (additionalExplosionSounds != null && additionalExplosionSounds.Length > 0)
        {
            for (int i = 0; i < additionalExplosionSounds.Length; i++)
            {
                if (additionalExplosionSounds[i] != null)
                {
                    float soundDelay = soundExplosionDelay * (i + 1);
                    StartCoroutine(PlayDelayedSound(additionalExplosionSounds[i], soundDelay, explosionVolume));
                }
            }
        }
    }



    private IEnumerator PlayDelayedSound(AudioClip clip, float delay, float volume)
    {
        yield return new WaitForSeconds(delay);
        // It now calls our main, reliable PlaySound method. The position is no longer needed.
        PlaySound(clip, volume);
    }

    // Damage handling methods - FIXED FOR EXACT DAMAGE
    public void HandleCollision(GameObject collidedObject, Vector2 contactPoint, GameObject projectileGameObject)
    {
        // First, get the lifecycle controller from the projectile that hit something.
        ProjectileLifecycleController lifecycleController = projectileGameObject.GetComponent<ProjectileLifecycleController>();
        if (lifecycleController == null || lifecycleController.hasBeenDestroyed) return;

        bool shouldDestroy = false;
        if (enableCollisionDestruction && ((1 << collidedObject.layer) & destructionLayers) != 0)
        {
            shouldDestroy = true;
        }

        // --- THIS IS THE CRITICAL CHECK ---
        // We only deal damage if the ball is a "real" one.
        if (lifecycleController.isReal && enableDamageSystem && damageOnCollision)
        {
            HandleCollisionDamage(collidedObject, contactPoint, projectileGameObject);
        }

        if (shouldDestroy)
        {
            // And we only trigger the big destruction sequence if the ball is real.
            if (lifecycleController.isReal)
            {
                DestroyBall(projectileGameObject, contactPoint);
            }
            else
            {
                // If it's just a visual ball, destroy it quietly.
                projectileGameObject.SetActive(false);

            }
        }
    }

    public void HandleTrigger(GameObject triggeredObject, GameObject projectileGameObject)
    {
        // This function follows the exact same logic as HandleCollision.
        ProjectileLifecycleController lifecycleController = projectileGameObject.GetComponent<ProjectileLifecycleController>();
        if (lifecycleController == null || lifecycleController.hasBeenDestroyed) return;

        bool shouldDestroy = false;
        if (enableCollisionDestruction && ((1 << triggeredObject.layer) & destructionLayers) != 0)
        {
            shouldDestroy = true;
        }

        if (lifecycleController.isReal && enableDamageSystem && damageOnCollision)
        {
            HandleTriggerDamage(triggeredObject, projectileGameObject.transform.position, projectileGameObject);
        }

        if (shouldDestroy)
        {
            if (lifecycleController.isReal)
            {
                DestroyBall(projectileGameObject, projectileGameObject.transform.position);
            }
            else
            {
                projectileGameObject.SetActive(false);

            }
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
                fleaHealth.TakeDamage(ballDamage, attackDirection); // EXACT DAMAGE - NO CONVERSION
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
                if (showDamageDebug)
                {
                    Debug.Log($"Projectile dealt {ballDamage} damage to Flea {target.name} at {impactPoint}");
                }
                return;
            }

            FleaHealthV2 FleaHealthV2 = target.GetComponent<FleaHealthV2>();
            if (FleaHealthV2 != null)
            {
                Vector2 attackDirection = (target.transform.position - projectileGameObject.transform.position).normalized;
                FleaHealthV2.TakeDamage(ballDamage, attackDirection); // EXACT DAMAGE - NO CONVERSION
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
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
                flyHealth.TakeDamage(ballDamage, attackDirection); // EXACT DAMAGE - NO CONVERSION
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
                if (showDamageDebug)
                {
                    Debug.Log($"Projectile dealt {ballDamage} damage to Fly {target.name} at {impactPoint}");
                }
                return;
            }

            InkHealth inkHealth = target.GetComponent<InkHealth>();
            if (inkHealth != null)
            {
                Vector2 attackDirection = (target.transform.position - projectileGameObject.transform.position).normalized;
                inkHealth.TakeDamage(ballDamage, attackDirection); // EXACT DAMAGE - NO CONVERSION
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
                if (showDamageDebug)
                {
                    Debug.Log($"Projectile dealt {ballDamage} damage to Ink {target.name} at {impactPoint}");
                }
                return;
            }

            RatKingHealth RatKingHealth = target.GetComponent<RatKingHealth>();
            if (RatKingHealth != null)
            {
                Vector2 attackDirection = (target.transform.position - projectileGameObject.transform.position).normalized;
                RatKingHealth.TakeDamage(ballDamage); // EXACT DAMAGE - NO CONVERSION
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
                if (showDamageDebug)
                {
                    Debug.Log($"Projectile dealt {ballDamage} trigger damage to Ink {target.name} at {impactPoint}");
                }
                return;
            }
            CheeseProjectile sporeMine = target.GetComponent<CheeseProjectile>();
            if (sporeMine != null)
            {

                sporeMine.TakeDamage(ballDamage, Vector2.zero, 0f);

            }
            DestructibleObject DestructibleObject = target.GetComponent<DestructibleObject>();
            if (DestructibleObject != null)
            {

                DestructibleObject.TakeDamage(ballDamage);

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
                fleaHealth.TakeDamage(ballDamage, attackDirection); // EXACT DAMAGE - NO CONVERSION
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
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
                sprayerHealth.TakeDamage(ballDamage, attackDirection); // EXACT DAMAGE - NO CONVERSION
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
                if (showDamageDebug)
                {
                    Debug.Log($"Projectile dealt {ballDamage} trigger damage to Sprayer {target.name} at {impactPoint}");
                }
                return;
            }
            FleaHealthV2 FleaHealthV2 = target.GetComponent<FleaHealthV2>();
            if (FleaHealthV2 != null)
            {
                Vector2 attackDirection = (target.transform.position - projectileGameObject.transform.position).normalized;
                FleaHealthV2.TakeDamage(ballDamage, attackDirection); // EXACT DAMAGE - NO CONVERSION
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
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
                flyHealth.TakeDamage(ballDamage, attackDirection); // EXACT DAMAGE - NO CONVERSION
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
                if (showDamageDebug)
                {
                    Debug.Log($"Projectile dealt {ballDamage} trigger damage to Fly {target.name} at {impactPoint}");
                }
                return;
            }

            InkHealth inkHealth = target.GetComponent<InkHealth>();
            if (inkHealth != null)
            {
                Vector2 attackDirection = (target.transform.position - projectileGameObject.transform.position).normalized;
                inkHealth.TakeDamage(ballDamage, attackDirection); // EXACT DAMAGE - NO CONVERSION
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
                if (showDamageDebug)
                {
                    Debug.Log($"Projectile dealt {ballDamage} trigger damage to Ink {target.name} at {impactPoint}");
                }
                return;
            }

            RatKingHealth RatKingHealth = target.GetComponent<RatKingHealth>();
            if (RatKingHealth != null)
            {
                Vector2 attackDirection = (target.transform.position - projectileGameObject.transform.position).normalized;
                RatKingHealth.TakeDamage(ballDamage); // EXACT DAMAGE - NO CONVERSION
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
                if (showDamageDebug)
                {
                    Debug.Log($"Projectile dealt {ballDamage} trigger damage to Ink {target.name} at {impactPoint}");
                }
                return;
            }
            CheeseProjectile sporeMine = target.GetComponent<CheeseProjectile>();
            if (sporeMine != null)
            {

                sporeMine.TakeDamage(ballDamage, Vector2.zero, 0f);

            }
            DestructibleObject DestructibleObject = target.GetComponent<DestructibleObject>();
            if (DestructibleObject != null)
            {

                DestructibleObject.TakeDamage(ballDamage);

            }

            if (fleaHealth == null && sprayerHealth == null && flyHealth == null && inkHealth == null && showDamageDebug)
            {
                Debug.LogWarning($"Enemy {target.name} doesn\"t have FleaHealth, SprayerHealth, FlyHealth, or InkHealth component!");
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
                Vector2 attackDirection = (enemyCollider.transform.position - (Vector3)explosionCenter).normalized;
                fleaHealth.TakeDamage(ballDamage, attackDirection); // EXACT DAMAGE - NO MULTIPLIERS
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
                if (showDamageDebug)
                {
                    Debug.Log($"Explosion dealt {ballDamage} damage to Flea {enemyCollider.name}");
                }
                continue;
            }

            SprayerHealth sprayerHealth = enemyCollider.GetComponent<SprayerHealth>();
            if (sprayerHealth != null)
            {
                Vector2 attackDirection = (enemyCollider.transform.position - (Vector3)explosionCenter).normalized;
                sprayerHealth.TakeDamage(ballDamage, attackDirection); // EXACT DAMAGE - NO MULTIPLIERS
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
                if (showDamageDebug)
                {
                    Debug.Log($"Explosion dealt {ballDamage} damage to Sprayer {enemyCollider.name}");
                }
                continue;
            }

            FleaHealthV2 FleaHealthV2 = enemyCollider.GetComponent<FleaHealthV2>();
            if (FleaHealthV2 != null)
            {
                Vector2 attackDirection = (enemyCollider.transform.position - (Vector3)explosionCenter).normalized;
                FleaHealthV2.TakeDamage(ballDamage, attackDirection); // EXACT DAMAGE - NO MULTIPLIERS
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
                if (showDamageDebug)
                {
                    Debug.Log($"Explosion dealt {ballDamage} damage to Sprayer {enemyCollider.name}");
                }
                continue;
            }

            FlyHealth flyHealth = enemyCollider.GetComponent<FlyHealth>();
            if (flyHealth != null)
            {
                Vector2 attackDirection = (enemyCollider.transform.position - (Vector3)explosionCenter).normalized;
                flyHealth.TakeDamage(ballDamage, attackDirection); // EXACT DAMAGE - NO MULTIPLIERS
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
                if (showDamageDebug)
                {
                    Debug.Log($"Explosion dealt {ballDamage} damage to Fly {enemyCollider.name}");
                }
                continue;
            }

            InkHealth inkHealth = enemyCollider.GetComponent<InkHealth>();
            if (inkHealth != null)
            {
                Vector2 attackDirection = (enemyCollider.transform.position - (Vector3)explosionCenter).normalized;
                inkHealth.TakeDamage(ballDamage, attackDirection); // EXACT DAMAGE - NO MULTIPLIERS
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
                if (showDamageDebug)
                {
                    Debug.Log($"Explosion dealt {ballDamage} damage to Ink {enemyCollider.name}");
                }
                continue;
            }

            RatKingHealth RatKingHealth = enemyCollider.GetComponent<RatKingHealth>();
            if (RatKingHealth != null)
            {
                Vector2 attackDirection = (enemyCollider.transform.position - (Vector3)explosionCenter).normalized;
                RatKingHealth.TakeDamage(ballDamage); // EXACT DAMAGE - NO MULTIPLIERS
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(ballDamage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(ballDamage);
                if (showDamageDebug)
                {
                    Debug.Log($"Explosion dealt {ballDamage} damage to Ink {enemyCollider.name}");
                }
                continue;
            }
            CheeseProjectile sporeMine = enemyCollider.GetComponent<CheeseProjectile>();
            if (sporeMine != null)
            {
              
                sporeMine.TakeDamage(ballDamage, Vector2.zero, 0f);

            }

            DestructibleObject DestructibleObject = enemyCollider.GetComponent<DestructibleObject>();
            if (DestructibleObject != null)
            {

                DestructibleObject.TakeDamage(ballDamage);

            }
            if (fleaHealth == null && sprayerHealth == null && flyHealth == null && inkHealth == null && showDamageDebug)
            {
                Debug.LogWarning($"Enemy {enemyCollider.name} doesn\"t have FleaHealth, SprayerHealth, FlyHealth, or InkHealth component!");
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

            // Draw GREEN LINE - actual projectile direction (controlled by trajectory visual point)
            Gizmos.color = Color.green;
            Vector2 launcherDirection = GetLauncherDirection();
            Gizmos.DrawRay(aimFromPos, launcherDirection * 3f);

            // Draw minimum distance circle
            Gizmos.color = IsMouseInDeadZone() ? Color.red : Color.white;
            Gizmos.DrawWireSphere(aimFromPos, minDistanceToAim);

            // Draw curved trajectory if enabled
            if (useCurvedTrajectory && trajectoryPoints.Count > 1)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < trajectoryPoints.Count - 1; i++)
                {
                    Gizmos.DrawLine(trajectoryPoints[i], trajectoryPoints[i + 1]);
                }
            }

            // Draw explosion damage radius if enabled
            if (enableDamageSystem && damageOnExplosion)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(aimFromPos, explosionDamageRadius);
            }
        }
    }
}

// Helper classes for projectile management
public class ProjectileLifecycleController : MonoBehaviour
{
    public RobustLauncherSystem launcherSystem;
    public float spawnTime;
    public bool isReal;
    public bool hasBeenDestroyed = false;
}

public class BallCollisionHandler : MonoBehaviour
{
    public RobustLauncherSystem launcherSystem;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (launcherSystem != null)
        {
            Vector2 contactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
            // We pass the entire GameObject to the handler now.
            launcherSystem.HandleCollision(collision.gameObject, contactPoint, this.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (launcherSystem != null)
        {
            // We pass the entire GameObject to the handler now.
            launcherSystem.HandleTrigger(other.gameObject, this.gameObject);
        }
    }
}


