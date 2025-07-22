using UnityEngine;

public class KritinaMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpPower = 12f;
    public float jumpReleaseCutMultiplier = 0.5f; // How much velocity is kept on early release

    [Header("Jump Buffer Settings")]
    private float jumpBufferTimer = 0f;
    public float jumpBufferTime = 0.4f; // How long we accept landing after jump press
    private bool jumpPressedInAir = false;

    // --- NEW: Double Jump Settings ---
    [Header("Double Jump Settings")]
    public int maxJumps = 2; // 1 for single jump, 2 for double jump, etc.
    private int jumpsRemaining;

    [Header("Ground Check")]
    public Transform groundCheckPoint;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;
    private bool wasGroundedLastFrame = false;

    [Header("Particle System")]
    public ParticleSystem dust;
    public ParticleSystem dustLand;

    [Header("Sound Settings")]
    public AudioClip jumpSoundClip;
    [Range(0f, 1f)]
    public float jumpVolume = 1f;
    public AudioClip doubleJumpSoundClip;
    [Range(0f, 1f)]
    public float doubleJumpVolume = 1f;
    public AudioClip landSoundClip;
    [Range(0f, 1f)]
    public float landVolume = 1f;

    private PlayerDash playerDash;


    private Rigidbody2D rb;
    public bool isFacingRight = true;
    private Animator animator;
    private float moveDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerDash = GetComponent<PlayerDash>();

        // Initialize jumpsRemaining at the start
        jumpsRemaining = maxJumps;
    }

    void Update()
    {
        if (playerDash != null && playerDash.IsDashing)
        {
            moveDirection = 0;
            return;
        }

        moveDirection = Input.GetAxisRaw("Horizontal");
        bool isMoving = Mathf.Abs(moveDirection) > 0.1f;

        animator.SetBool("isRunning", isMoving);

        if ((moveDirection > 0 && !isFacingRight) || (moveDirection < 0 && isFacingRight))
        {
            Flip();
        }

        // --- MODIFIED: Jump Input Handling for Double Jump ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetTrigger("Jump");
            if (IsGrounded())
            {
                PerformJump(false); // First jump
            }
            else // Player is in the air
            {
                // Check for double jump
                if (jumpsRemaining > 0) // If maxJumps is 2, and we have 2 remaining, it's the first jump. If 1 remaining, it's the second.
                {
                    PerformJump(true); // Perform the double jump
                }
                else
                {
                    // Start jump buffer if not grounded and no double jump available
                    jumpPressedInAir = true;
                    jumpBufferTimer = jumpBufferTime;
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (rb.velocity.y > 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpReleaseCutMultiplier);
            }
        }
    }

    void FixedUpdate()
    {
        if (playerDash != null && playerDash.IsDashing)
        {
            rb.velocity = Vector2.zero; // Stop movement during dash
            return;
        }

        float currentXVelocity = rb.velocity.x;
        float currentYVelocity = rb.velocity.y;

        // Apply horizontal movement
        rb.velocity = new Vector2(moveDirection * moveSpeed, currentYVelocity);

        // Handle buffered jump
        if (jumpPressedInAir)
        {
            jumpBufferTimer -= Time.fixedDeltaTime;

            if (IsGrounded())
            {
                PerformJump(false);
                jumpPressedInAir = false;
                jumpBufferTimer = 0f;
            }

            if (jumpBufferTimer <= 0f)
            {
                jumpPressedInAir = false;
            }
        }

        // Detect land
        bool currentlyGrounded = IsGrounded();
        if (currentlyGrounded && !wasGroundedLastFrame)
        {
            PlayLandDust();
            if (landSoundClip != null) AudioSource.PlayClipAtPoint(landSoundClip, transform.position, landVolume);
            // --- NEW: Reset jumps when landing ---
            jumpsRemaining = maxJumps;
        }
        // --- NEW: Also reset jumps if already grounded ---
        else if (currentlyGrounded)
        {
            jumpsRemaining = maxJumps;
        }


        wasGroundedLastFrame = currentlyGrounded;
    }

    void PerformJump(bool isDoubleJump)
    {
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        jumpsRemaining--; // Consume a jump when performing any jump

        if (isDoubleJump)
        {
            if (doubleJumpSoundClip != null) AudioSource.PlayClipAtPoint(doubleJumpSoundClip, transform.position, doubleJumpVolume);
        }
        else
        {
            if (jumpSoundClip != null) AudioSource.PlayClipAtPoint(jumpSoundClip, transform.position, jumpVolume);
        }
    }

    void Flip()
    {

        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        isFacingRight = !isFacingRight;

        if (IsGrounded())
            dust.Play();
    }

    void PlayLandDust()
    {
        if (dustLand != null)
        {
            dustLand.Play();
        }
    }

    public bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
    }

    bool IsTouchingWall(Vector2 direction)
    {
        Collider2D playerCollider = GetComponent<Collider2D>();
        if (playerCollider == null) return false;

        float raycastDistance = 0.1f;
        Vector2 origin = playerCollider.bounds.center;
        origin.y = playerCollider.bounds.min.y + playerCollider.bounds.size.y / 2;

        if (direction.x > 0) origin.x = playerCollider.bounds.max.x;
        else if (direction.x < 0) origin.x = playerCollider.bounds.min.x;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, raycastDistance, groundLayer);

        return hit.collider != null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);

        if (GetComponent<Collider2D>() != null)
        {
            Gizmos.color = Color.magenta;
            float raycastDistance = 0.1f;
            Vector2 originRight = GetComponent<Collider2D>().bounds.center;
            originRight.x = GetComponent<Collider2D>().bounds.max.x;
            Gizmos.DrawRay(originRight, Vector2.right * raycastDistance);

            Vector2 originLeft = GetComponent<Collider2D>().bounds.center;
            originLeft.x = GetComponent<Collider2D>().bounds.min.x;
            Gizmos.DrawRay(originLeft, Vector2.left * raycastDistance);
        }
    }
}