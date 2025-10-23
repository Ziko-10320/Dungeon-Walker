using UnityEngine;
using System.Collections;

public class VolcanoObstacle : MonoBehaviour
{
    [Header("Timing Settings")]
    [SerializeField] private float activeDuration = 1.5f;
    [SerializeField] private float dormantDuration = 2.0f;

    [Header("Damage & Knockback")]
    [SerializeField] private int damagePerTick = 10;
    [SerializeField] private float knockbackForce = 25f;

    [Header("Component References")]
    [SerializeField] private ParticleSystem mainParticles;
    [SerializeField] private Collider2D damageCollider;

    private Coroutine cycleCoroutine;
    private WaitForSeconds activeWait;
    private WaitForSeconds dormantWait;

    void Awake()
    {
        activeWait = new WaitForSeconds(activeDuration);
        dormantWait = new WaitForSeconds(dormantDuration);
        if (mainParticles == null) mainParticles = GetComponentInChildren<ParticleSystem>();
        if (damageCollider == null) damageCollider = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        // When spawned from the pool, immediately start the looping cycle.
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);
        cycleCoroutine = StartCoroutine(VolcanoCycle());
    }

    void OnDisable()
    {
        // When returned to the pool, stop the loop and reset to a safe state.
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);
        cycleCoroutine = null;
        SetActiveState(false);
    }

    private IEnumerator VolcanoCycle()
    {
        // Start dormant.
        SetActiveState(false);
        yield return dormantWait;

        // This loop runs forever as long as the GameObject is active.
        while (true)
        {
            SetActiveState(true);
            yield return activeWait;

            SetActiveState(false);
            yield return dormantWait;
        }
    }

    public void SetActiveState(bool isActive)
    {
        if (mainParticles != null)
        {
            if (isActive) mainParticles.Play();
            else mainParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (damageCollider != null)
        {
            damageCollider.enabled = isActive;
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!damageCollider.enabled) return;

        if (other.TryGetComponent<PlayerHealth>(out var playerHealth))
        {
            Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
            knockbackDirection.y = Mathf.Max(knockbackDirection.y, 0.3f);
            playerHealth.TakeDamage(damagePerTick, knockbackForce, knockbackDirection.normalized);
        }
        else if (other.TryGetComponent<L3antixHealth>(out var l3antixHealth))
        {
            Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
            knockbackDirection.y = Mathf.Max(knockbackDirection.y, 0.3f);
            l3antixHealth.TakeDamage(damagePerTick, knockbackForce, knockbackDirection.normalized);
        }
    }
}
