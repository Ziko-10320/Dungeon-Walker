using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this to every enemy you want to be soul-linkable.
/// It is intentionally lightweight: it only keeps per-enemy settings
/// and acts as the contact point for the SoulLinkChain manager.
/// </summary>
public class SoulLinkEnemy : MonoBehaviour
{
    [Header("Link configuration")]
    [Range(0f, 1f)] public float linkChance = 1f;
    public float maxLinkDistance = 5f;
    public int minLinkEnemies = 1;
    public int maxLinkEnemies = 3;
    public float lineSpeed = 12f;
    public float deathDelay = 0.25f;

    [Header("Visuals & references")]
    public LineRenderer linePrefab;         // assign a prefab with a LineRenderer
    public Material outlineMaterial;        // assign per-enemy (unique if you want)
    public Transform linePoint;             // where the line should connect on this enemy
    public SpriteRenderer[] spriteRenderers;// all sprites that must swap to outline

    [Header("Optional health references (used by ForceDieFromChain)")]
    public InkHealth inkHealth;
    public FleaHealth fleaHealth;
    public SprayerHealth sprayerHealth;
    public FlyHealth flyHealth;

    [HideInInspector] public bool inChain = false;  // <-- make sure default is false
    [HideInInspector] public SoulLinkChain chain = null;

    // originals
    private Material[] originalMaterials;

    void Awake()
    {
        if (linePoint == null) linePoint = transform;
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalMaterials = new Material[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
                if (spriteRenderers[i] != null)
                    originalMaterials[i] = spriteRenderers[i].material;
        }
    }

    /// <summary>
    /// Called (from TakeDamage) to attempt to start a chain.
    /// The actual chain creation + visuals run in SoulLinkChain.
    /// </summary>
    public void TryStartLink()
    {
        // --- THE FIX: We will perform the same check here ---
        // OLD WAY: if (!PowerUpManager.SoulLinkEquipped) return;

        // NEW WAY: Check both players directly.
        PowerUpManager p1Manager = FindObjectOfType<PowerUpManager>();
        PowerUpManagerL3antix p2Manager = FindObjectOfType<PowerUpManagerL3antix>();

        bool p1HasIt = p1Manager != null && p1Manager.HasPowerUp(PowerUpType.SoulLink);
        bool p2HasIt = p2Manager != null && p2Manager.HasPowerUp(PowerUpType.SoulLink);

        // If neither player has the power-up, exit.
        if (!p1HasIt && !p2HasIt) return;
        // --- END OF THE FIX ---

        // The rest of your function is correct.
        if (inChain) return;
        if (Random.value > linkChance) return;

        if (linePoint == null || linePrefab == null)
            return;

        SoulLinkChain.CreateChain(this, minLinkEnemies, maxLinkEnemies, maxLinkDistance,
                                  linePrefab, outlineMaterial, lineSpeed, deathDelay);
    }

    /// <summary>
    /// Called by health scripts *at the start of Die()* (before destroy).
    /// Notifies the chain manager (if any) that this member has died.
    /// </summary>
    public void NotifyDied()
    {
        // Only notify chain if enemy is linked
        if (inChain && chain != null)
            chain.OnMemberDied(this);
    }
    /// <summary>
    /// Called by the chain to force this enemy to die as part of the sequential kill.
    /// The chain expects the existing Die() implementation to run (effects + destroy).
    /// We simply call the health script Die method that you already have.
    /// </summary>
    public void ForceDieFromChain()
    {
        // Prevent the health.Die() from re-notifying the chain.
        // Clear chain flags immediately so Die() won't call NotifyDied() again.
        inChain = false;
        chain = null;

        // Prefer explicit health components in order (call their Die() which will destroy object).
        if (inkHealth != null) { inkHealth.Die(); return; }
        if (fleaHealth != null) { fleaHealth.Die(); return; }
        if (sprayerHealth != null) { sprayerHealth.Die(); return; }
        if (flyHealth != null) { flyHealth.Die(); return; }

        // Fallback: destroy object if no health component found
        Destroy(gameObject);
    }


    /// <summary>
    /// Swap all sprite renderers to the outline material (visual linked state).
    /// </summary>
    public void SetOutlineMaterial(Material mat)
    {
        if (spriteRenderers == null || mat == null) return;

        // Assign a unique instance of the outline material to each renderer so later
        // flash/material changes won't accidentally revert or share states.
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            var sr = spriteRenderers[i];
            if (sr == null) continue;

            // Ensure originalMaterials array is filled (in case Awake missed something)
            if (originalMaterials == null || originalMaterials.Length != spriteRenderers.Length)
            {
                originalMaterials = new Material[spriteRenderers.Length];
                for (int j = 0; j < spriteRenderers.Length; j++)
                    if (spriteRenderers[j] != null) originalMaterials[j] = spriteRenderers[j].material;
            }

            // Set a fresh instance of the outline material to avoid shared material side-effects
            Material inst = new Material(mat);
            sr.material = inst;
        }
    }


    /// <summary>
    /// Restore original sprite materials.
    /// </summary>
    public void RestoreOriginalMaterials()
    {
        if (spriteRenderers == null || originalMaterials == null) return;
        for (int i = 0; i < spriteRenderers.Length && i < originalMaterials.Length; i++)
            if (spriteRenderers[i] != null && originalMaterials[i] != null)
                spriteRenderers[i].material = originalMaterials[i];
    }
}
