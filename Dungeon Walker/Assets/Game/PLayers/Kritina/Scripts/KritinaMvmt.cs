using UnityEngine;
using Photon.Pun; // <<< 1. ADD THIS LINE to use Photon's library.

public class KritinaMovement : MonoBehaviour
{
    // --- All your existing variables are perfect and don't need changes ---
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpPower = 12f;
    public float jumpReleaseCutMultiplier = 0.5f;
    [Header("Mobile Controls")]
    public Joystick joystick;
    private bool jumpButtonPressed = false;
    private bool jumpButtonReleased = false;

    [Header("Jump Buffer Settings")]
    private float jumpBufferTimer = 0f;
    public float jumpBufferTime = 0.4f;
    private bool jumpPressedInAir = false;

    [Header("Double Jump Settings")]
    public int maxJumps = 2;
    private int jumpsRemaining;

    [Header("Ground Check")]
    public Transform groundCheckPoint;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;
    private bool wasGroundedLastFrame = false;

    [Header("Particle System")]
    public ParticleSystem dust;
    public ParticleSystem dustLand;

    [Header("Arm and Gun Flip")]
    public Transform[] playerArms;
    public Transform[] playerGuns;

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

    private PhotonView view; // <<< 2. ADD THIS VARIABLE to hold the PhotonView component.

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerDash = GetComponent<PlayerDash>();
        view = GetComponent<PhotonView>(); // <<< 3. GET THE COMPONENT when the script wakes up.

        jumpsRemaining = maxJumps;
    }

    void Update()
    {
        // <<< 4. THIS IS THE MAIN CHANGE. We wrap the entire Update loop in this check. >>>
        // This ensures that only the person controlling this character can read inputs for it.
        if (!view.IsMine)
        {
            return; // If this character is not mine, do nothing in Update.
        }

        // --- All your original Update code is now safely inside the check ---
        if (playerDash != null && playerDash.IsDashing)
        {
            moveDirection = 0;
            return;
        }

        moveDirection = Input.GetAxisRaw("Horizontal");
        if (moveDirection == 0 && joystick != null && joystick.gameObject.activeInHierarchy)
        {
            moveDirection = joystick.Horizontal;
        }

        bool isMoving = Mathf.Abs(moveDirection) > 0.1f;
        animator.SetBool("isRunning", isMoving);

        if ((moveDirection > 0 && !isFacingRight) || (moveDirection < 0 && isFacingRight))
        {
            Flip();
        }

        if (Input.GetKeyDown(KeyCode.Space) || jumpButtonPressed)
        {
            animator.SetTrigger("Jump");
            if (IsGrounded())
            {
                PerformJump(false);
            }
            else
            {
                if (jumpsRemaining > 0)
                {
                    PerformJump(true);
                }
                else
                {
                    jumpPressedInAir = true;
                    jumpBufferTimer = jumpBufferTime;
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.Space) || jumpButtonReleased)
        {
            if (rb.velocity.y > 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpReleaseCutMultiplier);
            }
        }

        jumpButtonPressed = false;
        jumpButtonReleased = false;
    }

    public void OnJumpButtonDown()
    {
        // We also need to protect this public function, just in case.
        if (!view.IsMine) return;
        jumpButtonPressed = true;
    }

    public void OnJumpButtonUp()
    {
        // And this one too.
        if (!view.IsMine) return;
        jumpButtonReleased = true;
    }

    void FixedUpdate()
    {
        // <<< 5. WE ALSO WRAP FixedUpdate. >>>
        // This is important because physics calculations and movement application happen here.
        if (!view.IsMine)
        {
            return; // If not my character, don't apply any physics movement.
        }

        // --- All your original FixedUpdate code is now safely inside the check ---
        if (playerDash != null && playerDash.IsDashing)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        float currentYVelocity = rb.velocity.y;
        rb.velocity = new Vector2(moveDirection * moveSpeed, currentYVelocity);

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

        bool currentlyGrounded = IsGrounded();
        if (currentlyGrounded && !wasGroundedLastFrame)
        {
            PlayLandDust();
            if (landSoundClip != null) AudioSource.PlayClipAtPoint(landSoundClip, transform.position, landVolume);
            jumpsRemaining = maxJumps;
        }
        else if (currentlyGrounded)
        {
            jumpsRemaining = maxJumps;
        }
        wasGroundedLastFrame = currentlyGrounded;
    }

    // --- The rest of your functions do not need the 'IsMine' check ---
    // This is because they don't read player input. They are called by Update/FixedUpdate,
    // which are already protected.
    void PerformJump(bool isDoubleJump)
    {
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        jumpsRemaining--;

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
        foreach (Transform arm in playerArms)
        {
            if (arm != null) arm.localScale = new Vector3(-arm.localScale.x, -arm.localScale.y, arm.localScale.z);
        }
        foreach (Transform gun in playerGuns)
        {
            if (gun != null) gun.localScale = new Vector3(-gun.localScale.x, -gun.localScale.y, gun.localScale.z);
        }
        isFacingRight = !isFacingRight;
        if (IsGrounded()) dust.Play();
    }

    void PlayLandDust()
    {
        if (dustLand != null) dustLand.Play();
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
