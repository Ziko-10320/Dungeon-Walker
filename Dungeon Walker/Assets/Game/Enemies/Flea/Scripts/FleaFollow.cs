using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FleaFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform playerTransform; // Reference to the player's Transform
    [SerializeField] private float stoppingDistance = 1.5f; // Distance at which the flea stops following

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f; // Speed at which the flea moves
    [SerializeField] private float flipBuffer = 0.1f; // Small buffer to prevent rapid flipping

    [Header("Component References")]
    [SerializeField] private Rigidbody2D rb; // Reference to the Flea's Rigidbody2D
    [SerializeField] private Animator fleaAnimator; // Reference to the Flea's Animator

    // We will directly use the string "IsWalking" for the Animator parameter
    // private int isWalkingHash; // No longer needed

    void Awake()
    {
        // Get component references if not assigned in Inspector
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (fleaAnimator == null) fleaAnimator = GetComponent<Animator>();

        // No need to get hash here anymore
        // if (fleaAnimator != null)
        // {
        //     isWalkingHash = Animator.StringToHash("IsWalking");
        // }

        // Find the player if not assigned (useful for quick setup, but assigning in Inspector is better)
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player"); // Ensure your player has the "Player" tag
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning("FleaFollow: Player not found! Please assign playerTransform or tag your player as 'Player'.", this);
                enabled = false; // Disable script if no player found
            }
        }
    }

    void FixedUpdate() // Use FixedUpdate for Rigidbody physics
    {
        if (playerTransform == null) return;

        // --- MODIFIED: Calculate distance based only on X-axis for stopping ---
        // We want to stop if the X distance is within stoppingDistance
        float xDistanceToPlayer = Mathf.Abs(playerTransform.position.x - transform.position.x);

        if (xDistanceToPlayer > stoppingDistance) // Check X distance for movement decision
        {
            // --- MODIFIED: Calculate direction only for X-axis movement ---
            // Create a target position that matches the flea's current Y, but player's X
            Vector2 targetPositionXOnly = new Vector2(playerTransform.position.x, transform.position.y);
            Vector2 direction = (targetPositionXOnly - (Vector2)transform.position).normalized;

            // Apply velocity only in the X direction, keeping current Y velocity
            rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);

            // Set walking animation using the string name directly
            if (fleaAnimator != null)
            {
                fleaAnimator.SetBool("IsWalking", true); // Using string "IsWalking"
            }

            // Handle flipping
            Flip(direction.x);
        }
        else
        {
            // Stop moving if within stopping distance (only X velocity needs to be zeroed)
            rb.velocity = new Vector2(0f, rb.velocity.y); // Keep current Y velocity

            // Set idle animation using the string name directly
            if (fleaAnimator != null)
            {
                fleaAnimator.SetBool("IsWalking", false); // Using string "IsWalking"
            }
        }
    }

    void Flip(float directionX)
    {
        // Check current facing direction based on localScale.x
        // Assuming positive scale.x means facing right, negative means facing left
        bool facingRight = transform.localScale.x > 0;

        // If moving right and facing left, or moving left and facing right, then flip
        if ((directionX > flipBuffer && !facingRight) || (directionX < -flipBuffer && facingRight))
        {
            Vector3 currentScale = transform.localScale;
            currentScale.x *= -1; // Invert the X scale
            transform.localScale = currentScale;
        }
    }

    // Optional: Visualize stopping distance in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // --- MODIFIED: Visualize stopping distance as a line on the X-axis ---
        // Draw a line to show the X-axis stopping range
        Vector3 leftStop = new Vector3(transform.position.x - stoppingDistance, transform.position.y, transform.position.z);
        Vector3 rightStop = new Vector3(transform.position.x + stoppingDistance, transform.position.y, transform.position.z);
        Gizmos.DrawLine(leftStop, rightStop);
        Gizmos.DrawWireSphere(leftStop, 0.1f); // Mark the ends
        Gizmos.DrawWireSphere(rightStop, 0.1f);
    }
}
