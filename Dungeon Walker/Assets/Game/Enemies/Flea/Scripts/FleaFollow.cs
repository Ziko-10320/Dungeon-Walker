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
    [Header("Comportement Général")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float stoppingDistance = 1.5f;
    [Tooltip("Rayon de détection quand la puce est perdue ou en errance.")]
    [SerializeField] private float detectionRadius = 7f;

    [Header("Paramètres d'Errance")]
    [Tooltip("Temps minimum/maximum que la puce marchera dans une direction en errance.")]
    [SerializeField] private Vector2 wanderTimeRange = new Vector2(2f, 5f);
    [SerializeField] private float wanderWaitTime = 1.5f;
    private Coroutine wanderCoroutine;

    [Header("Détection d'Environnement")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    [Tooltip("Distance pour les vérifications de sol et de mur.")]
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
        if (playerTransform != null)
        {
            Debug.Log("FleaFollow: Player was assigned manually. Using that target.");
        }
        else
        {
            // 2. If not, we search for any and all players in the scene.
            GameObject[] onlinePlayers = GameObject.FindGameObjectsWithTag("OnlinePlayer");
            GameObject[] offlinePlayers = GameObject.FindGameObjectsWithTag("Player");

            // 3. We combine these into one single list of potential targets.
            List<GameObject> allPlayers = new List<GameObject>();
            allPlayers.AddRange(onlinePlayers);
            allPlayers.AddRange(offlinePlayers);

            // 4. We find the player that is closest to this specific flea.
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

            // 5. If we found a closest player, we assign its transform as our target.
            if (closestPlayer != null)
            {
                playerTransform = closestPlayer.transform;
                Debug.Log("FleaFollow: Found closest player to target: " + closestPlayer.name);
            }
        }

        initialYPosition = transform.position.y;
        ChangeState(AIState.Patrolling);

        // La vérification suivante est maintenant une sécurité supplémentaire
        if (playerTransform == null)
        {
            Debug.LogError("Impossible de trouver le joueur ! Assurez-vous que votre joueur a le tag 'Player'. Puce: " + gameObject.name);
            enabled = false; // On désactive le script pour éviter plus d'erreurs.
        }
    }




    void Update()
    {
        if (health != null && health.isStunned)
        {
            StopMoving();
            fleaAnimator.SetBool("IsWalking", false);
            return; // Skip all AI logic if stunned
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
        if (transform.position.y < initialYPosition - 2f)
        {
            if (IsPlayerVisible()) ChangeState(AIState.Fallen);
            else if (currentState != AIState.Wandering) ChangeState(AIState.Wandering);
            return;
        }

        if (IsPlayerVisible())
        {
            if (transform.position.y > initialYPosition - 2f) ChangeState(AIState.Chasing);
            else ChangeState(AIState.Fallen);
        }
        else
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
        if (IsBlocked())
        {
            StopMoving();
            return;
        }
        if (Vector2.Distance(transform.position, playerTransform.position) < stoppingDistance)
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

    private bool IsPlayerVisible()
    {
        // Ajout d'une vérification de nullité pour playerTransform
        if (playerTransform == null)
        {
            Debug.LogWarning("Player Transform n'est pas assigné ou est nul pour la puce: " + gameObject.name);
            return false; // Le joueur n'est pas visible si sa référence est nulle
        }
        return Vector2.Distance(transform.position, playerTransform.position) < detectionRadius;
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

    // --- GIZMOS MODIFIÉS ICI ---
    void OnDrawGizmosSelected()
    {
        // Dessine la zone de détection comme une ligne horizontale.
        Gizmos.color = Color.yellow;
        Vector3 leftDetectPoint = transform.position - new Vector3(detectionRadius, 0, 0);
        Vector3 rightDetectPoint = transform.position + new Vector3(detectionRadius, 0, 0);
        Gizmos.DrawLine(leftDetectPoint, rightDetectPoint);
        // Ajoute des petites sphères aux extrémités pour mieux les voir.
        Gizmos.DrawWireSphere(leftDetectPoint, 0.2f);
        Gizmos.DrawWireSphere(rightDetectPoint, 0.2f);


        // Dessine les rayons de détection de l'environnement.
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
