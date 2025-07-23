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
    public int scorePerLoop = 1; // New: Score for completing a full loop

    [Header("Arrow Settings")]
    public ArrowPointer arrowPointer;

    [Header("Events")]
    public UnityEvent<int> OnCheckpointReached;
    public UnityEvent<int> OnLoopCompleted; // New: Event for loop completion
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<float> OnCheckpointTimerUpdate;

    private int currentCheckpointIndex = 0;
    private int totalScore = 0;
    private float currentCheckpointTimer = 0f;
    private bool isPlayerAtCheckpoint = false;
    private bool isTimerRunning = false;
    private bool hasPlayedRadiusSound = false; // New: Track if radius sound has been played
    private Coroutine checkpointTimerCoroutine;

    public int CurrentCheckpointIndex => currentCheckpointIndex;
    public int TotalScore => totalScore;
    public Vector3 CurrentTargetPosition => currentCheckpointIndex < checkpoints.Count ? checkpoints[currentCheckpointIndex].transform.position : Vector3.zero;
    public bool IsTimerRunning => isTimerRunning;

    void Start()
    {
        InitializeCheckpoints();
        UpdateArrowTarget();
    }

    void Update()
    {
        if (player != null && currentCheckpointIndex < checkpoints.Count)
        {
            CheckPlayerDistance();
        }
    }

    void InitializeCheckpoints()
    {
        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] != null)
            {
                checkpoints[i].SetCheckpointIndex(i);
                checkpoints[i].SetActive(i == 0);
            }
        }

        Debug.Log($"Initialized {checkpoints.Count} checkpoints. First target: {(checkpoints.Count > 0 ? checkpoints[0].name : "None")}");
    }

    void CheckPlayerDistance()
    {
        if (currentCheckpointIndex >= checkpoints.Count) return;

        Checkpoint currentCheckpoint = checkpoints[currentCheckpointIndex];
        if (currentCheckpoint == null) return;

        float distance = Vector3.Distance(player.position, currentCheckpoint.transform.position);

        if (distance <= currentCheckpoint.Radius) // Use individual checkpoint radius
        {
            if (!isPlayerAtCheckpoint)
            {
                isPlayerAtCheckpoint = true;
                isTimerRunning = true;
                hasPlayedRadiusSound = false; // Reset sound flag
                currentCheckpointTimer = currentCheckpoint.TimeToStay;

                // Play radius enter sound
                currentCheckpoint.PlayRadiusEnterSound();
                hasPlayedRadiusSound = true;

                if (checkpointTimerCoroutine != null) StopCoroutine(checkpointTimerCoroutine);
                checkpointTimerCoroutine = StartCoroutine(CheckpointTimer());
                Debug.Log($"Player entered checkpoint {currentCheckpointIndex + 1} radius. Starting timer...");
            }
        }
        else
        {
            if (isPlayerAtCheckpoint)
            {
                isPlayerAtCheckpoint = false;
                isTimerRunning = false;
                hasPlayedRadiusSound = false; // Reset sound flag
                if (checkpointTimerCoroutine != null) StopCoroutine(checkpointTimerCoroutine);
                currentCheckpointTimer = currentCheckpoint.TimeToStay; // Reset timer to full duration
                OnCheckpointTimerUpdate?.Invoke(0f); // Signal timer reset to UI
                Debug.Log($"Player left checkpoint {currentCheckpointIndex + 1} radius. Timer reset.");
            }
        }
    }

    IEnumerator CheckpointTimer()
    {
        while (currentCheckpointTimer > 0)
        {
            yield return null;
            if (isPlayerAtCheckpoint)
            {
                currentCheckpointTimer -= Time.deltaTime;
                OnCheckpointTimerUpdate?.Invoke(currentCheckpointTimer);
            }
            else
            {
                yield break;
            }
        }

        ReachCheckpoint();
    }

    void ReachCheckpoint()
    {
        if (checkpointTimerCoroutine != null) StopCoroutine(checkpointTimerCoroutine);
        isPlayerAtCheckpoint = false;
        isTimerRunning = false;
        hasPlayedRadiusSound = false; // Reset sound flag

        checkpoints[currentCheckpointIndex].SetReached();

        AddScore(scorePerCheckpoint);

        OnCheckpointReached?.Invoke(currentCheckpointIndex);

        Debug.Log($"Checkpoint {currentCheckpointIndex + 1} reached! Score: +{scorePerCheckpoint}");

        currentCheckpointIndex++;

        if (currentCheckpointIndex >= checkpoints.Count)
        {
            // Loop completed - always give loop score
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
        hasPlayedRadiusSound = false;
        if (checkpointTimerCoroutine != null) StopCoroutine(checkpointTimerCoroutine);
        currentCheckpointTimer = 0f;
        OnCheckpointTimerUpdate?.Invoke(0f);

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