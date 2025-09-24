using System.Collections;
using UnityEngine;

public class PlayerInvisibility3antix : MonoBehaviour
{
    [Header("References")]
    public GameObject[] playerChildren;    // assign in inspector
    public Material invisibleMaterial;     // assign the invisible material

    [Header("Settings")]
    public float invisibilityDuration = 5f;

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
            if (r != null) originalMaterials[i] = r.material;
        }
    }

    void Update()
    {
        // Cancel invisibility if player clicks mouse
        if (isInvisible && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
        {
            DeactivateInvisibility();
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
            if (r.material != invisibleMaterial)
            {
                r.material = invisibleMaterial;
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
        isInvisible = state;

        for (int i = 0; i < playerChildren.Length; i++)
        {
            if (playerChildren[i] == null) continue;
            Renderer r = playerChildren[i].GetComponent<Renderer>();
            if (r == null) continue;
            r.material = state ? invisibleMaterial : originalMaterials[i];
        }

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

    public void OnTakeDamage()
    {
        if (isInvisible) DeactivateInvisibility();
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
        PowerUpManagerL3antix pum = GetComponent<PowerUpManagerL3antix>();
        return pum != null && pum.HasPowerUp(PowerUpType.Invisibility);
    }
}
