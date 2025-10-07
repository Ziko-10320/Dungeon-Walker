using System.Collections;
using UnityEngine;

public class FleaChargeAttack : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Animator fleaAnimator;
    [SerializeField] private FleaFollow followScript;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] public Transform playerTransform;

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
    [Tooltip("Nombre maximum de charges consécutives si l'attaque rate.")]
    [SerializeField] private int maxConsecutiveCharges = 2;

    [Header("Visual Effects")]
    public ParticleSystem BurstDust;
    public ParticleSystem BurstDust2;

    // Attack Sound Variables
    public AudioClip attackSoundClip; // Audio clip to play when attacking
    [Range(0f, 1f)] public float attackSoundVolume = 0.7f; // Volume slider added here

    private AudioSource audioSource; // Reference to the AudioSource component

    private bool playerInRange = false;
    private bool canAttack = true;
    public bool isAttacking = false;
    private float decisionTimer = 0f;
    private float originalDrag;

    private int isAnticipatingHash;
    private int isChargingHash;
    private float checkTimer = 0f;
    private const float CHECK_INTERVAL = 0.3f;

    private Collider2D[] hitResults = new Collider2D[1];
    void Awake()
    {
        if (fleaAnimator == null) fleaAnimator = GetComponent<Animator>();
        if (followScript == null) followScript = GetComponent<FleaFollow>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        originalDrag = rb.drag;

       
        isAnticipatingHash = Animator.StringToHash("IsAnticipating");
        isChargingHash = Animator.StringToHash("IsCharging");

        // Get or add the AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false; // Ensure it doesn't play automatically
        audioSource.volume = attackSoundVolume; // Set initial volume
    }
    void OnEnable()
    {
        PlayerInvisibility.OnInvisibilityChanged += HandleInvisibility;
        PlayerInvisibility3antix.OnInvisibilityChanged += HandleInvisibility;
    }

    void OnDisable()
    {
        PlayerInvisibility.OnInvisibilityChanged -= HandleInvisibility;
        PlayerInvisibility3antix.OnInvisibilityChanged -= HandleInvisibility;
    }

    private void HandleInvisibility(bool invisible)
    {
        if (invisible)
        {
            playerTransform = null;
            if (followScript != null) followScript.playerTransform = null;
        }
        else
        {
            // reacquire
            FindPlayerAgain();
            if (followScript != null) followScript.playerTransform = playerTransform;
        }
    }

    private void FindPlayerAgain()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }


    void Update()
    {
        PlayerInvisibility invis = playerTransform.GetComponent<PlayerInvisibility>();
        PlayerInvisibility3antix invis3antix = playerTransform.GetComponent<PlayerInvisibility3antix>();
        if (invis != null && invis.IsInvisible()) return;
        if (invis3antix != null && invis3antix.IsInvisible()) return;
        if (playerTransform == null || isAttacking || !canAttack) return;


        float sqrDistanceToPlayer = (playerTransform.position - transform.position).sqrMagnitude;
        playerInRange = sqrDistanceToPlayer <= (attackRange * attackRange);

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

        // Play attack sound if assigned
        if (attackSoundClip != null && audioSource != null)
        {
            audioSource.volume = attackSoundVolume; // Apply volume
            audioSource.PlayOneShot(attackSoundClip);
        }

        int chargesMade = 0;
        bool hitPlayer = false;

        while (chargesMade < maxConsecutiveCharges && !hitPlayer)
        {
            chargesMade++;
            rb.velocity = Vector2.zero;

            fleaAnimator.SetBool(isAnticipatingHash, true);
            Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
            FlipTowards(directionToPlayer);
            yield return new WaitForSeconds(anticipationDuration);

            if (BurstDust != null) BurstDust.Play();
            if (BurstDust2 != null) BurstDust2.Play();
            fleaAnimator.SetBool(isAnticipatingHash, false);
            fleaAnimator.SetBool(isChargingHash, true);

            rb.drag = 0f;
            rb.AddForce(new Vector2(directionToPlayer.x * chargeForce, 0), ForceMode2D.Impulse);
            rb.drag = chargeDrag;

            float chargeTimer = 0f;
            while (chargeTimer < chargeDuration)
            {
                if (CheckForPlayerHit())
                {
                    hitPlayer = true;
                    break;
                }
                chargeTimer += Time.deltaTime;
                yield return null;
            }

            // --- CORRECTION APPLIQUÉE ICI ---
            // On s'assure d'arrêter le mouvement et l'animation APRÈS la boucle de charge,
            // que le joueur ait été touché (break) ou que le temps soit écoulé.
            rb.velocity = Vector2.zero;
            fleaAnimator.SetBool(isChargingHash, false); // <-- C'est la ligne clé !

            if (hitPlayer)
            {
                break;
            }

            if (chargesMade < maxConsecutiveCharges)
            {
                yield return new WaitForSeconds(0.2f);
            }
        }

        // --- Nettoyage final ---
        rb.drag = originalDrag;
        if (followScript != null) followScript.enabled = true;
        isAttacking = false;

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
        ResetDecisionTimer();
    }

    private bool CheckForPlayerHit()
    {
        Collider2D playerHit = Physics2D.OverlapCircle(rb.position, 0.5f, LayerMask.GetMask("Player"));
        if (playerHit != null)
        {
            PlayerHealth playerHealth = playerHit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Vector2 knockbackDirection = (playerHit.transform.position - transform.position).normalized;
                knockbackDirection.y = 0.5f;
                playerHealth.TakeDamage(attackDamage, knockbackForce, knockbackDirection.normalized);
            }
            L3antixHealth l3antixHealth = playerHit.GetComponent<L3antixHealth>();
            if (l3antixHealth != null)
            {
                Vector2 knockbackDirection = (playerHit.transform.position - transform.position).normalized;
                knockbackDirection.y = 0.5f;
                l3antixHealth.TakeDamage(attackDamage, knockbackForce, knockbackDirection.normalized);
            }
            return true;
        }
        return false;
    }

    private void FlipTowards(Vector2 direction)
    {
        float scaleValue = Mathf.Abs(transform.localScale.x);
        if (direction.x > 0)
        {
            transform.localScale = new Vector3(scaleValue, transform.localScale.y, transform.localScale.z);
        }
        else if (direction.x < 0)
        {
            transform.localScale = new Vector3(-scaleValue, transform.localScale.y, transform.localScale.z);
        }

        float targetFlipX = transform.localScale.x > 0 ? 0 : 1;
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