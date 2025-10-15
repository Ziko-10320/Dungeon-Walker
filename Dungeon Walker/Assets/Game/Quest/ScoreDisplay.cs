using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ScoreDisplay : MonoBehaviour
{
    [Header("Score Display Settings")]
    public TextMeshProUGUI scoreText;
    public string scorePrefix = "Score: "; // Custom prefix for score text
    public Image scoreImageUI; // Optional: Image UI for score
    private TutorialGameManager tutorialGameManager;
    [Header("Timer Display Settings")]
    public TextMeshProUGUI timerText;
    public string timerPrefix = "Quest Timer: "; // Custom prefix for timer text
    public TMP_FontAsset timerFontAsset; // Custom font asset for timer
    public FontStyles timerFontStyles = FontStyles.Normal; // Custom font styles for timer
    public string timerFormat = "F1"; // Custom format for timer (e.g., F1, F0)
    public Image timerImageUI; // New: Optional Image UI for timer

    [Header("E Button Display Settings")]
    public GameObject eButtonUI; // UI element for E button
    public TextMeshProUGUI eButtonText; // Text component for E button

    [Header("References")]
    public CheckpointManager checkpointManager;
    public Camera mainCamera; // Reference to the main camera
    public Canvas canvas; // Reference to the UI Canvas

    void Start()
    {
        tutorialGameManager = FindObjectOfType<TutorialGameManager>();

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

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("ScoreDisplay: Main Camera not found. Please assign it or ensure it\"s tagged \"MainCamera\".");
                enabled = false;
                return;
            }
        }

        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("ScoreDisplay: UI Canvas not found in scene. Please ensure you have a Canvas.");
                enabled = false;
                return;
            }
        }

        // Apply timer font settings
        if (timerText != null)
        {
            if (timerFontAsset != null)
            {
                timerText.font = timerFontAsset;
            }
            timerText.fontStyle = timerFontStyles;
        }

        // Subscribe to events
        checkpointManager.OnScoreChanged.AddListener(UpdateScoreDisplay);
        checkpointManager.OnLoopCompleted.AddListener(OnLoopComplete);
        checkpointManager.OnCheckpointTimerUpdate.AddListener(UpdateTimerDisplay);
        checkpointManager.OnShowEButton.AddListener(ShowEButton);
        checkpointManager.OnEButtonTextUpdate.AddListener(UpdateEButtonText);

        // Initial display update
        UpdateScoreDisplay(checkpointManager.TotalScore);
        UpdateTimerDisplay(0f);
        ShowEButton(false); // Initially hide E button

        // Initialize score image UI state
        if (scoreImageUI != null)
        {
            scoreImageUI.gameObject.SetActive(scoreImageUI.sprite != null);
        }
        // Initialize timer image UI state
        if (timerImageUI != null)
        {
            timerImageUI.gameObject.SetActive(false); // Initially hide timer image
        }
    }

    void Update()
    {
        // Continuously update position if E button is active
        if (eButtonUI != null && eButtonUI.activeSelf)
        {
            PositionEButtonUI();
        }
    }

    void UpdateScoreDisplay(int newScore)
    {
        scoreText.text = scorePrefix + newScore;
    }

    void UpdateTimerDisplay(float timeRemaining)
    {
        if (timerText != null)
        {
            if (timeRemaining > 0)
            {
                timerText.text = timerPrefix + timeRemaining.ToString(timerFormat) + "s";
                if (timerImageUI != null) timerImageUI.gameObject.SetActive(true);
            }
            else
            {
                timerText.text = "";
                if (timerImageUI != null) timerImageUI.gameObject.SetActive(false);
            }
        }
    }

    void ShowEButton(bool show)
    {
        if (eButtonUI != null)
        {
            eButtonUI.SetActive(show);
            if (show) // If showing, update position immediately
            {
                PositionEButtonUI();
            }
        }
    }

    void UpdateEButtonText(string text)
    {
        if (eButtonText != null)
        {
            eButtonText.text = text;
        }
    }

    void PositionEButtonUI()
    {
        if (eButtonUI == null || mainCamera == null || checkpointManager == null || canvas == null) return;

        // Get the current active checkpoint
        if (checkpointManager.CurrentCheckpointIndex < checkpointManager.checkpoints.Count)
        {
            Checkpoint currentCheckpoint = checkpointManager.checkpoints[checkpointManager.CurrentCheckpointIndex];
            if (currentCheckpoint != null)
            {
                // Get the world position of the checkpoint
                Vector3 checkpointWorldPos = currentCheckpoint.transform.position;

                // Convert world position to screen position
                Vector2 screenPos = mainCamera.WorldToScreenPoint(checkpointWorldPos);

                // Convert screen position to canvas position
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    screenPos,
                    canvas.worldCamera,
                    out Vector2 canvasPos
                );

                // Set the UI element's position
                // Assuming eButtonUI is a RectTransform and its pivot is centered (0.5, 0.5)
                eButtonUI.transform.localPosition = canvasPos;
            }
        }
    }

    void OnLoopComplete(int currentScore)
    {
        Debug.Log($"Loop completed! Current score: {currentScore}");
        if (tutorialGameManager != null)
        {
            tutorialGameManager.OnScoreUpdated(currentScore);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (checkpointManager != null)
        {
            checkpointManager.OnScoreChanged.RemoveListener(UpdateScoreDisplay);
            checkpointManager.OnLoopCompleted.RemoveListener(OnLoopComplete);
            checkpointManager.OnCheckpointTimerUpdate.RemoveListener(UpdateTimerDisplay);
            checkpointManager.OnShowEButton.RemoveListener(ShowEButton);
            checkpointManager.OnEButtonTextUpdate.RemoveListener(UpdateEButtonText);
        }
    }
}