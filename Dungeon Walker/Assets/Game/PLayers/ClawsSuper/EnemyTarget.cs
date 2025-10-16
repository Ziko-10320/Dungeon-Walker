using UnityEngine;
using System.Collections;

public class EnemySuperTarget : MonoBehaviour
{
    [Tooltip("The material instance used for the flash effect on this enemy type.")]
    public Material flashMaterial;

    [Tooltip("The transform where the claw effect should be spawned.")]
    public Transform clawSpawnPoint;

    [Tooltip("All sprite renderers for this enemy and its children that should flash.")]
    public SpriteRenderer[] renderersToFlash;

    [Tooltip("Drag all scripts that control movement and AI here (e.g., FleaFollow, FlyFollow).")]
    public MonoBehaviour[] scriptsToDisable;

    [Tooltip("The Animator component for this enemy.")]
    public Animator enemyAnimator;

    private Material[] originalMaterials;
    private const string FlashAmountProperty = "_FlashAmount";
    private bool isFlashing = false;

    private string[] originalSortingLayerNames;
    private int[] originalSortingOrders;
    private FleaHealth healthComponent;
    private FleaHealthV2 healthComponentV2;
    private FlyHealth flyHealth ;
    private InkHealth inkHealth ;
    private SprayerHealth sprayerHealth;
    private RatKingHealth ratKingHealth;
    private Rigidbody2D rb;
    private float originalGravityScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            originalGravityScale = rb.gravityScale;
        }
        healthComponent = GetComponent<FleaHealth>();
        healthComponentV2 = GetComponent<FleaHealthV2>();
        flyHealth = GetComponent<FlyHealth>();
        inkHealth = GetComponent<InkHealth>();
        sprayerHealth = GetComponent<SprayerHealth>();
        ratKingHealth = GetComponent<RatKingHealth>();

        // Find all renderers if not assigned in inspector
        if (renderersToFlash == null || renderersToFlash.Length == 0)
        {
            renderersToFlash = GetComponentsInChildren<SpriteRenderer>();
        }

        // Store the original materials AND sorting layers of all renderers
        if (renderersToFlash.Length > 0)
        {
            originalMaterials = new Material[renderersToFlash.Length];
            originalSortingLayerNames = new string[renderersToFlash.Length]; // <-- ADD THIS
            originalSortingOrders = new int[renderersToFlash.Length];       // <-- ADD THIS

            for (int i = 0; i < renderersToFlash.Length; i++)
            {
                if (renderersToFlash[i] != null)
                {
                    originalMaterials[i] = renderersToFlash[i].sharedMaterial;
                    originalSortingLayerNames[i] = renderersToFlash[i].sortingLayerName; // <-- ADD THIS
                    originalSortingOrders[i] = renderersToFlash[i].sortingOrder;         // <-- ADD THIS
                }
            }
        }
    }

   
    public void StartFlash()
    {
        if (!isFlashing && flashMaterial != null && renderersToFlash.Length > 0)
        {
            StartCoroutine(FlashCoroutine(true));
        }
    }

    /// <summary>
    /// Reverts the flash effect.
    /// </summary>
    public void EndFlash()
    {
        if (isFlashing)
        {
            StartCoroutine(FlashCoroutine(false));
        }
    }

    private IEnumerator FlashCoroutine(bool fadeIn)
    {
        if (fadeIn)
        {
            isFlashing = true;
            foreach (var script in scriptsToDisable)
            {
                if (script != null) script.enabled = false;
            }

            // 2. Disable Animator
            if (enemyAnimator != null)
            {
                enemyAnimator.enabled = false;
            }

            // 3. Freeze Rigidbody and set gravity to 0
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.gravityScale = 0f;
                if (sprayerHealth != null)
                {
                    rb.bodyType = RigidbodyType2D.Static;
                }
            }
            if (healthComponent != null)
            {
                healthComponent.isStunned = true; // <-- STUN THE ENEMY
            }
            if (healthComponentV2 != null)
            {
                healthComponentV2.isStunned = true; // <-- STUN THE ENEMY
            }
            if (flyHealth != null)
            {
                flyHealth.isStunned = true; // <-- STUN THE ENEMY
            }
            if (inkHealth != null)
            {
                inkHealth.isStunned = true; // <-- STUN THE ENEMY
            }
            if (sprayerHealth != null)
            {
                sprayerHealth.isStunned = true; // <-- STUN THE ENEMY
            }
            if (ratKingHealth != null)
            {
                ratKingHealth.isStunned = true; // <-- STUN THE ENEMY
            }
            // Apply the flash material and move to top sorting layer
            for (int i = 0; i < renderersToFlash.Length; i++)
            {
                if (renderersToFlash[i] != null)
                {
                    renderersToFlash[i].sharedMaterial = flashMaterial;
                    renderersToFlash[i].sortingLayerName = "SuperMoveTop"; // <-- CHANGE THIS
                    renderersToFlash[i].sortingOrder = 100; // A high number to be safe
                }
            }
            // Animate flash amount to 1
           
        }
        else
        {
            // Animate flash amount to 0
           
            yield return new WaitForSeconds(0.1f);
            // --- RE-ENABLE COMPONENTS ---
            // 1. Re-enable AI scripts
            foreach (var script in scriptsToDisable)
            {
                if (script != null) script.enabled = true;
            }

            // 2. Re-enable Animator
            if (enemyAnimator != null)
            {
                enemyAnimator.enabled = true;
            }

            // 3. Restore Rigidbody gravity
            if (rb != null)
            {
                rb.gravityScale = originalGravityScale;
                if (sprayerHealth != null)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                }
            }
            // --- END OF RE-ENABLE ---
            if (healthComponent != null)
            {
                healthComponent.isStunned = false; // <-- UN-STUN THE ENEMY
            }
            if (healthComponentV2 != null)
            {
                healthComponentV2.isStunned = false; // <-- UN-STUN THE ENEMY
            }
            if (flyHealth != null)
            {
                flyHealth.isStunned = false; // <-- UN-STUN THE ENEMY
            }
            if (inkHealth != null)
            {
                inkHealth.isStunned = false; // <-- UN-STUN THE ENEMY
            }
            if (sprayerHealth != null)
            {
                sprayerHealth.isStunned = false; // <-- UN-STUN THE ENEMY
            }
            if (ratKingHealth != null)
            {
                ratKingHealth.isStunned = false; // <-- UN-STUN THE ENEMY
            }
            // Restore original materials and sorting layers
            for (int i = 0; i < renderersToFlash.Length; i++)
            {
                if (renderersToFlash[i] != null && originalMaterials[i] != null)
                {
                    renderersToFlash[i].sharedMaterial = originalMaterials[i];
                    renderersToFlash[i].sortingLayerName = originalSortingLayerNames[i]; // <-- CHANGE THIS
                    renderersToFlash[i].sortingOrder = originalSortingOrders[i];         // <-- CHANGE THIS
                }
            }
            isFlashing = false;
        }
        yield return null;
    }
    public void Stun(bool value)
    {
        // Stun all health components
        if (healthComponent != null) healthComponent.isStunned = value;
        if (healthComponentV2 != null) healthComponentV2.isStunned = value;
        if (flyHealth != null) flyHealth.isStunned = value;
        if (inkHealth != null) inkHealth.isStunned = value;
        if (sprayerHealth != null) sprayerHealth.isStunned = value;
        if (ratKingHealth != null) ratKingHealth.isStunned = value;

        // Freeze Rigidbody if stun
        if (rb != null)
        {
            if (value)
            {
                rb.velocity = Vector2.zero;
                rb.gravityScale = 0f;
            }
            else
            {
                rb.gravityScale = originalGravityScale;
            }
        }

        // Disable/Enable AI scripts
        foreach (var script in scriptsToDisable)
        {
            if (script != null) script.enabled = !value;
        }

        // Disable/Enable Animator
        if (enemyAnimator != null)
        {
            enemyAnimator.enabled = !value;
        }
    }
}
