using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDash : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private GhostEffect ghostEffect; // Reference to the GhostEffect script

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 25f; // The speed of the player during the dash
    [SerializeField] private float dashDuration = 0.2f; // How long the dash lasts
    [SerializeField] private float dashCooldown = 1f; // Time between dashes

    // State variables
    private bool canDash = true;
    private bool isDashing = false;
    private float dashTimer;
    private float originalGravity;

    // Public property for other scripts to check if the player is currently dashing
    public bool IsDashing => isDashing;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (ghostEffect == null) ghostEffect = GetComponent<GhostEffect>();
        originalGravity = rb.gravityScale;
    }

    void Update()
    {
        // Check for dash input (e.g., Left Shift key)
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            // We need a reference to the movement script to check if grounded,
            // or we can just dash anytime. For now, let's assume we can dash anytime.
            StartDash();
        }
    }

    void FixedUpdate()
    {
        // This is the core logic. It runs every physics frame to ensure
        // the velocity is maintained during the dash.
        if (isDashing)
        {
            // Determine dash direction based on player's facing direction
            // We get this from the KritinaMovement script's public variable
            KritinaMovement movementScript = GetComponent<KritinaMovement>();
            float dashDirection = movementScript.isFacingRight ? 1f : -1f;

            // Continuously set the velocity during the dash
            rb.velocity = new Vector2(dashDirection * dashSpeed, 0f);

            // Increment the timer
            dashTimer += Time.fixedDeltaTime;

            // Check if the dash duration has ended
            if (dashTimer >= dashDuration)
            {
                EndDash();
            }
        }
    }

    private void StartDash()
    {
        isDashing = true;
        canDash = false;
        dashTimer = 0f;

        // Prepare the Rigidbody for the dash
        rb.gravityScale = 0f;

        // Activate the ghost effect
        if (ghostEffect != null)
        {
            ghostEffect.StartGhostEffect();
        }
    }

    private void EndDash()
    {
        isDashing = false;

        // Reset the Rigidbody
        rb.velocity = Vector2.zero; // Stop the player
        rb.gravityScale = originalGravity;

        // Stop the ghost effect
        if (ghostEffect != null)
        {
            ghostEffect.StopGhostEffect();
        }

        // Start the cooldown using a coroutine
        StartCoroutine(DashCooldown());
    }

    private IEnumerator DashCooldown()
    {
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}
