using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FleaFollow : MonoBehaviour
{
    private enum AIState { Patrolling, Chasing, Idle, Fallen, Wandering }
    private AIState currentState;

    [Header("Références Essentielles")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator fleaAnimator;
    [SerializeField] public Transform playerTransform;
    private FleaHealth health;

    [Header("Comportement Général (Ranges)")]
    [SerializeField] private Vector2 patrolSpeedRange = new Vector2(1.5f, 2.5f);
    [SerializeField] public Vector2 chaseSpeedRange = new Vector2(3.5f, 5f);
    [SerializeField] private Vector2 detectionRadiusRange = new Vector2(6f, 9f);
    [SerializeField] private Vector2 stopDistanceRange = new Vector2(1f, 2.5f);

    private float patrolSpeed;
    public float chaseSpeed;
    private float randomDetectionRadius;
    private float randomStopDistance;

    [Header("Paramètres d'Errance")]
    [SerializeField] private Vector2 wanderTimeRange = new Vector2(2f, 5f);
    [SerializeField] private float wanderWaitTime = 1.5f;
    private Coroutine wanderCoroutine;

    [Header("Détection d'Environnement")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float checkDistance = 0.2f;
    [SerializeField] private LayerMask platformLayer;

    private float currentMoveDirection = 1f;
    private float initialYPosition;
    private float timeSinceLastFlip = 0f;
    private const float FLIP_COOLDOWN = 0.5f;
    public bool IsChasing => currentState == AIState.Chasing;
    [Header("Performance / Time Slicing")]
    [Tooltip("How many SECONDS to wait before re-evaluating the AI state. Higher numbers = better performance.")]
    [Range(0.05f, 1.0f)]
    public float thinkInterval = 0.2f; // Think 5 times per second

    private float thinkTimer = 0f;
    void Awake()
    {
        // Awake should ONLY get references to its own components.
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (fleaAnimator == null) fleaAnimator = GetComponent<Animator>();
        health = GetComponent<FleaHealth>();
    }

    void Start()
    {
        // --- Randomize values ---
        patrolSpeed = Random.Range(patrolSpeedRange.x, patrolSpeedRange.y);
        chaseSpeed = Random.Range(chaseSpeedRange.x, chaseSpeedRange.y);
        randomDetectionRadius = Random.Range(detectionRadiusRange.x, detectionRadiusRange.y);
        randomStopDistance = Random.Range(stopDistanceRange.x, stopDistanceRange.y);
        randomStopDistance = Mathf.Clamp(randomStopDistance, 0.5f, randomDetectionRadius - 0.5f);

        if (playerTransform == null)
        {
            GameObject[] onlinePlayers = GameObject.FindGameObjectsWithTag("OnlinePlayer");
            GameObject[] offlinePlayers = GameObject.FindGameObjectsWithTag("Player");

            List<GameObject> allPlayers = new List<GameObject>();
            allPlayers.AddRange(onlinePlayers);
            allPlayers.AddRange(offlinePlayers);

            GameObject closestPlayer = null;
            float minDistance = float.MaxValue;

            foreach (GameObject player in allPlayers)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPlayer = player;
                }
            }

            if (closestPlayer != null)
            {
                playerTransform = closestPlayer.transform;
                Debug.Log("FleaFollow: Found closest player to target: " + closestPlayer.name);
            }
        }

        initialYPosition = transform.position.y;
        ChangeState(AIState.Patrolling);

        if (playerTransform == null)
        {
            Debug.LogError("Impossible de trouver le joueur ! Vérifiez vos tags 'Player' ou 'OnlinePlayer'.");
            enabled = false;
        }
    }
    void OnEnable()
{
    PlayerInvisibility.OnInvisibilityChanged += HandleInvisibility;
        PlayerInvisibility3antix.OnInvisibilityChanged += HandleInvisibility;
    }

void OnDisable()
{
    PlayerInvisibility.OnInvisibilityChanged -= HandleInvisibility;
        PlayerInvisibility3antix.OnInvisibilityChanged -= HandleInvisibility;
    }

private void HandleInvisibility(bool invisible)
{
    if (invisible)
    {
        // lose reference
        playerTransform = null;
    }
    else
    {
        // reacquire
        FindPlayerAgain();
    }
}
    public void InitializeAndReset(Transform player)
    {
        // 1. Forcefully get the player reference.
        playerTransform = player;

        // 2. Apply randomization to this instance's stats.
        patrolSpeed = Random.Range(patrolSpeedRange.x, patrolSpeedRange.y);
        chaseSpeed = Random.Range(chaseSpeedRange.x, chaseSpeedRange.y);
        randomDetectionRadius = Random.Range(detectionRadiusRange.x, detectionRadiusRange.y);
        randomStopDistance = Random.Range(stopDistanceRange.x, stopDistanceRange.y);
        // Ensure stop distance is always less than detection radius to prevent bugs.
        randomStopDistance = Mathf.Clamp(randomStopDistance, 0.5f, randomDetectionRadius - 0.5f);

        // 3. Reset all critical state variables.
        initialYPosition = transform.position.y;
        timeSinceLastFlip = 0f;

        // 4. Forcefully reset the flip and physics state.
        currentMoveDirection = 1f;
        FaceCurrentDirection(); // This visually resets the flip.
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // 5. Stop any old AI logic and start fresh.
        StopAllCoroutines();
        ChangeState(AIState.Patrolling); // Start by patrolling.

        // 6. Ensure the script is enabled.
        this.enabled = true;
    }

    private void FindPlayerAgain()
{
    GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
    GameObject closest = null;
    float minDist = float.MaxValue;

    foreach (GameObject p in players)
    {
        float d = Vector3.Distance(transform.position, p.transform.position);
        if (d < minDist)
        {
            minDist = d;
            closest = p;
        }
    }

    if (closest != null) playerTransform = closest.transform;
}

    void Update()
    {
        // --- 1. THE TIMER ---
        thinkTimer += Time.deltaTime;

        // --- 2. THE "THINKING" BLOCK ---
        // Only run the expensive AI logic if the timer has passed our interval.
        if (thinkTimer >= thinkInterval)
        {
            thinkTimer = 0f; // Reset the timer

            // This is your original Update logic, now running periodically.
            if (health != null && health.isStunned)
            {
                StopMoving();
                fleaAnimator.SetBool("IsWalking", false);
                return;
            }
            timeSinceLastFlip += thinkInterval; // Use the interval for consistent timing
            UpdateAIState();
        }

        // These parts need to run every frame for responsiveness.
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
        if (playerTransform == null) return;

        // --- THIS IS THE GUARANTEED FIX ---
        // If the AI is currently in the Idle state (stopped and waiting),
        // DO NOT let this method change its state. Let it stay Idle.
        if (currentState == AIState.Idle)
        {
            // However, if the player runs away, we should start chasing again.
            if (Vector2.Distance(transform.position, playerTransform.position) > randomStopDistance * 1.1f) // 1.1f gives a small buffer
            {
                ChangeState(AIState.Chasing);
            }
            return; // Exit the method.
        }

        PlayerInvisibility invis = playerTransform.GetComponent<PlayerInvisibility>();
        PlayerInvisibility3antix invis3antix = playerTransform.GetComponent<PlayerInvisibility3antix>();
        if (invis != null && invis.IsInvisible())
        {
            ChangeState(AIState.Patrolling);
            return;
        }
        if (invis3antix != null && invis3antix.IsInvisible())
        {
            ChangeState(AIState.Patrolling);
            return;
        }
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // This logic will now only run if the state is NOT Idle.
        if (distanceToPlayer < randomDetectionRadius)
        {
            ChangeState(AIState.Chasing);
        }
        else if (distanceToPlayer > randomDetectionRadius * 1.2f)
        {
            if (currentState == AIState.Chasing)
            {
                ChangeState(AIState.Patrolling);
            }
        }
    }

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case AIState.Patrolling:
                HandlePatrolling();
                break;
            case AIState.Chasing:
                HandleChasing();
                break;
            case AIState.Idle:
                // --- ADD THIS CASE ---
                // When Idle, we do nothing but stop and face the player.
                // This is the "stopped and waiting" state.
                StopMoving();
                FaceTarget(playerTransform.position);
                break;
            // --- END OF ADDITION ---
            case AIState.Fallen:
                HandleFallen();
                break;
            case AIState.Wandering:
                break;
        }
    }


    private void HandlePatrolling()
    {
        if (IsBlocked() && timeSinceLastFlip > FLIP_COOLDOWN)
        {
            FlipDirection();
        }
        MoveInCurrentDirection(patrolSpeed);
    }

    private void HandleChasing()
    {
        if (playerTransform == null)
        {
            ChangeState(AIState.Patrolling);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // If we are WITHIN the stopping distance...
        if (distanceToPlayer <= randomStopDistance)
        {
            // ...we change our state to Idle and do nothing else.
            ChangeState(AIState.Idle);
            return;
        }

        if (IsBlocked())
        {
            StopMoving();
            return;
        }

        MoveTowards(playerTransform.position, chaseSpeed);
    }



    private void HandleFallen()
    {
        if (IsBlocked())
        {
            ChangeState(AIState.Wandering);
            return;
        }
        MoveTowards(playerTransform.position, chaseSpeed);
    }

    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            while (!IsBlocked())
            {
                MoveInCurrentDirection(patrolSpeed);
                yield return null;
            }
            StopMoving();
            yield return new WaitForSeconds(wanderWaitTime);
            FlipDirection();
        }
    }

    private void MoveTowards(Vector3 target, float speed)
    {
        float newDirection = Mathf.Sign(target.x - transform.position.x);
        if (newDirection != currentMoveDirection && timeSinceLastFlip > FLIP_COOLDOWN)
        {
            currentMoveDirection = newDirection;
            timeSinceLastFlip = 0f;
        }
        MoveInCurrentDirection(speed);
    }

    private void MoveInCurrentDirection(float speed)
    {
        rb.velocity = new Vector2(currentMoveDirection * speed, rb.velocity.y);
        FaceCurrentDirection();
    }

    private void StopMoving()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    private void FlipDirection()
    {
        currentMoveDirection *= -1;
        timeSinceLastFlip = 0f;
    }

    private void FaceCurrentDirection()
    {
        float scaleValue = Mathf.Abs(transform.localScale.x);
        transform.localScale = new Vector3(currentMoveDirection * scaleValue, transform.localScale.y, transform.localScale.z);
    }

    private void FaceTarget(Vector3 target)
    {
        float directionToTarget = Mathf.Sign(target.x - transform.position.x);
        if (directionToTarget != Mathf.Sign(transform.localScale.x) && timeSinceLastFlip > FLIP_COOLDOWN)
        {
            float scaleValue = Mathf.Abs(transform.localScale.x);
            transform.localScale = new Vector3(directionToTarget * scaleValue, transform.localScale.y, transform.localScale.z);
            timeSinceLastFlip = 0f;
        }
    }

    private bool IsBlocked()
    {
        return !IsGroundAhead() || IsWallAhead();
    }

    private bool IsGroundAhead()
    {
        return Physics2D.Raycast(groundCheck.position, Vector2.down, checkDistance, platformLayer);
    }

    private bool IsWallAhead()
    {
        return Physics2D.Raycast(wallCheck.position, new Vector2(currentMoveDirection, 0), checkDistance, platformLayer);
    }

    private void UpdateAnimation()
    {
        fleaAnimator.SetBool("IsWalking", Mathf.Abs(rb.velocity.x) > 0.1f);
    }

    // --- DEBUG GIZMOS ---
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, randomDetectionRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, randomStopDistance);

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * checkDistance);
        }
        if (wallCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + new Vector3(checkDistance * currentMoveDirection, 0, 0));
        }
    }
}
