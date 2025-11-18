using UnityEngine;

public class DamageSource : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float upwardKnockbackMultiplier = 0.5f; // New: Multiplier for upward force

    [Header("Target Settings")]
    [SerializeField] private string targetTag = "Player"; // Tag of the GameObject that can take damage

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(targetTag))
        {
            PlayerHealth player = other.GetComponent<PlayerHealth>();
            if (player != null)
            {
                Vector2 knockbackDirection = (player.transform.position - transform.position).normalized;

                // Add an upward component to the knockback direction
                knockbackDirection.y += upwardKnockbackMultiplier;
                knockbackDirection.Normalize(); // Normalize again after adding upward component

                player.TakeDamage(damageAmount, knockbackForce, knockbackDirection);
            }
            L3antixHealth L3antixHealth = other.GetComponent<L3antixHealth>();
            if (L3antixHealth != null)
            {
                Vector2 knockbackDirection = (L3antixHealth.transform.position - transform.position).normalized;

                // Add an upward component to the knockback direction
                knockbackDirection.y += upwardKnockbackMultiplier;
                knockbackDirection.Normalize(); // Normalize again after adding upward component

                L3antixHealth.TakeDamage(damageAmount, knockbackForce, knockbackDirection);
            }
        }
    }
}
