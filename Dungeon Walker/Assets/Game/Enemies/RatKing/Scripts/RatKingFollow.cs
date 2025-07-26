using UnityEngine;
using System.Collections;

public class RatKingBoss : MonoBehaviour
{
    private enum AIState { Wandering, Chasing }
    private AIState currentState;

    [Header("Essential References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator ratKingAnimator;
    [SerializeField] private Transform playerTransform;

    [Header("General Behavior")]
    public bool CanMove = true;
    [SerializeField] private float wanderSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] public float stoppingDistance = 1.5f;
    [SerializeField] private float detectionRadius = 7f;
    [SerializeField] private float lostSightRadius = 10f;

    [Header("Wandering Parameters")]
    [SerializeField] private Vector2 wanderTimeRange = new Vector2(2f, 5f);
    private Coroutine wanderCoroutine;

    [Header("Environment Detection")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] public Transform groundCheck;
    [SerializeField] public float checkDistance = 0.5f;
    [SerializeField] public LayerMask whatIsGround;
    [SerializeField] public float groundCheckRadius = 0.2f; // New: Radius for ground check zone

    private float moveDirection = 1f;
    private float timeSinceLastFlip = 0f;
    private const float FLIP_COOLDOWN = 0.5f;

    private bool zonesDeactivated = false;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (ratKingAnimator == null) ratKingAnimator = GetComponent<Animator>();
        if (playerTransform == null || wallCheck == null || groundCheck == null)
        {
            Debug.LogError("One or more essential references are not assigned!", this);
            enabled = false;
            return;
        }
    }

    void Start()
    {
        ChangeState(AIState.Wandering);
    }

    void Update()
    {
        timeSinceLastFlip += Time.deltaTime;

        if (!CanMove)
        {
            StopMoving();
            return;
        }

        UpdateAIState();
        ExecuteCurrentState();
        UpdateAnimation();
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState) return;
        currentState = newState;

        if (wanderCoroutine != null)
        {
            StopCoroutine(wanderCoroutine);
            wanderCoroutine = null;
        }

        if (currentState == AIState.Wandering)
        {
            wanderCoroutine = StartCoroutine(WanderRoutine());
        }
    }

    private void UpdateAIState()
    {
        if (zonesDeactivated) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (currentState == AIState.Wandering)
        {
            if (distanceToPlayer < detectionRadius)
            {
                ChangeState(AIState.Chasing);
            }
        }
        else if (currentState == AIState.Chasing)
        {
            if (distanceToPlayer > lostSightRadius)
            {
                ChangeState(AIState.Wandering);
            }
        }
    }

    private void ExecuteCurrentState()
    {
        if (IsBlocked())
        {
            StopMoving();
            Flip();
            zonesDeactivated = true;
            if (currentState == AIState.Chasing)
            {
                ChangeState(AIState.Wandering);
            }
            return;
        }

        if (zonesDeactivated && Mathf.Abs(rb.velocity.x) > 0.1f)
        {
            zonesDeactivated = false;
        }

        switch (currentState)
        {
            case AIState.Wandering:
                MoveInCurrentDirection(wanderSpeed);
                break;
            case AIState.Chasing:
                HandleChasing();
                break;
        }
    }

    private void HandleChasing()
    {
        if (Vector2.Distance(transform.position, playerTransform.position) > stoppingDistance)
        {
            MoveTowards(playerTransform.position, chaseSpeed);
        }
        else
        {
            StopMoving();
            FaceTarget(playerTransform.position);
        }
    }

    private IEnumerator WanderRoutine()
    {
        while (currentState == AIState.Wandering)
        {
            float wanderTime = Random.Range(wanderTimeRange.x, wanderTimeRange.y);
            float elapsedTime = 0f;

            while (elapsedTime < wanderTime)
            {
                if (IsBlocked())
                {
                    break;
                }
                MoveInCurrentDirection(wanderSpeed);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (!IsBlocked() && currentState == AIState.Wandering)
            {
                StopMoving();
                yield return new WaitForSeconds(0.5f);
                Flip();
            }
            yield return null;
        }
    }

    private void MoveTowards(Vector3 target, float speed)
    {
        float directionToTarget = Mathf.Sign(target.x - transform.position.x);
        if (directionToTarget != moveDirection)
        {
            Flip();
        }
        MoveInCurrentDirection(speed);
    }

    private void MoveInCurrentDirection(float speed)
    {
        rb.velocity = new Vector2(moveDirection * speed, rb.velocity.y);
    }

    public void StopMoving()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    private void Flip()
    {
        if (timeSinceLastFlip < FLIP_COOLDOWN) return;

        moveDirection *= -1;
        transform.Rotate(0f, 180f, 0f);
        timeSinceLastFlip = 0f;
    }

    private void FaceTarget(Vector3 target)
    {
        float directionToTarget = Mathf.Sign(target.x - transform.position.x);
        if (directionToTarget != moveDirection)
        {
            Flip();
        }
    }

    private bool IsBlocked()
    {
        return !IsGroundAhead() || IsWallAhead();
    }

    private bool IsGroundAhead()
    {
        // Changed to OverlapCircle for zone-based ground check
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
    }

    private bool IsWallAhead()
    {
        return Physics2D.Raycast(wallCheck.position, new Vector2(moveDirection, 0), checkDistance, whatIsGround);
    }

    private void UpdateAnimation()
    {
        ratKingAnimator.SetBool("IsWalking", Mathf.Abs(rb.velocity.x) > 0.1f && CanMove);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, lostSightRadius);

        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + new Vector3(checkDistance * moveDirection, 0, 0));
        }
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            // Changed to DrawWireSphere for ground check zone visualization
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
