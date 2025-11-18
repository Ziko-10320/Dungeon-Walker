using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlyFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTransform;

    [Header("Base Values (used if randomizeOnStart = false)")]
    public float moveSpeed = 3f;
    public float minDistance = 5f;
    public float maxDistance = 7f;
    public LayerMask obstacleLayer ; // Default to everything
    public float idealDistance = 6f;
    public float heightOffset = 3f;
    public float avoidanceForce = 5f;
    public float avoidanceDistance = 2f;
    public float wallDetectionDistance = 1f;
    public float wallFollowingForce = 3f;
    public float smoothTime = 0.1f;
    public float randomMoveRange = 1f;
    public float randomMoveSpeed = 1f;
    public float randomMoveInterval = 2f;

    [Header("Randomization Settings")]
    public bool randomizeOnStart = true;

    public Vector2 moveSpeedRange = new Vector2(2.5f, 3.5f);
    public Vector2 minDistanceRange = new Vector2(4f, 6f);
    public Vector2 maxDistanceRange = new Vector2(6f, 8f);
    public Vector2 idealDistanceRange = new Vector2(5f, 7f);
    public Vector2 heightOffsetRange = new Vector2(2f, 4f);
    public Vector2 avoidanceForceRange = new Vector2(3f, 7f);
    public Vector2 avoidanceDistanceRange = new Vector2(1f, 3f);
    public Vector2 wallDetectionDistanceRange = new Vector2(0.5f, 2f);
    public Vector2 wallFollowingForceRange = new Vector2(1f, 4f);
    public Vector2 smoothTimeRange = new Vector2(0.05f, 0.3f);
    public Vector2 randomMoveRangeRange = new Vector2(0.5f, 2f);
    public Vector2 randomMoveSpeedRange = new Vector2(0.5f, 2f);
    public Vector2 randomMoveIntervalRange = new Vector2(1f, 3f);

    [Header("Detection Settings")]
    public float detectionRadius = 8f;
    public float lostSightRadius = 10f;
    public Transform DetectionPoint;
    private bool playerDetected = false;

    // Internals
    private Rigidbody2D rb;
    private bool facingRight = true;
    private Vector2 currentVelocity = Vector2.zero;
    private float randomMoveTimer = 0f;
    private Vector2 randomTargetPosition;
    private FlyHealth health;

    public void Initialize(Transform player)
    {
        playerTransform = player;

        // --- FAILSAFE FIX ---
        // If the player reference is STILL null, try to get it from the WaveManager directly.
        if (playerTransform == null && FindObjectOfType<WaveManager>() != null)
        {
            playerTransform = FindObjectOfType<WaveManager>().playerTransform;
        }
        // --- END OF FIX ---

        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<FlyHealth>();
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;

        if (randomizeOnStart) ApplyRandomRanges();

        playerDetected = false;
        if (!facingRight) Flip();

        randomMoveTimer = Random.Range(0f, randomMoveInterval);
        GenerateRandomTarget();
    }


    private void ApplyRandomRanges()
    {
        moveSpeed = Rand(moveSpeedRange, moveSpeed);
        minDistance = Rand(minDistanceRange, minDistance);
        maxDistance = Rand(maxDistanceRange, maxDistance);
        idealDistance = Rand(idealDistanceRange, idealDistance);

        // Ensure logical order: min <= ideal <= max
        if (minDistance > idealDistance) minDistance = idealDistance - 0.1f;
        if (idealDistance > maxDistance) maxDistance = idealDistance + 0.1f;

        heightOffset = Rand(heightOffsetRange, heightOffset);
        avoidanceForce = Rand(avoidanceForceRange, avoidanceForce);
        avoidanceDistance = Rand(avoidanceDistanceRange, avoidanceDistance);
        wallDetectionDistance = Rand(wallDetectionDistanceRange, wallDetectionDistance);
        wallFollowingForce = Rand(wallFollowingForceRange, wallFollowingForce);
        smoothTime = Rand(smoothTimeRange, smoothTime);
        randomMoveRange = Rand(randomMoveRangeRange, randomMoveRange);
        randomMoveSpeed = Rand(randomMoveSpeedRange, randomMoveSpeed);
        randomMoveInterval = Rand(randomMoveIntervalRange, randomMoveInterval);
    }

    private float Rand(Vector2 range, float fallback)
    {
        if (range.x == range.y) return range.x;
        float min = Mathf.Min(range.x, range.y);
        float max = Mathf.Max(range.x, range.y);
        return Random.Range(min, max);
    }

    void FixedUpdate()
    {
  
        Vector2 currentPosition = rb.position;
        Vector2 desiredMovement = Vector2.zero;

        if (playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(currentPosition, playerTransform.position);

            // Check detection zone
            if (!playerDetected && distanceToPlayer <= detectionRadius)
            {
                playerDetected = true; // start chasing
            }

            // Check lost sight zone
            if (playerDetected && distanceToPlayer > lostSightRadius)
            {
                playerDetected = false; // stop chasing
            }

            bool playerInvisible = false;
            PlayerInvisibility invis = playerTransform.GetComponent<PlayerInvisibility>();
            PlayerInvisibility3antix invis3antix = playerTransform.GetComponent<PlayerInvisibility3antix>();
            if (invis != null) playerInvisible = invis.IsInvisible();
            if (invis3antix != null) playerInvisible = invis3antix.IsInvisible();

            if (playerDetected && !playerInvisible)
            {
                // --- Chase player ---
                Vector2 targetPosition = new Vector2(playerTransform.position.x, playerTransform.position.y + heightOffset);
                Vector2 directionToPlayer = (targetPosition - currentPosition).normalized;

                float currentDistance = Vector2.Distance(currentPosition, targetPosition);

                if (currentDistance < minDistance)
                    desiredMovement = -directionToPlayer * moveSpeed * 1.5f; // move away if too close
                else if (currentDistance > maxDistance)
                    desiredMovement = directionToPlayer * moveSpeed; // move closer if too far
                else if (currentDistance < idealDistance)
                    desiredMovement = -directionToPlayer * moveSpeed * 0.5f; // gently move away
                else if (currentDistance > idealDistance)
                    desiredMovement = directionToPlayer * moveSpeed * 0.5f; // gently move closer
                else
                    desiredMovement = Vector2.zero;

                // Always adjust vertical position
                if (Mathf.Abs(targetPosition.y - currentPosition.y) > 0.1f)
                {
                    desiredMovement.y = (targetPosition.y > currentPosition.y) ? moveSpeed : -moveSpeed;
                }
            }
            else
            {
                // --- Idle hover ---
                float hoverSpeed = 2f;    // speed of up/down movement
                float hoverHeight = 0.5f; // amplitude of hover
                desiredMovement = new Vector2(0, Mathf.Sin(Time.time * hoverSpeed) * hoverHeight);
            }
        }
        else
        {
            // --- No player: just hover ---
            float hoverSpeed = 2f;
            float hoverHeight = 0.5f;
            desiredMovement = new Vector2(0, Mathf.Sin(Time.time * hoverSpeed) * hoverHeight);
        }

        // --- Smooth movement ---
        rb.velocity = Vector2.SmoothDamp(rb.velocity, desiredMovement, ref currentVelocity, smoothTime);

        // --- Flip to look at player (only if detected) ---
        if (playerDetected && playerTransform != null)
        {
            if (playerTransform.position.x > transform.position.x && !facingRight) Flip();
            else if (playerTransform.position.x < transform.position.x && facingRight) Flip();
        }
    }

    void GenerateRandomTarget()
    {
        randomTargetPosition = rb.position + Random.insideUnitCircle * randomMoveRange;
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x *= -1;
        transform.localScale = s;
    }

    void OnDrawGizmosSelected()
    {
        if (rb == null) return;

        Vector2 currentPosition = Application.isPlaying ? rb.position : (Vector2)transform.position;
        Vector2 forwardDir = rb.velocity.sqrMagnitude > 0.01f ? rb.velocity.normalized : Vector2.right;

        Gizmos.color = Color.cyan;
        Vector2[] rayDirs = {
            forwardDir,
            Quaternion.Euler(0,0,30) * forwardDir,
            Quaternion.Euler(0,0,-30) * forwardDir,
            Vector2.up, Vector2.down, Vector2.right, Vector2.left
        };
        foreach (Vector2 dir in rayDirs)
            Gizmos.DrawRay(currentPosition, dir * avoidanceDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(currentPosition, forwardDir * wallDetectionDistance);

        if (playerTransform != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(new Vector2(playerTransform.position.x, playerTransform.position.y + heightOffset), 0.2f);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerTransform.position, minDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerTransform.position, maxDistance);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(playerTransform.position, idealDistance);
        }

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(randomTargetPosition, 0.1f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(currentPosition, detectionRadius);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(currentPosition, lostSightRadius);
    }
}

