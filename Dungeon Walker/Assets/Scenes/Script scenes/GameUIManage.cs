using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
public class GameUIManager : MonoBehaviour
{
    // ---- NEW: A toggle for easy testing ----
    [Header("Developer Settings")]
    [Tooltip("If true, the game will use the characters enabled in the Hierarchy instead of PlayerPrefs. FOR TESTING ONLY.")]
    [SerializeField] private bool developerMode = false;
    [Header("Scene Transition")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    [Header("Character Management")]
    [SerializeField] private GameObject catCharacterObject;
    [SerializeField] private GameObject catManagerObject;
    [SerializeField] private GameObject manCharacterObject;
    [SerializeField] private GameObject manManagerObject;
       [Header("Camera Control")]
    [SerializeField] private CameraFollowMouseHorizontal cameraFollowScript;
    [SerializeField] private Transform catFollowTarget;
    [SerializeField] private Transform manFollowTarget;
    [SerializeField] private StatCounterAnimation statAnimation;
    [Header("Game State")]
    public static bool isGamePaused = false;

    [Header("Death Panel UI")]
    [SerializeField] private GameObject deathPanel;

    [Header("Pause Panel UI")]
    [SerializeField] private GameObject pausePanel;

    void Awake()
    {
        // ---- NEW: Check for Developer Mode at the start ----
        // If this is true, we skip all the PlayerPrefs logic and use what's active in the scene.
        if (developerMode)
        {
            Debug.LogWarning("DEVELOPER MODE is ON. Using characters enabled in the scene hierarchy.");

            // We still need to tell the camera who to follow based on the active character.
            if (catCharacterObject != null && catCharacterObject.activeInHierarchy)
            {
                if (cameraFollowScript != null && catFollowTarget != null)
                {
                    cameraFollowScript.SetTarget(catFollowTarget);
                }
            }
            else if (manCharacterObject != null && manCharacterObject.activeInHierarchy)
            {
                if (cameraFollowScript != null && manFollowTarget != null)
                {
                    cameraFollowScript.SetTarget(manFollowTarget);
                }
            }
            // Exit the function early so the PlayerPrefs code below doesn't run.
            return;
        }

        // ---- Your existing PlayerPrefs logic ----
        // This code will only run if 'developerMode' is false.
        string selectedCharacter = PlayerPrefs.GetString("SelectedCharacter", "Cat");

        if (selectedCharacter == "Cat")
        {
            // Enable Cat objects
            if (catCharacterObject != null) catCharacterObject.SetActive(true);
            if (catManagerObject != null) catManagerObject.SetActive(true);
            // Disable Man objects
            if (manCharacterObject != null) manCharacterObject.SetActive(false);
            if (manManagerObject != null) manManagerObject.SetActive(false);
            // Set camera target
            if (cameraFollowScript != null && catFollowTarget != null)
            {
                cameraFollowScript.SetTarget(catFollowTarget);
            }
            Debug.Log("Activating Cat, CatManager, and setting camera target.");
        }
        else if (selectedCharacter == "Man")
        {
            // Enable Man objects
            if (manCharacterObject != null) manCharacterObject.SetActive(true);
            if (manManagerObject != null) manManagerObject.SetActive(true);
            // Disable Cat objects
            if (catCharacterObject != null) catCharacterObject.SetActive(false);
            if (catManagerObject != null) catManagerObject.SetActive(false);
            // Set camera target
            if (cameraFollowScript != null && manFollowTarget != null)
            {
                cameraFollowScript.SetTarget(manFollowTarget);
            }
            Debug.Log("Activating Man, ManManager, and setting camera target.");
        }
    }

    void Start()
    {
        if (statAnimation != null) statAnimation.ResetUI();
        // Hide UI panels
        if (deathPanel != null) deathPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (fadeCanvasGroup != null)
        {
            StartCoroutine(FadeIn());
        }
        // Ensure game is running
        Time.timeScale = 1f;
        isGamePaused = false;
    }

    // ... (The rest of your script remains unchanged) ...
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (deathPanel != null && deathPanel.activeInHierarchy) return;
            if (isGamePaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f;
        isGamePaused = true;
    }

    public void ResumeGame()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
        isGamePaused = false;
    }

    public void ShowDeathScreen()
    {
        // First, make sure the panel exists before we do anything.
        if (deathPanel == null)
        {
            Debug.LogError("Death Panel is not assigned in the Inspector!");
            return;
        }

        // --- THIS IS THE CORRECTED LOGIC FLOW ---

        // Find the active CheckpointManager to get the score
        CheckpointManager activeCheckpointManager = FindObjectOfType<CheckpointManager>();

        // Make sure the PlayerStatsManager exists before doing anything
        if (PlayerStatsManager.Instance != null)
        {
            // 1. SET THE FINAL SCORE FIRST
            // This is critical. The score must be set before we check it.
            if (activeCheckpointManager != null)
            {
                PlayerStatsManager.Instance.SetFinalScore(activeCheckpointManager.TotalScore);
            }
            else
            {
                // If there's no manager, at least set the score to 0.
                PlayerStatsManager.Instance.SetFinalScore(0);
                Debug.LogWarning("Could not find an active CheckpointManager. Score set to 0.");
            }

            // 2. NOW, CHECK FOR HIGH SCORES
            // This will set the 'newHighScoreAchieved' flags correctly.
            PlayerStatsManager.Instance.CheckAndSaveHighScores();

            // 3. SHOW THE PANEL
            // It's better to show the panel *after* the data is ready.
            deathPanel.SetActive(true);

            // 4. FINALLY, START THE ANIMATION
            // The animation script will now have the correct data to work with.
            if (statAnimation != null)
            {
                statAnimation.StartAnimation(
                    PlayerStatsManager.Instance.finalScore,
                    PlayerStatsManager.Instance.enemiesKilled,
                    PlayerStatsManager.Instance.coinsGathered
                );
            }
        }
        else
        {
            Debug.LogError("PlayerStatsManager.Instance is not found! Cannot show stats.");
        }

        // Pause the game at the very end.
        Time.timeScale = 0f;
        isGamePaused = true;
    }
    public void HideDeathScreen()
    {
        // This method simply deactivates the death panel.
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }

        // We also make sure the game is not paused if we are reviving.
        // The AdsManager already sets Time.timeScale back to 1, but this is an extra safeguard.
        if (isGamePaused)
        {
            Time.timeScale = 1f;
            isGamePaused = false;
        }
    }
    public void RestartGame()
    {
        StartCoroutine(FadeAndRestart());
    }

    public void ReturnToMenu()
    {
        StartCoroutine(FadeAndLoadScene(0));
    }
    private IEnumerator FadeIn()
    {

        if (VFX_Director.Instance != null)
        {
            VFX_Director.Instance.ResetInitializationFlag();
        }
        fadeCanvasGroup.alpha = 1;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime; // Use unscaled time for UI that works when paused
            fadeCanvasGroup.alpha = 1 - (timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 0;
    }

    private IEnumerator FadeAndLoadScene(int sceneIndex)
    {
        if (VFX_Director.Instance != null)
        {
            VFX_Director.Instance.ResetInitializationFlag();
        }
        Time.timeScale = 1f; // Ensure time is running before we fade
        isGamePaused = false;
        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.ResetStats();
            Debug.Log("Player stats reset before returning to menu.");
        }
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = timer / fadeDuration;
            yield return null;
        }
        fadeCanvasGroup.alpha = 1;

        SceneManager.LoadScene(sceneIndex);
    }

    private IEnumerator FadeAndRestart()
    {
        if (VFX_Director.Instance != null)
        {
            VFX_Director.Instance.ResetInitializationFlag();
        }
        Time.timeScale = 1f;
        isGamePaused = false;

        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.ResetStats();
        }

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = timer / fadeDuration;
            yield return null;
        }
        fadeCanvasGroup.alpha = 1;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
