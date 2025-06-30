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

    [Header("Ground Check")]
    public Transform groundCheckPoint;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;
    private bool wasGroundedLastFrame = false;

    [Header("Particle System")]
    public ParticleSystem dust;  
    public ParticleSystem dustLand;
    

    private Rigidbody2D rb;
    public bool isFacingRight = true;
    private Animator animator;
    private float moveDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Input
        moveDirection = Input.GetAxisRaw("Horizontal");
        bool isMoving = Mathf.Abs(moveDirection) > 0.1f;
        animator.SetBool("isRunning", isMoving);

        // Flip
        if ((moveDirection > 0 && !isFacingRight) || (moveDirection < 0 && isFacingRight))
        {
            Flip();
        }

        // Jump Input Handling
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (IsGrounded())
            {
                PerformJump();
               
            }
            else
            {
                // Start jump buffer if not grounded
                jumpPressedInAir = true;
                jumpBufferTimer = jumpBufferTime;
            }
        }

        // Jump Hold Logic - Release early to jump less
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
        // Move the player directly via Rigidbody
        rb.velocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);

        // Handle buffered jump
        if (jumpPressedInAir)
        {
            jumpBufferTimer -= Time.fixedDeltaTime;

            if (IsGrounded())
            {
                PerformJump();
               
                jumpPressedInAir = false;
                jumpBufferTimer = 0f;
            }

            if (jumpBufferTimer <= 0f)
            {
                jumpPressedInAir = false; // Cancel buffer if not landed in time
            }
        }

        

        // Detect land
        if (IsGrounded() && !wasGroundedLastFrame)
        {
            PlayLandDust();
        }

        wasGroundedLastFrame = IsGrounded();
    }

    void PerformJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0); // Reset vertical velocity
        rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
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
        if ( dustLand != null)
        {
            
            dustLand.Play();
        }
    }


    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
    }

    bool isGroundedAndWasNotBefore()
    {
        return IsGrounded() && !wasGroundedLastFrame;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}