using UnityEngine;
using System.Collections;

public class VolcanoObstacle : MonoBehaviour
{
    [Header("Timing Settings")]
    [SerializeField] private float activeDuration = 1.5f;
    [SerializeField] private float dormantDuration = 2.0f;

    [Header("Damage & Knockback")]
    [Tooltip("The center point from where the damage radius originates.")]
    [SerializeField] private Transform damagePoint;
    [Tooltip("The radius of the damage area.")]
    [SerializeField] private float damageRadius = 2f;
    [Tooltip("How often the volcano checks for players to damage while active (in seconds).")]
    [SerializeField] private float damageTickRate = 0.2f;
    [SerializeField] private int damagePerTick = 10;
    [SerializeField] private float knockbackForce = 35f;

    [Header("Component References")]
    [SerializeField] private ParticleSystem mainParticles;
    // We no longer need the damageCollider reference.

    private Coroutine cycleCoroutine;
    private WaitForSeconds activeWait;
    private WaitForSeconds dormantWait;
    private LayerMask playerLayer; // We will get this automatically.

    void Awake()
    {
        activeWait = new WaitForSeconds(activeDuration);
        dormantWait = new WaitForSeconds(dormantDuration);
        if (mainParticles == null) mainParticles = GetComponentInChildren<ParticleSystem>();

        // If no specific damage point is assigned, default to this object's transform.
        if (damagePoint == null)
        {
            damagePoint = this.transform;
        }

        // Get the player layer automatically by its name.
        playerLayer = LayerMask.GetMask("Player");
    }

    void OnEnable()
    {
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);
        cycleCoroutine = StartCoroutine(VolcanoCycle());
    }

    void OnDisable()
    {
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);
        cycleCoroutine = null;
        SetActiveState(false);
    }

    private IEnumerator VolcanoCycle()
    {
        // Start dormant.
        SetActiveState(false);
        yield return dormantWait;

        while (true)
        {
            // --- ACTIVE PHASE ---
            SetActiveState(true);
            // Start a new coroutine that will handle the damage ticks.
            Coroutine damageRoutine = StartCoroutine(DamageTickLoop());
            yield return activeWait;
            // Stop the damage tick loop when the active phase ends.
            StopCoroutine(damageRoutine);


            // --- DORMANT PHASE ---
            SetActiveState(false);
            yield return dormantWait;
        }
    }

    // This is the new damage logic that runs only when the volcano is active.
    private IEnumerator DamageTickLoop()
    {
        WaitForSeconds tickWait = new WaitForSeconds(damageTickRate);
        while (true)
        {
            // Find all colliders on the player layer within our damage radius.
            Collider2D[] playersInRange = Physics2D.OverlapCircleAll(damagePoint.position, damageRadius, playerLayer);

            foreach (Collider2D playerCollider in playersInRange)
            {
                // --- THIS IS THE NEW KNOCKBACK LOGIC ---
                // 1. Calculate the direction from the volcano's center to the player.
                Vector2 directionToPlayer = (playerCollider.transform.position - damagePoint.position).normalized;

                // 2. Create a PURELY horizontal knockback vector.
                // We use Mathf.Sign to get either 1 (right) or -1 (left).
                Vector2 horizontalKnockback = new Vector2(Mathf.Sign(directionToPlayer.x), 0);
                // --- END OF NEW KNOCKBACK LOGIC ---

                // Try to get the health script and apply damage with the new knockback.
                if (playerCollider.TryGetComponent<PlayerHealth>(out var playerHealth))
                {
                    playerHealth.TakeDamage(damagePerTick, knockbackForce, horizontalKnockback);
                }
                else if (playerCollider.TryGetComponent<L3antixHealth>(out var l3antixHealth))
                {
                    l3antixHealth.TakeDamage(damagePerTick, knockbackForce, horizontalKnockback);
                }
            }
            // Wait for the next tick.
            yield return tickWait;
        }
    }


    public void SetActiveState(bool isActive)
    {
        if (mainParticles != null)
        {
            if (isActive) mainParticles.Play();
            else mainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        // We no longer need to enable/disable the collider.
    }

    // This method is automatically called by Unity in the Scene view.
    private void OnDrawGizmosSelected()
    {
        // Use the damagePoint's position if it's assigned, otherwise default to this object's position.
        Vector3 center = (damagePoint != null) ? damagePoint.position : transform.position;

        // Draw a red wireframe circle to visualize the damage radius.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, damageRadius);
    }
}
