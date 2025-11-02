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



    public void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || Camera.main == null) return;

        // Create a clean, independent object for the sound
        GameObject soundPlayerObject = new GameObject("Invisibility_FORCE_PLAY_SOUND");

        // --- THIS IS THE CRITICAL FIX for volume issues ---
        // Position it directly on the camera to guarantee it's heard at full volume
        soundPlayerObject.transform.position = Camera.main.transform.position;

        // Add and aggressively configure the AudioSource
        AudioSource tempAudioSource = soundPlayerObject.AddComponent<AudioSource>();

        tempAudioSource.clip = clip;

        // --- CRITICAL OVERRIDES ---
        tempAudioSource.volume = volume;
        tempAudioSource.spatialBlend = 0.0f;              // Force 2D sound
        tempAudioSource.priority = 0;                     // Highest priority
        tempAudioSource.bypassEffects = true;             // Ignore mixers
        tempAudioSource.bypassListenerEffects = true;     // Ignore listener effects
        tempAudioSource.bypassReverbZones = true;         // Ignore reverb zones

        // Play the sound and schedule its destruction
        tempAudioSource.Play();
        Destroy(soundPlayerObject, clip.length);
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

        // --- THIS IS THE GUARANTEED FIX ---
        if (state) // If we are BECOMING invisible...
        {
            PlaySound(becomeInvisibleSound, invisibleVolume);

            // Swap to the invisible material.
            for (int i = 0; i < playerChildren.Length; i++)
            {
                if (playerChildren[i] == null) continue;
                Renderer r = playerChildren[i].GetComponent<Renderer>();
                if (r != null)
                {
                    r.sharedMaterial = invisibleMaterial;
                }
            }
        }
        else // If we are BECOMING visible...
        {
            PlaySound(becomeVisibleSound, visibleVolume);

            // Swap back to the original materials we saved in Awake().
            for (int i = 0; i < playerChildren.Length; i++)
            {
                if (playerChildren[i] == null) continue;
                Renderer r = playerChildren[i].GetComponent<Renderer>();
                if (r != null && originalMaterials[i] != null)
                {
                    r.sharedMaterial = originalMaterials[i];
                }
            }
        }
        // --- END OF FIX ---

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
