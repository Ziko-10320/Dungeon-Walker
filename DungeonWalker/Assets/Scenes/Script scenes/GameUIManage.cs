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
    [SerializeField] private Button doubleCoinsButton;
    private bool hasUsedDoubleCoinsAd = false;
    void Awake()
    {
        // Developer Mode check is perfect, we leave it as is.
        if (developerMode)
        {
            Debug.LogWarning("DEVELOPER MODE is ON. Using characters enabled in the scene hierarchy.");
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
            return;
        }

        // --- THIS IS THE FINAL, GUARANTEED FIX ---
        // This code will only run if 'developerMode' is false.
        string selectedCharacter = PlayerPrefs.GetString("SelectedCharacter", "Cat");

        if (selectedCharacter == "Cat")
        {
            // If we chose Cat, ONLY disable Man.
            // We assume Cat is already active in the scene.
            if (catCharacterObject != null) catCharacterObject.SetActive(true);
            if (catManagerObject != null) catManagerObject.SetActive(true);
            if (manCharacterObject != null) manCharacterObject.SetActive(false);
            if (manManagerObject != null) manManagerObject.SetActive(false);

            // Set the camera to follow the Cat.
            if (cameraFollowScript != null && catFollowTarget != null)
            {
                cameraFollowScript.SetTarget(catFollowTarget);
            }
            Debug.Log("Character Selected: Cat. Disabling Man and setting camera target.");
        }
        else // This covers the "Man" case and any other possibility.
        {
            // If we chose Man, ONLY disable Cat.
            // We assume Man is already active in the scene.
            if (manCharacterObject != null) manCharacterObject.SetActive(true);
            if (manManagerObject != null) manManagerObject.SetActive(true);
            if (catCharacterObject != null) catCharacterObject.SetActive(false);
            if (catManagerObject != null) catManagerObject.SetActive(false);

            // Set the camera to follow the Man.
            if (cameraFollowScript != null && manFollowTarget != null)
            {
                cameraFollowScript.SetTarget(manFollowTarget);
            }
            Debug.Log("Character Selected: Man. Disabling Cat and setting camera target.");
        }
        // --- END OF FIX ---
#if UNITY_WEBGL
        // This code will only be included in WebGL builds.
        Debug.Log("Platform is WebGL. Configuring ads for CrazyGames.");
        if (doubleCoinsButton != null)
        {
            // Find the old Unity Ads button script and disable it.
            RewardedAdButton unityAdButton = doubleCoinsButton.GetComponent<RewardedAdButton>();
            if (unityAdButton != null)
            {
                unityAdButton.enabled = false;
            }

            // Make sure the new CrazyGames button script is enabled.
            RewardedAdButtonCrazy crazyAdButton = doubleCoinsButton.GetComponent<RewardedAdButtonCrazy>();
            if (crazyAdButton != null)
            {
                crazyAdButton.enabled = true;
            }
        }
#else
    //This code will run on all other platforms (like Android).
    Debug.Log("Platform is NOT WebGL. Configuring ads for Unity Ads.");
    if (doubleCoinsButton != null)
    {
        // Find the new CrazyGames button script and disable it.
        RewardedAdButtonCrazy crazyAdButton = doubleCoinsButton.GetComponent<RewardedAdButtonCrazy>();
        if (crazyAdButton != null)
        {
            crazyAdButton.enabled = false;
        }

        // Make sure the old Unity Ads button script is enabled.
        RewardedAdButton unityAdButton = doubleCoinsButton.GetComponent<RewardedAdButton>();
        if (unityAdButton != null)
        {
            unityAdButton.enabled = true;
        }
    }
#endif
    }
    void Start()
    {
        if (statAnimation != null)
        {
            // Subscribe to the OnCountUpComplete event.
            // When it fires, our ShowDoubleCoinsButton method will be called.
            statAnimation.OnCountUpComplete.AddListener(ShowDoubleCoinsButton);
        }
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
        if (doubleCoinsButton != null)
        {
            doubleCoinsButton.gameObject.SetActive(false);
        }

        // --- THIS IS THE CORRECTED LOGIC FLOW ---

        // Find the active CheckpointManager to get the score
        CheckpointManager activeCheckpointManager = FindObjectOfType<CheckpointManager>();

        // Make sure the PlayerStatsManager exists before doing anything
        if (PlayerStatsManager.Instance != null)
        {
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
        StartCoroutine(RefreshAdStatusLoop());
    }
    private void ShowDoubleCoinsButton()
    {
        Debug.Log("[GameUIManager] Stat animation finished. Checking if Double Coins button should be shown.");
        bool hasCoinsToDouble = (PlayerStatsManager.Instance != null && PlayerStatsManager.Instance.coinsGathered > 0);

        if (doubleCoinsButton != null)
        {
            // Show the button if there are coins and the ad hasn't been used.
            doubleCoinsButton.gameObject.SetActive(hasCoinsToDouble && !hasUsedDoubleCoinsAd);
        }
    }

    /// <summary>
    /// This is the "callback" method that the AdManager will execute ONLY if the ad is watched successfully.
    /// </summary>
    public void RewardDoubleCoins()
    {
        Debug.Log("Ad successfully completed! Doubling coins.");

        hasUsedDoubleCoinsAd = true;
        if (doubleCoinsButton != null)
        {
            // The RewardedAdButton script will disable the button, but we can hide it for good.
            doubleCoinsButton.gameObject.SetActive(false);
        }

        if (PlayerStatsManager.Instance != null && WalletManager.Instance != null)
        {
            int coinsThisRun = PlayerStatsManager.Instance.coinsGathered;
            if (coinsThisRun > 0)
            {
                Debug.Log($"Player gathered {coinsThisRun} coins this run. Adding them again to the wallet.");
                WalletManager.Instance.AddCoins(coinsThisRun);
                if (statAnimation != null)
                {
                    // Call the new method to animate only the coins to the new doubled value.
                    statAnimation.AnimateCoinStat(coinsThisRun * 2);
                }

            }
        }
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
#if UNITY_WEBGL
        // For WebGL, tell the CrazyGamesManager to handle everything.
        if (CrazyGamesManager.Instance != null)
        {
            CrazyGamesManager.Instance.ShowMidGameAdAndRestart();
        }
#else
    // For other platforms (like Android), just restart directly.
    StartCoroutine(FadeAndRestart());
#endif
    }

    public void ReturnToMenu()
    {
        StartCoroutine(FadeAndLoadScene(0));
    }
    public void ReturnToMenuDeath()
    {
#if UNITY_WEBGL
        // For WebGL, tell the CrazyGamesManager to use our new, flexible function.
        if (CrazyGamesManager.Instance != null)
        {
            // We tell the manager: "Show an ad, and the action to perform afterward
            // is to load the main menu scene (Scene 0)."
            CrazyGamesManager.Instance.ShowMidGameAdAndPerformAction(() => {
                StartCoroutine(FadeAndLoadScene(0));
            });
        }
#else
    // For other platforms (like Android), just go to the menu directly.
    // You could add a different ad logic here if you wanted.
    StartCoroutine(FadeAndLoadScene(0));
#endif
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
    private IEnumerator RefreshAdStatusLoop()
    {
        // This loop will run as long as the death panel is active.
        while (deathPanel.activeInHierarchy)
        {
            // Find the RewardedAdButton component on your double coins button.
            RewardedAdButton adButton = doubleCoinsButton.GetComponent<RewardedAdButton>();
            if (adButton != null)
            {
                // Manually call its UpdateAdStatus() method.
                // This will force it to check with the AdManager again.
                adButton.UpdateAdStatus();
            }

            // If the button is STILL not interactable, it means the ad is not ready.
            // We should tell the AdManager to try loading it again.
            if (!doubleCoinsButton.interactable && AdManager_New.Instance != null)
            {
                // This is a safe way to try reloading.
                AdManager_New.Instance.LoadSpecificRewardedAd("Rewarded_Android_DoubleCoins");
            }

            // Wait for a few seconds before checking again.
            // We don't want to spam the ad server.
            yield return new WaitForSecondsRealtime(5f); // Check every 5 seconds.
        }
    }
}
