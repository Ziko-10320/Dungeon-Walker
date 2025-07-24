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
    public AudioClip reachSound;
    [Range(0f, 1f)]
    public float reachSoundVolume = 1f;
    public AudioClip radiusEnterSound;
    [Range(0f, 1f)]
    public float radiusEnterVolume = 1f;
    public AudioClip radiusExitPrematureSound;
    [Range(0f, 1f)]
    public float radiusExitPrematureVolume = 1f;
    public AudioClip waitingSound;
    [Range(0f, 1f)]
    public float waitingSoundVolume = 1f;

    [Header("Checkpoint Settings")]
    public float timeToStay = 3f;
    public float radius = 2f;

    private int checkpointIndex;
    private bool isActive = false;
    private bool isReached = false;
    private AudioSource audioSource;
    private AudioSource waitingAudioSource;

    // New states for interaction
    private bool playerInRadius = false;
    private bool questStarted = false;
    private bool timerEnded = false;

    public int CheckpointIndex => checkpointIndex;
    public bool IsActive => isActive;
    public bool IsReached => isReached;
    public float TimeToStay => timeToStay;
    public float Radius => radius;
    public bool PlayerInRadius => playerInRadius;
    public bool QuestStarted => questStarted;
    public bool TimerEnded => timerEnded;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        waitingAudioSource = gameObject.AddComponent<AudioSource>();
        waitingAudioSource.loop = true;
        waitingAudioSource.playOnAwake = false;
    }

    void Start()
    {
        UpdateVisuals();
    }

    public void SetCheckpointIndex(int index)
    {
        checkpointIndex = index;
        gameObject.name = $"Checkpoint_{index + 1}";
    }

    public void SetActive(bool active)
    {
        if (isReached) return;

        isActive = active;
        UpdateVisuals();

        if (active)
        {
            Debug.Log($"Checkpoint {checkpointIndex + 1} is now active");
        }
    }

    public void SetReached()
    {
        if (isReached) return;

        isReached = true;
        isActive = false;
        questStarted = false;
        timerEnded = false;
        playerInRadius = false;
        UpdateVisuals();

        PlayReachEffects();
        StopWaitingSound();

        Debug.Log($"Checkpoint {checkpointIndex + 1} reached!");
    }

    public void ResetCheckpoint()
    {
        isReached = false;
        isActive = false;
        questStarted = false;
        timerEnded = false;
        playerInRadius = false;
        UpdateVisuals();
        StopWaitingSound();
    }

    void UpdateVisuals()
    {
        if (activeVisual != null) activeVisual.SetActive(false);
        if (inactiveVisual != null) inactiveVisual.SetActive(false);
        if (reachedVisual != null) reachedVisual.SetActive(false);

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
        if (reachEffect != null)
        {
            reachEffect.Play();
        }

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

    public void SetPlayerInRadius(bool inRadius)
    {
        playerInRadius = inRadius;
    }

    public void StartQuest()
    {
        if (!questStarted)
        {
            questStarted = true;
            timerEnded = false;
            StartWaitingSound();
            Debug.Log($"Checkpoint {checkpointIndex + 1} quest started!");
        }
    }

    public void EndTimer()
    {
        if (questStarted && !timerEnded)
        {
            timerEnded = true;
            StopWaitingSound();
            Debug.Log($"Checkpoint {checkpointIndex + 1} timer ended!");
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isReached ? Color.green : (isActive ? Color.yellow : Color.gray);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}