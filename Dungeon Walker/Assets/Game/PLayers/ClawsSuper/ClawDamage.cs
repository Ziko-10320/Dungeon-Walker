using UnityEngine;
using System.Collections;

public class DelayedDamageClaw : MonoBehaviour
{
    [HideInInspector] public SuperMoveController superMoveController; // This is our link to the main script

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
        if (attackPoint == null)
        {
            attackPoint = transform;
        }
        StartCoroutine(ApplyDamageAfterDelay());
    }

    private IEnumerator ApplyDamageAfterDelay()
    {
        // Wait for the damage delay
        yield return new WaitForSeconds(damageDelay);

        // Find all enemies within the attack radius
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, enemyLayer);

        // --- THIS IS THE COMPLETE AND CORRECTED DAMAGE LOGIC ---
        // Apply damage to each enemy found
        foreach (Collider2D enemy in hitEnemies)
        {
            // We use TryGetComponent for safety and performance.

            if (enemy.TryGetComponent<FleaHealth>(out var fleaHealth))
            {
                fleaHealth.TakeDamage(damageAmount, Vector2.zero, 0f);
            }
            else if (enemy.TryGetComponent<FleaHealthV2>(out var fleaHealthV2))
            {
                fleaHealthV2.TakeDamage(damageAmount, Vector2.zero, 0f);
            }
            else if (enemy.TryGetComponent<FlyHealth>(out var flyHealth))
            {
                flyHealth.TakeDamage(damageAmount, Vector2.zero, 0f);
            }
            else if (enemy.TryGetComponent<InkHealth>(out var inkHealth))
            {
                inkHealth.TakeDamage(damageAmount, Vector2.zero, 0f);
            }
            else if (enemy.TryGetComponent<SprayerHealth>(out var sprayerHealth))
            {
                sprayerHealth.TakeDamage(damageAmount, Vector2.zero, 0f);
            }
            else if (enemy.TryGetComponent<RatKingHealth>(out var ratKingHealth))
            {
                ratKingHealth.TakeDamage(damageAmount, Vector2.zero, 0f);
            }
        }
        // --- END OF DAMAGE LOGIC ---

        // Destroy the claw effect after a short duration
        Destroy(gameObject, 2f);
    }

    // Visualize the attack radius in the editor
    void OnDrawGizmosSelected()
    {
        Transform point = (attackPoint == null) ? transform : attackPoint;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(point.position, attackRadius);
    }
}
