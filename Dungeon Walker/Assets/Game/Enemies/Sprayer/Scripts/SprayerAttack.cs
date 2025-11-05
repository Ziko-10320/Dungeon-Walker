using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SprayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 2f;
    public float baseDamagePerSecond = 10f; 
    public float damagePerSecond;
    [SerializeField] private float damageDuration = 1f;
    [SerializeField] private float damageDelay = 0.3f;
    [SerializeField] private float damageInterval = 0.5f;
    [Header("VFX Settings")]
    [SerializeField] private GameObject sprayEffectPrefab;
    [Tooltip("Assign a child GameObject where the spray VFX should spawn.")]
    [SerializeField] private Transform sprayEffectSpawnPoint;

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
  
    [SerializeField] private int effectPoolSize = 3;

    // The local pool and active effect reference
    private Queue<ParticleSystem> effectPool;
    private ParticleSystem activeSprayVFX;
    [Header("Performance / Time Slicing")]
    [Tooltip("How many SECONDS to wait before checking if the sprayer can attack.")]
    [Range(0.1f, 1.0f)]
    public float attackCheckInterval = 0.33f; // Check about 3 times per second

    private float attackCheckTimer = 0f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        CreateLocalVFXPool();
    }
    private void CreateLocalVFXPool()
    {
        effectPool = new Queue<ParticleSystem>();
        for (int i = 0; i < effectPoolSize; i++)
        {
            GameObject obj = Instantiate(sprayEffectPrefab, sprayEffectSpawnPoint);
            ParticleSystem ps = obj.GetComponent<ParticleSystem>();
            obj.SetActive(false);
            effectPool.Enqueue(ps);
        }
    }
    void Start()
    {
        sprayerAnimator = GetComponent<Animator>();
        lastAttackTime = -attackCooldown;

        if (sprayEffectSpawnPoint == null)
        {
            Debug.LogError("SprayerAttack: Spray Effect Spawn Point is not assigned! The VFX needs a location to spawn.", this);
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
        // --- 1. THE TIMER ---
        attackCheckTimer += Time.deltaTime;

        // --- 2. THE "THINKING" BLOCK ---
        if (attackCheckTimer >= attackCheckInterval)
        {
            attackCheckTimer = 0f; // Reset the timer

            // This is your original Update logic, now running periodically.
            if (isAttacking) return;

            if (playerTransform == null)
            {
                // Try to find the player if the reference is lost
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
                else return; // Exit if still no player
            }

            PlayerInvisibility invis = playerTransform.GetComponent<PlayerInvisibility>();
            if (invis != null && invis.IsInvisible()) return;
            PlayerInvisibility3antix invis3antix = playerTransform.GetComponent<PlayerInvisibility3antix>();
            if (invis3antix != null && invis3antix.IsInvisible()) return;

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                if (IsPlayerInAttackRange())
                {
                    StartAttack();
                }
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

        if (effectPool.Count > 0)
        {
            activeSprayVFX = effectPool.Dequeue();
            activeSprayVFX.gameObject.SetActive(true);
            activeSprayVFX.Play();
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
                if (activeSprayVFX != null)
                {
                    activeSprayVFX.Stop();
                    activeSprayVFX.gameObject.SetActive(false);
                    effectPool.Enqueue(activeSprayVFX);
                    activeSprayVFX = null;
                }
                break; // exit attack loop
            }
            if (IsPlayerInvisible3antix())
            {
                if (activeSprayVFX != null)
                {
                    activeSprayVFX.Stop();
                    activeSprayVFX.gameObject.SetActive(false);
                    effectPool.Enqueue(activeSprayVFX);
                    activeSprayVFX = null;
                }
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
        if (activeSprayVFX != null)
        {
            activeSprayVFX.Stop();
            activeSprayVFX.gameObject.SetActive(false);
            effectPool.Enqueue(activeSprayVFX);
            activeSprayVFX = null;
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
