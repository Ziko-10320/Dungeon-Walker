using System.Collections;
using UnityEngine;

public class PlayerInvisibility : MonoBehaviour
{
    [Header("References")]
    public GameObject[] playerChildren;    // assign in inspector
    public Material invisibleMaterial;     // assign the invisible material

    [Header("Settings")]
    public float invisibilityDuration = 5f;
    [Header("Audio")]
    [SerializeField] private AudioClip becomeInvisibleSound; // Sound for turning invisible.
    [Range(0f, 1f)]
    [SerializeField] private float invisibleVolume = 1f;

    [SerializeField] private AudioClip becomeVisibleSound;   // Sound for turning visible again.
    [Range(0f, 1f)]
    [SerializeField] private float visibleVolume = 1f;
    private Material[] originalMaterials;
    private Coroutine invisibilityCoroutine;
    private bool isInvisible = false;

    // Event for enemies to listen to
    public delegate void InvisibilityEvent(bool invisible);
    public static event InvisibilityEvent OnInvisibilityChanged;

    void Awake()
    {
        // Backup original materials, but DON'T swap anything yet
        originalMaterials = new Material[playerChildren.Length];
        for (int i = 0; i < playerChildren.Length; i++)
        {
            if (playerChildren[i] == null) continue;
            Renderer r = playerChildren[i].GetComponent<Renderer>();
            if (r != null) originalMaterials[i] = r.sharedMaterial;
        }
    }

   
    void LateUpdate()
    {
        // Only check if currently invisible
        if (!isInvisible) return;

        for (int i = 0; i < playerChildren.Length; i++)
        {
            if (playerChildren[i] == null) continue;
            Renderer r = playerChildren[i].GetComponent<Renderer>();
            if (r == null) continue;

            // If a child lost its invisibility material, force invisible again
            if (r.sharedMaterial != invisibleMaterial)
            {
                r.sharedMaterial = invisibleMaterial;
            }
        }
    }

    public void ActivateInvisibility(float durationOverride = -1f)
    {
        if (!HasPowerUpEquipped()) return; // ✅ Only work if equipped

        float duration = durationOverride > 0f ? durationOverride : invisibilityDuration;
        if (invisibilityCoroutine != null) StopCoroutine(invisibilityCoroutine);
        invisibilityCoroutine = StartCoroutine(InvisibilityRoutine(duration));
    }

    private IEnumerator InvisibilityRoutine(float duration)
    {
        SetInvisible(true);
        yield return new WaitForSeconds(duration);
        SetInvisible(false);
        invisibilityCoroutine = null;
    }

    private void SetInvisible(bool state)
    {
        if (state) // If we are BECOMING invisible...
        {
            // Play the "become invisible" sound.
            if (becomeInvisibleSound != null)
            {
                AudioSource.PlayClipAtPoint(becomeInvisibleSound, transform.position, invisibleVolume);
            }
        }
        else // If we are BECOMING visible...
        {
            // Play the "become visible" sound.
            if (becomeVisibleSound != null)
            {
                AudioSource.PlayClipAtPoint(becomeVisibleSound, transform.position, visibleVolume);
            }
        }
        isInvisible = state;

        // The new alpha value: 0 for invisible, 1 for visible.
        float targetAlpha = state ? 0f : 1f;

        // Loop through all the renderers and just change their alpha.
        foreach (GameObject child in playerChildren)
        {
            if (child == null) continue;
            SpriteRenderer r = child.GetComponent<SpriteRenderer>();
            if (r == null) continue;

            // Get the current color, change only the alpha, and set it back.
            Color currentColor = r.color;
            currentColor.a = targetAlpha;
            r.color = currentColor;
        }

        // Your event call is still correct.
        OnInvisibilityChanged?.Invoke(state);
    }
    public void DeactivateInvisibility()
    {
        if (!isInvisible) return;

        if (invisibilityCoroutine != null)
        {
            StopCoroutine(invisibilityCoroutine);
            invisibilityCoroutine = null;
        }

        SetInvisible(false);
    }

  
    public void ForceVisible()
    {
        if (isInvisible)
        {
            if (invisibilityCoroutine != null) StopCoroutine(invisibilityCoroutine);
            SetInvisible(false);
            invisibilityCoroutine = null;
        }
    }
    public bool IsInvisible() => isInvisible;

    // --- Keep your PowerUpManager check ---
    private bool HasPowerUpEquipped()
    {
        PowerUpManager pum = GetComponent<PowerUpManager>();
        return pum != null && pum.HasPowerUp(PowerUpType.Invisibility);
    }
}
