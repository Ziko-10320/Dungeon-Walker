using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class TutorialGameManager : MonoBehaviour
{
    // ... (All your variables at the top remain the same) ...
    [Header("Character Setup")]
    public GameObject existingCharacterObject;
    public GameObject existingManagerObject;

    [Header("Camera Control")]
    public CameraFollowMouseHorizontal cameraFollowScript;
    public Transform characterFollowTarget;

    [Header("Tutorial Goal")]
    public int scoreToComplete = 1;

    [Header("Completion UI")]
    public GameObject tutorialCompletePanel;
    public Button mainMenuButton;
    public CanvasGroup panelCanvasGroup; // This is for the fade OUT now.
    public float fadeDuration = 0.5f;

    private const string TutorialCompletedKey = "TutorialCompleted";
    private bool tutorialFinished = false;

    // Awake() and Start() are now simplified
    void Awake()
    {
        // ... (Character and Camera setup logic is unchanged) ...
    }

    void Start()
    {
        // We just need to make sure the panel is hidden at the start.
        if (tutorialCompletePanel != null)
        {
            tutorialCompletePanel.SetActive(false);
        }
    }

    // OnScoreUpdated is unchanged
    public void OnScoreUpdated(int newScore)
    {
        if (newScore >= scoreToComplete && !tutorialFinished)
        {
            tutorialFinished = true;
            CompleteTutorial();
        }
    }

    // ----> THIS FUNCTION IS NOW SIMPLER <----
    // It just shows the panel and pauses the game. No fade.
    private void CompleteTutorial()
    {
        PlayerPrefs.SetInt(TutorialCompletedKey, 1);
        PlayerPrefs.Save();

        if (tutorialCompletePanel != null)
        {
            Debug.Log("Showing completion panel instantly.");
            tutorialCompletePanel.SetActive(true); // Show the panel immediately.
            Time.timeScale = 0f; // Pause the game.
        }
    }

    // ----> THIS FUNCTION NOW STARTS THE FADE-OUT <----
    // This is the public function your button calls from the Inspector.
    public void GoToMainMenu()
    {
        // Start the fade-out process.
        StartCoroutine(FadeAndLoadLobby());
    }

    // ----> THIS IS THE NEW FADE-OUT COROUTINE <----
    private IEnumerator FadeAndLoadLobby()
    {
        // 1. Unpause the game so the fade works correctly if using scaled time,
        //    but we will use unscaled time to be safe.
        Time.timeScale = 1f;

        // 2. Fade Out Logic (just like in your StartMenuController)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            // Use unscaledDeltaTime because it's good practice for UI fades.
            timer += Time.unscaledDeltaTime;
            panelCanvasGroup.alpha = 1 - (timer / fadeDuration); // Fade from 1 to 0
            yield return null;
        }
        panelCanvasGroup.alpha = 0;

        // 3. Load the Lobby Scene using its index.
        Debug.Log("Fade complete. Loading Lobby Scene (Index 0).");
        SceneManager.LoadScene(0); // Use index 0 for the lobby.
    }
}
