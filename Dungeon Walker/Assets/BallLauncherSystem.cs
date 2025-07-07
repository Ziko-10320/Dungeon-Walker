using UnityEngine;
using UnityEngine.InputSystem;

public class DualOffsetLauncherAiming : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private GameObject Gun;
    [SerializeField] private GameObject Arm;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private Transform launcherAimPoint; // Specific point on the launcher for aiming
    [SerializeField] private Transform minDistancePoint; // Transform point for minimum distance visualization

    [Header("Aiming Settings")]
    [Tooltip("Maximum angle (in degrees) the gun/arm can rotate upward")]
    public float maxUpwardAngle = 80f;
    [Tooltip("Maximum angle (in degrees) the gun/arm can rotate downward")]
    public float maxDownwardAngle = 20f;
    [Tooltip("Reference to the player's transform for flip detection")]
    public Transform playerTransform;
    [Tooltip("Minimum distance required to rotate gun/arm")]
    public float minDistanceToAim = 0.5f;

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

    [Header("Auto-Calibration")]
    [Tooltip("Automatically calibrate launcher alignment on start")]
    public bool autoCalibrate = true;
    [Tooltip("Show calibration info in console")]
    public bool showCalibrationDebug = true;

    private Vector2 aimDirection; // The direction we want to aim
    private Vector2 mouseWorldPosition; // Mouse world position
    private bool isPlayerFacingRight = true; // Track player facing direction
    private Vector2 aimFromPosition; // Position we're aiming from

    // World space rotation tracking (independent of player flip)
    private float worldArmRotation = 0f;
    private float worldLauncherRotation = 0f;

    void Start()
    {
        if (autoCalibrate)
        {
            CalibrateAiming();
        }

        // Initialize min distance point position
        UpdateMinDistancePointPosition();
    }

    void Update()
    {
        HandleAiming();
        UpdateMinDistancePointPosition();

        // Force world space rotations (completely independent of player)
        ApplyWorldSpaceRotations();
    }

    private void HandleAiming()
    {
        // Get mouse position in world space
        mouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // Update player facing direction (for reference only, doesn't affect aiming)
        UpdatePlayerFacingDirection();

        // Calculate aim direction using the launcher aim point
        CalculateAimDirection();
    }

    private void UpdatePlayerFacingDirection()
    {
        // Get player facing direction from KritinaMovement script
        KritinaMovement playerMovement = playerTransform.GetComponent<KritinaMovement>();
        if (playerMovement != null)
        {
            isPlayerFacingRight = playerMovement.isFacingRight;
        }
        else
        {
            // Fallback to scale detection
            isPlayerFacingRight = playerTransform.localScale.x > 0;
        }
    }

    private void CalculateAimDirection()
    {
        // Use launcher aim point if available, otherwise use gun position
        aimFromPosition = launcherAimPoint != null ? launcherAimPoint.position : Gun.transform.position;
        Vector2 directionToMouse = (mouseWorldPosition - aimFromPosition);
        float distanceToMouse = directionToMouse.magnitude;

        // Only aim if mouse is far enough
        if (distanceToMouse < minDistanceToAim)
        {
            // Default to current world rotation when mouse is too close
            return;
        }

        // Calculate world space angle directly to mouse (completely independent of player orientation)
        float worldAngleToMouse = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;

        // Clamp the world angle
        float clampedWorldAngle = ClampWorldAngle(worldAngleToMouse);

        // Set world space rotations with direction-specific offsets
        worldArmRotation = clampedWorldAngle;

        // Apply different launcher offsets based on player direction
        float currentLauncherOffset = isPlayerFacingRight ? launcherRotationOffsetRight : launcherRotationOffsetLeft;
        worldLauncherRotation = clampedWorldAngle + currentLauncherOffset;

        // Calculate aim direction (always points toward mouse in world space)
        aimDirection = directionToMouse.normalized;
    }

    private float ClampWorldAngle(float worldAngle)
    {
        // Normalize angle to -180 to 180 range
        while (worldAngle > 180f) worldAngle -= 360f;
        while (worldAngle < -180f) worldAngle += 360f;

        // Clamp based on world space limits (not player direction)
        // Right side (0° to 90° and 270° to 360°)
        if (worldAngle >= -maxDownwardAngle && worldAngle <= maxUpwardAngle)
        {
            return Mathf.Clamp(worldAngle, -maxDownwardAngle, maxUpwardAngle);
        }
        // Left side (90° to 270°)
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
        // Apply world space rotations directly (completely ignores player flip)
        Quaternion armWorldRotation = Quaternion.Euler(0, 0, worldArmRotation);
        Quaternion launcherWorldRotation = Quaternion.Euler(0, 0, worldLauncherRotation);

        if (useInstantRotation)
        {
            // Apply instant world space rotation
            Arm.transform.rotation = armWorldRotation;
            if (independentLauncherRotation)
            {
                Gun.transform.rotation = launcherWorldRotation;
            }
            else
            {
                Gun.transform.rotation = armWorldRotation;
            }
        }
        else
        {
            // Apply smooth world space rotation
            Arm.transform.rotation = Quaternion.Lerp(Arm.transform.rotation, armWorldRotation, rotationSpeed * Time.deltaTime);
            if (independentLauncherRotation)
            {
                Gun.transform.rotation = Quaternion.Lerp(Gun.transform.rotation, launcherWorldRotation, rotationSpeed * Time.deltaTime);
            }
            else
            {
                Gun.transform.rotation = Quaternion.Lerp(Gun.transform.rotation, armWorldRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void UpdateMinDistancePointPosition()
    {
        if (minDistancePoint == null) return;

        // Position the MinDistancePoint at the minimum distance from the aim position
        // Use current aim direction for positioning
        Vector2 minDistancePosition = aimFromPosition + (aimDirection.normalized * minDistanceToAim);

        minDistancePoint.position = minDistancePosition;
    }

    private void CalibrateAiming()
    {
        if (showCalibrationDebug)
        {
            Debug.Log("=== Dual Offset Launcher Aiming Calibration ===");
            Debug.Log($"Player facing right: {isPlayerFacingRight}");
            Debug.Log($"Launcher rotation offset (Right): {launcherRotationOffsetRight}°");
            Debug.Log($"Launcher rotation offset (Left): {launcherRotationOffsetLeft}°");
            Debug.Log($"Independent launcher rotation: {independentLauncherRotation}");
            Debug.Log($"Launcher aim point assigned: {launcherAimPoint != null}");
            Debug.Log($"Min distance point assigned: {minDistancePoint != null}");
            Debug.Log($"Min distance to aim: {minDistanceToAim}");
        }

        // Set initial world space alignment (pointing right)
        worldArmRotation = 0f;
        float currentOffset = isPlayerFacingRight ? launcherRotationOffsetRight : launcherRotationOffsetLeft;
        worldLauncherRotation = currentOffset;
        aimDirection = Vector2.right;

        // Apply initial rotation
        ApplyWorldSpaceRotations();

        // Update min distance point
        UpdateMinDistancePointPosition();

        if (showCalibrationDebug)
        {
            Debug.Log($"Calibration complete - Using offset: {currentOffset}° for current direction");
        }
    }

    // Public method to manually calibrate during runtime
    [ContextMenu("Calibrate Aiming")]
    public void ManualCalibrate()
    {
        CalibrateAiming();
    }

    // Public method to adjust launcher offset for right direction during runtime
    public void SetLauncherOffsetRight(float offsetDegrees)
    {
        launcherRotationOffsetRight = offsetDegrees;
        if (isPlayerFacingRight)
        {
            worldLauncherRotation = worldArmRotation + launcherRotationOffsetRight;
        }
        if (showCalibrationDebug)
        {
            Debug.Log($"Launcher offset (Right) set to: {offsetDegrees}°");
        }
    }

    // Public method to adjust launcher offset for left direction during runtime
    public void SetLauncherOffsetLeft(float offsetDegrees)
    {
        launcherRotationOffsetLeft = offsetDegrees;
        if (!isPlayerFacingRight)
        {
            worldLauncherRotation = worldArmRotation + launcherRotationOffsetLeft;
        }
        if (showCalibrationDebug)
        {
            Debug.Log($"Launcher offset (Left) set to: {offsetDegrees}°");
        }
    }

    // Public method to get the exact direction the launcher is pointing (world space)
    public Vector2 GetLauncherDirection()
    {
        float launcherAngleRad = worldLauncherRotation * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(launcherAngleRad), Mathf.Sin(launcherAngleRad));
    }

    // Public method to get current aim direction (world space)
    public Vector2 GetAimDirection()
    {
        return aimDirection;
    }

    // Public method to check if aiming is valid
    public bool IsAimingValid()
    {
        float distanceToMouse = (mouseWorldPosition - aimFromPosition).magnitude;
        return distanceToMouse >= minDistanceToAim;
    }

    // Public method to get distance from aim point to mouse
    public float GetDistanceToMouse()
    {
        return (mouseWorldPosition - aimFromPosition).magnitude;
    }

    // Public method to check if mouse is in dead zone
    public bool IsMouseInDeadZone()
    {
        return GetDistanceToMouse() < minDistanceToAim;
    }

    // Public method to get current world rotation angles
    public float GetWorldArmRotation()
    {
        return worldArmRotation;
    }

    public float GetWorldLauncherRotation()
    {
        return worldLauncherRotation;
    }

    // Public method to get current launcher offset being used
    public float GetCurrentLauncherOffset()
    {
        return isPlayerFacingRight ? launcherRotationOffsetRight : launcherRotationOffsetLeft;
    }

    // Debug visualization
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

            // Draw aim direction from launcher aim point
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(aimFromPos, aimDirection * 3f);

            // Draw launcher direction (where projectiles will go)
            Gizmos.color = Color.green;
            Gizmos.DrawRay(gunPos, GetLauncherDirection() * 4f);

            // Draw mouse position
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(mouseWorldPosition, 0.2f);

            // Draw minimum aim distance circle
            Gizmos.color = IsMouseInDeadZone() ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(aimFromPos, minDistanceToAim);

            // Draw line from aim point to mouse
            Gizmos.color = IsAimingValid() ? Color.white : Color.red;
            Gizmos.DrawLine(aimFromPos, mouseWorldPosition);

            // Draw MinDistancePoint position if assigned
            if (minDistancePoint != null)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawWireSphere(minDistancePoint.position, 0.1f);
                Gizmos.DrawLine(aimFromPos, minDistancePoint.position);
            }

            // Draw world space rotation indicators
            Gizmos.color = Color.white;
            Vector2 worldRight = Vector2.right;
            Gizmos.DrawRay(aimFromPos, worldRight * 1f);

            // Draw current offset indicator
            Gizmos.color = isPlayerFacingRight ? Color.blue : Color.red;
            float currentOffset = GetCurrentLauncherOffset();
            Vector2 offsetDirection = new Vector2(Mathf.Cos(currentOffset * Mathf.Deg2Rad), Mathf.Sin(currentOffset * Mathf.Deg2Rad));
            Gizmos.DrawRay(aimFromPos, offsetDirection * 0.5f);
        }
    }
}
