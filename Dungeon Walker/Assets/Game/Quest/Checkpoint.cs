using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Visual Settings")]
    public GameObject activeVisual;
    public GameObject inactiveVisual;
    public GameObject reachedVisual;

    [Header("Effects")]
    public ParticleSystem reachEffect;
    public AudioClip reachSound; // Existing reach sound
    [Range(0f, 1f)]
    public float reachSoundVolume = 1f; // Volume for reach sound
    public AudioClip radiusEnterSound; // Sound for entering radius
    [Range(0f, 1f)]
    public float radiusEnterVolume = 1f; // Volume for radius enter sound
    public AudioClip radiusExitPrematureSound; // New: Sound for exiting radius prematurely
    [Range(0f, 1f)]
    public float radiusExitPrematureVolume = 1f; // New: Volume for premature exit sound
    public AudioClip waitingSound; // New: Continuous sound while waiting in radius
    [Range(0f, 1f)]
    public float waitingSoundVolume = 1f; // New: Volume for waiting sound

    [Header("Checkpoint Settings")]
    public float timeToStay = 3f; // Individual time to stay at this checkpoint
    public float radius = 2f; // New: Individual radius for this checkpoint

    private int checkpointIndex;
    private bool isActive = false;
    private bool isReached = false;
    private AudioSource audioSource;
    private AudioSource waitingAudioSource; // Separate AudioSource for continuous waiting sound

    public int CheckpointIndex => checkpointIndex;
    public bool IsActive => isActive;
    public bool IsReached => isReached;
    public float TimeToStay => timeToStay;
    public float Radius => radius;

    void Awake()
    {
        // Get or add AudioSource component for one-shot sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false; // Ensure it doesn\'t play automatically

        // Get or add a separate AudioSource for the continuous waiting sound
        waitingAudioSource = gameObject.AddComponent<AudioSource>();
        waitingAudioSource.loop = true; // Make it loop
        waitingAudioSource.playOnAwake = false; // Don\'t play on awake
    }

    void Start()
    {
        // Initialize visual state
        UpdateVisuals();
    }

    public void SetCheckpointIndex(int index)
    {
        checkpointIndex = index;
        gameObject.name = $"Checkpoint_{index + 1}";
    }

    public void SetActive(bool active)
    {
        if (isReached) return; // Can\'t activate a reached checkpoint

        isActive = active;
        UpdateVisuals();

        if (active)
        {
            Debug.Log($"Checkpoint {checkpointIndex + 1} is now active");
        }
    }

    public void SetReached()
    {
        if (isReached) return; // Already reached

        isReached = true;
        isActive = false;
        UpdateVisuals();

        // Play effects
        PlayReachEffects();

        Debug.Log($"Checkpoint {checkpointIndex + 1} reached!");
    }

    public void ResetCheckpoint()
    {
        isReached = false;
        isActive = false;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        // Hide all visuals first
        if (activeVisual != null) activeVisual.SetActive(false);
        if (inactiveVisual != null) inactiveVisual.SetActive(false);
        if (reachedVisual != null) reachedVisual.SetActive(false);

        // Show appropriate visual
        if (isReached)
        {
            if (reachedVisual != null) reachedVisual.SetActive(true);
        }
        else if (isActive)
        {
            if (activeVisual != null) activeVisual.SetActive(true);
        }
        else
        {
            if (inactiveVisual != null) inactiveVisual.SetActive(true);
        }
    }

    void PlayReachEffects()
    {
        // Play particle effect
        if (reachEffect != null)
        {
            reachEffect.Play();
        }

        // Play sound effect
        if (reachSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(reachSound, reachSoundVolume);
        }
    }

    public void PlayRadiusEnterSound()
    {
        if (radiusEnterSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(radiusEnterSound, radiusEnterVolume);
        }
    }

    public void PlayRadiusExitPrematureSound()
    {
        if (radiusExitPrematureSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(radiusExitPrematureSound, radiusExitPrematureVolume);
        }
    }

    public void StartWaitingSound()
    {
        if (waitingSound != null && waitingAudioSource != null && !waitingAudioSource.isPlaying)
        {
            waitingAudioSource.clip = waitingSound;
            waitingAudioSource.volume = waitingSoundVolume;
            waitingAudioSource.Play();
        }
    }

    public void StopWaitingSound()
    {
        if (waitingAudioSource != null && waitingAudioSource.isPlaying)
        {
            waitingAudioSource.Stop();
        }
    }

    // Optional: Visual feedback when player is near
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isActive)
        {
            // You can add visual feedback here like scaling or glowing
            StartCoroutine(PulseEffect());
        }
    }

    IEnumerator PulseEffect()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;

        // Scale up
        float time = 0;
        while (time < 0.2f)
        {
            transform.localScale = Vector3.Lerp(originalScale, targetScale, time / 0.2f);
            time += Time.deltaTime;
            yield return null;
        }

        // Scale down
        time = 0;
        while (time < 0.2f)
        {
            transform.localScale = Vector3.Lerp(targetScale, originalScale, time / 0.2f);
            time += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    // Helper method for debugging
    void OnDrawGizmos()
    {
        // Draw checkpoint index
        Gizmos.color = isReached ? Color.green : (isActive ? Color.yellow : Color.gray);
        Gizmos.DrawWireSphere(transform.position, radius);

        // Draw checkpoint number
#if UNITY_EDITOR
        // UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, $"{checkpointIndex + 1}");
#endif
    }
}
