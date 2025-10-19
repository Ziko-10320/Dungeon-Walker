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

    [Tooltip("How much random force to apply to the smaller spores when they split.")]
    public float splitForce = 5f;
    private GameObject cheese1Instance; // Store reference to the first spawned cheese
    private GameObject cheese2Instance; // Store reference to the second spawned cheese
    private Vector3 lastPlayerPosition; // Store the player's last known position

    // Public properties to access RatKingBoss variables
    public float StoppingDistance => ratKingBoss.stoppingDistance;
    public Transform GroundCheck => ratKingBoss.groundCheck;
    public float GroundCheckRadius => ratKingBoss.groundCheckRadius;
    public LayerMask WhatIsGround => ratKingBoss.whatIsGround;

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

    void Start()
    {
        lastJumpTime = -jumpCooldown;
        lastThrowTime = -throwCooldown;
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

        // SIMPLIFIED THROW ATTACK TRIGGER - ALWAYS TRY TO THROW IF CONDITIONS ARE MET
        if (!isJumping && ratKingBoss.CanMove && Time.time >= lastThrowTime + throwCooldown)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            Debug.Log($"Distance to player: {distanceToPlayer}, ThrowRange: {throwAttackRange}, StoppingDistance: {StoppingDistance}");

            if (distanceToPlayer <= throwAttackRange)
            {
                Debug.Log("TRIGGERING THROW ATTACK ANIMATION!");
                ratKingAnimator.SetTrigger("ThrowAttack");
                lastThrowTime = Time.time;
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

    private IEnumerator JumpAttackRoutine()
    {
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
        if (cheese1Instance != null) Destroy(cheese1Instance);
        if (cheese2Instance != null) Destroy(cheese2Instance);

        cheese1Instance = Instantiate(cheesePrefab, cheeseSpawnPoint1.position, Quaternion.identity);

        // --- THIS IS THE FINAL FIX ---
        // We are now calling the corrected Initialize method with the correct parameters.
        cheese1Instance.AddComponent<CheeseProjectile>().Initialize(
            true, cheeseDamageAmount, playerLayer, explosionParticlesPrefab, WhatIsGround,
            cheeseImpactParticlesPrefab1, cheeseImpactParticlesPrefab2, cheeseImpactParticlesPrefab3,
            cheeseExplosionRadius, cheeseKnockbackForce,
            isV2SporeAttack, splitDelay, splitScaleMultiplier, splitForce, playerTransform
        );
        // --- END OF FIX ---
    }

    public void SpawnCheese2()
    {
        cheese2Instance = Instantiate(cheesePrefab, cheeseSpawnPoint2.position, Quaternion.identity);

        // --- THIS IS THE FINAL FIX (for the second cheese) ---
        cheese2Instance.AddComponent<CheeseProjectile>().Initialize(
            true, cheeseDamageAmount, playerLayer, explosionParticlesPrefab, WhatIsGround,
            cheeseImpactParticlesPrefab1,cheeseImpactParticlesPrefab2, cheeseImpactParticlesPrefab3,
            cheeseExplosionRadius, cheeseKnockbackForce,
            isV2SporeAttack, splitDelay, splitScaleMultiplier, splitForce, playerTransform
        );
        // --- END OF FIX ---
    }
    public void ThrowCheese1()
    {
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
    public  LayerMask groundLayer;
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

    public void Initialize(
     bool isReal, int damage, LayerMask playerL, ParticleSystem explosionPrefab, LayerMask groundL,
     ParticleSystem impactPrefab1, ParticleSystem impactPrefab2, ParticleSystem impactPrefab3,
     float explosionRadius, float knockbackForce,
     // V2 PARAMETERS
     bool isV2, float splitTime, float newScale, float newForce, Transform player,
     // --- THIS IS THE FIX ---
     bool isChildSpore = false) // Add this new parameter with a default value
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

        // Store the V2 parameters AND the player reference
        isV2Spore = isV2;
        splitDelay = splitTime;
        splitScale = newScale;
        splitForce = newForce;
        playerTransform = player;

        // Reset state for object pooling
        hasSplit = isChildSpore; // If it's a child, it has "already split"
        isMine = false;

        // Get components and save original scale
        rb = GetComponent<Rigidbody2D>();
        originalScale = transform.localScale;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
        }

        // --- THIS IS THE GUARANTEED FIX ---
        // Only start the split routine if this is the ORIGINAL V2 spore.
        // Child spores will have 'isChildSpore' as true, so this block will be skipped for them.
        if (isV2Spore && !isChildSpore)
        {
            StartCoroutine(SplitRoutine());
        }
        // --- END OF FIX ---
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
                // Initialize the smaller spore (this logic is correct).
                smallerSporeScript.Initialize(
                    true, damageAmount, playerLayer, explosionParticlesPrefab, groundLayer,
                    cheeseImpactParticlesPrefab1, cheeseImpactParticlesPrefab2, cheeseImpactParticlesPrefab3,
                    cheeseExplosionRadius, cheeseKnockbackForce,
                    true, splitDelay, splitScale, splitForce, playerTransform,
                    true // Mark as a child spore
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
        // Mark this spore as a mine.
        isMine = true;

        // --- THIS IS THE GUARANTEED FIX ---
        // 1. We DO NOT set the collider to be a trigger.
        // By keeping it as a solid collider, it can never pass through the ground.
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false; // Force it to be a solid collider.
        }

        // 2. We lock the Rigidbody in place by making it Static.
        // A Static Rigidbody with a solid collider will NOT move, but other
        // Dynamic rigidbodies (like the player) can still pass through it and
        // trigger OnCollisionEnter2D events. This is the perfect "mine" behavior.
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Static;
        }
        // --- END OF FIX ---

        // Optional: You could play a "plant" sound or particle effect here.
    }
    private void Explode()
    {
        // If already destroyed, do nothing.
        if (hasBeenDestroyed) return;
        hasBeenDestroyed = true; // Mark as destroyed immediately.

        // Apply the explosion damage and knockback.
        ApplyExplosionDamageAndKnockback();

        // Destroy the cheese/spore.
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

    public void TakeDamage(int damageAmount, Vector2 attackDirection, float knockbackForce = 0f)
    {
        Explode();
    }
}



