using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FleaFollow : MonoBehaviour
{
    private enum AIState { Patrolling, Chasing, Fallen, Wandering }
    private AIState currentState;

    [Header("Références Essentielles")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator fleaAnimator;
    [SerializeField] public Transform playerTransform;
    private FleaHealth health;

    [Header("Comportement Général (Ranges)")]
    [SerializeField] private Vector2 patrolSpeedRange = new Vector2(1.5f, 2.5f);
    [SerializeField] private Vector2 chaseSpeedRange = new Vector2(3.5f, 5f);
    [SerializeField] private Vector2 detectionRadiusRange = new Vector2(6f, 9f);
    [SerializeField] private Vector2 stopDistanceRange = new Vector2(1f, 2.5f);

    private float patrolSpeed;
    private float chaseSpeed;
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

    void Awake()
    {
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
}

void OnDisable()
{
    PlayerInvisibility.OnInvisibilityChanged -= HandleInvisibility;
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
        if (health != null && health.isStunned)
        {
            StopMoving();
            fleaAnimator.SetBool("IsWalking", false);
            return;
        }
        timeSinceLastFlip += Time.deltaTime;
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
        if (playerTransform == null) return; // don't chase


        // --- NEW: ignore invisible player ---
        PlayerInvisibility invis = playerTransform.GetComponent<PlayerInvisibility>();
        if (invis != null && invis.IsInvisible())
        {
            ChangeState(AIState.Patrolling);
            return;
        }
        float distanceToPlayer = playerTransform != null
            ? Vector2.Distance(transform.position, playerTransform.position)
            : Mathf.Infinity;

        if (transform.position.y < initialYPosition - 2f)
        {
            if (distanceToPlayer < randomDetectionRadius) ChangeState(AIState.Fallen);
            else if (currentState != AIState.Wandering) ChangeState(AIState.Wandering);
            return;
        }

        // --- CHASE LOGIC WITH HYSTERESIS ---
        if (distanceToPlayer < randomDetectionRadius)
        {
            if (transform.position.y > initialYPosition - 2f) ChangeState(AIState.Chasing);
            else ChangeState(AIState.Fallen);
        }
        else if (distanceToPlayer > randomDetectionRadius * 1.2f)
        {
            if (currentState == AIState.Chasing || currentState == AIState.Fallen)
            {
                if (transform.position.y > initialYPosition - 2f) ChangeState(AIState.Patrolling);
                else ChangeState(AIState.Wandering);
            }
        }
    }

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case AIState.Patrolling: HandlePatrolling(); break;
            case AIState.Chasing: HandleChasing(); break;
            case AIState.Fallen: HandleFallen(); break;
            case AIState.Wandering: break;
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
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (IsBlocked())
        {
            StopMoving();
            return;
        }

        if (distanceToPlayer <= randomStopDistance)
        {
            StopMoving();
            FaceTarget(playerTransform.position);
        }
        else
        {
            MoveTowards(playerTransform.position, chaseSpeed);
        }
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
