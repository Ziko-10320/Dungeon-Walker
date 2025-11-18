using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class CompletelyFixedTrajectoryLineRenderer : MonoBehaviour
{
    [Header("Trajectory Line Settings")]
    [Tooltip("Reference to the launcher system")]
    [SerializeField] private RobustLauncherSystem launcherSystem;

    [Tooltip("Enable trajectory line visualization")]
    public bool showTrajectoryLine = true;

    [Tooltip("Number of points to calculate for trajectory")]
    [Range(10, 100)]
    public int trajectoryResolution = 30;

    [Tooltip("Time step between trajectory points")]
    [Range(0.01f, 0.2f)]
    public float timeStep = 0.1f;

    [Tooltip("Maximum trajectory distance")]
    [Range(5f, 50f)]
    public float maxTrajectoryDistance = 15f;

    [Tooltip("Maximum trajectory time")]
    [Range(1f, 10f)]
    public float maxTrajectoryTime = 3f;

    [Header("Line Renderer Settings")]
    [Tooltip("Width of the trajectory line at start")]
    [Range(0.01f, 0.5f)]
    public float lineStartWidth = 0.1f;

    [Tooltip("Width of the trajectory line at end")]
    [Range(0.01f, 0.5f)]
    public float lineEndWidth = 0.05f;

    [Tooltip("Material for the trajectory line (compatible with shader graph)")]
    public Material trajectoryMaterial;

    [Tooltip("Use gradient colors for the trajectory line")]
    public bool useGradientColors = true;

    [Tooltip("Start color of the trajectory line")]
    public Color startColor = new Color(0f, 1f, 0f, 1f); // Green

    [Tooltip("End color of the trajectory line")]
    public Color endColor = new Color(1f, 0f, 0f, 1f); // Red

    [Header("Performance Settings")]
    [Tooltip("Update frequency for trajectory calculation (updates per second)")]
    [Range(10f, 60f)]
    public float updateFrequency = 30f;

    [Tooltip("Only show trajectory when aiming is valid")]
    public bool hideWhenInvalidAim = true;

    [Tooltip("Fade out trajectory when not aiming")]
    public bool fadeWhenNotAiming = true;

    [Tooltip("Fade speed when not aiming")]
    [Range(1f, 10f)]
    public float fadeSpeed = 5f;

    [Header("Advanced Settings")]
    [Tooltip("Use physics gravity for trajectory calculation")]
    public bool usePhysicsGravity = true;

    [Tooltip("Custom gravity value (used when usePhysicsGravity is false)")]
    public float customGravity = -9.81f;

    [Tooltip("Trajectory prediction accuracy")]
    [Range(0.5f, 2f)]
    public float predictionAccuracy = 1f;

    [Tooltip("Show debug information")]
    public bool showDebugInfo = false;

    // Private variables
    private LineRenderer lineRenderer;
    private List<Vector3> trajectoryPoints = new List<Vector3>();
    private float lastUpdateTime = 0f;
    private float updateInterval;
    private bool isInitialized = false;
    private float currentAlpha = 1f;
    private Vector3 lastLaunchPosition;
    private Vector2 lastLaunchVelocity;

    // Trajectory calculation cache
    private Vector3[] pointsCache;
    private int validPointsCount = 0;

    // Color constants to avoid any color errors
    private readonly Color debugYellow = new Color(1f, 1f, 0f, 1f);
    private readonly Color debugRed = new Color(1f, 0f, 0f, 1f);
    private readonly Color debugGreen = new Color(0f, 1f, 0f, 1f);
    private readonly Color debugWhite = new Color(1f, 1f, 1f, 1f);
    private readonly Color debugBlack = new Color(0f, 0f, 0f, 1f);

    void Start()
    {
        InitializeTrajectoryLine();
    }

    void Update()
    {
        if (!isInitialized) return;

        // Calculate update interval based on frequency
        updateInterval = 1f / updateFrequency;

        // Update trajectory at specified frequency
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateTrajectoryLine();
            lastUpdateTime = Time.time;
        }

        // Handle fading
        if (fadeWhenNotAiming)
        {
            HandleTrajectoryFading();
        }
    }
    private void InitializeTrajectoryLine()
    {
        // Get or create LineRenderer component
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // Configure LineRenderer settings
        ConfigureLineRenderer();

        // Initialize points cache
        pointsCache = new Vector3[trajectoryResolution];

        // Try to find launcher system if not assigned
        if (launcherSystem == null)
        {
            launcherSystem = FindObjectOfType<RobustLauncherSystem>();
            if (launcherSystem == null)
            {
                Debug.LogWarning("CompletelyFixedTrajectoryLineRenderer: No RobustLauncherSystem found! Please assign it manually.");
                return;
            }
        }

        isInitialized = true;

        if (showDebugInfo)
        {
            Debug.Log("CompletelyFixedTrajectoryLineRenderer initialized successfully");
        }
    }

    private void ConfigureLineRenderer()
    {
        // Basic LineRenderer setup
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineStartWidth;
        lineRenderer.endWidth = lineEndWidth;

        // Set material if provided
        if (trajectoryMaterial != null)
        {
            lineRenderer.material = trajectoryMaterial;
        }

        // Configure colors properly using gradient (this is the correct way for LineRenderer)
        SetLineRendererColors();

        // Set initial state
        lineRenderer.enabled = showTrajectoryLine;
        lineRenderer.positionCount = 0;
    }

    private void SetLineRendererColors()
    {
        // Always use gradient for LineRenderer - this is the proper way
        Gradient gradient = new Gradient();

        if (useGradientColors)
        {
            // Ensure colors are properly defined
            Color safeStartColor = new Color(startColor.r, startColor.g, startColor.b, startColor.a);
            Color safeEndColor = new Color(endColor.r, endColor.g, endColor.b, endColor.a);

            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(safeStartColor, 0.0f),
                    new GradientColorKey(safeEndColor, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(safeStartColor.a, 0.0f),
                    new GradientAlphaKey(safeEndColor.a, 1.0f)
                }
            );
        }
        else
        {
            // Use single color for both start and end
            Color safeColor = new Color(startColor.r, startColor.g, startColor.b, startColor.a);

            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(safeColor, 0.0f),
                    new GradientColorKey(safeColor, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(safeColor.a, 0.0f),
                    new GradientAlphaKey(safeColor.a, 1.0f)
                }
            );
        }

        lineRenderer.colorGradient = gradient;
    }

    private void UpdateTrajectoryLine()
    {
        if (launcherSystem == null || !showTrajectoryLine)
        {
            lineRenderer.enabled = false;
            return;
        }

        // Check if aiming is valid
        bool aimingValid = launcherSystem.IsAimingValid();

        if (hideWhenInvalidAim && !aimingValid)
        {
            lineRenderer.enabled = false;
            return;
        }

        // Calculate trajectory
        CalculateTrajectoryPoints();

        // Update LineRenderer
        if (validPointsCount > 1)
        {
            lineRenderer.enabled = true;
            lineRenderer.positionCount = validPointsCount;
            lineRenderer.SetPositions(pointsCache);
        }
        else
        {
            lineRenderer.enabled = false;
        }

        if (showDebugInfo && validPointsCount > 0)
        {
            Debug.Log($"Trajectory updated: {validPointsCount} points, Distance: {Vector3.Distance(pointsCache[0], pointsCache[validPointsCount - 1]):F2}");
        }
    }

    private void CalculateTrajectoryPoints()
    {
        validPointsCount = 0;

        if (launcherSystem == null) return;

        // Get launch parameters from launcher system
        Vector3 launchPosition = GetLaunchPosition();
        Vector2 launchDirection = launcherSystem.GetLauncherDirection();
        float launchForce = launcherSystem.GetCurrentCalculatedForce();

        // Calculate initial velocity
        Vector2 initialVelocity = launchDirection * launchForce;

        // Get gravity value
        float gravity = usePhysicsGravity ? Physics2D.gravity.y : customGravity;

        // Calculate trajectory points using kinematic equations
        for (int i = 0; i < trajectoryResolution; i++)
        {
            float time = i * timeStep * predictionAccuracy;

            // Stop if we exceed maximum time
            if (time > maxTrajectoryTime)
                break;

            // Calculate position at time t using kinematic equation
            // s = ut + (1/2)at²
            Vector3 position = launchPosition;
            position.x += initialVelocity.x * time;
            position.y += initialVelocity.y * time + 0.5f * gravity * time * time;

            // Check if trajectory goes too far
            float distance = Vector3.Distance(launchPosition, position);
            if (distance > maxTrajectoryDistance)
                break;

            // Check if trajectory goes below launch position significantly (hit ground)
            if (position.y < launchPosition.y - 10f)
                break;

            // Add point to cache
            pointsCache[validPointsCount] = position;
            validPointsCount++;
        }

        // Store current launch parameters for comparison
        lastLaunchPosition = launchPosition;
        lastLaunchVelocity = initialVelocity;
    }

    private Vector3 GetLaunchPosition()
    {
        if (launcherSystem == null) return transform.position;

        // Try to get position from launcher system's projectile spawn point
        // This should be accessible through the launcher system
        return transform.position; // Fallback to this transform position
    }

    private void HandleTrajectoryFading()
    {
        if (launcherSystem == null) return;

        bool shouldShow = showTrajectoryLine && (!hideWhenInvalidAim || launcherSystem.IsAimingValid());
        float targetAlpha = shouldShow ? 1f : 0f;

        // Smooth fade transition
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);

        // Apply alpha to line renderer using gradient (proper way)
        Gradient gradient = new Gradient();

        if (useGradientColors)
        {
            // Create safe colors with alpha
            Color startColorWithAlpha = new Color(startColor.r, startColor.g, startColor.b, currentAlpha);
            Color endColorWithAlpha = new Color(endColor.r, endColor.g, endColor.b, currentAlpha);

            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(startColorWithAlpha, 0.0f),
                    new GradientColorKey(endColorWithAlpha, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(currentAlpha, 0.0f),
                    new GradientAlphaKey(currentAlpha, 1.0f)
                }
            );
        }
        else
        {
            // Single color with alpha
            Color colorWithAlpha = new Color(startColor.r, startColor.g, startColor.b, currentAlpha);

            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(colorWithAlpha, 0.0f),
                    new GradientColorKey(colorWithAlpha, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(currentAlpha, 0.0f),
                    new GradientAlphaKey(currentAlpha, 1.0f)
                }
            );
        }

        lineRenderer.colorGradient = gradient;

        // Disable line renderer if fully faded
        if (currentAlpha <= 0.01f && !shouldShow)
        {
            lineRenderer.enabled = false;
        }
        else if (currentAlpha > 0.01f && shouldShow)
        {
            lineRenderer.enabled = true;
        }
    }

    // Public methods for runtime control
    public void SetShowTrajectoryLine(bool show)
    {
        showTrajectoryLine = show;
        if (!show)
        {
            lineRenderer.enabled = false;
        }
    }

    public void SetTrajectoryResolution(int resolution)
    {
        trajectoryResolution = Mathf.Clamp(resolution, 10, 100);
        pointsCache = new Vector3[trajectoryResolution];

        if (showDebugInfo)
        {
            Debug.Log($"Trajectory resolution set to: {trajectoryResolution}");
        }
    }

    public void SetUpdateFrequency(float frequency)
    {
        updateFrequency = Mathf.Clamp(frequency, 10f, 60f);

        if (showDebugInfo)
        {
            Debug.Log($"Update frequency set to: {updateFrequency} Hz");
        }
    }

    public void SetLineWidth(float startWidth, float endWidth)
    {
        lineStartWidth = startWidth;
        lineEndWidth = endWidth;

        if (lineRenderer != null)
        {
            lineRenderer.startWidth = lineStartWidth;
            lineRenderer.endWidth = lineEndWidth;
        }
    }

    public void SetTrajectoryColors(Color start, Color end)
    {
        // Ensure colors are safe
        startColor = new Color(start.r, start.g, start.b, start.a);
        endColor = new Color(end.r, end.g, end.b, end.a);

        // Update line renderer colors using proper method
        if (lineRenderer != null)
        {
            SetLineRendererColors();
        }
    }

    public void SetTrajectoryMaterial(Material material)
    {
        trajectoryMaterial = material;

        if (lineRenderer != null && material != null)
        {
            lineRenderer.material = material;
        }
    }

    public void SetLauncherSystem(RobustLauncherSystem launcher)
    {
        launcherSystem = launcher;

        if (showDebugInfo)
        {
            Debug.Log($"Launcher system assigned: {(launcher != null ? launcher.name : "null")}");
        }
    }

    public void SetMaxTrajectoryDistance(float distance)
    {
        maxTrajectoryDistance = Mathf.Clamp(distance, 5f, 50f);

        if (showDebugInfo)
        {
            Debug.Log($"Max trajectory distance set to: {maxTrajectoryDistance}");
        }
    }

    public void SetTimeStep(float step)
    {
        timeStep = Mathf.Clamp(step, 0.01f, 0.2f);

        if (showDebugInfo)
        {
            Debug.Log($"Time step set to: {timeStep}");
        }
    }

    public void SetUsePhysicsGravity(bool usePhysics)
    {
        usePhysicsGravity = usePhysics;

        if (showDebugInfo)
        {
            Debug.Log($"Use physics gravity set to: {usePhysics}");
        }
    }

    public void SetCustomGravity(float gravity)
    {
        customGravity = gravity;

        if (showDebugInfo)
        {
            Debug.Log($"Custom gravity set to: {gravity}");
        }
    }

    // Getter methods
    public bool IsTrajectoryVisible()
    {
        return lineRenderer != null && lineRenderer.enabled && currentAlpha > 0.01f;
    }

    public int GetValidPointsCount()
    {
        return validPointsCount;
    }

    public Vector3[] GetTrajectoryPoints()
    {
        Vector3[] points = new Vector3[validPointsCount];
        for (int i = 0; i < validPointsCount; i++)
        {
            points[i] = pointsCache[i];
        }
        return points;
    }

    public float GetTrajectoryLength()
    {
        if (validPointsCount < 2) return 0f;

        float length = 0f;
        for (int i = 0; i < validPointsCount - 1; i++)
        {
            length += Vector3.Distance(pointsCache[i], pointsCache[i + 1]);
        }
        return length;
    }

    public Vector3 GetTrajectoryEndPoint()
    {
        if (validPointsCount > 0)
        {
            return pointsCache[validPointsCount - 1];
        }
        return Vector3.zero;
    }

    // Context menu methods for testing
    [ContextMenu("Toggle Trajectory Line")]
    public void ToggleTrajectoryLine()
    {
        SetShowTrajectoryLine(!showTrajectoryLine);
    }

    [ContextMenu("Reconfigure Line Renderer")]
    public void ReconfigureLineRenderer()
    {
        if (lineRenderer != null)
        {
            ConfigureLineRenderer();
            Debug.Log("Line Renderer reconfigured");
        }
    }

    [ContextMenu("Force Update Trajectory")]
    public void ForceUpdateTrajectory()
    {
        UpdateTrajectoryLine();
        Debug.Log($"Trajectory force updated: {validPointsCount} points");
    }

    [ContextMenu("Reset Colors to Default")]
    public void ResetColorsToDefault()
    {
        startColor = debugGreen; // Green
        endColor = debugRed; // Red
        SetLineRendererColors();
        Debug.Log("Colors reset to default (Green to Red)");
    }

    // Debug visualization with safe colors
    private void OnDrawGizmos()
    {
        if (!showDebugInfo || validPointsCount < 2) return;

        // Draw trajectory points as small spheres
        Gizmos.color = debugYellow;
        for (int i = 0; i < validPointsCount; i++)
        {
            Gizmos.DrawWireSphere(pointsCache[i], 0.05f);
        }

        // Draw trajectory end point
        if (validPointsCount > 0)
        {
            Gizmos.color = debugRed;
            Gizmos.DrawWireSphere(pointsCache[validPointsCount - 1], 0.1f);
        }

        // Draw launch position
        if (launcherSystem != null)
        {
            Gizmos.color = debugGreen;
            Gizmos.DrawWireSphere(GetLaunchPosition(), 0.08f);
        }
    }
}

