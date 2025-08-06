using UnityEngine;

public class FlyFollow : MonoBehaviour
{
    public Transform playerTransform;
    public float moveSpeed = 3f;
    public float minDistance = 5f;
    public float maxDistance = 7f;
    public float idealDistance = 6f; // New variable for the ideal distance
    public float heightOffset = 3f;
    public LayerMask obstacleLayer;
    public float avoidanceForce = 5f;
    public float avoidanceDistance = 2f;
    public float raycastOffset = 0.5f; // Offset for additional raycasts
    public float wallDetectionDistance = 1f; // Distance to detect walls for wall-following
    public float wallFollowingForce = 3f; // Force to apply when wall-following
    public float smoothTime = 0.1f; // For smooth movement

    // Random movement when stationary
    public float randomMoveRange = 1f;
    public float randomMoveSpeed = 1f;
    public float randomMoveInterval = 2f;

    private Rigidbody2D rb;
    private bool facingRight = true;
    private Vector2 currentVelocity = Vector2.zero;
    private float randomMoveTimer = 0f;
    private Vector2 randomTargetPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("FlyFollow: Rigidbody2D not found on this GameObject. Please add one.");
            enabled = false; // Disable the script if no Rigidbody2D is found
        }
       
        GenerateRandomTarget();
    }

    void FixedUpdate()
    {
        if (playerTransform == null || rb == null) return;

        Vector2 targetPosition = new Vector2(playerTransform.position.x, playerTransform.position.y + heightOffset);
        Vector2 currentPosition = rb.position;

        Vector2 desiredMovement = Vector2.zero;

        // Calculate direction to player
        Vector2 directionToPlayer = targetPosition - currentPosition;

        // Maintain distance
        float currentDistance = directionToPlayer.magnitude;

        if (currentDistance < minDistance)
        {
            // Move away from player if too close (below minDistance)
            desiredMovement = -directionToPlayer.normalized * moveSpeed * 1.5f; // Stronger push away
        }
        else if (currentDistance > maxDistance)
        {
            // Move towards player if too far (above maxDistance)
            desiredMovement = directionToPlayer.normalized * moveSpeed;
        }
        else if (currentDistance < idealDistance)
        {
            // If between minDistance and idealDistance, gently move away
            desiredMovement = -directionToPlayer.normalized * moveSpeed * 0.5f;
        }
        else if (currentDistance > idealDistance)
        {
            // If between idealDistance and maxDistance, gently move towards
            desiredMovement = directionToPlayer.normalized * moveSpeed * 0.5f;
        }
        else
        {
            // Exactly at idealDistance, try to stay there
            desiredMovement = Vector2.zero;
        }

        // Always adjust vertical position if not at target height
        if (Mathf.Abs(targetPosition.y - currentPosition.y) > 0.1f)
        {
            desiredMovement.y = (targetPosition.y > currentPosition.y) ? moveSpeed : -moveSpeed;
        }

        // If stationary (or very slow) and not actively moving towards/away from player, perform random movement
        if (desiredMovement.magnitude < 0.1f && rb.velocity.magnitude < 0.1f)
        {
            randomMoveTimer -= Time.fixedDeltaTime;
            if (randomMoveTimer <= 0f)
            {
                GenerateRandomTarget();
                randomMoveTimer = randomMoveInterval;
            }
            desiredMovement = (randomTargetPosition - currentPosition).normalized * randomMoveSpeed;
        }

        // Obstacle avoidance (using multiple Raycasts for better detection and wall-following)
        Vector2 avoidanceDirection = Vector2.zero;
        Vector2[] raycastDirections = {
            rb.velocity.normalized, // Forward
            Quaternion.Euler(0, 0, 30) * rb.velocity.normalized, // Forward-right (slight angle)
            Quaternion.Euler(0, 0, -30) * rb.velocity.normalized, // Forward-left (slight angle)
            Vector2.up, // Up
            Vector2.down, // Down
            Vector2.right, // Right
            Vector2.left // Left
        };

        foreach (Vector2 dir in raycastDirections)
        {
            RaycastHit2D hit = Physics2D.Raycast(currentPosition, dir, avoidanceDistance, obstacleLayer);
            if (hit.collider != null)
            {
                // Calculate avoidance force based on hit normal and distance
                Vector2 perpendicular = Vector2.Perpendicular(hit.normal);
                // Determine if we should go left or right around the obstacle
                float dot = Vector2.Dot(perpendicular, rb.velocity.normalized);
                if (dot < 0) perpendicular = -perpendicular;

                avoidanceDirection += perpendicular * (avoidanceDistance - hit.distance);
            }
        }

        // Wall following logic
        RaycastHit2D frontHit = Physics2D.Raycast(currentPosition, rb.velocity.normalized, wallDetectionDistance, obstacleLayer);
        if (frontHit.collider != null)
        {
            // If we hit a wall directly in front, try to move along it
            Vector2 wallNormal = frontHit.normal;
            Vector2 wallTangent = Vector2.Perpendicular(wallNormal);

            // Determine if we should go up or down along the wall
            float dotUp = Vector2.Dot(wallTangent, Vector2.up);
            if (dotUp < 0) wallTangent = -wallTangent; // Adjust tangent to go upwards if possible

            avoidanceDirection += wallTangent * wallFollowingForce;
        }

        // Normalize avoidance direction if there\"s any avoidance force
        if (avoidanceDirection.magnitude > 0.1f)
        {
            avoidanceDirection.Normalize();
        }

        // Combine movement and avoidance
        Vector2 finalDesiredVelocity = desiredMovement + (avoidanceDirection * avoidanceForce);

        // Apply smooth movement
        rb.velocity = Vector2.SmoothDamp(rb.velocity, finalDesiredVelocity, ref currentVelocity, smoothTime);

        // Flip logic: only flip if player surpasses the fly horizontally
        if (playerTransform.position.x > transform.position.x && !facingRight)
        {
            Flip();
        }
        else if (playerTransform.position.x < transform.position.x && facingRight)
        {
            Flip();
        }
    }

    void GenerateRandomTarget()
    {
        randomTargetPosition = rb.position + Random.insideUnitCircle * randomMoveRange;
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    // Optional: Draw Gizmos for visualization in the editor
    void OnDrawGizmosSelected()
    {
        if (rb == null) return;

        Vector2 currentPosition = rb.position;

        // Draw avoidance raycasts
        Gizmos.color = Color.cyan; // Changed to Cyan for better visibility
        Vector2[] raycastDirections = {
            rb.velocity.normalized, // Forward
            Quaternion.Euler(0, 0, 30) * rb.velocity.normalized, // Forward-right (slight angle)
            Quaternion.Euler(0, 0, -30) * rb.velocity.normalized, // Forward-left (slight angle)
            Vector2.up, // Up
            Vector2.down, // Down
            Vector2.right, // Right
            Vector2.left // Left
        };

        foreach (Vector2 dir in raycastDirections)
        {
            Gizmos.DrawRay(currentPosition, dir * avoidanceDistance);
        }

        // Draw wall detection ray
        Gizmos.color = Color.yellow; // Changed to Yellow for better visibility
        Gizmos.DrawRay(currentPosition, rb.velocity.normalized * wallDetectionDistance);

        // Draw target position
        if (playerTransform != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(new Vector2(playerTransform.position.x, playerTransform.position.y + heightOffset), 0.2f);
        }

        // Draw min/max distance circles
        if (playerTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerTransform.position, minDistance);
            Gizmos.color = Color.red; // Changed to Red for max distance for better contrast
            Gizmos.DrawWireSphere(playerTransform.position, maxDistance);

            // Draw ideal distance circle (new Gizmo)
            Gizmos.color = Color.white; // White for ideal distance
            Gizmos.DrawWireSphere(playerTransform.position, idealDistance);
        }

        // Draw random movement target
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(randomTargetPosition, 0.1f);
    }
}

