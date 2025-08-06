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
        Debug.Log("SpawnCheese1 called!");
        // Clear any existing cheese instances
        if (cheese1Instance != null) Destroy(cheese1Instance);
        if (cheese2Instance != null) Destroy(cheese2Instance);

        // Randomly decide which cheese will be real (1 or 2)
        bool isCheese1Real = Random.value < 0.5f;
        cheese1Instance = Instantiate(cheesePrefab, cheeseSpawnPoint1.position, Quaternion.identity);
        cheese1Instance.AddComponent<CheeseProjectile>().Initialize(isCheese1Real, cheeseDamageAmount, playerLayer, explosionParticlesPrefab, WhatIsGround, cheeseImpactParticlesPrefab1, cheeseImpactParticlesPrefab2, cheeseImpactParticlesPrefab3, cheeseExplosionRadius, cheeseKnockbackForce);
        Debug.Log($"Cheese1 spawned - IsReal: {isCheese1Real}");
    }

    public void SpawnCheese2()
    {
        Debug.Log("SpawnCheese2 called!");
        // Ensure the other cheese has the opposite real/fake status
        bool isReal = (cheese1Instance != null && cheese1Instance.GetComponent<CheeseProjectile>() != null) ? !cheese1Instance.GetComponent<CheeseProjectile>().IsRealCheese : (Random.value < 0.5f);
        cheese2Instance = Instantiate(cheesePrefab, cheeseSpawnPoint2.position, Quaternion.identity);
        cheese2Instance.AddComponent<CheeseProjectile>().Initialize(isReal, cheeseDamageAmount, playerLayer, explosionParticlesPrefab, WhatIsGround, cheeseImpactParticlesPrefab1, cheeseImpactParticlesPrefab2, cheeseImpactParticlesPrefab3, cheeseExplosionRadius, cheeseKnockbackForce);
        Debug.Log($"Cheese2 spawned - IsReal: {isReal}");
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

    public bool IsRealCheese { get; private set; } // Public getter for isRealCheese

    public void Initialize(bool isReal, int damage, LayerMask playerL, ParticleSystem explosionPrefab, LayerMask groundL, ParticleSystem impactPrefab1, ParticleSystem impactPrefab2, ParticleSystem impactPrefab3, float explosionRadius, float knockbackForce)
    {
        isRealCheese = isReal;
        IsRealCheese = isReal; // Set the public property
        damageAmount = damage;
        playerLayer = playerL;
        explosionParticlesPrefab = explosionPrefab;
        groundLayer = groundL;
        cheeseImpactParticlesPrefab1 = impactPrefab1;
        cheeseImpactParticlesPrefab2 = impactPrefab2;
        cheeseImpactParticlesPrefab3 = impactPrefab3;
        cheeseExplosionRadius = explosionRadius;
        cheeseKnockbackForce = knockbackForce;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer not found on CheeseProjectile!", this);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the collision is with the player or ground
        bool collidedWithPlayer = ((1 << collision.gameObject.layer) & playerLayer) != 0;
        bool collidedWithGround = ((1 << collision.gameObject.layer) & groundLayer) != 0;

        Debug.Log($"Cheese collision detected - IsReal: {IsRealCheese}, CollidedWithPlayer: {collidedWithPlayer}, CollidedWithGround: {collidedWithGround}");

        if (IsRealCheese)
        {
            if (collidedWithPlayer || collidedWithGround)
            {
                ApplyExplosionDamageAndKnockback();
                DestroyCheese();
            }
        }
        else // Fake Cheese
        {
            // Fake cheese only fades out on collision with player or ground, and does no damage
            if (collidedWithPlayer || collidedWithGround)
            {
                Debug.Log("Fake cheese hit - starting fade out");
                StartCoroutine(FadeOutAndDestroy());
            }
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

    IEnumerator FadeOutAndDestroy()
    {
        if (spriteRenderer == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Color originalColor = spriteRenderer.color;
        float fadeDuration = 0.5f; // Duration of the fade effect
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            originalColor.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            spriteRenderer.color = originalColor;
            yield return null;
        }
        Destroy(gameObject);
    }
}



