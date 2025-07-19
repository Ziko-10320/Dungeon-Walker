using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InkAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private GameObject inkBallPrefab; // Assign your InkBall prefab here
    [SerializeField] private float attackCooldown = 2f; // Time between attacks
    [SerializeField] private int inkBallDamage = 10; // Damage dealt by InkBall
    [SerializeField] private float inkBallKnockbackForce = 5f; // Knockback force of InkBall
    [SerializeField] private float inkBallLifetime = 3f; // How long the ink ball lasts before destroying itself

    [Header("Aiming Settings")]
    [SerializeField] private Transform spawnPoint; // Where the InkBall will be spawned
    [SerializeField] private Transform aimPointLow; // Transform for low aim line
    [SerializeField] private Transform aimPointHigh; // Transform for high aim line
    [SerializeField] private Vector2 aimOffsetLow; // Offset for the low aim point
    [SerializeField] private Vector2 aimOffsetHigh; // Offset for the high aim point
    [SerializeField] private float projectileSpeed = 10f; // Speed of the ink ball

    [Header("Player Detection")]
    [SerializeField] private LayerMask playerLayer; // Layer of the player
    private Transform playerTransform; // Reference to the player's transform

    [Header("Explosion Effect")]
    [SerializeField] private GameObject explosionParticleSystemPrefab; // Assign your particle system prefab here

    [Header("Flipping Control")]
    [Tooltip("Sprites that should NOT flip when enemy faces different directions")]
    [SerializeField] private SpriteRenderer[] unflippableSprites;
    [Tooltip("Should unflippable sprites be automatically found?")]
    [SerializeField] private bool autoFindUnflippableSprites = true;

    private bool canAttack = true;
    private bool facingRight = true; // To keep track of enemy's facing direction
    private InkHealth inkHealthComponent; // Reference to InkHealth component
    private Vector3[] originalUnflippableSpriteScales; // Store original scales of unflippable sprites

    void Start()
    {
        // Find the player in the scene. Make sure your player GameObject has the tag "Player"
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("Player GameObject not found! Make sure it's tagged as \"Player\".");
            enabled = false; // Disable script if player not found
        }

        // Get InkHealth component reference
        inkHealthComponent = GetComponent<InkHealth>();
        if (inkHealthComponent == null)
        {
            Debug.LogWarning("InkAttack: InkHealth component not found. Invincibility checks will be skipped.");
        }

        // Auto-find unflippable sprites if enabled
        if (autoFindUnflippableSprites && (unflippableSprites == null || unflippableSprites.Length == 0))
        {
            unflippableSprites = GetComponentsInChildren<SpriteRenderer>();
        }

        // Store original scales of unflippable sprites
        if (unflippableSprites != null && unflippableSprites.Length > 0)
        {
            originalUnflippableSpriteScales = new Vector3[unflippableSprites.Length];
            for (int i = 0; i < unflippableSprites.Length; i++)
            {
                if (unflippableSprites[i] != null)
                {
                    originalUnflippableSpriteScales[i] = unflippableSprites[i].transform.localScale;
                }
            }
        }

        StartCoroutine(AttackRoutine());
    }

    void Update()
    {
        if (playerTransform != null)
        {
            // Always face the player
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            if (directionToPlayer.x > 0 && !facingRight)
            {
                Flip();
            }
            else if (directionToPlayer.x < 0 && facingRight)
            {
                Flip();
            }
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;

        // Keep unflippable sprites at their original scale
        if (unflippableSprites != null && originalUnflippableSpriteScales != null)
        {
            for (int i = 0; i < unflippableSprites.Length && i < originalUnflippableSpriteScales.Length; i++)
            {
                if (unflippableSprites[i] != null)
                {
                    unflippableSprites[i].transform.localScale = originalUnflippableSpriteScales[i];
                }
            }
        }
    }

    IEnumerator AttackRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackCooldown);

            // Check if enemy can attack (not invincible and other conditions)
            if (playerTransform != null && canAttack && CanAttack())
            {
                Attack();
            }
        }
    }

    // Check if the enemy can attack (considering invincibility)
    private bool CanAttack()
    {
        // Check if enemy is invincible
        if (inkHealthComponent != null && inkHealthComponent.IsInvincible())
        {
            return false; // Cannot attack while invincible
        }

        return true; // Can attack
    }

    void Attack()
    {
        canAttack = false;

        // Determine random aim (low or high) and apply offset
        Vector3 targetAimPosition;
        Vector2 currentAimOffsetLow = aimOffsetLow;
        Vector2 currentAimOffsetHigh = aimOffsetHigh;

        // Adjust aim offsets based on facing direction
        if (!facingRight)
        {
            currentAimOffsetLow.x *= -1;
            currentAimOffsetHigh.x *= -1;
        }

        if (Random.Range(0, 2) == 0)
        {
            targetAimPosition = aimPointLow.position + (Vector3)currentAimOffsetLow;
        }
        else
        {
            targetAimPosition = aimPointHigh.position + (Vector3)currentAimOffsetHigh;
        }

        // Calculate direction to target aim point
        Vector2 direction = (targetAimPosition - spawnPoint.position).normalized;

        // Instantiate InkBall
        GameObject inkBall = Instantiate(inkBallPrefab, spawnPoint.position, Quaternion.identity);

        // Get or add Rigidbody2D and Collider2D to the instantiated InkBall
        Rigidbody2D rb = inkBall.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = inkBall.AddComponent<Rigidbody2D>();
            Debug.LogWarning("InkAttack: Rigidbody2D not found on InkBall prefab. Added one as fallback. Consider adding it to the prefab.");
        }
        rb.gravityScale = 0; // Ensure no gravity affects the projectile
        rb.isKinematic = false; // Make sure it's not kinematic so physics can move it
        rb.velocity = direction * projectileSpeed;

        Collider2D inkBallCollider = inkBall.GetComponent<Collider2D>();
        if (inkBallCollider == null)
        {
            CircleCollider2D circle = inkBall.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            Debug.LogWarning("InkAttack: Collider2D not found on InkBall prefab. Added CircleCollider2D as fallback. Consider adding it to the prefab.");
        }
        else
        {
            inkBallCollider.isTrigger = true; // Ensure it's a trigger for collision detection
        }

        // Add a component to handle InkBall behavior (or use a direct method call if simpler)
        // For this approach, we will use a dedicated MonoBehaviour on the InkBall itself
        // that we will initialize from here.
        InkBallBehavior inkBallBehavior = inkBall.AddComponent<InkBallBehavior>();
        inkBallBehavior.Initialize(
            inkBallDamage,
            inkBallKnockbackForce,
            playerLayer,
            explosionParticleSystemPrefab,
            inkBallLifetime,
            gameObject // Pass the spawner object for collision checks
        );

        StartCoroutine(ResetAttackCooldown());
    }

    IEnumerator ResetAttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    // Draw gizmos in editor to visualize aim lines and offsets
    void OnDrawGizmos()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.red;
            Vector2 gizmoAimOffsetLow = aimOffsetLow;
            Vector2 gizmoAimOffsetHigh = aimOffsetHigh;

            // Adjust gizmo aim offsets based on facing direction
            if (!facingRight)
            {
                gizmoAimOffsetLow.x *= -1;
                gizmoAimOffsetHigh.x *= -1;
            }

            if (aimPointLow != null)
            {
                Vector3 actualAimLow = aimPointLow.position + (Vector3)gizmoAimOffsetLow;
                Gizmos.DrawLine(spawnPoint.position, actualAimLow);
                Gizmos.DrawSphere(actualAimLow, 0.1f); // Draw a small sphere at the aim point
            }
            if (aimPointHigh != null)
            {
                Vector3 actualAimHigh = aimPointHigh.position + (Vector3)gizmoAimOffsetHigh;
                Gizmos.DrawLine(spawnPoint.position, actualAimHigh);
                Gizmos.DrawSphere(actualAimHigh, 0.1f); // Draw a small sphere at the aim point
            }
        }
    }
}

// This class will be added to the InkBall prefab to hold its specific data
// and handle its collision events, calling back to the parent InkAttack script.
public class InkBallBehavior : MonoBehaviour
{
    private int damageAmount;
    private float knockbackForce;
    private LayerMask playerLayer;
    private GameObject explosionEffect;
    private float lifetime;
    private GameObject spawnerObject; // Reference to the object that spawned this InkBall

    private bool hasBeenDestroyed = false; // Flag to prevent multiple destructions

    public void Initialize(
        int damage,
        float knockback,
        LayerMask pLayer,
        GameObject expEffect,
        float projLifetime,
        GameObject spawner)
    {
        damageAmount = damage;
        knockbackForce = knockback;
        playerLayer = pLayer;
        explosionEffect = expEffect;
        lifetime = projLifetime;
        spawnerObject = spawner;

        // Start lifetime countdown
        StartCoroutine(LifetimeCountdown());
    }

    private IEnumerator LifetimeCountdown()
    {
        yield return new WaitForSeconds(lifetime);
        if (!hasBeenDestroyed)
        {
            Debug.Log("InkBallBehavior: Lifetime ended. Destroying ink ball.");
            DestroyInkBall();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenDestroyed) return; // Prevent multiple calls if already destroying

        // Check for collision with Player
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Calculate knockback direction based on ink ball's velocity
                Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
                playerHealth.TakeDamage(damageAmount, knockbackForce, knockbackDirection);
            }
            DestroyInkBall();
        }
        else
        {
            // Only destroy if it's not the enemy itself or another InkBall
            // This prevents self-collision or collision with other projectiles from the same enemy
            // We also need to ensure it doesn't collide with the spawner itself.
            if (other.gameObject != spawnerObject && other.GetComponent<InkBallBehavior>() == null)
            {
                DestroyInkBall();
            }
        }
    }

    void DestroyInkBall()
    {
        if (hasBeenDestroyed) return; // Prevent multiple destructions
        hasBeenDestroyed = true;

        // Play explosion particle system
        if (explosionEffect != null)
        {
            GameObject explosionInstance = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            ParticleSystem ps = explosionInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                // Destroy the particle system GameObject after its duration
                Destroy(explosionInstance, ps.main.duration + ps.main.startLifetime.constantMax + 0.1f);
            }
            else
            {
                Debug.LogWarning("ExplosionEffect does not have a ParticleSystem component. Destroying after a default time.");
                Destroy(explosionInstance, 3f); // Default destroy if no PS found
            }
        }
        else
        {
            Debug.LogWarning("ExplosionEffect is null. No explosion effect will play.");
        }
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        hasBeenDestroyed = true;
    }
}

