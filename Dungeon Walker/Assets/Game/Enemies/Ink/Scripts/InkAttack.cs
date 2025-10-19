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
    [SerializeField] private bool isV2Attack = false;
    [Header("Audio Settings")]
    [SerializeField] private AudioClip attackSound; // Assign the attack sound clip here
    [SerializeField, Range(0f, 1f)] private float attackSoundVolume = 1f; // Volume slider for attack sound

    [Header("Aiming Settings")]
    [SerializeField] private Transform spawnPoint; // Where the InkBall will be spawned
    [SerializeField] private Transform aimPointLow; // Transform for low aim line
    [SerializeField] private Transform aimPointHigh; // Transform for high aim line
    [SerializeField] private Vector2 aimOffsetLow; // Offset for the low aim point
    [SerializeField] private Vector2 aimOffsetHigh; // Offset for the high aim point
    [SerializeField] private float projectileSpeed = 10f; // Speed of the ink ball

    [Header("Player Detection")]
    [SerializeField] private LayerMask playerLayer; // Layer of the player
    [SerializeField] private Transform detectionZoneTransform; // Transform defining the detection zone's center
    [SerializeField] private float detectionRangeX = 10f; // X-axis range for detection
    [SerializeField] private float detectionRangeY = 5f; // Y-axis range for detection
    private bool playerInDetectionRange = false; // Is the player currently in detection range?
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

    private void OnEnable()
    {
        ResetAttackState();
    }
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
            Debug.LogWarning("InkAttack: InkHealth component not found. Invincibility/hiding checks will be skipped.");
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
        if (playerTransform == null) return;

        // Check if the player is within the detection zone
        CheckPlayerDetection();

        // Only face the player if they are in range and the enemy is not hiding
        if (playerInDetectionRange && !IsHiding() && !IsPlayerInvisible() && !IsPlayerInvisible3antix())
        {
            Vector3 directionToPlayer = playerTransform.position - transform.position;
            if (directionToPlayer.x > 0 && !facingRight)
                Flip();
            else if (directionToPlayer.x < 0 && facingRight)
                Flip();
        }
    }
    private bool IsPlayerInvisible()
    {
        if (playerTransform == null) return false;
        PlayerInvisibility invis = playerTransform.GetComponent<PlayerInvisibility>();
        return invis != null && invis.IsInvisible();
    }
    private bool IsPlayerInvisible3antix()
    {
        if (playerTransform == null) return false;
        PlayerInvisibility3antix invis3antix = playerTransform.GetComponent<PlayerInvisibility3antix>();
        return invis3antix != null && invis3antix.IsInvisible();
    }
    private void CheckPlayerDetection()
    {
        if (detectionZoneTransform == null || playerTransform == null)
        {
            playerInDetectionRange = false;
            return;
        }

        // Use OverlapBox to check for the player within the defined range
        Vector2 boxCenter = detectionZoneTransform.position;
        Vector2 boxSize = new Vector2(detectionRangeX * 2, detectionRangeY * 2);
        playerInDetectionRange = Physics2D.OverlapBox(boxCenter, boxSize, 0, playerLayer);
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;

        // Your existing logic for un-flippable sprites is all that's needed here.
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

    public void ResetAttackState()
    {
        canAttack = true;
        playerInDetectionRange = false; // Reset detection state

        // If the enemy was flipped, reset it to the default facing direction
        if (!facingRight)
        {
            Flip();
        }

        // Stop any attack coroutines and restart the main attack loop
        StopAllCoroutines();
        StartCoroutine(AttackRoutine());
    }
    IEnumerator AttackRoutine()
    {

        while (true)
        {
            yield return new WaitForSeconds(attackCooldown);

            // Check if enemy can attack: must be in range, not on cooldown, and not hiding/invincible
            if (playerInDetectionRange && canAttack && !IsHiding() && !IsPlayerInvisible() && !IsPlayerInvisible3antix())
            {
                Attack();
            }
        }
    }

    // A helper method to check if the enemy is hiding/invincible via the InkHealth script
    private bool IsHiding()
    {
        if (inkHealthComponent != null)
        {
            return inkHealthComponent.IsInvincible();
        }
        return false; // If no health component, assume not hiding
    }
    void Attack()
    {
        if (IsPlayerInvisible() || IsPlayerInvisible3antix()) return;
        canAttack = false;

        if (attackSound != null)
        {
            AudioSource.PlayClipAtPoint(attackSound, spawnPoint.position, attackSoundVolume);
        }

        // --- THIS IS THE NEW, UPGRADED LOGIC ---

        // Define a helper method to fire one projectile.
        // This avoids duplicating code.
        void FireOneProjectile(Vector3 targetAimPosition)
        {
            Vector2 direction = (targetAimPosition - spawnPoint.position).normalized;
            GameObject inkBall = Instantiate(inkBallPrefab, spawnPoint.position, Quaternion.identity);

            Rigidbody2D rb = inkBall.GetComponent<Rigidbody2D>();
            if (rb == null) rb = inkBall.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.velocity = direction * projectileSpeed;

            Collider2D inkBallCollider = inkBall.GetComponent<Collider2D>();
            if (inkBallCollider == null)
            {
                CircleCollider2D circle = inkBall.AddComponent<CircleCollider2D>();
                circle.isTrigger = true;
            }
            else
            {
                inkBallCollider.isTrigger = true;
            }

            InkBallBehavior inkBallBehavior = inkBall.AddComponent<InkBallBehavior>();
            inkBallBehavior.Initialize(
                inkBallDamage,
                inkBallKnockbackForce,
                playerLayer,
                explosionParticleSystemPrefab,
                inkBallLifetime,
                gameObject
            );
        }

        // Get the correct aim positions based on facing direction.
        Vector2 currentAimOffsetLow = aimOffsetLow;
        Vector2 currentAimOffsetHigh = aimOffsetHigh;
        if (!facingRight)
        {
            currentAimOffsetLow.x *= -1;
            currentAimOffsetHigh.x *= -1;
        }
        Vector3 targetLow = aimPointLow.position + (Vector3)currentAimOffsetLow;
        Vector3 targetHigh = aimPointHigh.position + (Vector3)currentAimOffsetHigh;

        // The "Brain" Logic: Check if this is a V2 attack.
        if (isV2Attack)
        {
            // V2 ATTACK: Fire both projectiles at the same time.
            FireOneProjectile(targetLow);
            FireOneProjectile(targetHigh);
        }
        else
        {
            // NORMAL ATTACK: Fire one projectile randomly, high or low.
            if (Random.Range(0, 2) == 0)
            {
                FireOneProjectile(targetLow);
            }
            else
            {
                FireOneProjectile(targetHigh);
            }
        }
        // --- END OF NEW LOGIC ---

        StartCoroutine(ResetAttackCooldown());
    }


    IEnumerator ResetAttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    // Draw gizmos in editor to visualize aim lines and detection zone
    void OnDrawGizmosSelected()
    {
        // Draw Aiming Gizmos
        if (spawnPoint != null)
        {
            Gizmos.color = Color.red;
            Vector2 gizmoAimOffsetLow = aimOffsetLow;
            Vector2 gizmoAimOffsetHigh = aimOffsetHigh;

            // Adjust gizmo aim offsets based on current facing direction in the editor
            float editorFacingDirection = (transform.localScale.x > 0) ? 1f : -1f;
            if (editorFacingDirection < 0)
            {
                gizmoAimOffsetLow.x *= -1;
                gizmoAimOffsetHigh.x *= -1;
            }

            if (aimPointLow != null)
            {
                Vector3 actualAimLow = aimPointLow.position + (Vector3)gizmoAimOffsetLow;
                Gizmos.DrawLine(spawnPoint.position, actualAimLow);
                Gizmos.DrawSphere(actualAimLow, 0.1f);
            }
            if (aimPointHigh != null)
            {
                Vector3 actualAimHigh = aimPointHigh.position + (Vector3)gizmoAimOffsetHigh;
                Gizmos.DrawLine(spawnPoint.position, actualAimHigh);
                Gizmos.DrawSphere(actualAimHigh, 0.1f);
            }
        }

        // Draw Detection Zone Gizmo
        if (detectionZoneTransform != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 boxCenter = detectionZoneTransform.position;
            Vector3 boxSize = new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0);
            Gizmos.DrawWireCube(boxCenter, boxSize);
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
            DestroyInkBall();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenDestroyed) return;

        // Check for collision with Player
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
                playerHealth.TakeDamage(damageAmount, knockbackForce, knockbackDirection);
            }
            L3antixHealth l3antixHealth = other.GetComponent<L3antixHealth>();
            if (l3antixHealth != null)
            {
                Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
                l3antixHealth.TakeDamage(damageAmount, knockbackForce, knockbackDirection);
            }
            DestroyInkBall();
        }
        else
        {
            // Prevent self-collision or collision with other projectiles from the same enemy
            if (other.gameObject != spawnerObject && other.GetComponent<InkBallBehavior>() == null)
            {
                DestroyInkBall();
            }
        }
    }

    void DestroyInkBall()
    {
        if (hasBeenDestroyed) return;
        hasBeenDestroyed = true;

        if (explosionEffect != null)
        {
            GameObject explosionInstance = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            ParticleSystem ps = explosionInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(explosionInstance, ps.main.duration);
            }
            else
            {
                Destroy(explosionInstance, 3f);
            }
        }
        Destroy(gameObject);
    }
}

