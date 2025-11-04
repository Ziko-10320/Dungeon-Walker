using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CheckpointType
{
    None,   // For C1 and C5 (do nothing)
    Wash,   // For C2 (washing animation + particles during stay)
    Dry,    // For C3 (pick random sprite, stays until player reaches checkpoint)
    Clothes // For C4 (spawn sheet -> move to nextPoint -> destroy sheet -> move box)
}

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Type")]
    public CheckpointType checkpointType = CheckpointType.None;

    [Header("Visual Settings")]
    public GameObject activeVisual;
    public GameObject inactiveVisual;
    public GameObject reachedVisual;

    [Header("Effects")]
    public ParticleSystem reachEffect;
    public AudioClip reachSound;
    [Range(0f, 1f)] public float reachSoundVolume = 1f;
    public AudioClip radiusEnterSound;
    [Range(0f, 1f)] public float radiusEnterVolume = 1f;
    public AudioClip radiusExitPrematureSound;
    [Range(0f, 1f)] public float radiusExitPrematureVolume = 1f;
    public AudioClip waitingSound;
    [Range(0f, 1f)] public float waitingSoundVolume = 1f;

    [Header("Checkpoint Settings")]
    [Tooltip("How long the player must 'stay' for the checkpoint quest.")]
    public float timeToStay = 3f;
    public float radius = 2f;

    // Index + states
    private int checkpointIndex;
    private bool isActive = false;
    private bool isReached = false;
    private AudioSource audioSource;
    private AudioSource waitingAudioSource;

    // Interaction states
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

    // ------------------ C2 (Wash) ------------------
    [Header("C2 - Wash (Animation + Particles)")]
    [Tooltip("Play washing animation while stay time is active.")]
    public bool playWashAnimation = true;
    [Tooltip("Animator that has a boolean parameter to loop wash animation.")]
    public Animator washAnimator;
    [Tooltip("Name of boolean parameter in animator to toggle wash animation.")]
    public string washAnimatorBoolName = "IsWashing";
    [Tooltip("ParticleSystem to play while washing (loop will be controlled in script).")]
    public ParticleSystem washParticles;
    private Coroutine washCoroutine;
    [Tooltip("The vertical (Y-axis) offset for the arrow pointer when it's on-screen at this checkpoint.")]
    public float arrowYOffset = 1.0f; // Default value, you can change this
    // ------------------ C3 (Dry) ------------------
    [Header("C3 - Dry (random sprites)")]
    [Tooltip("SpriteRenderers representing dry clothes visuals. Script will disable them on Start.")]
    public SpriteRenderer[] drySprites;
    private int activeDryIndex = -1;    // currently enabled this run
    private int lastDryIndex = -1;      // last time the player completed this checkpoint (used to avoid repetition)

    // ------------------ C4 (Clothes) ------------------
    [Header("C4 - Sheet & Box Movement")]
    [Tooltip("Prefab of the cloth sheet to spawn (will be destroyed after reaching next point).")]
    public GameObject sheetPrefab;
    [Tooltip("Spawn point (Transform) where sheet will appear.")]
    public Transform sheetSpawnPoint;
    [Tooltip("Target (next) point the sheet will move to.")]
    public Transform sheetNextPoint;
    [Tooltip("Duration in seconds the sheet takes to move to the next point.")]
    public float sheetMoveDuration = 1f;

    [Tooltip("Box that will move after the sheet finishes.")]
    public Transform boxTransform;
    [Tooltip("Final point for the box to move to.")]
    public Transform boxFinalPoint;
    [Tooltip("Duration for the box to move to final point.")]
    public float boxMoveDuration = 1f;

    // runtime
    private GameObject spawnedSheetInstance;
    private Vector3 boxOriginalPosition;
    private Coroutine sheetMoveCoroutine;
    private Coroutine boxMoveCoroutine;

    void Awake()
    {
        // audio sources setup
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        waitingAudioSource = gameObject.AddComponent<AudioSource>();
        waitingAudioSource.loop = true;
        waitingAudioSource.playOnAwake = false;

        // save original box position for reset
        if (boxTransform != null)
            boxOriginalPosition = boxTransform.position;
    }
    void Update()
    {
        // Failsafe: If the waiting audio source doesn't exist, do nothing.
        if (waitingAudioSource == null) return;

        // Condition 1: Is the game currently paused?
        bool isGamePaused = Time.timeScale == 0f;

        // Condition 2: Is the quest active and the sound supposed to be playing?
        // The sound should only play if the quest has started AND the timer hasn't ended yet.
        bool shouldBePlaying = questStarted && !timerEnded;

        // Now, we sync the audio source's state with our desired state.
        if (shouldBePlaying && !isGamePaused)
        {
            // If it SHOULD be playing and the game is NOT paused...
            if (!waitingAudioSource.isPlaying)
            {
                // ...and it's not already playing, then play it.
                // This handles starting the sound and unpausing.
                waitingAudioSource.Play();
            }
        }
        else
        {
            // If it SHOULD NOT be playing (or the game is paused)...
            if (waitingAudioSource.isPlaying)
            {
                // ...and it IS currently playing, then pause it.
                // Using Pause() is better than Stop() because it will resume from the same spot.
                waitingAudioSource.Pause();
            }
        }
    }
    void Start()
    {
        // Ensure dry sprites are disabled on start so script controls them
        if (drySprites != null && drySprites.Length > 0)
        {
            for (int i = 0; i < drySprites.Length; i++)
            {
                if (drySprites[i] != null)
                    drySprites[i].enabled = false;
            }
        }

        UpdateVisuals();
    }

    // --- Basic API used by your CheckpointManager ---
    public void SetCheckpointIndex(int index)
    {
        checkpointIndex = index;
        // optional: rename for easier debugging
        gameObject.name = $"Checkpoint_{index + 1}";
    }

    public void SetActive(bool active)
    {
        if (isReached) return;
        isActive = active;
        UpdateVisuals();
        if (active) Debug.Log($"Checkpoint {checkpointIndex + 1} is now active");
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

        // finalize per-type
        CleanupTypeOnReached();

        Debug.Log($"Checkpoint {checkpointIndex + 1} reached!");
    }

    public void ResetCheckpoint()
    {
        // Reset states (called by manager on loop/reset)
        isReached = false;
        isActive = false;
        questStarted = false;
        timerEnded = false;
        playerInRadius = false;

        UpdateVisuals();
        StopWaitingSound();

        // reset type specific visuals / coroutines
        CleanupTypeOnReset();
    }

    // Visual helpers
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
            reachEffect.Play();

        if (reachSound != null && audioSource != null)
            audioSource.PlayOneShot(reachSound, reachSoundVolume);
    }

    // Sounds
    public void PlayRadiusEnterSound()
    {
        if (radiusEnterSound != null && audioSource != null)
            audioSource.PlayOneShot(radiusEnterSound, radiusEnterVolume);
    }

    public void PlayRadiusExitPrematureSound()
    {
        if (radiusExitPrematureSound != null && audioSource != null)
            audioSource.PlayOneShot(radiusExitPrematureSound, radiusExitPrematureVolume);
    }

    public void StartWaitingSound()
    {
        // We don't need to do anything here anymore.
        // The 'questStarted' flag is set in StartQuest(), and our new Update() method will see that
        // and automatically start the sound on the next frame.
        // You can leave this method empty or remove the old code.
        if (waitingSound != null && waitingAudioSource != null)
        {
            waitingAudioSource.clip = waitingSound;
            waitingAudioSource.volume = waitingSoundVolume;
        }
    }

    public void StopWaitingSound()
    {
        // We also don't need to do anything here.
        // The 'timerEnded' flag is set in EndTimer(), and our new Update() method will see that
        // and automatically pause the sound on the next frame.
    }

    public void SetPlayerInRadius(bool inRadius)
    {
        playerInRadius = inRadius;
    }

    // Called by CheckpointManager when player presses the first E (to start the quest)
    public void StartQuest()
    {
        if (questStarted) return;

        questStarted = true;
        timerEnded = false;
        StartWaitingSound();

        // type-specific start
        switch (checkpointType)
        {
            case CheckpointType.Wash:
                StartC2_Wash();
                break;

            case CheckpointType.Dry:
                StartC3_Dry();
                break;

            case CheckpointType.Clothes:
                StartC4_Clothes();
                break;

            case CheckpointType.None:
            default:
                // nothing special
                break;
        }

        Debug.Log($"Checkpoint {checkpointIndex + 1} quest started!");
    }

    // Called by CheckpointManager when the timer reaches zero
    public void EndTimer()
    {
        if (!questStarted || timerEnded) return;

        timerEnded = true;
        StopWaitingSound();

        switch (checkpointType)
        {
            case CheckpointType.Wash:
                EndC2_Wash();
                break;

            case CheckpointType.Dry:
                // intentionally do nothing: chosen dry sprite remains until player actually reaches the checkpoint
                break;

            case CheckpointType.Clothes:
                // allow sheet/box to finish naturally; cleanup happens on SetReached
                break;

            case CheckpointType.None:
            default:
                break;
        }

        Debug.Log($"Checkpoint {checkpointIndex + 1} timer ended!");
    }

    // -----------------------
    // --- C2: Wash logic ---
    // -----------------------
    private void StartC2_Wash()
    {
        if (!playWashAnimation)
            return;

        // Start the animator loop
        if (washAnimator != null && !string.IsNullOrEmpty(washAnimatorBoolName))
        {
            washAnimator.SetBool(washAnimatorBoolName, true);
        }

        // Enable emission and play particles so they spawn continuously while stay timer runs
        if (washParticles != null)
        {
            var emission = washParticles.emission;
            emission.enabled = true;
            if (!washParticles.isPlaying) washParticles.Play();

            // Optional safeguard: start a coroutine to ensure it keeps playing while questStarted && !timerEnded
            if (washCoroutine != null) StopCoroutine(washCoroutine);
            washCoroutine = StartCoroutine(WashKeepPlayingUntilTimerEnds());
        }
    }

    private IEnumerator WashKeepPlayingUntilTimerEnds()
    {
        // Keep the system playing until timerEnded is true (EndTimer was called) or we are reset
        while (!timerEnded && questStarted)
        {
            if (washParticles != null && !washParticles.isPlaying)
                washParticles.Play();
            yield return null;
        }

        // After timer ended (or quest cancelled), stop emission but allow existing particles to finish
        if (washParticles != null)
        {
            var emission = washParticles.emission;
            emission.enabled = false;
            washParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        // Switch off animator bool
        if (washAnimator != null && !string.IsNullOrEmpty(washAnimatorBoolName))
        {
            washAnimator.SetBool(washAnimatorBoolName, false);
        }

        washCoroutine = null;
    }

    private void EndC2_Wash()
    {
        // Stop & disable emission, animator bool off
        if (washCoroutine != null)
        {
            StopCoroutine(washCoroutine);
            washCoroutine = null;
        }

        if (washParticles != null)
        {
            var emission = washParticles.emission;
            emission.enabled = false;
            washParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        if (washAnimator != null && !string.IsNullOrEmpty(washAnimatorBoolName))
        {
            washAnimator.SetBool(washAnimatorBoolName, false);
        }
    }

    // -----------------------
    // --- C3: Dry logic ---
    // -----------------------
    private void StartC3_Dry()
    {
        if (drySprites == null || drySprites.Length == 0) return;

        // Choose a random sprite, avoiding the one used the last completed run (lastDryIndex) if possible
        int pick = 0;
        if (drySprites.Length == 1)
        {
            pick = 0;
        }
        else
        {
            pick = UnityEngine.Random.Range(0, drySprites.Length);
            // try to avoid lastDryIndex
            int attempts = 0;
            while (drySprites.Length > 1 && pick == lastDryIndex && attempts < 10)
            {
                pick = UnityEngine.Random.Range(0, drySprites.Length);
                attempts++;
            }
        }

        // enable the selected sprite
        activeDryIndex = pick;
        if (drySprites[activeDryIndex] != null)
            drySprites[activeDryIndex].enabled = true;
    }

    // Will be called when the player actually reaches the checkpoint (SetReached()) or if Reset is called
    private void EndC3_Dry_OnReachedOrReset()
    {
        if (activeDryIndex >= 0 && drySprites != null && activeDryIndex < drySprites.Length)
        {
            var sr = drySprites[activeDryIndex];
            if (sr != null)
                sr.enabled = false;

            // store this as last used, so next StartQuest tries to avoid it
            lastDryIndex = activeDryIndex;
        }

        activeDryIndex = -1;
    }

    // -----------------------
    // --- C4: Sheet & Box ---
    // -----------------------
    private void StartC4_Clothes()
    {
        // Spawn sheet if prefab and spawn point exist
        if (sheetPrefab != null && sheetSpawnPoint != null)
        {
            // Clean previous
            if (spawnedSheetInstance != null)
            {
                Destroy(spawnedSheetInstance);
                spawnedSheetInstance = null;
            }

            spawnedSheetInstance = Instantiate(sheetPrefab, sheetSpawnPoint.position, sheetSpawnPoint.rotation, null);

            // Start moving sheet to nextPoint if available
            if (sheetNextPoint != null)
            {
                if (sheetMoveCoroutine != null) StopCoroutine(sheetMoveCoroutine);
                sheetMoveCoroutine = StartCoroutine(MoveTransformOverTime(spawnedSheetInstance.transform, sheetNextPoint.position, sheetMoveDuration, OnSheetReachedNextPoint));
            }
            else
            {
                // No next point: immediately destroy and maybe move box
                Destroy(spawnedSheetInstance);
                spawnedSheetInstance = null;
                OnSheetReachedNextPoint();
            }
        }
    }

    private void OnSheetReachedNextPoint()
    {
        // sheet reached nextPoint -> destroy it and start box move
        if (spawnedSheetInstance != null)
        {
            Destroy(spawnedSheetInstance);
            spawnedSheetInstance = null;
        }

        // start box movement only after sheet finished
        if (boxTransform != null && boxFinalPoint != null)
        {
            if (boxMoveCoroutine != null) StopCoroutine(boxMoveCoroutine);
            boxMoveCoroutine = StartCoroutine(MoveTransformOverTime(boxTransform, boxFinalPoint.position, boxMoveDuration, null));
        }
    }

    // Generic transform lerp with completion callback
    private IEnumerator MoveTransformOverTime(Transform t, Vector3 target, float duration, Action onComplete)
    {
        if (t == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        if (duration <= 0f)
        {
            t.position = target;
            onComplete?.Invoke();
            yield break;
        }

        Vector3 start = t.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            t.position = Vector3.Lerp(start, target, p);
            yield return null;
        }
        t.position = target;
        onComplete?.Invoke();
    }

    // -----------------------
    // --- Cleanup Helpers ---
    // -----------------------
    private void CleanupTypeOnReached()
    {
        // Called when player confirms checkpoint (SetReached)
        switch (checkpointType)
        {
            case CheckpointType.Wash:
                // ensure wash is stopped
                EndC2_Wash();
                break;

            case CheckpointType.Dry:
                // Disable the active sprite (player collected it)
                EndC3_Dry_OnReachedOrReset();
                break;

            case CheckpointType.Clothes:
                // destroy sheet if still present and reset box
                if (spawnedSheetInstance != null)
                {
                    Destroy(spawnedSheetInstance);
                    spawnedSheetInstance = null;
                }

                if (boxTransform != null)
                {
                    boxTransform.position = boxOriginalPosition;
                }

                if (sheetMoveCoroutine != null) { StopCoroutine(sheetMoveCoroutine); sheetMoveCoroutine = null; }
                if (boxMoveCoroutine != null) { StopCoroutine(boxMoveCoroutine); boxMoveCoroutine = null; }
                break;

            case CheckpointType.None:
            default:
                break;
        }
    }

    private void CleanupTypeOnReset()
    {
        // Called when ResetCheckpoint is called (manager resets for loops)
        switch (checkpointType)
        {
            case CheckpointType.Wash:
                EndC2_Wash();
                if (washCoroutine != null) { StopCoroutine(washCoroutine); washCoroutine = null; }
                break;

            case CheckpointType.Dry:
                // disable any active sprite but keep lastDryIndex so next run avoids previous selection
                if (activeDryIndex >= 0) EndC3_Dry_OnReachedOrReset();
                break;

            case CheckpointType.Clothes:
                if (spawnedSheetInstance != null) { Destroy(spawnedSheetInstance); spawnedSheetInstance = null; }
                if (sheetMoveCoroutine != null) { StopCoroutine(sheetMoveCoroutine); sheetMoveCoroutine = null; }
                if (boxMoveCoroutine != null) { StopCoroutine(boxMoveCoroutine); boxMoveCoroutine = null; }
                if (boxTransform != null) boxTransform.position = boxOriginalPosition;
                break;

            case CheckpointType.None:
            default:
                break;
        }
    }

    // -----------------------
    // --- Debug / Gizmos ----
    // -----------------------
    void OnDrawGizmos()
    {
        Gizmos.color = isReached ? Color.green : (isActive ? Color.yellow : Color.gray);
        Gizmos.DrawWireSphere(transform.position, radius);

#if UNITY_EDITOR
        if (checkpointType == CheckpointType.Clothes)
        {
            Gizmos.color = Color.cyan;
            if (sheetSpawnPoint != null) Gizmos.DrawSphere(sheetSpawnPoint.position, 0.08f);
            if (sheetNextPoint != null) Gizmos.DrawSphere(sheetNextPoint.position, 0.08f);
            if (boxFinalPoint != null) Gizmos.DrawSphere(boxFinalPoint.position, 0.08f);
        }
#endif
    }
}
