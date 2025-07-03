using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FleaChargeAttack : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Animator fleaAnimator;
    [SerializeField] private FleaFollow followScript;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform playerTransform;
    // NOTE: We are not using the damageHitbox in this version, as we are returning to the OverlapCircle logic.

    [Header("Attack Range & Timing")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float minTimeBeforeAttack = 1.0f;
    [SerializeField] private float maxTimeBeforeAttack = 3.0f;
    [SerializeField] private float attackCooldown = 2.0f;

    [Header("Attack Properties")]
    [SerializeField] private float anticipationDuration = 0.5f;
    [SerializeField] private float chargeForce = 40f;
    [SerializeField] private float chargeDuration = 0.4f;
    [SerializeField] private float chargeDrag = 5f;
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float knockbackForce = 20f;
    public ParticleSystem BurstDust;
    public ParticleSystem BurstDust2;

    private bool playerInRange = false;
    private bool canAttack = true;
    private bool isAttacking = false;
    private float decisionTimer = 0f;
    private float originalDrag;

    private int isAnticipatingHash;
    private int isChargingHash;

    void Awake()
    {
        if (fleaAnimator == null) fleaAnimator = GetComponent<Animator>();
        if (followScript == null) followScript = GetComponent<FleaFollow>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        originalDrag = rb.drag;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        isAnticipatingHash = Animator.StringToHash("IsAnticipating");
        isChargingHash = Animator.StringToHash("IsCharging");
    }

    void Update()
    {
        if (playerTransform == null || isAttacking || !canAttack) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        playerInRange = distanceToPlayer <= attackRange;

        if (playerInRange)
        {
            decisionTimer -= Time.deltaTime;
            if (decisionTimer <= 0)
            {
                StartCoroutine(PerformChargeAttack());
            }
        }
        else
        {
            ResetDecisionTimer();
        }
    }

    private void ResetDecisionTimer()
    {
        decisionTimer = Random.Range(minTimeBeforeAttack, maxTimeBeforeAttack);
    }

    private IEnumerator PerformChargeAttack()
    {
        isAttacking = true;
        canAttack = false;

        if (followScript != null) followScript.enabled = false;
        rb.velocity = Vector2.zero;

        fleaAnimator.SetBool(isAnticipatingHash, true);

        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        FlipTowards(directionToPlayer);

        yield return new WaitForSeconds(anticipationDuration);

        if (BurstDust != null) BurstDust.Play();
        if (BurstDust2 != null) BurstDust2.Play(); // Corrected this line

        fleaAnimator.SetBool(isAnticipatingHash, false);
        fleaAnimator.SetBool(isChargingHash, true);

        rb.drag = 0f;
        rb.AddForce(new Vector2(directionToPlayer.x * chargeForce, 0), ForceMode2D.Impulse);
        rb.drag = chargeDrag;

        float chargeTimer = 0f;
        while (chargeTimer < chargeDuration)
        {
            // RESTORED: Using CheckForPlayerHit() during the charge
            if (CheckForPlayerHit())
            {
                break;
            }
            chargeTimer += Time.deltaTime;
            yield return null;
        }

        rb.velocity = Vector2.zero;
        rb.drag = originalDrag;
        fleaAnimator.SetBool(isChargingHash, false);

        if (followScript != null) followScript.enabled = true;

        isAttacking = false;

        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
        ResetDecisionTimer();
    }

    // RESTORED: The CheckForPlayerHit method using OverlapCircle
    private bool CheckForPlayerHit()
    {
        // Using a small circle overlap to detect the player
        // Make sure your player is on the "Player" layer
        Collider2D playerHit = Physics2D.OverlapCircle(rb.position, 0.5f, LayerMask.GetMask("Player"));

        if (playerHit != null)
        {
            Debug.Log("Flea hit the player via OverlapCircle!");

            PlayerHealth playerHealth = playerHit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Vector2 knockbackDirection = (playerHit.transform.position - transform.position).normalized;
                knockbackDirection.y = 0.5f;

                playerHealth.TakeDamage(attackDamage, knockbackForce, knockbackDirection.normalized);
            }
            return true; // Hit was successful
        }
        return false; // No hit
    }

    // FIXED: The FlipTowards method with your original structure
    private void FlipTowards(Vector2 direction)
    {
        // Flip the flea's scale
        if (direction.x > 0 && transform.localScale.x < 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (direction.x < 0 && transform.localScale.x > 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

        // --- THIS IS THE FIXED PART ---
        // Determine the target flip value based on the flea's CURRENT facing direction
        // If localScale.x is positive, it's facing RIGHT, so particles should NOT be flipped (flip value = 0).
        // If localScale.x is negative, it's facing LEFT, so particles SHOULD be flipped (flip value = 1).
        float targetFlipX = transform.localScale.x > 0 ? 0 : 1;

        // Apply the calculated flip value to the particle systems
        if (BurstDust != null)
        {
            var renderer = BurstDust.GetComponent<ParticleSystemRenderer>();
            renderer.flip = new Vector2(targetFlipX, 0);
        }
        if (BurstDust2 != null)
        {
            var renderer = BurstDust2.GetComponent<ParticleSystemRenderer>();
            renderer.flip = new Vector2(targetFlipX, 0);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
