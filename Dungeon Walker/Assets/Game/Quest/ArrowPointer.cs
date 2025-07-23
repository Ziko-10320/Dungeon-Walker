using UnityEngine;

public class ArrowPointer : MonoBehaviour
{
    [Header("Arrow Settings")]
    public Transform target;
    public Camera mainCamera;
    public float borderOffset = 10f;
    public SpriteRenderer arrowSpriteRenderer;

    [Header("Scaling Settings")]
    public float maxScale = 1.5f;
    public float minScale = 0.3f;
    public float maxDistance = 20f; // Distance at which arrow is at max scale
    public float minDistance = 5f;  // Distance at which arrow is at min scale

    private CheckpointManager checkpointManager;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        if (arrowSpriteRenderer == null)
        {
            arrowSpriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (arrowSpriteRenderer == null)
        {
            Debug.LogError("ArrowPointer: SpriteRenderer not found. Please assign one or add it to the GameObject.");
            enabled = false;
        }

        checkpointManager = FindObjectOfType<CheckpointManager>();
        if (checkpointManager == null)
        {
            Debug.LogError("ArrowPointer: CheckpointManager not found in scene.");
        }
    }

    void Update()
    {
        if (target == null || mainCamera == null || arrowSpriteRenderer == null || checkpointManager == null)
        {
            arrowSpriteRenderer.enabled = false;
            return;
        }

        // Hide arrow if timer is running (player is in checkpoint radius)
        if (checkpointManager.IsTimerRunning)
        {
            arrowSpriteRenderer.enabled = false;
            return;
        }

        // Check if the target is within the camera\"s view
        Vector3 screenPoint = mainCamera.WorldToViewportPoint(target.position);
        bool onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;

        // Always show arrow and point to target, but scale based on distance
        arrowSpriteRenderer.enabled = true;

        Vector3 arrowPosition;
        float distanceToTarget = checkpointManager.GetDistanceToCurrentCheckpoint();

        if (onScreen)
        {
            // Target is on screen, position arrow at target location
            arrowPosition = target.position;
        }
        else
        {
            // Target is off screen, position arrow at screen edge
            Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);
            screenPos.x = Mathf.Clamp(screenPos.x, borderOffset, Screen.width - borderOffset);
            screenPos.y = Mathf.Clamp(screenPos.y, borderOffset, Screen.height - borderOffset);
            arrowPosition = mainCamera.ScreenToWorldPoint(screenPos);
        }

        // Set arrow position
        transform.position = new Vector3(arrowPosition.x, arrowPosition.y, 0);

        // Rotate arrow to point towards target
        Vector3 direction = target.position - transform.position;
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        // Scale arrow based on distance
        float normalizedDistance = Mathf.InverseLerp(minDistance, maxDistance, distanceToTarget);
        float scale = Mathf.Lerp(minScale, maxScale, normalizedDistance);
        transform.localScale = Vector3.one * scale;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target == null && arrowSpriteRenderer != null)
        {
            arrowSpriteRenderer.enabled = false;
        }
    }
}