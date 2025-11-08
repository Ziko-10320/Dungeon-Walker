using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyStun : MonoBehaviour
{
    [Header("Stun Settings")]
    public float stunDuration = 2f; // default, can override per enemy
    public Animator enemyAnimator;
    public Rigidbody2D rb;

    [Tooltip("Scripts to disable during stun (ex: AI, movement, attack scripts)")]
    public List<MonoBehaviour> scriptsToDisable = new List<MonoBehaviour>();

    [Header("Stun Effect")]
    public GameObject stunEffectPrefab;   // prefab with particle system
    public Transform stunEffectSpawnPoint; // where to spawn the effect

    private GameObject activeStunEffect;

    private bool isStunned = false;

    [Header("Pooling")]
    [Tooltip("How many stun effects to keep ready in the pool.")]
    [SerializeField] private int stunEffectPoolSize = 3;
    private Queue<GameObject> stunEffectPool;
    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (enemyAnimator == null) enemyAnimator = GetComponent<Animator>();

        stunEffectPool = new Queue<GameObject>();
        if (stunEffectPrefab != null)
        {
            for (int i = 0; i < stunEffectPoolSize; i++)
            {
                GameObject effect = Instantiate(stunEffectPrefab, stunEffectSpawnPoint);
                effect.SetActive(false);
                stunEffectPool.Enqueue(effect);
            }
        }
    }

    public void Stun(float duration)
    {
        if (isStunned) return;
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        // --- THIS IS THE REPLACEMENT ---
        // Spawn stun effect from our new pool
        if (stunEffectPool != null && stunEffectPool.Count > 0)
        {
            activeStunEffect = stunEffectPool.Dequeue();
            activeStunEffect.transform.position = stunEffectSpawnPoint.position;
            activeStunEffect.SetActive(true);
        }
        // --- END OF REPLACEMENT ---

        // Disable animator
        if (enemyAnimator != null)
            enemyAnimator.enabled = false;

        // Disable chosen scripts
        foreach (var script in scriptsToDisable)
        {
            if (script != null) script.enabled = false;
        }

        // Freeze physics
        if (rb != null)
            rb.bodyType = RigidbodyType2D.Static;

        yield return new WaitForSeconds(duration);

        // --- THIS IS THE REPLACEMENT FOR THE CLEANUP ---
        // We just call our new ResetStunState method to clean everything up perfectly.
        ResetStunState();
        // --- END OF REPLACEMENT ---
    }

    public void ResetStunState()
    {
        // This is our "factory reset" button.
        StopAllCoroutines(); // Stop any lingering stun timers.

        // Force-enable all components that might have been disabled.
        if (enemyAnimator != null) enemyAnimator.enabled = true;
        foreach (var script in scriptsToDisable)
        {
            if (script != null) script.enabled = true;
        }

        // Unfreeze physics.
        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic;

        // Hide any active stun effect and return it to the pool.
        if (activeStunEffect != null)
        {
            activeStunEffect.SetActive(false);
            stunEffectPool.Enqueue(activeStunEffect);
            activeStunEffect = null;
        }

        isStunned = false;
    }

}
