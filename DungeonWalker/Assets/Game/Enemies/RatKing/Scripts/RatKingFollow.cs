using UnityEngine;
using System.Collections;

public class RatKingBoss : MonoBehaviour
{
    private enum AIState { Wandering, Chasing }
    private AIState currentState;

    [Header("Essential References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator ratKingAnimator;
    [SerializeField] public Transform playerTransform;
    [SerializeField] private RatKingAttack ratKingAttack; // Reference to the attack script
    private RatKingHealth health;
    [Header("General Behavior")]
    public bool CanMove = true;
    [SerializeField] private float wanderSpeed = 2f;
    public Vector2 chaseSpeedRange = new Vector2(3.5f, 4.5f); // Add this for variety
    public float chaseSpeed = 4f;
    public float stoppingDistance = 1.5f; // Made public
    [SerializeField] private float detectionRadius = 7f;
    [SerializeField] private float lostSightRadius = 10f;

    [Header("Wandering Parameters")]
    [SerializeField] private Vector2 wanderTimeRange = new Vector2(2f, 5f);
    private Coroutine wanderCoroutine;

    [Header("Environment Detection")]
    [SerializeField] private Transform wallCheck;
    public Transform groundCheck; // Made public
    [SerializeField] private float checkDistance = 0.5f;
    public LayerMask whatIsGround; // Made public
    public float groundCheckRadius = 0.2f; // Made public

    private float moveDirection = 1f;
    private float timeSinceLastFlip = 0f;
    private const float FLIP_COOLDOWN = 0.5f;

    private bool zonesDeactivated = false;
 
    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (ratKingAnimator == null) ratKingAnimator = GetComponent<Animator>();
        if (ratKingAttack == null) ratKingAttack = GetComponent<RatKingAttack>();
        health = GetComponent<RatKingHealth>();
        if ( wallCheck == null || groundCheck == null)
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

    public void Initialize(Transform player)
    {
        // Get player reference reliably
        playerTransform = player;

        // Reset core AI state
        CanMove = true;
        zonesDeactivated = false;
        timeSinceLastFlip = 0f;

        // --- NEW, MORE ROBUST FLIP RESET ---
        // 1. Force the internal move direction to default (1 for right).
        moveDirection = 1f;

        // 2. Force the visual scale to its default, non-flipped state.
        // This is more reliable than using rotation for 2D sprites.
        Vector3 localScale = transform.localScale;
        localScale.x = Mathf.Abs(localScale.x); // Ensures the X scale is always positive
        transform.localScale = localScale;
        // --- END OF THE NEW FIX ---

        // Stop all previous AI coroutines and start fresh
        if (wanderCoroutine != null)
        {
            StopCoroutine(wanderCoroutine);
        }
        StopAllCoroutines(); // A failsafe to stop any other routines
        ChangeState(AIState.Wandering); // Always start in the Wandering state
    }
    private void HandleInvisibility(bool invisible)
    {
        if (invisible)
        {
            // Player is invisible. Lose the reference and go back to wandering.
            Debug.Log("RatKing: Player has become invisible. Losing target.");
            playerTransform = null;
            ChangeState(AIState.Wandering); // Force the AI to go back to its passive state.
        }
        else
        {
            // Player is visible again. Find them.
            Debug.Log("RatKing: Player is visible again. Re-acquiring target.");
            FindPlayerAgain();
        }
    }

    private void FindPlayerAgain()
    {
        // This is the same robust logic to find the player.
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            playerTransform = p.transform;
        }
    }

    void Update()
    {
        if (health != null && health.isStunned)
        {
            StopMoving();
            ratKingAnimator.SetBool("IsWalking", false);
            return; // Skip AI logic
        }
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
    private bool IsPlayerInvisible()
    {
        if (playerTransform == null) return true;
        PlayerInvisibility invis = playerTransform.GetComponent<PlayerInvisibility>();
        return invis != null && invis.IsInvisible();
    }

    private bool IsPlayerInvisible3antix()
    {
        if (playerTransform == null) return true;
        PlayerInvisibility3antix invis3antix = playerTransform.GetComponent<PlayerInvisibility3antix>();
        return invis3antix != null && invis3antix.IsInvisible();
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
        if (IsPlayerInvisible() || IsPlayerInvisible3antix())
        {
            // If they are, and we are currently chasing, switch to wandering.
            if (currentState == AIState.Chasing)
            {
                ChangeState(AIState.Wandering);
            }
            return; // Do not proceed with any other AI logic.
        }
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
        // Check for jump attack first if in Chasing state and not blocked
        if (currentState == AIState.Chasing && !IsBlocked())
        {
            if (ratKingAttack != null && ratKingAttack.CanPerformJumpAttack())
            {
                ratKingAttack.PerformJumpAttack();
                return; // Prevent other movement logic if jump attack is initiated
            }
        }

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
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
