using UnityEngine;
using System.Collections;

public class RatKingAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator ratKingAnimator;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private RatKingBoss ratKingBoss; // Reference to the main boss script

    [Header("Jump Attack Parameters")]
    [SerializeField] private float jumpAttackRange = 5f;
    [SerializeField] private float jumpForceX = 8f;
    [SerializeField] private float jumpForceY = 10f;
    [SerializeField] private float jumpCooldown = 3f;
    [SerializeField][Range(0, 1)] private float jumpProbability = 0.5f;
    [SerializeField] private float jumpAnticipationDuration = 0.2f; // Time before actual jump

    private float lastJumpTime;
    private bool isJumping = false;
    private bool isFalling = false;
    private Vector2 jumpTargetPosition; // Store player's position at jump initiation

    [Header("Ground Impact Effects")]
    [SerializeField] private ParticleSystem groundImpactParticles; // Particle system to play on impact
    [SerializeField] private Transform damageZoneOrigin; // Origin for the damage zone
    [SerializeField] private float damageZoneRadius = 1f;
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private LayerMask playerLayer; // Layer of the player for damage detection

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (ratKingAnimator == null) ratKingAnimator = GetComponent<Animator>();
        if (ratKingBoss == null) ratKingBoss = GetComponent<RatKingBoss>();

        // Ensure essential references are assigned
        if (playerTransform == null || damageZoneOrigin == null)
        {
            Debug.LogError("Essential references for RatKingAttack are not assigned!", this);
            enabled = false;
            return;
        }
    }

    void Start()
    {
        lastJumpTime = -jumpCooldown; // Allow immediate jump at start
    }

    void Update()
    {
        // Only consider jump attack if not already jumping and RatKing is allowed to move
        if (!isJumping && ratKingBoss.CanMove && Time.time >= lastJumpTime + jumpCooldown)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

            // If player is within jump attack range and not too close (to avoid jumping over them)
            if (distanceToPlayer > ratKingBoss.stoppingDistance && distanceToPlayer < jumpAttackRange)
            {
                if (Random.value < jumpProbability)
                {
                    StartCoroutine(JumpAttackRoutine());
                }
            }
        }

        // Update falling animation state
        if (rb.velocity.y < -0.1f && !isFalling && isJumping) // If moving downwards and was jumping
        {
            isFalling = true;
            ratKingAnimator.SetBool("IsFalling", true);
            ratKingAnimator.SetBool("IsJumping", false);
        }
        else if (rb.velocity.y >= -0.1f && isFalling) // If no longer falling
        {
            isFalling = false;
            ratKingAnimator.SetBool("IsFalling", false);
        }
    }

    private IEnumerator JumpAttackRoutine()
    {
        isJumping = true;
        ratKingBoss.CanMove = false; // Prevent other movements during jump attack
        ratKingBoss.StopMoving(); // Ensure it stops before jumping

        // Store player's last position
        jumpTargetPosition = playerTransform.position;

        // Anticipation animation
        ratKingAnimator.SetTrigger("JumpAnticipation");
        yield return new WaitForSeconds(jumpAnticipationDuration);

        // Perform the jump
        ratKingAnimator.SetBool("IsJumping", true);
        float directionToTargetX = Mathf.Sign(jumpTargetPosition.x - transform.position.x);
        rb.velocity = new Vector2(directionToTargetX * jumpForceX, jumpForceY);

        lastJumpTime = Time.time;

        // Wait until landed (velocity.y is near zero and on ground)
        yield return new WaitUntil(() => rb.velocity.y < 0.1f && IsGrounded());

        // Landed
        ratKingAnimator.SetTrigger("Land");
        ratKingAnimator.SetBool("IsJumping", false);
        ratKingAnimator.SetBool("IsFalling", false);

        PlayGroundImpactEffects();
        ApplyDamage();

        isJumping = false;
        ratKingBoss.CanMove = true; // Allow movement again
    }

    private bool IsGrounded()
    {
        // Use the same ground check logic as RatKingBoss for consistency
        return Physics2D.OverlapCircle(ratKingBoss.groundCheck.position, ratKingBoss.groundCheckRadius, ratKingBoss.whatIsGround);
    }

    private void PlayGroundImpactEffects()
    {
        if (groundImpactParticles != null)
        {
            groundImpactParticles.transform.position = damageZoneOrigin.position; // Or where the impact happens
            groundImpactParticles.Play();
        }
    }

    private void ApplyDamage()
    {
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(damageZoneOrigin.position, damageZoneRadius, playerLayer);
        foreach (Collider2D player in hitPlayers)
        {
            // In a real game, you would call a method on the player to take damage
            Debug.Log($"Player {player.name} took {damageAmount} damage from RatKing impact!");
            // Example: player.GetComponent<PlayerHealth>().TakeDamage(damageAmount);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw jump attack range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, jumpAttackRange);

        // Draw damage zone
        if (damageZoneOrigin != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(damageZoneOrigin.position, damageZoneRadius);
        }
    }
}
