using UnityEngine;
using System.Collections;

public class SprayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float damagePerSecond = 10f;
    [SerializeField] private float damageDuration = 1f;
    [SerializeField] private float damageDelay = 0.3f;
    [SerializeField] private float damageInterval = 0.5f;
    [SerializeField] private ParticleSystem sprayParticles;

    [Header("Damage Zone Settings")]
    [SerializeField] private Vector2 damageZoneSize = new Vector2(2f, 1f);
    [SerializeField] private Vector2 damageZoneOffset = new Vector2(1f, 0f);
    [SerializeField] private LayerMask playerLayer;
    private Animator sprayerAnimator;

    [Header("Movement Control")]
    [SerializeField] private SprayerFollow sprayerFollowScript;

    [Header("Attack Sound Settings")]
    [SerializeField] private AudioClip attackSoundClip;
    [SerializeField][Range(0f, 1f)] private float attackSoundVolume = 0.7f; // Volume slider

    private AudioSource audioSource;
    private float lastAttackTime;
    private bool isAttacking = false;
    private float currentSprayerDirection = 1f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = attackSoundVolume; // Set initial volume
    }

    void Start()
    {
        if (sprayerAnimator == null) sprayerAnimator = GetComponent<Animator>();
        lastAttackTime = -attackCooldown;

        if (sprayParticles == null)
        {
            Debug.LogError("SprayerAttack: Spray Particles not assigned!", this);
            enabled = false;
            return;
        }

        if (sprayerFollowScript == null)
        {
            Debug.LogError("SprayerAttack: SprayerFollow script not assigned. Damage zone and attack range will not flip correctly.", this);
            enabled = false;
            return;
        }

        currentSprayerDirection = sprayerFollowScript.transform.localScale.x > 0 ? 1f : -1f;
    }

    void Update()
    {
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
        Collider2D playerInRange = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
        return playerInRange != null;
    }

    private void StartAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        sprayParticles.Play();
        DisableMovement();

        // Play attack sound with adjustable volume
        if (attackSoundClip != null && audioSource != null)
        {
            audioSource.volume = attackSoundVolume;
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
            ApplyDamageTick();
            timer += damageInterval;
            yield return new WaitForSeconds(damageInterval);
        }
        EndAttack();
    }

    private void EndAttack()
    {
        isAttacking = false;
        sprayParticles.Stop();
        EnableMovement();
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
        Gizmos.color = Color.red;
        Vector2 attackOrigin = (Vector2)transform.position + FlippedDamageZoneOffset;
        Gizmos.DrawWireCube(attackOrigin, damageZoneSize);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}