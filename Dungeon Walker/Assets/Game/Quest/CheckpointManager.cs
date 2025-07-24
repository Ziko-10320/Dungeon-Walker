using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CheckpointManager : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public List<Checkpoint> checkpoints = new List<Checkpoint>();
    public Transform player;
    public bool loopCheckpoints = true;

    [Header("Score Settings")]
    public int scorePerCheckpoint = 100;
    public int scorePerLoop = 1;

    [Header("Arrow Settings")]
    public ArrowPointer arrowPointer;

    [Header("Events")]
    public UnityEvent<int> OnCheckpointReached;
    public UnityEvent<int> OnLoopCompleted;
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<float> OnCheckpointTimerUpdate;
    public UnityEvent<bool> OnShowEButton; // New: Event to show/hide E button
    public UnityEvent<string> OnEButtonTextUpdate; // New: Event to update E button text

    private int currentCheckpointIndex = 0;
    private int totalScore = 0;
    private float currentCheckpointTimer = 0f;
    private bool isPlayerAtCheckpoint = false; // Tracks if player is currently within the active checkpoint\"s radius
    private bool isTimerRunning = false; // Tracks if the quest timer is actively counting down
    private Coroutine checkpointTimerCoroutine;

    public int CurrentCheckpointIndex => currentCheckpointIndex;
    public int TotalScore => totalScore;
    public Vector3 CurrentTargetPosition => currentCheckpointIndex < checkpoints.Count ? checkpoints[currentCheckpointIndex].transform.position : Vector3.zero;
    public bool IsTimerRunning => isTimerRunning;

    void Start()
    {
        InitializeCheckpoints();
        UpdateArrowTarget();
        OnShowEButton?.Invoke(false); // Ensure E button is hidden at start
        OnCheckpointTimerUpdate?.Invoke(0f); // Ensure timer display is clear at start
    }

    void Update()
    {
        if (player == null || checkpoints.Count == 0) return;

        // Ensure currentCheckpointIndex is valid
        if (currentCheckpointIndex >= checkpoints.Count)
        {
            // This case should ideally be handled by loop logic or game end, but as a safeguard
            return;
        }

        Checkpoint currentCheckpoint = checkpoints[currentCheckpointIndex];
        if (currentCheckpoint == null) return;

        float distance = Vector3.Distance(player.position, currentCheckpoint.transform.position);
        bool playerIsNowInRadius = (distance <= currentCheckpoint.Radius);

        // Handle player entering/leaving radius
        if (playerIsNowInRadius && !isPlayerAtCheckpoint)
        {
            isPlayerAtCheckpoint = true;
            currentCheckpoint.SetPlayerInRadius(true);
            currentCheckpoint.PlayRadiusEnterSound();
            UpdateEButtonVisibility();
            Debug.Log($"Player entered checkpoint {currentCheckpointIndex + 1} radius.");
        }
        else if (!playerIsNowInRadius && isPlayerAtCheckpoint)
        {
            isPlayerAtCheckpoint = false;
            currentCheckpoint.SetPlayerInRadius(false);
            OnShowEButton?.Invoke(false); // Hide E button when leaving radius

            // Only play exit sound if quest hasn\"t started yet and timer is not running
            if (!currentCheckpoint.QuestStarted && !isTimerRunning)
            {
                currentCheckpoint.PlayRadiusExitPrematureSound();
            }
            Debug.Log($"Player left checkpoint {currentCheckpointIndex + 1} radius.");
        }

        // Handle E-press input
        HandleInput();
    }

    void InitializeCheckpoints()
    {
        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] != null)
            {
                checkpoints[i].SetCheckpointIndex(i);
                checkpoints[i].SetActive(i == 0); // Only the first checkpoint is active initially
            }
        }

        Debug.Log($"Initialized {checkpoints.Count} checkpoints. First target: {(checkpoints.Count > 0 ? checkpoints[0].name : "None")}");
    }

    void UpdateEButtonVisibility()
    {
        if (currentCheckpointIndex >= checkpoints.Count) return;
        Checkpoint currentCheckpoint = checkpoints[currentCheckpointIndex];

        if (isPlayerAtCheckpoint)
        {
            if (!currentCheckpoint.QuestStarted)
            {
                OnShowEButton?.Invoke(true);
                OnEButtonTextUpdate?.Invoke("Press E to Start Quest");
            }
            else if (currentCheckpoint.TimerEnded)
            {
                OnShowEButton?.Invoke(true);
                OnEButtonTextUpdate?.Invoke("Press E to Complete Quest");
            }
            else
            {
                OnShowEButton?.Invoke(false); // Hide if quest started but timer not ended
            }
        }
        else
        {
            OnShowEButton?.Invoke(false); // Hide if player not in radius
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && currentCheckpointIndex < checkpoints.Count)
        {
            Checkpoint currentCheckpoint = checkpoints[currentCheckpointIndex];

            // Player must be in radius to interact with E button
            if (!isPlayerAtCheckpoint) return;

            if (!currentCheckpoint.QuestStarted)
            {
                // Start the quest
                currentCheckpoint.StartQuest();
                currentCheckpointTimer = currentCheckpoint.TimeToStay;
                isTimerRunning = true;

                // Hide E button and start timer
                OnShowEButton?.Invoke(false);

                if (checkpointTimerCoroutine != null) StopCoroutine(checkpointTimerCoroutine);
                checkpointTimerCoroutine = StartCoroutine(CheckpointTimer());

                Debug.Log($"Quest started at checkpoint {currentCheckpointIndex + 1}");
            }
            else if (currentCheckpoint.TimerEnded)
            {
                // Complete the quest
                ReachCheckpoint();
            }
        }
    }

    IEnumerator CheckpointTimer()
    {
        while (currentCheckpointTimer > 0)
        {
            yield return null;
            currentCheckpointTimer -= Time.deltaTime;
            OnCheckpointTimerUpdate?.Invoke(currentCheckpointTimer);
        }

        // Timer ended
        if (currentCheckpointIndex < checkpoints.Count)
        {
            Checkpoint currentCheckpoint = checkpoints[currentCheckpointIndex];
            currentCheckpoint.EndTimer();
            isTimerRunning = false;

            // Show E button to complete quest if player is still in radius
            UpdateEButtonVisibility();

            Debug.Log($"Timer ended for checkpoint {currentCheckpointIndex + 1}");
        }
    }

    void ReachCheckpoint()
    {
        if (checkpointTimerCoroutine != null) StopCoroutine(checkpointTimerCoroutine);
        isTimerRunning = false;

        // Hide E button
        OnShowEButton?.Invoke(false);

        checkpoints[currentCheckpointIndex].SetReached();

        AddScore(scorePerCheckpoint);

        OnCheckpointReached?.Invoke(currentCheckpointIndex);

        Debug.Log($"Checkpoint {currentCheckpointIndex + 1} reached! Score: +{scorePerCheckpoint}");

        currentCheckpointIndex++;

        if (currentCheckpointIndex >= checkpoints.Count)
        {
            // Loop completed
            AddScore(scorePerLoop);
            OnLoopCompleted?.Invoke(totalScore);
            Debug.Log($"Loop completed! Score: +{scorePerLoop}. Total Score: {totalScore}");

            // Reset for next loop
            currentCheckpointIndex = 0;
            foreach (Checkpoint cp in checkpoints)
            {
                cp.ResetCheckpoint();
            }
            checkpoints[currentCheckpointIndex].SetActive(true);
        }
        else
        {
            checkpoints[currentCheckpointIndex].SetActive(true);
            Debug.Log($"Next target: {checkpoints[currentCheckpointIndex].name}");
        }

        UpdateArrowTarget();
    }

    void AddScore(int points)
    {
        totalScore += points;
        OnScoreChanged?.Invoke(totalScore);
    }

    void UpdateArrowTarget()
    {
        if (arrowPointer != null && currentCheckpointIndex < checkpoints.Count)
        {
            arrowPointer.SetTarget(checkpoints[currentCheckpointIndex].transform);
        }
        else if (arrowPointer != null)
        {
            arrowPointer.SetTarget(null);
        }
    }

    public void ResetCheckpoints()
    {
        currentCheckpointIndex = 0;
        totalScore = 0;
        isPlayerAtCheckpoint = false;
        isTimerRunning = false;
        if (checkpointTimerCoroutine != null) StopCoroutine(checkpointTimerCoroutine);
        currentCheckpointTimer = 0f;
        OnCheckpointTimerUpdate?.Invoke(0f);
        OnShowEButton?.Invoke(false);

        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] != null)
            {
                checkpoints[i].ResetCheckpoint();
                checkpoints[i].SetActive(i == 0);
            }
        }

        OnScoreChanged?.Invoke(totalScore);
        UpdateArrowTarget();
        Debug.Log("Checkpoints reset!");
    }

    public void AddCheckpoint(Checkpoint checkpoint)
    {
        if (!checkpoints.Contains(checkpoint))
        {
            checkpoints.Add(checkpoint);
            checkpoint.SetCheckpointIndex(checkpoints.Count - 1);
        }
    }

    public Vector3 GetNextCheckpointPosition()
    {
        if (currentCheckpointIndex < checkpoints.Count)
        {
            return checkpoints[currentCheckpointIndex].transform.position;
        }
        return Vector3.zero;
    }

    public bool HasNextCheckpoint()
    {
        return currentCheckpointIndex < checkpoints.Count;
    }

    public float GetDistanceToCurrentCheckpoint()
    {
        if (player != null && currentCheckpointIndex < checkpoints.Count)
        {
            return Vector3.Distance(player.position, checkpoints[currentCheckpointIndex].transform.position);
        }
        return float.MaxValue;
    }
}
