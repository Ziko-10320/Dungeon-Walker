using FirstGearGames.SmoothCameraShaker;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RatKingAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator ratKingAnimator;
    [SerializeField] public Transform playerTransform;
    [SerializeField] private RatKingBoss ratKingBoss; // Reference to the main boss script

    [Header("Jump Attack Parameters")]
    [SerializeField] private float jumpAttackRange = 5f;
    [SerializeField] private float jumpForceY = 10f; // Initial vertical force
    [SerializeField] private float jumpCooldown = 3f;
    [SerializeField][Range(0, 1)] private float jumpProbability = 1f;
    [SerializeField] private float jumpAnticipationDuration = 0.2f;
    public ShakeData cameraShakeImpact;
    private float lastJumpTime;
    private bool isJumping = false;
    private bool isFalling = false;
    private Vector2 jumpTargetPosition; // Store player's position at jump initiation
    private float calculatedJumpXVelocity; // Dynamically calculated X velocity

    [Header("X-Axis Movement during Jump")]
    [SerializeField] private float horizontalSpeedMultiplier = 1f; // Multiplier for horizontal speed, user adjustable

    [Header("Ground Impact Effects")]
    [SerializeField] private ParticleSystem groundImpactParticles;
    [SerializeField] private Transform damageZoneOrigin;
    [SerializeField] private float damageZoneRadius = 1f;
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private LayerMask playerLayer;

    [Header("Player Knockback")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f; // This is for player's invincibility/stun, not directly used for force application here

    [Header("Consecutive Jump Attack")]
    [SerializeField][Range(0, 1)] private float consecutiveJumpChance = 0.5f; // Chance for a consecutive jump
    [SerializeField] private float consecutiveJumpDelay = 0.5f; // Delay before next jump if consecutive
    private bool canPerformConsecutiveJump = false;

    [Header("Cheese Attack Parameters")]
    [SerializeField] private float throwAttackRange = 7f;
    [SerializeField] private GameObject cheesePrefab; // Single prefab for both real and fake cheese
    [SerializeField] private Transform cheeseSpawnPoint1;
    [SerializeField] private Transform cheeseSpawnPoint2;
    [SerializeField] private float cheeseThrowSpeed = 15f; // Vitesse horizontale du fromage
    [SerializeField] private float cheeseThrowHeight = 5f; // Hauteur supplémentaire pour l'arc du lancer
    [SerializeField] private float cheeseTorque = 5f;
    [SerializeField] private ParticleSystem explosionParticlesPrefab;
    [SerializeField] private ParticleSystem cheeseImpactParticlesPrefab1; // New particle system 1
    [SerializeField] private ParticleSystem cheeseImpactParticlesPrefab2; // New particle system 2
    [SerializeField] private ParticleSystem cheeseImpactParticlesPrefab3; // New particle system 3
    [SerializeField] private float throwCooldown = 2f;
    [SerializeField][Range(0, 1)] private float throwProbability = 1f; // Set to 1 for testing
    [SerializeField] private int cheeseDamageAmount = 5; // Damage dealt by the real cheese
    [SerializeField] private float cheeseExplosionRadius = 2f; // Radius for AoE damage
    [SerializeField] private float cheeseKnockbackForce = 10f; // Knockback force from cheese explosion
    private float lastThrowTime;
    [Header("V2 Spore Mine Attack")]
    [Tooltip("If true, the boss will throw splitting spores instead of normal cheese.")]
    public bool isV2SporeAttack = false;

    [Tooltip("How long after being thrown the spore will split (in seconds).")]
    public float splitDelay = 0.4f;

    [Tooltip("The scale multiplier for the smaller spores after splitting (e.g., 0.6 for 60%).")]
    [Range(0.1f, 1.0f)]
    public float splitScaleMultiplier = 0.6f;

    [Header("V2 Spore Mine Settings")]
    [Tooltip("The material used to make the spore mines flash red.")]
    public Material sporeFlashMaterial;
    [Tooltip("How fast the mines flash (seconds between flashes).")]
    public float sporeFlashInterval = 0.2f;
    [Tooltip("How long a spore mine lasts before exploding on its own.")]
    public float sporeMineLifetime = 5f;

    [Tooltip("How much random force to apply to the smaller spores when they split.")]
    public float splitForce = 5f;
    private GameObject cheese1Instance; // Store reference to the first spawned cheese
    private GameObject cheese2Instance; // Store reference to the second spawned cheese
    private Vector3 lastPlayerPosition; // Store the player's last known position
    [Header("Animation Failsafe")]
    [Tooltip("How long the Rat King can be in a 'stuck' state before we force a reset.")]
    [SerializeField] private float maxStuckTime = 4f;
    private Coroutine animationWatchdogCoroutine;

    // Public properties to access RatKingBoss variables
    public float StoppingDistance => ratKingBoss.stoppingDistance;
    public Transform GroundCheck => ratKingBoss.groundCheck;
    public float GroundCheckRadius => ratKingBoss.groundCheckRadius;
    public LayerMask WhatIsGround => ratKingBoss.whatIsGround;
    private int frameCounter = 0;
    private int updateRate = 1; // Default to update every frame (High Priority)
    private Coroutine aiLODCoroutine;
    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (ratKingAnimator == null) ratKingAnimator = GetComponent<Animator>();
        if (ratKingBoss == null) ratKingBoss = GetComponent<RatKingBoss>();

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        if (playerTransform == null || damageZoneOrigin == null || cheeseSpawnPoint1 == null || cheeseSpawnPoint2 == null)
        {
            Debug.LogError("Essential references for RatKingAttack are not assigned!", this);
            enabled = false;
            return;
        }
    }
    void OnEnable()
    {
        // Subscribe to the events when this enemy becomes active.
        PlayerInvisibility.OnInvisibilityChanged += HandleInvisibility;
        PlayerInvisibility3antix.OnInvisibilityChanged += HandleInvisibility;
    }

    void OnDisable()
    {
        // Unsubscribe when this enemy is disabled or destroyed to prevent memory leaks.
        PlayerInvisibility.OnInvisibilityChanged -= HandleInvisibility;
        PlayerInvisibility3antix.OnInvisibilityChanged -= HandleInvisibility;
    }

    void Start()
    {
        lastJumpTime = -jumpCooldown;
        lastThrowTime = -throwCooldown;
        if (animationWatchdogCoroutine == null)
        {
            animationWatchdogCoroutine = StartCoroutine(AnimationWatchdog());
        }
    }
    private void HandleInvisibility(bool invisible)
    {
        if (invisible)
        {
            // Player is invisible. Lose the reference.
            Debug.Log("RatKingAttack: Player has become invisible. Clearing target.");
            playerTransform = null;
        }
        else
        {
            // Player is visible again. Find them.
            Debug.Log("RatKingAttack: Player is visible again. Re-acquiring target.");
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

    public void Initialize(Transform player)
    {
        // Get player reference reliably
        playerTransform = player;

        // Reset attack cooldowns
        lastJumpTime = -jumpCooldown;
        lastThrowTime = -throwCooldown;

        // Reset attack states
        isJumping = false;
        isFalling = false;
        canPerformConsecutiveJump = false;

        // Stop any leftover attack coroutines from the previous life
        StopAllCoroutines();
        aiLODCoroutine = StartCoroutine(UpdateAI_LOD_Routine());
        animationWatchdogCoroutine = StartCoroutine(AnimationWatchdog());
    }
    private IEnumerator UpdateAI_LOD_Routine()
    {
        // Create the wait object once to be efficient.
        WaitForSeconds wait = new WaitForSeconds(0.5f); // Check distance twice per second.

        while (true)
        {
            // Check for all required components first.
            if (playerTransform == null || AI_LOD_Manager.Instance == null)
            {
                // If something is missing, wait and try again.
                yield return wait;
                continue;
            }

            // If everything exists, calculate the distance and set the update rate.
            float dist = Vector2.Distance(transform.position, playerTransform.position);

            if (dist > AI_LOD_Manager.Instance.lowPriorityRange)
            {
                // Player is far away. Think very slowly.
                updateRate = AI_LOD_Manager.Instance.lowPriorityUpdateRate;
            }
            else if (dist > AI_LOD_Manager.Instance.midPriorityRange)
            {
                // Player is at a medium distance. Think a bit slower.
                updateRate = AI_LOD_Manager.Instance.midPriorityUpdateRate;
            }
            else
            {
                // Player is close. Think at maximum speed.
                updateRate = 1;
            }

            yield return wait;
        }
    }

    public bool CanPerformJumpAttack()
    {
        return !isJumping && ratKingBoss.CanMove && Time.time >= lastJumpTime + jumpCooldown;
    }

    public void PerformJumpAttack()
    {
        if (CanPerformJumpAttack())
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer > StoppingDistance && distanceToPlayer < jumpAttackRange)
            {
                if (Random.value < jumpProbability)
                {
                    StartCoroutine(JumpAttackRoutine());
                }
            }
        }
    }

    public bool CanPerformThrowAttack()
    {
        return !isJumping && ratKingBoss.CanMove && Time.time >= lastThrowTime + throwCooldown;
    }

    public void PerformThrowAttack()
    {
        // Check conditions again before triggering, in case this is called from an external script
        if (CanPerformThrowAttack())
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer > StoppingDistance && distanceToPlayer < throwAttackRange)
            {
                if (Random.value < throwProbability)
                {
                    Debug.Log("Triggering ThrowAttack animation!");
                    ratKingAnimator.SetTrigger("ThrowAttack");
                    lastThrowTime = Time.time;
                }
            }
        }
    }

    void Update()
    {
        if (playerTransform == null)
        {
            return; // Exit the Update method immediately.
        }
        frameCounter++;
        // If it's not time to "think" yet, skip the entire Update method.
        if (frameCounter < updateRate)
        {
            return;
        }
        // If it IS time to think, reset the counter and proceed with the rest of the logic.
        frameCounter = 0;
        // Update falling animation state based on vertical velocity
        if (isJumping && rb.velocity.y < -0.1f && !isFalling)
        {
            isFalling = true;
            ratKingAnimator.SetBool("IsFalling", true);
            ratKingAnimator.SetBool("IsJumping", false); // Ensure jump animation stops when falling
        }
        else if (isFalling && IsGrounded()) // If no longer falling (e.g., hit ground)
        {
            isFalling = false;
            ratKingAnimator.SetBool("IsFalling", false);
        }

        // Store player's last position for cheese throwing
        lastPlayerPosition = playerTransform.position;

        if (!isJumping && ratKingBoss.CanMove && Time.time >= lastThrowTime + throwCooldown)
        {
            // --- THIS IS THE FIX ---
            // Check for invisibility BEFORE triggering the attack.
            if (IsPlayerInvisible() || IsPlayerInvisible3antix())
            {
                // Do nothing if the player is invisible.
            }
            else if (playerTransform != null)
            // --- END OF FIX ---
            {
                float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
                if (distanceToPlayer <= throwAttackRange && distanceToPlayer > StoppingDistance)
                {
                    ratKingAnimator.SetTrigger("ThrowAttack");
                    lastThrowTime = Time.time;
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (isJumping && !IsGrounded())
        {
            // Continuously apply horizontal velocity during jump
            rb.velocity = new Vector2(calculatedJumpXVelocity * horizontalSpeedMultiplier, rb.velocity.y);
        }
    }
    private IEnumerator AnimationWatchdog()
    {
        float stuckTimer = 0f;

        // This loop runs forever in the background.
        while (true)
        {
            // We only care about getting stuck if the Rat King is in a jump or fall state.
            if (isJumping || isFalling)
            {
                // If we are in a "stuckable" state, the timer starts counting up.
                stuckTimer += Time.deltaTime;

                // If the timer exceeds our max allowed stuck time...
                if (stuckTimer > maxStuckTime)
                {
                    Debug.LogWarning($"RAT KING STUCK! In state (Jumping: {isJumping}, Falling: {isFalling}) for too long. Forcing a reset.");

                    // --- FORCE RESET ---
                    ForceResetState();

                    // Reset the timer after the fix.
                    stuckTimer = 0f;
                }
            }
            else
            {
                // If we are not in a stuckable state, the timer is always reset to zero.
                stuckTimer = 0f;
            }

            // Wait for the next frame before checking again.
            yield return null;
        }
    }
    private void ForceResetState()
    {
        // Reset all state booleans
        isJumping = false;
        isFalling = false;
        canPerformConsecutiveJump = false;

        // Allow movement again
        ratKingBoss.CanMove = true;

        // Reset the Rigidbody to prevent flying off
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.gravityScale = 1f; // Ensure gravity is normal
        }

        // Reset all animator parameters to their default "idle" state
        if (ratKingAnimator != null)
        {
            ratKingAnimator.SetBool("IsJumping", false);
            ratKingAnimator.SetBool("IsFalling", false);
            ratKingAnimator.SetTrigger("Land"); // Force a land animation to break out of jumps
            ratKingAnimator.ResetTrigger("ThrowAttack");
            ratKingAnimator.ResetTrigger("JumpAnticipation");
        }

        // Reset attack cooldowns to allow a new attack soon
        lastJumpTime = Time.time;
        lastThrowTime = Time.time;

        // Stop any attack coroutines that might be stuck
        StopCoroutine("JumpAttackRoutine");
    }
    private void ResetAnimatorStates()
    {
        if (ratKingAnimator == null) return;

        // Reset all boolean flags that could get stuck
        ratKingAnimator.SetBool("IsJumping", false);
        ratKingAnimator.SetBool("IsFalling", false);

        // Reset any triggers that might have been fired but not consumed
        ratKingAnimator.ResetTrigger("JumpAnticipation");
        ratKingAnimator.ResetTrigger("ThrowAttack");
        ratKingAnimator.ResetTrigger("Land");
    }
    private bool IsPlayerInvisible()
    {
        if (playerTransform == null) return true; // If we truly have no target, they are "invisible" to us.
        PlayerInvisibility invis = playerTransform.GetComponent<PlayerInvisibility>();
        return invis != null && invis.IsInvisible();
    }

    private bool IsPlayerInvisible3antix()
    {
        if (playerTransform == null) return true;
        PlayerInvisibility3antix invis3antix = playerTransform.GetComponent<PlayerInvisibility3antix>();
        return invis3antix != null && invis3antix.IsInvisible();
    }

    private IEnumerator JumpAttackRoutine()
    {
        if (IsPlayerInvisible() || IsPlayerInvisible3antix())
        {
            isJumping = false;
            ratKingBoss.CanMove = true;
            yield break; // Abort the jump if player is invisible.
        }
        ResetAnimatorStates();
        isJumping = true;
        ratKingBoss.CanMove = false; // Prevent other movements during jump attack
        rb.velocity = Vector2.zero; // Stop all movement before anticipation

        // Store player's last position before anticipation
        jumpTargetPosition = playerTransform.position;

        // Calculate time to reach apex and total jump duration
        float timeToApex = jumpForceY / Mathf.Abs(Physics2D.gravity.y);
        float totalJumpDuration = 2 * timeToApex; // Assuming symmetrical jump arc

        // Calculate required horizontal velocity to reach target
        float horizontalDistance = jumpTargetPosition.x - transform.position.x;
        calculatedJumpXVelocity = horizontalDistance / totalJumpDuration;

        // Anticipation animation
        ratKingAnimator.SetTrigger("JumpAnticipation");
        yield return new WaitForSeconds(jumpAnticipationDuration);

        // Perform the initial vertical jump force and calculated horizontal velocity
        ratKingAnimator.SetBool("IsJumping", true);
        rb.velocity = new Vector2(calculatedJumpXVelocity * horizontalSpeedMultiplier, jumpForceY);

        lastJumpTime = Time.time;

        // Wait until landed (velocity.y is near zero and on ground)
        yield return new WaitUntil(() => IsGrounded() && rb.velocity.y <= 0.1f); // Check for grounded and near-zero vertical velocity

        // Landed
        ratKingAnimator.SetTrigger("Land");
        ratKingAnimator.SetBool("IsJumping", false);
        ratKingAnimator.SetBool("IsFalling", false);

        // Stop horizontal movement on landing
        rb.velocity = new Vector2(0f, rb.velocity.y);

        PlayGroundImpactEffects();
        ApplyDamage();
        CameraShakerHandler.Shake(cameraShakeImpact);
        isJumping = false;
        ratKingBoss.CanMove = true; // Allow movement again

        // Determine if a consecutive jump should be performed
        canPerformConsecutiveJump = Random.value < consecutiveJumpChance;

        if (canPerformConsecutiveJump)
        {
            yield return new WaitForSeconds(consecutiveJumpDelay);
            // Directly call the routine again if consecutive jump is allowed
            StartCoroutine(JumpAttackRoutine());
        }
    }

    public void SpawnCheese1()
    {
        if (playerTransform == null) return;
        if (cheese1Instance != null) Destroy(cheese1Instance);
        if (cheese2Instance != null) Destroy(cheese2Instance);

        cheese1Instance = Instantiate(cheesePrefab, cheeseSpawnPoint1.position, Quaternion.identity);

        // --- THIS IS THE FINAL FIX ---
        // We are now calling the corrected Initialize method with the correct parameters.
        cheese1Instance.AddComponent<CheeseProjectile>().Initialize(
    true, cheeseDamageAmount, playerLayer, explosionParticlesPrefab, WhatIsGround,
    cheeseImpactParticlesPrefab1, cheeseImpactParticlesPrefab2, cheeseImpactParticlesPrefab3,
    cheeseExplosionRadius, cheeseKnockbackForce,
    isV2SporeAttack, splitDelay, splitScaleMultiplier, splitForce, playerTransform,
    sporeFlashMaterial, sporeFlashInterval, sporeMineLifetime, // The mine settings
    false
        );
        // --- END OF FIX ---
    }

    public void SpawnCheese2()
    {
        if (playerTransform == null) return;
        if (isV2SporeAttack) return;
        cheese2Instance = Instantiate(cheesePrefab, cheeseSpawnPoint2.position, Quaternion.identity);

        // --- THIS IS THE FINAL FIX (for the second cheese) ---
        cheese2Instance.AddComponent<CheeseProjectile>().Initialize(
         true, cheeseDamageAmount, playerLayer, explosionParticlesPrefab, WhatIsGround,
         cheeseImpactParticlesPrefab1, cheeseImpactParticlesPrefab2, cheeseImpactParticlesPrefab3,
         cheeseExplosionRadius, cheeseKnockbackForce,
         isV2SporeAttack, splitDelay, splitScaleMultiplier, splitForce, playerTransform,
         sporeFlashMaterial, sporeFlashInterval, sporeMineLifetime,
         false // isChildSpore
     );
        // --- END OF FIX ---
    }
    public void ThrowCheese1()
    {
        if (playerTransform == null) return;
        Debug.Log("ThrowCheese1 called!");
        if (cheese1Instance != null)
        {
            Rigidbody2D cheeseRb = cheese1Instance.GetComponent<Rigidbody2D>();
            if (cheeseRb != null)
            {
                Vector2 startPos = cheese1Instance.transform.position;
                Vector2 targetPos = lastPlayerPosition; // Target the last known player position

                // Calculate the direction and distance
                Vector2 direction = targetPos - startPos;
                float xDistance = direction.x;
                float yDistance = direction.y;

                // Calculate the initial velocity needed for a parabolic arc
                float gravity = Physics2D.gravity.y;

                // Calculate time to reach target based on horizontal distance and desired horizontal speed
                float timeToTarget = Mathf.Abs(xDistance) / cheeseThrowSpeed;
                if (timeToTarget < 0.1f) timeToTarget = 0.1f; // Avoid division by zero

                // Calculate initial vertical velocity to reach target height
                float initialVelocityY = (yDistance + cheeseThrowHeight) / timeToTarget - (0.5f * gravity * timeToTarget);

                // Apply the velocity
                cheeseRb.velocity = new Vector2(xDistance < 0 ? -cheeseThrowSpeed : cheeseThrowSpeed, initialVelocityY);
                cheeseRb.AddTorque(Random.Range(-cheeseTorque, cheeseTorque), ForceMode2D.Impulse);
                Debug.Log($"Cheese1 thrown towards player at {lastPlayerPosition} with velocity {cheeseRb.velocity}");
            }
        }
    }

    public void ThrowCheese2()
    {
        if (playerTransform == null) return;
        if (isV2SporeAttack) return;
        Debug.Log("ThrowCheese2 called!");
        if (cheese2Instance != null)
        {
            Rigidbody2D cheeseRb = cheese2Instance.GetComponent<Rigidbody2D>();
            if (cheeseRb != null)
            {
                Vector2 startPos = cheese2Instance.transform.position;
                Vector2 targetPos = lastPlayerPosition; // Target the last known player position

                // Calculate the direction and distance
                Vector2 direction = targetPos - startPos;
                float xDistance = direction.x;
                float yDistance = direction.y;

                // Calculate the initial velocity needed for a parabolic arc
                float gravity = Physics2D.gravity.y;

                // Calculate time to reach target based on horizontal distance and desired horizontal speed
                float timeToTarget = Mathf.Abs(xDistance) / cheeseThrowSpeed;
                if (timeToTarget < 0.1f) timeToTarget = 0.1f; // Avoid division by zero

                // Calculate initial vertical velocity to reach target height
                float initialVelocityY = (yDistance + cheeseThrowHeight) / timeToTarget - (0.5f * gravity * timeToTarget);

                // Apply the velocity
                cheeseRb.velocity = new Vector2(xDistance < 0 ? -cheeseThrowSpeed : cheeseThrowSpeed, initialVelocityY);
                cheeseRb.AddTorque(Random.Range(-cheeseTorque, cheeseTorque), ForceMode2D.Impulse);
                Debug.Log($"Cheese2 thrown towards player at {lastPlayerPosition} with velocity {cheeseRb.velocity}");
            }
        }
    }

    private bool IsGrounded()
    {
        // Use the public ground check logic from RatKingBoss for consistency
        return Physics2D.OverlapCircle(GroundCheck.position, GroundCheckRadius, WhatIsGround);
    }

    private void PlayGroundImpactEffects()
    {
        if (groundImpactParticles != null)
        {
            groundImpactParticles.transform.position = damageZoneOrigin.position;
            groundImpactParticles.Play();
        }
    }

    private void ApplyDamage()
    {
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(damageZoneOrigin.position, damageZoneRadius, playerLayer);
        foreach (Collider2D player in hitPlayers)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Calculate knockback direction from RatKing to player
                Vector2 knockbackDirection = (player.transform.position - transform.position).normalized;
                playerHealth.TakeDamage(damageAmount, knockbackForce, knockbackDirection);
                Debug.Log($"Player {player.name} took {damageAmount} damage from RatKing impact and was knocked back!");
            }
            L3antixHealth l3antixHealth = player.GetComponent<L3antixHealth>();
            if (l3antixHealth != null)
            {
                // Calculate knockback direction from RatKing to player
                Vector2 knockbackDirection = (player.transform.position - transform.position).normalized;
                l3antixHealth.TakeDamage(damageAmount, knockbackForce, knockbackDirection);
                Debug.Log($"Player {player.name} took {damageAmount} damage from RatKing impact and was knocked back!");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw jump attack range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, jumpAttackRange);

        // Draw throw attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, throwAttackRange);

        // Draw damage zone
        if (damageZoneOrigin != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(damageZoneOrigin.position, damageZoneRadius);
        }
        // Draw damage zone
        if (cheesePrefab != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(cheesePrefab.transform.position, cheeseExplosionRadius);
        }

        // Draw cheese spawn points
        if (cheeseSpawnPoint1 != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(cheeseSpawnPoint1.position, 0.2f);
        }
        if (cheeseSpawnPoint2 != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(cheeseSpawnPoint2.position, 0.2f);
        }
    }
}

// NEW: CheeseProjectile script (nested class for simplicity, or can be a separate file)
// This class will be added to the cheese prefabs at runtime.
public class CheeseProjectile : MonoBehaviour
{
    private bool isRealCheese;
    private int damageAmount;
    private LayerMask playerLayer;
    public LayerMask groundLayer;
    private ParticleSystem explosionParticlesPrefab;
    private ParticleSystem cheeseImpactParticlesPrefab1;
    private ParticleSystem cheeseImpactParticlesPrefab2;
    private ParticleSystem cheeseImpactParticlesPrefab3;
    private SpriteRenderer spriteRenderer;
    private float cheeseExplosionRadius;
    private float cheeseKnockbackForce;
    private bool hasBeenDestroyed = false;
    private bool isV2Spore = false;
    private float splitDelay;
    private float splitScale;
    private float splitForce;
    private bool hasSplit = false;
    private bool isMine = false;
    private Rigidbody2D rb;
    private Vector3 originalScale;
    private Transform playerTransform;
    public bool IsRealCheese { get; private set; } // Public getter for isRealCheese
    [Header("Mine Settings")]
    private Material originalMaterial;
    private Coroutine mineRoutine;
    private Material flashMaterial;
    private float flashInterval;
    private float mineLifetime;
    public void Initialize(
    // --- Standard Parameters ---
    bool isReal, int damage, LayerMask playerL, ParticleSystem explosionPrefab, LayerMask groundL,
    ParticleSystem impactPrefab1, ParticleSystem impactPrefab2, ParticleSystem impactPrefab3,
    float explosionRadius, float knockbackForce,
    // --- V2 Parameters ---
    bool isV2, float splitTime, float newScale, float newForce, Transform player,
    // --- Mine Parameters ---
    Material flashMat, float flashInt, float lifetime,
    // --- Internal State Parameter ---
    bool isChildSpore = false)
    {
        // Your existing initialization is preserved
        isRealCheese = isReal;
        damageAmount = damage;
        playerLayer = playerL;
        explosionParticlesPrefab = explosionPrefab;
        cheeseImpactParticlesPrefab1 = impactPrefab1;
        cheeseImpactParticlesPrefab2 = impactPrefab2;
        groundLayer = groundL;
        cheeseExplosionRadius = explosionRadius;
        cheeseKnockbackForce = knockbackForce;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.material;
        }

        // Store the V2 parameters
        isV2Spore = isV2;
        splitDelay = splitTime;
        splitScale = newScale;
        splitForce = newForce;
        playerTransform = player;

        // --- ASSIGN THE MINE SETTINGS ---
        this.flashMaterial = flashMat;
        this.flashInterval = flashInt;
        this.mineLifetime = lifetime;

        // Reset internal state
        hasSplit = isChildSpore;
        isMine = false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;
        }
        // Get components
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
        }

        // Only start the split routine if this is the ORIGINAL V2 spore.
        if (isV2Spore && !isChildSpore)
        {
            StartCoroutine(SplitRoutine());
        }
    }


    private IEnumerator SplitRoutine()
    {
        yield return new WaitForSeconds(splitDelay);

        if (hasBeenDestroyed) yield break;

        hasSplit = true;

        // --- THIS IS THE GUARANTEED FIX ---
        // Calculate the base direction to the player ONCE.
        Vector2 directionToPlayer = Vector2.zero;
        if (playerTransform != null)
        {
            directionToPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        }

        // This loop runs twice, once for each smaller spore.
        for (int i = 0; i < 2; i++)
        {
            GameObject smallerSpore = Instantiate(gameObject, transform.position, Quaternion.identity);
            smallerSpore.transform.localScale = originalScale * splitScale;

            CheeseProjectile smallerSporeScript = smallerSpore.GetComponent<CheeseProjectile>();
            if (smallerSporeScript != null)
            {
                // --- THIS IS THE FIX ---
                // We now pass ALL parameters, including the mine settings, to the child spores.
                smallerSporeScript.Initialize(
    true, damageAmount, playerLayer, explosionParticlesPrefab, groundLayer,
    cheeseImpactParticlesPrefab1, cheeseImpactParticlesPrefab2, cheeseImpactParticlesPrefab3,
    cheeseExplosionRadius, cheeseKnockbackForce,
    true, splitDelay, splitScale, splitForce, playerTransform,
    this.flashMaterial, this.flashInterval, this.mineLifetime, // The mine settings
    true // isChildSpore
);
            }


            Rigidbody2D smallerRb = smallerSpore.GetComponent<Rigidbody2D>();
            if (smallerRb != null)
            {
                // --- THE TRAP LOGIC ---
                // The first spore (i=0) will go left and high.
                // The second spore (i=1) will go right and high.
                float horizontalOffset = (i == 0) ? -0.5f : 0.5f;
                float verticalOffset = 0.5f; // Both spores get a high arc.

                // Create a unique offset vector for this spore.
                Vector2 spreadOffset = new Vector2(horizontalOffset, verticalOffset);

                // Add the spread offset to the base direction to the player.
                Vector2 finalDirection = (directionToPlayer + spreadOffset).normalized;

                // Give it a targeted force in its new, unique direction.
                smallerRb.AddForce(finalDirection * splitForce, ForceMode2D.Impulse);
                // --- END OF TRAP LOGIC ---
            }
        }
        // --- END OF FIX ---

        // Destroy the original large spore.
        Destroy(gameObject);
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        // If already destroyed, do nothing.
        if (hasBeenDestroyed) return;

        bool hitPlayer = ((1 << collision.gameObject.layer) & playerLayer) != 0;
        bool hitGround = ((1 << collision.gameObject.layer) & groundLayer) != 0;

        // --- SCENARIO 1: IT'S A MINE AND IT HITS THE PLAYER ---
        // This happens when the player walks into a mine that is already on the ground.
        if (isMine && hitPlayer)
        {
            Explode();
            return;
        }

        // If it's already a mine, it shouldn't react to anything else.
        if (isMine) return;

        // --- SCENARIO 2: IT'S A PROJECTILE (NOT YET A MINE) ---

        // If it hits the player mid-air, it explodes.
        if (hitPlayer)
        {
            Explode();
        }
        // If it hits the ground...
        else if (hitGround)
        {
            // ...and it's a NORMAL cheese, it explodes.
            if (!isV2Spore)
            {
                Explode();
            }
            // ...and it's a V2 SPORE, it becomes a mine.
            else
            {
                BecomeMine();
            }
        }
    }
    private void BecomeMine()
    {
        isMine = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true; // Set to trigger so the player can walk INTO it
        }

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Static; // Lock it in place
        }

        // --- THE FIX: Start the new mine behavior coroutine ---
        if (mineRoutine == null)
        {
            mineRoutine = StartCoroutine(MineBehaviorRoutine());
        }
    }
    private IEnumerator MineBehaviorRoutine()
    {
        float lifetimeTimer = 0f;
        bool isFlashing = false;

        // Loop as long as the mine exists
        while (lifetimeTimer < mineLifetime)
        {
            // Toggle the material to create a flash effect
            isFlashing = !isFlashing;
            spriteRenderer.material = isFlashing ? flashMaterial : originalMaterial;

            // Wait for the flash interval
            yield return new WaitForSeconds(flashInterval);

            // Increment the lifetime timer
            lifetimeTimer += flashInterval;
        }

        // If the loop finishes, it means 5 seconds have passed. Time to explode.
        Debug.Log("Mine lifetime expired. Self-destructing.");
        Explode();
    }

    private void Explode()
    {
        if (mineRoutine != null)
        {
            StopCoroutine(mineRoutine);
        }
        // If already destroyed, do nothing.
        if (hasBeenDestroyed) return;
        hasBeenDestroyed = true; // Mark as destroyed immediately.

        // Apply the explosion damage and knockback.
        ApplyExplosionDamageAndKnockback();
        DestroyCheese();
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        // This method is primarily for the mines.
        if (hasBeenDestroyed) return;

        bool hitPlayer = ((1 << other.gameObject.layer) & playerLayer) != 0;

        // If this is a mine AND it was touched by the player...
        if (isMine && hitPlayer)
        {
            // ...it explodes.
            ApplyExplosionDamageAndKnockback();
            DestroyCheese();
        }
    }

    private void ApplyExplosionDamageAndKnockback()
    {
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(transform.position, cheeseExplosionRadius, playerLayer);
        foreach (Collider2D player in hitPlayers)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Vector2 knockbackDirection = (player.transform.position - transform.position).normalized;
                playerHealth.TakeDamage(damageAmount, cheeseKnockbackForce, knockbackDirection);
                Debug.Log($"Player {player.name} took {damageAmount} damage from Cheese explosion and was knocked back!");
            }
            L3antixHealth l3antixHealth = player.GetComponent<L3antixHealth>();
            if (l3antixHealth != null)
            {
                Vector2 knockbackDirection = (player.transform.position - transform.position).normalized;
                l3antixHealth.TakeDamage(damageAmount, cheeseKnockbackForce, knockbackDirection);
                Debug.Log($"Player {player.name} took {damageAmount} damage from Cheese explosion and was knocked back!");
            }
        }

        LayerMask enemyLayer = LayerMask.GetMask("Enemy"); // Assumes your enemies are on a layer named "Enemy"

        // Now, we find all colliders on the "Enemy" layer within the explosion radius.
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, cheeseExplosionRadius, enemyLayer);

        // We loop through every enemy we found.
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            // This is the exact logic you provided, which is the correct way to do it.
            // We check for every possible type of enemy health script.

            FleaHealth fleaHealth = enemyCollider.GetComponent<FleaHealth>();
            if (fleaHealth != null)
            {
                Vector2 directionToEnemy = (enemyCollider.transform.position - transform.position).normalized;
                fleaHealth.TakeDamage(damageAmount, directionToEnemy, cheeseKnockbackForce);
            }

            FleaHealthV2 fleaHealthV2 = enemyCollider.GetComponent<FleaHealthV2>();
            if (fleaHealthV2 != null)
            {
                Vector2 directionToEnemy = (enemyCollider.transform.position - transform.position).normalized;
                fleaHealthV2.TakeDamage(damageAmount, directionToEnemy, cheeseKnockbackForce);
            }

            FlyHealth flyHealth = enemyCollider.GetComponent<FlyHealth>();
            if (flyHealth != null)
            {
                Vector2 directionToEnemy = (enemyCollider.transform.position - transform.position).normalized;
                flyHealth.TakeDamage(damageAmount, directionToEnemy, cheeseKnockbackForce);
            }

            SprayerHealth sprayerHealth = enemyCollider.GetComponent<SprayerHealth>();
            if (sprayerHealth != null)
            {
                Vector2 directionToEnemy = (enemyCollider.transform.position - transform.position).normalized;
                sprayerHealth.TakeDamage(damageAmount, directionToEnemy, cheeseKnockbackForce);
            }

            InkHealth inkHealth = enemyCollider.GetComponent<InkHealth>();
            if (inkHealth != null)
            {
                Vector2 directionToEnemy = (enemyCollider.transform.position - transform.position).normalized;
                inkHealth.TakeDamage(damageAmount, directionToEnemy, cheeseKnockbackForce);
            }
        }
    }

    void DestroyCheese()
    {
        if (explosionParticlesPrefab != null)
        {
            ParticleSystem explosionInstance = Instantiate(explosionParticlesPrefab, transform.position, Quaternion.identity);
            explosionInstance.Play();
            Destroy(explosionInstance.gameObject, explosionInstance.main.duration);
        }
        if (cheeseImpactParticlesPrefab1 != null)
        {
            ParticleSystem impactInstance1 = Instantiate(cheeseImpactParticlesPrefab1, transform.position, Quaternion.identity);
            impactInstance1.Play();
            Destroy(impactInstance1.gameObject, impactInstance1.main.duration);
        }
        if (cheeseImpactParticlesPrefab2 != null)
        {
            ParticleSystem impactInstance2 = Instantiate(cheeseImpactParticlesPrefab2, transform.position, Quaternion.identity);
            impactInstance2.Play();
            Destroy(impactInstance2.gameObject, impactInstance2.main.duration);
        }
        if (cheeseImpactParticlesPrefab3 != null)
        {
            ParticleSystem impactInstance3 = Instantiate(cheeseImpactParticlesPrefab3, transform.position, Quaternion.identity);
            impactInstance3.Play();
            Destroy(impactInstance3.gameObject, impactInstance3.main.duration);
        }
        Destroy(gameObject);
    }
    void OnDestroy()
    {
        // --- THIS IS THE MATERIAL FIX ---
        // If the mine coroutine was running, stop it.
        if (mineRoutine != null)
        {
            StopCoroutine(mineRoutine);
        }

        // No matter what, reset the material back to the original.
        // This prevents the "stuck on red" bug if the object is destroyed mid-flash.
        if (spriteRenderer != null && originalMaterial != null)
        {
            spriteRenderer.material = originalMaterial;
        }
        // --- END OF MATERIAL FIX ---
    }
    public void TakeDamage(int damageAmount, Vector2 attackDirection, float knockbackForce = 0f)
    {
        Explode();
    }
}



