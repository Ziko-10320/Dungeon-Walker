using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SprayerFollow : MonoBehaviour
{
    private enum AIState { Wandering, Chasing }
    private AIState currentState;

    [Header("Références Essentielles")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator sprayerAnimator;
    [SerializeField] public Transform playerTransform;
    private SprayerHealth health;
    [Header("Systèmes de Particules")]
    [SerializeField] private ParticleSystem groundDustImpactParticles;
    [SerializeField] private ParticleSystem otherParticles;

    [Header("Comportement Général")]
    public bool CanMove = true;
    [SerializeField] private float wanderSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float stoppingDistance = 1.5f;
    [SerializeField] private float detectionRadius = 7f;
    [SerializeField] private float lostSightRadius = 10f;

    [Header("Paramètres d'Errance")]
    [SerializeField] private Vector2 wanderTimeRange = new Vector2(2f, 5f);
    private Coroutine wanderCoroutine;

    [Header("Paramètres de Saut")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float jumpHorizontalForce = 3f;
    [SerializeField] private float jumpCooldown = 2f;
    [SerializeField][Range(0, 1)] private float jumpChance = 0.1f;
    [SerializeField] private float jumpAnticipationDuration = 0.2f;
    private float lastJumpTime;
    private bool isAnticipatingJump = false;

    [Header("Détection d’Environnement")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float checkDistance = 0.5f; // Une seule variable pour la distance
    [SerializeField] private LayerMask whatIsGround;

    private float moveDirection = 1f;
    private float timeSinceLastFlip = 0f;
    private const float FLIP_COOLDOWN = 0.5f; // Cooldown pour éviter le double flip

    void Awake()
    {
        if (playerTransform != null)
        {
            Debug.Log("FleaFollow: Player was assigned manually. Using that target.");
        }
        else
        {
            // 2. If not, we search for any and all players in the scene.
           
            GameObject[] offlinePlayers = GameObject.FindGameObjectsWithTag("Player");

            // 3. We combine these into one single list of potential targets.
            List<GameObject> allPlayers = new List<GameObject>();
            
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
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (sprayerAnimator == null) sprayerAnimator = GetComponent<Animator>();
        health = GetComponent<SprayerHealth>();      

    }

    void Start()
    {

        ChangeState(AIState.Wandering);
    }
    void OnEnable()
    {
        PlayerInvisibility3antix.OnInvisibilityChanged += HandleInvisibility;
        PlayerInvisibility3antix.OnInvisibilityChanged += HandleInvisibility;
    }

    void OnDisable()
    {
        PlayerInvisibility.OnInvisibilityChanged += HandleInvisibility;
        PlayerInvisibility3antix.OnInvisibilityChanged += HandleInvisibility;
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
            sprayerAnimator.SetBool("IsWalking", false);
            return; // Skip AI logic
        }
        if (playerTransform == null)
        {
            // Si non, on essaie de le trouver.
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                // Si on le trouve, on stocke sa référence.
                playerTransform = playerObj.transform;
            }
            else
            {
                // Si on ne le trouve PAS, c'est qu'il n'existe pas (ou plus).
                // On arrête TOUT pour cet ennemi.
                StopMoving(); // On arrête le mouvement.
                enabled = false; // On désactive complètement le script pour éviter d'autres erreurs.
                return; // On quitte la fonction Update pour cette frame.
            }
        }

        timeSinceLastFlip += Time.deltaTime;

        if (!CanMove || isAnticipatingJump)
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
        if (playerTransform == null) return; // don't chase
        // --- NEW: ignore invisible player ---
        PlayerInvisibility invis = playerTransform.GetComponent<PlayerInvisibility>();
        PlayerInvisibility3antix invis3antix = playerTransform.GetComponent<PlayerInvisibility3antix>();
        if (invis != null && invis.IsInvisible())
        {
            ChangeState(AIState.Wandering);
            return;
        }
        if (invis3antix != null && invis3antix.IsInvisible())
        {
            ChangeState(AIState.Wandering);
            return;
        }
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
        // Logique inspirée de FleaFollow : si on est bloqué, on s'arrête et on gère la situation.
        if (IsBlocked())
        {
            StopMoving();
            // Si on est bloqué pendant la poursuite, on s'arrête simplement.
            // Si on est en errance, la coroutine gérera le retournement.
            if (currentState == AIState.Chasing)
            {
                FaceTarget(playerTransform.position); // Fait face au joueur même si bloqué
            }
            return; // Ne pas exécuter le reste du mouvement
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

        // La logique de saut reste la même
        if (Time.time > lastJumpTime + jumpCooldown && Random.value < jumpChance && !isAnticipatingJump)
        {
            StartCoroutine(JumpRoutine());
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

            // Bouge pendant un certain temps ou jusqu'à être bloqué
            while (elapsedTime < wanderTime)
            {
                if (IsBlocked()) break; // Sort de la boucle si bloqué
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Une fois bloqué ou le temps écoulé, s'arrête et se retourne
            StopMoving();
            yield return new WaitForSeconds(0.5f); // Petite pause
            if (currentState == AIState.Wandering) // Vérifie si on est toujours en errance avant de tourner
            {
                Flip();
            }
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

    private void StopMoving()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    // *** FONCTION FLIP AMÉLIORÉE ***
    // Ajout de la vérification du cooldown pour empêcher les flips multiples et rapides.
    private void Flip()
    {
        if (timeSinceLastFlip < FLIP_COOLDOWN) return; // Ne pas flipper si on vient de le faire

        moveDirection *= -1;
        transform.Rotate(0f, 180f, 0f);
        timeSinceLastFlip = 0f; // Réinitialise le timer
    }

    private void FaceTarget(Vector3 target)
    {
        float directionToTarget = Mathf.Sign(target.x - transform.position.x);
        if (directionToTarget != moveDirection)
        {
            Flip();
        }
    }

    private IEnumerator JumpRoutine()
    {
        isAnticipatingJump = true;
        StopMoving();
        sprayerAnimator.SetTrigger("JumpAnticipation");
        yield return new WaitForSeconds(jumpAnticipationDuration);

        // On s'assure de ne pas sauter dans le vide
        if (!IsBlocked())
        {
            rb.velocity = new Vector2(rb.velocity.x, 0);
            rb.AddForce(new Vector2(moveDirection * jumpHorizontalForce, jumpForce), ForceMode2D.Impulse);
            lastJumpTime = Time.time;
            sprayerAnimator.SetTrigger("Jump");
        }

        isAnticipatingJump = false;
    }

    // *** FONCTIONS DE DÉTECTION CENTRALISÉES (COMME DANS FLEAFOLLOW) ***
    private bool IsBlocked()
    {
        return !IsGroundAhead() || IsWallAhead();
    }

    private bool IsGroundAhead()
    {
        return Physics2D.Raycast(groundCheck.position, Vector2.down, checkDistance, whatIsGround);
    }

    private bool IsWallAhead()
    {
        return Physics2D.Raycast(wallCheck.position, new Vector2(moveDirection, 0), checkDistance, whatIsGround);
    }

    private void UpdateAnimation()
    {
        sprayerAnimator.SetBool("IsWalking", Mathf.Abs(rb.velocity.x) > 0.1f && CanMove && !isAnticipatingJump);
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
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + (Vector3.down * checkDistance));
        }
    }

    public void PlayGroundDustImpactParticles()
    {
        if (groundDustImpactParticles != null) groundDustImpactParticles.Play();
    }

    public void PlayOtherParticles()
    {
        if (otherParticles != null) otherParticles.Play();
    }
}


