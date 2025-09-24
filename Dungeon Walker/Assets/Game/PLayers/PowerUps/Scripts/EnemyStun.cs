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

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (enemyAnimator == null) enemyAnimator = GetComponent<Animator>();
    }

    public void Stun(float duration)
    {
        if (isStunned) return;
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        // Spawn stun effect
        if (stunEffectPrefab != null && stunEffectSpawnPoint != null)
        {
            activeStunEffect = Instantiate(stunEffectPrefab, stunEffectSpawnPoint.position, Quaternion.identity, transform);
        }

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

        // Re-enable
        if (enemyAnimator != null)
            enemyAnimator.enabled = true;

        foreach (var script in scriptsToDisable)
        {
            if (script != null) script.enabled = true;
        }

        if (rb != null)
            rb.bodyType = RigidbodyType2D.Dynamic;

        // Destroy stun effect
        if (activeStunEffect != null)
            Destroy(activeStunEffect);

        isStunned = false;
    }

}
