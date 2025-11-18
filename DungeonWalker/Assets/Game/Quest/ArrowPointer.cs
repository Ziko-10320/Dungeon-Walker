// ArrowPointer.cs
using UnityEngine;

public class ArrowPointer : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public SpriteRenderer arrowSpriteRenderer;
    public Transform currentCheckpointTarget; // The checkpoint we are pointing to

    [Header("On-Screen Behaviour")]
    public bool pointDownOnScreen = true;

    [Header("Scaling Settings")]
    public float maxScale = 1.5f;
    public float minScale = 0.3f;
    public float maxDistance = 20f;
    public float minDistance = 5f;

    // Private references for the active player
    private Transform activePlayer;
    private PointerTargetSettings playerSettings;
    private CheckpointManager checkpointManager;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (arrowSpriteRenderer == null) arrowSpriteRenderer = GetComponent<SpriteRenderer>();

        checkpointManager = FindObjectOfType<CheckpointManager>();
        if (checkpointManager == null) Debug.LogError("ArrowPointer: CheckpointManager not found!");

        // You need a way to tell the arrow who the player is when the game starts or when you switch.
        // For example, if your player has a "Player" tag:
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            SetPlayer(playerObject.transform);
        }
    }

    // --- This is the most important method ---
    // Call this from your character switching logic!
    public void SetPlayer(Transform newPlayer)
    {
        activePlayer = newPlayer;
        if (activePlayer != null)
        {
            // Get the settings component from the new player
            playerSettings = activePlayer.GetComponent<PointerTargetSettings>();
            if (playerSettings == null)
            {
                Debug.LogError("The new player is missing the 'PointerTargetSettings' component!", activePlayer);
            }
        }
    }

    void Update()
    {
        // Check if everything is valid
        if (currentCheckpointTarget == null || activePlayer == null || playerSettings == null || mainCamera == null || arrowSpriteRenderer == null || checkpointManager == null)
        {
            if (arrowSpriteRenderer != null) arrowSpriteRenderer.enabled = false;
            return;
        }

        if (checkpointManager.IsTimerRunning)
        {
            arrowSpriteRenderer.enabled = false;
            return;
        }

        arrowSpriteRenderer.enabled = true;

        // --- CORE LOGIC ---
        Vector3 screenPoint = mainCamera.WorldToViewportPoint(currentCheckpointTarget.position);
        bool onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;

        Vector3 arrowPosition;
        float angle;

        // Get settings from the active player's component
        Vector3 circleCenter = activePlayer.position + (Vector3)playerSettings.arrowCenterOffset;
        float radius = playerSettings.arrowCircleRadius;

        if (onScreen)
        {
            // 1. Get the Checkpoint component from our target
            Checkpoint targetCheckpoint = currentCheckpointTarget.GetComponent<Checkpoint>();

            // 2. Calculate the base position (the checkpoint's center)
            Vector3 basePosition = currentCheckpointTarget.position;

            // 3. If we found the component, add its custom Y offset
            if (targetCheckpoint != null)
            {
                arrowPosition = basePosition + new Vector3(0, targetCheckpoint.arrowYOffset, 0);
            }
            else
            {
                // Fallback: If for some reason there's no Checkpoint script, just use the base position
                arrowPosition = basePosition;
            }

            // The angle calculation remains the same
            angle = pointDownOnScreen ? -90f : Mathf.Atan2((currentCheckpointTarget.position - transform.position).y, (currentCheckpointTarget.position - transform.position).x) * Mathf.Rad2Deg;
        }
        else
        {
            Vector3 direction = (currentCheckpointTarget.position - circleCenter).normalized;
            arrowPosition = circleCenter + (direction * radius);
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        // --- APPLY TRANSFORMATIONS ---
        transform.position = new Vector3(arrowPosition.x, arrowPosition.y, 0);
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Scale based on distance
        float distanceToTarget = Vector3.Distance(circleCenter, currentCheckpointTarget.position);
        float normalizedDistance = Mathf.InverseLerp(minDistance, maxDistance, distanceToTarget);
        float scale = Mathf.Lerp(minScale, maxScale, normalizedDistance);
        transform.localScale = Vector3.one * scale;
    }

    public void SetTarget(Transform newTarget)
    {
        currentCheckpointTarget = newTarget;
    }
}
