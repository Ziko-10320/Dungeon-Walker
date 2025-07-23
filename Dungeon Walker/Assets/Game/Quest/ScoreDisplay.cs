using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public CheckpointManager checkpointManager;

    void Start()
    {
        if (scoreText == null)
        {
            Debug.LogError("ScoreDisplay: scoreText (TextMeshProUGUI) is not assigned.");
            enabled = false;
            return;
        }

        if (checkpointManager == null)
        {
            checkpointManager = FindObjectOfType<CheckpointManager>();
            if (checkpointManager == null)
            {
                Debug.LogError("ScoreDisplay: CheckpointManager not found in scene.");
                enabled = false;
                return;
            }
        }

        // Subscribe to events
        checkpointManager.OnScoreChanged.AddListener(UpdateScoreDisplay);
        checkpointManager.OnLoopCompleted.AddListener(OnLoopComplete); // Updated event
        checkpointManager.OnCheckpointTimerUpdate.AddListener(UpdateTimerDisplay);

        // Initial display update
        UpdateScoreDisplay(checkpointManager.TotalScore);
        UpdateTimerDisplay(0f); // Initialize timer display to empty
    }

    void UpdateScoreDisplay(int newScore)
    {
        scoreText.text = $"Score: {newScore}";
    }

    void UpdateTimerDisplay(float timeRemaining)
    {
        if (timerText != null)
        {
            if (timeRemaining > 0)
            {
                timerText.text = $"Stay: {timeRemaining:F1}s";
            }
            else
            {
                timerText.text = ""; // Clear timer text when not needed
            }
        }
    }

    void OnLoopComplete(int currentScore)
    {
        // You can add special effects or notifications here for loop completion
        Debug.Log($"Loop completed! Current score: {currentScore}");
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (checkpointManager != null)
        {
            checkpointManager.OnScoreChanged.RemoveListener(UpdateScoreDisplay);
            checkpointManager.OnLoopCompleted.RemoveListener(OnLoopComplete);
            checkpointManager.OnCheckpointTimerUpdate.RemoveListener(UpdateTimerDisplay);
        }
    }
}