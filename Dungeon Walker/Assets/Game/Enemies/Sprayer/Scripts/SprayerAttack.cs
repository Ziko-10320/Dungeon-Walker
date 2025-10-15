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

    [Header("Movement Control")]
    [SerializeField] private SprayerFollow sprayerFollowScript; // Used to pause movement

    [Header("Attack Sound Settings")]
    [SerializeField] private AudioClip attackSoundClip;
    [SerializeField][Range(0f, 1f)] private float attackSoundVolume = 0.7f;

    // *** NEW ROBUST DIRECTION REFERENCE FOR BONE-BASED ENEMIES ***
    [Header("Direction Reference (for bone-based enemies)")]
    [Tooltip("Assign a child GameObject/bone that consistently points in the Sprayer\"s visual forward direction (e.g., a hand, weapon, or an empty child object placed in front of the character). This is crucial for accurate flipping.")]
    [SerializeField] private Transform visualDirectionReference;

    private Animator sprayerAnimator;
    private AudioSource audioSource;
    private float lastAttackTime;
    private bool isAttacking = false;
    private float attackDirection; // Stores the direction (1 or -1) when attack starts
    private Transform playerTransform;
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        sprayerAnimator = GetComponent<Animator>();
        lastAttackTime = -attackCooldown;

        if (sprayParticles == null)
        {
            Debug.LogError("SprayerAttack: Spray Particles not assigned!", this);
            enabled = false;
        }

        // Warn if visualDirectionReference is not set, as it's crucial for bone-based enemies
        if (visualDirectionReference == null)
        {
            Debug.LogWarning("SprayerAttack: visualDirectionReference is not assigned. Damage zone flipping might not be reliable for bone-based enemies. Please assign a child bone/GameObject that points forward.", this);
        }
    }
    public void InitializeAndReset(Transform player)
    {
        // 1. Forcefully get the player reference.
        playerTransform = player;

        // 2. Reset the ACTUAL state variables from your script.
        isAttacking = false; // THIS IS THE KEY FIX. It allows the Update loop to run again.
        lastAttackTime = -attackCooldown; // This resets the attack cooldown timer.

        // 3. Stop any old attack coroutines that might be stuck.
        StopAllCoroutines();

        // 4. Ensure the script is enabled and ready to go.
        this.enabled = true;
    }


    void Update()
    {
        if (isAttacking)
        {
            return; // Don\"t do anything else while an attack is in progress
        }
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            PlayerInvisibility invis = playerObj.GetComponent<PlayerInvisibility>();
            PlayerInvisibility3antix invis3antix = playerObj.GetComponent<PlayerInvisibility3antix>();
            if (invis != null && invis.IsInvisible())
            {
                // Stop particles & disable movement
                if (sprayParticles != null && sprayParticles.isPlaying) sprayParticles.Stop();
                EnableMovement(); // Ensure enemy can move
                return;
            }
            if (invis3antix != null && invis3antix.IsInvisible())
            {
                // Stop particles & disable movement
                if (sprayParticles != null && sprayParticles.isPlaying) sprayParticles.Stop();
                EnableMovement(); // Ensure enemy can move
                return;
            }
        }
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            if (IsPlayerInAttackRange())
            {
                StartAttack();
            }
        }
    }

    private bool IsPlayerInAttackRange()
    {
        return Physics2D.OverlapCircle(transform.position, attackRange, playerLayer) != null;
    }

    private void StartAttack()
    {
        if (IsPlayerInvisible())
        {
            // Player is invisible → cancel attack
            return;
        }
        if (IsPlayerInvisible3antix())
        {
            // Player is invisible → cancel attack
            return;
        }
        isAttacking = true;
        lastAttackTime = Time.time;

        // *** THE MOST ROBUST FLIPPING LOGIC FOR BONE-BASED ENEMIES ***
        // Determine attack direction based on the visualDirectionReference\"s world X-axis.
        // We check if its 'right' vector (local X-axis) is pointing globally right or left.
        if (visualDirectionReference != null)
        {
            attackDirection = Mathf.Sign(visualDirectionReference.right.x);
        }
        else
        {
            // Fallback to localScale.x if no visualDirectionReference is assigned.
            // This might not be reliable for bone-based enemies, but it's a last resort.
            attackDirection = Mathf.Sign(transform.localScale.x);
            Debug.LogWarning("Using transform.localScale.x for flipping. Assign visualDirectionReference for more reliable flipping with bone-based enemies.", this);
        }

        DisableMovement();

        if (sprayParticles != null)
        {
            sprayParticles.Play();
        }

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
            if (IsPlayerInvisible())
            {
                // Stop particles immediately
                if (sprayParticles != null) sprayParticles.Stop();
                break; // exit attack loop
            }
            if (IsPlayerInvisible3antix())
            {
                // Stop particles immediately
                if (sprayParticles != null) sprayParticles.Stop();
                break; // exit attack loop
            }
            ApplyDamageTick();
            timer += damageInterval;
            yield return new WaitForSeconds(damageInterval);
        }
        EndAttack();
    } 
    private bool IsPlayerInvisible()
    {
        // Find player in the layer
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
        if (playerCollider == null) return false;

        PlayerInvisibility invis = playerCollider.GetComponent<PlayerInvisibility>();
       
        return invis != null && invis.IsInvisible();

    }
    private bool IsPlayerInvisible3antix()
    {
        // Find player in the layer
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
        if (playerCollider == null) return false;

        PlayerInvisibility3antix invis3antix = playerCollider.GetComponent<PlayerInvisibility3antix>();

        return invis3antix != null && invis3antix.IsInvisible();

    }
    private void ApplyDamageTick()
    {

        // Use the stored \"attackDirection\" for consistent damage zone position
        Vector2 flippedOffset = new Vector2(damageZoneOffset.x * attackDirection, damageZoneOffset.y);
        Vector2 attackOrigin = (Vector2)transform.position + flippedOffset;

        Collider2D[] hits = Physics2D.OverlapBoxAll(attackOrigin, damageZoneSize, 0f, playerLayer);

        foreach (Collider2D hit in hits)
        {
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                PlayerInvisibility invis = hit.GetComponent<PlayerInvisibility>();
                PlayerInvisibility3antix invis3antix = hit.GetComponent<PlayerInvisibility3antix>();
                if (invis != null && invis.IsInvisible())
                {
                    // Player is invisible, skip damage
                    continue;
                }
                if (invis3antix != null && invis3antix.IsInvisible())
                {
                    // Player is invisible, skip damage
                    continue;
                }
                float damageThisTick = damagePerSecond * damageInterval;
                playerHealth.TakeDamage(Mathf.RoundToInt(damageThisTick), 0f, Vector2.zero);
            }
            L3antixHealth l3antixHealth = hit.GetComponent<L3antixHealth>();
            if (l3antixHealth != null)
            {
                float damageThisTick = damagePerSecond * damageInterval;
                l3antixHealth.TakeDamage(Mathf.RoundToInt(damageThisTick), 0f, Vector2.zero);
            }
        }
    }

    private void EndAttack()
    {
        isAttacking = false;
        if (sprayParticles != null)
        {
            sprayParticles.Stop();
        }
        EnableMovement();
    }

    private void DisableMovement()
    {
        if (sprayerFollowScript != null)
        {
            sprayerFollowScript.enabled = false;
        }
        if (sprayerAnimator != null)
        {
            sprayerAnimator.SetBool("IsWalking", false);
        }
    }

    private void EnableMovement()
    {
        if (sprayerFollowScript != null)
        {
            sprayerFollowScript.enabled = true;
        }
        if (sprayerAnimator != null)
        {
            sprayerAnimator.SetBool("IsWalking", true);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.red;

        // Determine gizmo direction based on the visualDirectionReference for editor consistency
        float gizmoDirection = 1f;
        if (visualDirectionReference != null)
        {
            gizmoDirection = Mathf.Sign(visualDirectionReference.right.x);
        }
        else
        {
            // Fallback to localScale.x for gizmo if no reference is assigned
            gizmoDirection = Mathf.Sign(transform.localScale.x);
        }

        Vector2 flippedOffset = new Vector2(damageZoneOffset.x * gizmoDirection, damageZoneOffset.y);
        Vector2 attackOrigin = (Vector2)transform.position + flippedOffset;
        Gizmos.DrawWireCube(attackOrigin, damageZoneSize);
    }
}
