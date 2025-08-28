using UnityEngine;
using System.Collections;

public class DelayedDamageClaw : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("The amount of damage to apply.")]
    public float damageAmount = 50f;

    [Tooltip("The radius of the damage area from the attack point.")]
    public float attackRadius = 1.5f;

    [Tooltip("The layer(s) that contain the enemies to be damaged.")]
    public LayerMask enemyLayer;

    [Tooltip("The transform representing the center of the attack. If null, this object's transform is used.")]
    public Transform attackPoint;

    [Header("Timing")]
    [Tooltip("The delay in seconds after spawning before damage is applied.")]
    public float damageDelay = 0.8f;

    void Start()
    {
        // If no attack point is assigned, use this object's transform
        if (attackPoint == null)
        {
            attackPoint = transform;
        }

        // Start the coroutine to apply damage after a delay
        StartCoroutine(ApplyDamageAfterDelay());
    }

    private IEnumerator ApplyDamageAfterDelay()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(damageDelay);

        // Find all colliders within the attack radius on the enemy layer
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer);

        // Apply damage to each enemy found
        foreach (Collider2D enemy in hitEnemies)
        {
            // We get the FleaHealth component to apply damage.
            // This makes it compatible with your existing health system.
            FleaHealth healthComponent = enemy.GetComponent<FleaHealth>();
            if (healthComponent != null)
            {
                // We can pass a zero vector for knockback since this is a special attack
                healthComponent.TakeDamage(damageAmount, Vector2.zero, 0f);
            }
            FlyHealth flyhealthComponent = enemy.GetComponent<FlyHealth>();
            if (flyhealthComponent != null)
            {
                // We can pass a zero vector for knockback since this is a special attack
                flyhealthComponent.TakeDamage(damageAmount, Vector2.zero, 0f);
            }
            InkHealth inkHealth = enemy.GetComponent<InkHealth>();
            if (inkHealth != null)
            {
                // We can pass a zero vector for knockback since this is a special attack
                inkHealth.TakeDamage(damageAmount, Vector2.zero, 0f);
            }
            SprayerHealth sprayerHealth = enemy.GetComponent<SprayerHealth>();
            if (sprayerHealth != null)
            {
               // We can pass a zero vector for knockback since this is a special attack
                sprayerHealth.TakeDamage(damageAmount, Vector2.zero, 0f);
            }
            RatKingHealth ratKingHealth = enemy.GetComponent<RatKingHealth>();
            if (ratKingHealth != null)
            {
                // We can pass a zero vector for knockback since this is a special attack
                ratKingHealth.TakeDamage(damageAmount);
            }


        }

        // Optional: Destroy the claw effect after it has done its job
        // You might want to wait for the animation to finish first.
        // For now, we'll destroy it after a short duration.
        Destroy(gameObject, 2f);
    }

    // Visualize the attack radius in the editor for easy setup
    void OnDrawGizmosSelected()
    {
        Transform point = (attackPoint == null) ? transform : attackPoint;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(point.position, attackRadius);
    }
}
