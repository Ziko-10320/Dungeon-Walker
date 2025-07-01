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

    // Animator parameter hash for performance
    private int isWalkingHash;

    void Awake()
    {
        // Get component references if not assigned in Inspector
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (fleaAnimator == null) fleaAnimator = GetComponent<Animator>();

        // Get the hash for the IsWalking parameter for efficient access
        if (fleaAnimator != null)
        {
            isWalkingHash = Animator.StringToHash("IsWalking");
        }

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

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer > stoppingDistance)
        {
            // Move towards the player
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            rb.velocity = direction * moveSpeed;

            // Set walking animation
            if (fleaAnimator != null)
            {
                fleaAnimator.SetBool(isWalkingHash, true);
            }

            // Handle flipping
            Flip(direction.x);
        }
        else
        {
            // Stop moving if within stopping distance
            rb.velocity = Vector2.zero;

            // Set idle animation
            if (fleaAnimator != null)
            {
                fleaAnimator.SetBool(isWalkingHash, false);
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
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
}
