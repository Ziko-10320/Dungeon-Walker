using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NeedleAttackSystem : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Animator playerAnimator; // Reference to the player's Animator
    [SerializeField] private string attackTriggerName = "NeedleAttack"; // Name of the Trigger in the Animator
    [SerializeField] private float attackCooldown = 1.0f; // Cooldown duration for the attack (in seconds)
    [SerializeField] private int damage = 20; // Damage amount dealt by the attack

    [Header("Damage Area Settings")]
    [SerializeField] private Transform attackPoint; // Origin point of the attack (usually in front of the player)
    [SerializeField] private float attackRange = 0.5f; // Radius of the attack area (circle)
    [SerializeField] private LayerMask enemyLayers; // Layers of enemies that can receive damage

    private float nextAttackTime = 0f; // Time when the next attack is allowed
    private bool canDealDamage = false; // Flag to control damage application once per attack

    void Update()
    {
        // Check for 'X' key press and cooldown
        if (Input.GetKeyDown(KeyCode.X) && Time.time >= nextAttackTime)
        {
            PerformNeedleAttack();
        }
    }

    void PerformNeedleAttack()
    {
        // Reset the next attack time
        nextAttackTime = Time.time + attackCooldown;

        // Trigger the attack animation
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(attackTriggerName);
        }

        // Enable damage application. ApplyDamage() will be called via Animation Event
        canDealDamage = true;
    }

    // This function will be called as an Animation Event at the specified frame
    public void ApplyDamage()
    {
        if (!canDealDamage) return; // Ensure we haven't already applied damage for this attack

        // Detect enemies in the attack range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        // Apply damage to each detected enemy
        foreach (Collider2D enemy in hitEnemies)
        {
            FleaHealth fleaHealth = enemy.GetComponent<FleaHealth>();
            if (fleaHealth != null)
            {
                // Calculate the direction of the knockback
                Vector2 attackDirection = (Vector2)(enemy.transform.position - attackPoint.position).normalized;
                Debug.Log("Attack direction: " + attackDirection);

                // Deal damage to the mushroom
                fleaHealth.TakeDamage(damage, attackDirection);
            }

                // For testing purposes, we can print the enemy's name
                Debug.Log("Hit " + enemy.name + " for " + damage + " damage!");
        }

        canDealDamage = false; // Disable damage application after it's been dealt
    }

    // To visualize the attack range in the Scene View (for debugging only)
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}

