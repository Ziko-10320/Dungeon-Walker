using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using FirstGearGames.SmoothCameraShaker;
using UnityEngine.UI;

public class BowSystems : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private GameObject BowGameObject;
    [SerializeField] private GameObject Arm;
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private Transform bowAimPoint; // Specific point on the bow for aiming
    [SerializeField] private Transform minDistancePoint; // Transform point for minimum distance visualization
    [SerializeField] private Transform trajectoryVisualPoint; // Transform point for trajectory visualization (controls green line)
    public Joystick aimJoystick;
    // --- MODIFICATION: Removed the shootButton as it's no longer needed ---
    // public Button shootButton;

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
    private List<GameObject> activeArrows = new List<GameObject>();
    private Dictionary<GameObject, List<GameObject>> arrowHitEnemies = new Dictionary<GameObject, List<GameObject>>();

    // Performance optimization variables
    private float lastAimUpdate = 0f;
    private float aimUpdateInterval = 0.02f;

    // --- MODIFICATION: This new variable tracks if we are currently using the joystick ---
    private bool isAimingWithJoystick = false;
    public ShakeData CameraShakeImpact;

    void OnEnable()
    {
        if (aimJoystick != null)
        {
            aimJoystick.gameObject.SetActive(true);
        }
    }

    void OnDisable()
    {
        if (aimJoystick != null)
        {
            aimJoystick.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        InitializeArrowPreview();
        if (autoCalibrate) CalibrateAiming();
        UpdateMinDistancePointPosition();
        UpdateTrajectoryVisualPoint();
    }

    void Update()
    {
        // --- MODIFICATION: The entire Update loop is now simplified to call one master function ---
        HandleInputAndShooting();

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
    }

    // --- MODIFICATION: This is the new, unified input handling method ---
    private void HandleInputAndShooting()
    {
        bool isAimingThisFrame = false;
        bool shootPressedThisFrame = false;
        bool shootHeldThisFrame = false;
        bool shootReleasedThisFrame = false;

        // Check if the aim joystick is being used
        bool isJoystickCurrentlyActive = aimJoystick != null && aimJoystick.Direction.sqrMagnitude > 0.1f;

        if (isJoystickCurrentlyActive)
        {
            // --- MOBILE JOYSTICK MODE ---
            isAimingThisFrame = true;

            if (!isAimingWithJoystick)
            {
                shootPressedThisFrame = true;
                isAimingWithJoystick = true;
            }

            shootHeldThisFrame = true;
            Vector3 joystickDirection = new Vector3(aimJoystick.Direction.x, aimJoystick.Direction.y, 0);
            mouseWorldPosition = bowAimPoint.position + joystickDirection * 10f;
        }
        else
        {
            // --- PC MOUSE MODE (or joystick released) ---
            if (isAimingWithJoystick)
            {
                shootReleasedThisFrame = true;
                isAimingWithJoystick = false;
            }

            isAimingThisFrame = true;
            shootPressedThisFrame = shootPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame;
            shootHeldThisFrame = shootHeldThisFrame || Mouse.current.leftButton.isPressed;
            shootReleasedThisFrame = shootReleasedThisFrame || Mouse.current.leftButton.wasReleasedThisFrame;
            mouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        }

        // --- UNIFIED AIMING LOGIC ---
        if (isAimingThisFrame)
        {
            stabilizedMouseWorldPosition = mouseWorldPosition;
            UpdatePlayerFacingDirection();
            CalculateAimDirection();
            ApplyWorldSpaceRotations();
        }

        // --- UNIFIED SHOOTING LOGIC ---
        if (shootPressedThisFrame && Time.time >= lastShootTime + shootCooldown)
        {
            isCharging = true;
            currentChargeTime = 0f;
            if (currentPreviewArrow != null) currentPreviewArrow.SetActive(false);
        }

        if (shootHeldThisFrame && isCharging)
        {
            currentChargeTime += Time.deltaTime;
            currentChargeTime = Mathf.Min(currentChargeTime, maxChargeTime);
        }

        if (shootReleasedThisFrame && isCharging)
        {
            isCharging = false;
            ShootArrow();
            lastShootTime = Time.time;
            if (currentPreviewArrow != null) currentPreviewArrow.SetActive(true);
        }
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

        if (currentPreviewArrow.GetComponent<Rigidbody2D>() != null) DestroyImmediate(currentPreviewArrow.GetComponent<Rigidbody2D>());
        foreach (Collider2D col in currentPreviewArrow.GetComponents<Collider2D>()) col.enabled = false;

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

    private void ShootArrow()
    {
        if (arrowPrefab == null || arrowSpawnPoint == null)
        {
            if (showArrowDebug) Debug.LogWarning("Arrow Prefab or Arrow Spawn Point not assigned!");
            return;
        }

        float chargePercentage = currentChargeTime / maxChargeTime;
        float arrowSpeed = Mathf.Lerp(minArrowSpeed, maxArrowSpeed, chargePercentage);
        float calculatedGravityScale = Mathf.Lerp(minGravityScale, maxGravityScale, chargePercentage);

        GameObject newArrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, BowGameObject.transform.rotation);
        activeArrows.Add(newArrow);

        ArrowLifecycleController lifecycleController = newArrow.AddComponent<ArrowLifecycleController>();
        lifecycleController.bowSystem = this;
        lifecycleController.spawnTime = Time.time;
        lifecycleController.hasBeenDestroyed = false;
        lifecycleController.chargePercentage = chargePercentage;

        Rigidbody2D arrowRb = newArrow.GetComponent<Rigidbody2D>();
        Vector2 launchDirection = GetBowDirection();

        if (randomSpread > 0f)
        {
            float spreadAngle = Random.Range(-randomSpread, randomSpread);
            launchDirection = Quaternion.Euler(0, 0, spreadAngle) * launchDirection;
        }

        if (arrowRb != null)
        {
            arrowRb.velocity = launchDirection * arrowSpeed;
            arrowRb.gravityScale = calculatedGravityScale;
        }
        else if (showArrowDebug) Debug.LogWarning($"Arrow prefab \"{arrowPrefab.name}\" doesn't have Rigidbody2D component!");

        Collider2D arrowCollider = newArrow.GetComponent<Collider2D>();
        if (arrowCollider != null) arrowCollider.isTrigger = true;
        else if (showArrowDebug) Debug.LogWarning($"Arrow prefab \"{arrowPrefab.name}\" doesn't have a Collider2D component!");

        if (showArrowDebug) Debug.Log($"Spawned {arrowPrefab.name} with speed {arrowSpeed:F2}, gravityScale {calculatedGravityScale:F2}");

        if (enableSoundEffects && shootSound != null) PlaySoundAtPosition(shootSound, arrowSpawnPoint.position, shootSoundVolume);

        ArrowRotationController arrowRotController = newArrow.AddComponent<ArrowRotationController>();
        if (arrowRotController != null) arrowRotController.rb = arrowRb;
    }
    private void UpdatePlayerFacingDirection()
    {
        if (playerTransform != null)
        {
            // This assumes you have a script like "KritinaMovement" on your player
            // that manages the facing direction. If not, it falls back to checking the scale.
            KritinaMovement playerMovement = playerTransform.GetComponentInParent<KritinaMovement>();
            if (playerMovement != null)
            {
                isPlayerFacingRight = playerMovement.isFacingRight;
            }
            else
            {
                // Fallback method if the movement script isn't found
                isPlayerFacingRight = playerTransform.localScale.x > 0;
            }
        }
    }

    private void CalculateAimDirection()
    {
        aimFromPosition = bowAimPoint != null ? bowAimPoint.position : BowGameObject.transform.position;
        Vector2 directionToMouse = (stabilizedMouseWorldPosition - aimFromPosition);
        if (directionToMouse.magnitude < minDistanceToAim)
        {
            // If the aim is within the dead zone, don't update the direction
            return;
        }

        float worldAngleToMouse = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
        float clampedWorldAngle = ClampWorldAngle(worldAngleToMouse);

        worldArmRotation = clampedWorldAngle;
        worldBowRotation = clampedWorldAngle + (isPlayerFacingRight ? bowRotationOffsetRight : bowRotationOffsetLeft);
        worldTrajectoryRotation = clampedWorldAngle + (isPlayerFacingRight ? trajectoryRotationOffsetRight : trajectoryRotationOffsetLeft);
        aimDirection = directionToMouse.normalized;
    }

    private float ClampWorldAngle(float worldAngle)
    {
        // Normalize angle to be within -180 to 180 range.
        if (worldAngle > 180) worldAngle -= 360;
        if (worldAngle < -180) worldAngle += 360;

        if (isPlayerFacingRight)
        {
            return Mathf.Clamp(worldAngle, -maxDownwardAngle, maxUpwardAngle);
        }
        else // Player is facing left
        {
            // This logic handles clamping when the player is flipped.
            // It correctly clamps the world-space angle based on the player's orientation.
            if (worldAngle > 90 || worldAngle < -90) // Aiming left
            {
                if (worldAngle > 0) // Top-left quadrant
                    return Mathf.Clamp(worldAngle, 180 - maxUpwardAngle, 180);
                else // Bottom-left quadrant
                    return Mathf.Clamp(worldAngle, -180, -180 + maxDownwardAngle);
            }
            else // Aiming right (while facing left)
            {
                return Mathf.Clamp(worldAngle, -maxDownwardAngle, maxUpwardAngle);
            }
        }
    }

    private void ApplyWorldSpaceRotations()
    {
        Quaternion armWorldRotation = Quaternion.Euler(0, 0, worldArmRotation);
        Quaternion bowWorldRotation = Quaternion.Euler(0, 0, worldBowRotation);
        Quaternion trajectoryRotation = Quaternion.Euler(0, 0, worldTrajectoryRotation);

        if (useInstantRotation)
        {
            if (Arm != null) Arm.transform.rotation = armWorldRotation;
            if (BowGameObject != null) BowGameObject.transform.rotation = independentBowRotation ? bowWorldRotation : armWorldRotation;
            if (trajectoryVisualPoint != null) trajectoryVisualPoint.rotation = trajectoryRotation;
        }
        else
        {
            float step = rotationSpeed * Time.deltaTime;
            if (Arm != null) Arm.transform.rotation = Quaternion.Lerp(Arm.transform.rotation, armWorldRotation, step);
            if (BowGameObject != null) BowGameObject.transform.rotation = Quaternion.Lerp(BowGameObject.transform.rotation, independentBowRotation ? bowWorldRotation : armWorldRotation, step);
            if (trajectoryVisualPoint != null) trajectoryVisualPoint.rotation = Quaternion.Lerp(trajectoryVisualPoint.rotation, trajectoryRotation, step);
        }
    }

    private void UpdateMinDistancePointPosition()
    {
        if (minDistancePoint == null) return;
        minDistancePoint.position = aimFromPosition + (aimDirection.normalized * minDistanceToAim);
    }

    private void UpdateTrajectoryVisualPoint()
    {
        if (trajectoryVisualPoint == null) return;
        if (arrowSpawnPoint != null) trajectoryVisualPoint.position = arrowSpawnPoint.position;
        else if (bowAimPoint != null) trajectoryVisualPoint.position = bowAimPoint.position;
        else trajectoryVisualPoint.position = BowGameObject.transform.position;
    }

    private void CalibrateAiming()
    {
        if (showCalibrationDebug) Debug.Log("=== Bow System Calibration ===");
        worldArmRotation = 0f;
        worldBowRotation = isPlayerFacingRight ? bowRotationOffsetRight : bowRotationOffsetLeft;
        worldTrajectoryRotation = isPlayerFacingRight ? trajectoryRotationOffsetRight : trajectoryRotationOffsetLeft;
        aimDirection = Vector2.right;
        ApplyWorldSpaceRotations();
        UpdateMinDistancePointPosition();
        UpdateTrajectoryVisualPoint();
        if (showCalibrationDebug) Debug.Log("Calibration complete!");
    }

    public void DestroyArrow(GameObject arrowToDestroy, Vector2 impactPosition)
    {
        if (arrowToDestroy == null) return;
        ArrowLifecycleController lc = arrowToDestroy.GetComponent<ArrowLifecycleController>();
        if (lc != null && lc.hasBeenDestroyed) return;
        if (lc != null) lc.hasBeenDestroyed = true;

        if (showDestructionDebug) Debug.Log($"Destroying arrow at {impactPosition}");

        if (arrowDestroyParticleSystem != null)
        {
            ParticleSystem ps = Instantiate(arrowDestroyParticleSystem, impactPosition, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, ps.main.duration);
        }

        activeArrows.Remove(arrowToDestroy);
        Destroy(arrowToDestroy);
    }

    public void HandleDamage(GameObject target, Vector2 impactPoint, float chargePercentage, GameObject arrowGameObject)
    {
        if (!enableDamageSystem || ((1 << target.layer) & enemyLayers) == 0) return;

        float damageToDeal = Mathf.Lerp(minArrowDamage, maxArrowDamage, chargePercentage);
        Vector2 attackDirection = (target.transform.position - arrowGameObject.transform.position).normalized;

        // Using TryGetComponent for safer access to health scripts
        if (target.TryGetComponent<FleaHealth>(out var fleaHealth)) fleaHealth.TakeDamage((int)damageToDeal, attackDirection);
        else if (target.TryGetComponent<SprayerHealth>(out var sprayerHealth)) sprayerHealth.TakeDamage((int)damageToDeal, attackDirection);
        else if (target.TryGetComponent<FlyHealth>(out var flyHealth)) flyHealth.TakeDamage((int)damageToDeal, attackDirection);
        else if (target.TryGetComponent<InkHealth>(out var inkHealth)) inkHealth.TakeDamage(Mathf.RoundToInt(damageToDeal), attackDirection, 1f);
        else if (target.TryGetComponent<RatKingHealth>(out var ratKingHealth)) ratKingHealth.TakeDamage(Mathf.RoundToInt(damageToDeal));
        else if (target.TryGetComponent<BarrelExplosion>(out var barrelExplosion)) barrelExplosion.TakeDamage(Mathf.RoundToInt(damageToDeal));
        else { if (showDamageDebug) Debug.LogWarning($"Enemy {target.name} has no recognized health component!"); return; }

        if (showDamageDebug) Debug.Log($"Arrow dealt {damageToDeal} damage to {target.name}");
        if (enableSoundEffects && enemyImpactSound != null) PlaySoundAtPosition(enemyImpactSound, impactPoint, enemyImpactSoundVolume);
    }

    private void OnDrawGizmos()
    {
        if (BowGameObject == null) return;
        Vector2 bowPos = BowGameObject.transform.position;
        Vector2 aimFromPos = bowAimPoint != null ? bowAimPoint.position : bowPos;

        if (bowAimPoint != null) { Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(bowAimPoint.position, 0.15f); }
        if (trajectoryVisualPoint != null) { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(trajectoryVisualPoint.position, 0.12f); }
        if (arrowSpawnPoint != null) { Gizmos.color = CanShoot() ? Color.green : Color.red; Gizmos.DrawWireSphere(arrowSpawnPoint.position, 0.1f); }
        if (nextArrowPreviewPoint != null) { Gizmos.color = showNextArrowPreview ? Color.yellow : Color.gray; Gizmos.DrawWireSphere(nextArrowPreviewPoint.position, 0.08f); }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(aimFromPos, aimFromPos + aimDirection * 2f);

        if (minDistancePoint != null) { Gizmos.color = Color.red; Gizmos.DrawWireSphere(minDistancePoint.position, 0.05f); }
    }

    public void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    public Vector2 GetBowDirection()
    {
        float trajectoryAngleRad = worldTrajectoryRotation * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(trajectoryAngleRad), Mathf.Sin(trajectoryAngleRad));
    }

    public bool CanShoot() => Time.time >= lastShootTime + shootCooldown && IsAimingValid();
    public bool IsAimingValid() => (stabilizedMouseWorldPosition - aimFromPosition).magnitude >= minDistanceToAim;
}

// --- Helper Scripts ---
// It's best practice to have these in their own separate files in your Unity project.

// File: ArrowLifecycleController.cs
public class ArrowLifecycleController : MonoBehaviour
{
    public BowSystems bowSystem;
    public float spawnTime;
    public bool hasBeenDestroyed = false;
    public float chargePercentage;
    private List<GameObject> hitEnemies = new List<GameObject>();

    void Update()
    {
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

        bool isEnemy = ((1 << other.gameObject.layer) & bowSystem.enemyLayers) != 0;
        bool isDestructible = ((1 << other.gameObject.layer) & bowSystem.destructionLayers) != 0;

        if (isEnemy && !hitEnemies.Contains(other.gameObject))
        {
            bowSystem.HandleDamage(other.gameObject, other.ClosestPoint(transform.position), chargePercentage, gameObject);
            hitEnemies.Add(other.gameObject);
        }
        else if (isDestructible && !isEnemy) // Destroy on walls, but not on enemies (to allow piercing)
        {
            if (bowSystem.enableCollisionDestruction)
            {
                if (bowSystem.enableSoundEffects && bowSystem.wallImpactSound != null)
                {
                    bowSystem.PlaySoundAtPosition(bowSystem.wallImpactSound, transform.position, bowSystem.wallImpactSoundVolume);
                }
                bowSystem.DestroyArrow(gameObject, transform.position);
            }
        }
    }
}

// File: ArrowRotationController.cs
public class ArrowRotationController : MonoBehaviour
{
    public Rigidbody2D rb;

    void Update()
    {
        if (rb != null && rb.velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
}
