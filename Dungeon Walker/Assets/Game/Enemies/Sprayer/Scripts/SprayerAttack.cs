using UnityEngine;
using System.Collections;

public class SprayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float damagePerSecond = 10f;
    [SerializeField] private float damageDuration = 1f; // New: How long the damage is applied
    [SerializeField] private float damageDelay = 0.3f; // New: Delay before damage starts
    [SerializeField] private float damageInterval = 0.5f; // New: Time between damage ticks
    [SerializeField] private ParticleSystem sprayParticles;

    [Header("Damage Zone Settings")]
    [SerializeField] private Vector2 damageZoneSize = new Vector2(2f, 1f);
    [SerializeField] private Vector2 damageZoneOffset = new Vector2(1f, 0f);
    [SerializeField] private LayerMask playerLayer;
    private Animator sprayerAnimator;
    [Header("Movement Control")]
    // [SerializeField] private MonoBehaviour movementScript; // Removed: No longer needed to disable entire script
    [SerializeField] private SprayerFollow sprayerFollowScript; // Reference to the Sprayer\"s movement script

    // Attack Sound Variables
    public AudioClip attackSoundClip; // Audio clip to play when attacking
    private AudioSource audioSource; // Reference to the AudioSource component

    private float lastAttackTime;
    private bool isAttacking = false;
    private float currentSprayerDirection = 1f; // 1 for right, -1 for left

    void Awake()
    {
        // Get or add the AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false; // Ensure it doesn\'t play automatically
    }

    void Start()
    {
        if (sprayerAnimator == null) sprayerAnimator = GetComponent<Animator>();
        lastAttackTime = -attackCooldown; // Allow immediate attack at start

        if (sprayParticles == null)
        {
            Debug.LogError("SprayerAttack: Spray Particles not assigned!", this);
            enabled = false;
            return;
        }

        // if (movementScript == null)
        // {
        //     Debug.LogWarning("SprayerAttack: Movement script not assigned. Sprayer will not stop movement during attack.", this);
        // }

        if (sprayerFollowScript == null)
        {
            Debug.LogError("SprayerAttack: SprayerFollow script not assigned. Damage zone and attack range will not flip correctly.", this);
            enabled = false;
            return;
        }
        // Initialize currentSprayerDirection based on the SprayerFollow script\"s initial direction
        currentSprayerDirection = sprayerFollowScript.transform.localScale.x > 0 ? 1f : -1f;
    }

    void Update()
    {
        // Update currentSprayerDirection based on the Sprayer\"s actual facing direction
        currentSprayerDirection = transform.localScale.x > 0 ? 1f : -1f;

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            if (IsPlayerInAttackRange())
            {
                StartAttack();
            }
        }
    }

    private Vector2 FlippedDamageZoneOffset
    {
        get { return new Vector2(damageZoneOffset.x * currentSprayerDirection, damageZoneOffset.y); }
    }

    private bool IsPlayerInAttackRange()
    {
        // Check if player is within the general circular attack range first
        Collider2D playerInRange = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
        return playerInRange != null;
    }

    private void StartAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        sprayParticles.Play();
        DisableMovement(); // Use the new method

        // Play attack sound if assigned
        if (attackSoundClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSoundClip);
        }

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(damageDelay);

        float timer = 0f;
        while (timer < damageDuration)
        {
            ApplyDamageTick(); // Call a new method for a single damage tick
            timer += damageInterval;
            yield return new WaitForSeconds(damageInterval);
        }
        EndAttack();
    }

    private void EndAttack()
    {
        isAttacking = false;
        sprayParticles.Stop();
        EnableMovement(); // Use the new method
    }

    private void ApplyDamageTick()
    {
        Vector2 attackOrigin = (Vector2)transform.position + FlippedDamageZoneOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(attackOrigin, damageZoneSize, 0f, playerLayer);

        foreach (Collider2D hit in hits)
        {
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Calculate damage per tick based on damagePerSecond and damageInterval
                float damageThisTick = damagePerSecond * damageInterval;
                playerHealth.TakeDamage(Mathf.RoundToInt(damageThisTick), 0f, Vector2.zero);
            }
        }
    }

    private void DisableMovement()
    {
        if (sprayerFollowScript != null)
        {
            sprayerFollowScript.CanMove = false;
        }
        sprayerAnimator.SetBool("IsWalking", false);
    }

    private void EnableMovement()
    {
        if (sprayerFollowScript != null)
        {
            sprayerFollowScript.CanMove = true;
        }
        sprayerAnimator.SetBool("IsWalking", true);
    }




    void OnDrawGizmosSelected()
    {
        // Visualize Damage Zone
        Gizmos.color = Color.red;
        Vector2 attackOrigin = (Vector2)transform.position + FlippedDamageZoneOffset;
        Gizmos.DrawWireCube(attackOrigin, damageZoneSize);

        // Visualize Attack Range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

