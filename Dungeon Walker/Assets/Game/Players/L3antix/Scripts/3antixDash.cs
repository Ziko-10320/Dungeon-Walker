using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class L3antixDash : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private GhostEffect ghostEffect; // Reference to the GhostEffect script
    [SerializeField] private L3antixMovement l3antixMovement; // Reference to the KritinaMovement script

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 25f; // The speed of the player during the dash
    [SerializeField] private float dashDuration = 0.2f; // How long the dash lasts
    [SerializeField] private float dashCooldown = 1f; // Time between dashes

    [Header("Sound Settings")]
    public AudioClip dashSoundClip;
    [Range(0f, 1f)]
    public float dashVolume = 1f;

    // State variables
    private bool canDash = true;
    private bool isDashing = false;
    private float dashTimer;
    private float originalGravity;
    private Vector2 dashDirectionVector; // Store the dash direction

    // Public property for other scripts to check if the player is currently dashing
    public bool IsDashing => isDashing;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (ghostEffect == null) ghostEffect = GetComponent<GhostEffect>();
        if (l3antixMovement == null) l3antixMovement = GetComponent<L3antixMovement>();
        originalGravity = rb.gravityScale;
    }

    void Update()
    {
        // Check for dash input (e.g., Left Shift key)
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDashing)
        {
            StartDash();
        }
    }
    public void TriggerDash()
    {
        // On vérifie les mêmes conditions que pour le clavier.
        if (canDash && !isDashing)
        {
            StartDash();
        }
    }
    void FixedUpdate()
    {
        if (isDashing)
        {
            // Directly manipulate the transform position for guaranteed movement
            // This bypasses Rigidbody physics during the dash, ensuring movement.
            transform.position += (Vector3)dashDirectionVector * dashSpeed * Time.fixedDeltaTime;

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
        Debug.Log("Starting dash...");

        isDashing = true;
        canDash = false;
        dashTimer = 0f;

        // Determine dash direction based on player's facing direction at the start of the dash
        if (l3antixMovement != null)
        {
            dashDirectionVector = l3antixMovement.isFacingRight ? Vector2.right : Vector2.left;
            
            l3antixMovement.enabled = false;
            Debug.Log($"Dash direction: {dashDirectionVector}, isFacingRight: {l3antixMovement.isFacingRight}");
        }
        else
        {
            Debug.LogWarning("KritinaMovement script not found on player. Defaulting dash direction to right.");
            dashDirectionVector = Vector2.right;
        }

        // Play dash sound
        if (dashSoundClip != null)
        {
            AudioSource.PlayClipAtPoint(dashSoundClip, transform.position, dashVolume);
        }

        // Prepare the Rigidbody for the dash
        // Set velocity to zero and disable gravity to prevent interference with direct transform manipulation
        rb.velocity = Vector2.zero;
        rb.gravityScale = 0f;

        Debug.Log($"Rigidbody velocity reset and gravity disabled.");

        // Activate the ghost effect
        if (ghostEffect != null)
        {
            ghostEffect.StartGhostEffect();
        }
    }

    private void EndDash()
    {
        Debug.Log("Ending dash...");

        isDashing = false;

        
        if (l3antixMovement != null)
        {
            l3antixMovement.enabled = true;
        }

        // Reset the Rigidbody properties
        rb.gravityScale = originalGravity; // Restore original gravity
        rb.velocity = Vector2.zero; // Ensure player stops after dash

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
        Debug.Log("Dash cooldown finished. Can dash again.");
    }
}
