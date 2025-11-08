using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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
    [Header("UI Pop-In Animation")]
    [Tooltip("The list of UI elements to animate in sequence.")]
    public List<GameObject> animatedElements;
    [Tooltip("The time to wait between each element popping in.")]
    public float delayBetweenAnimations = 0.5f;

    [Tooltip("How long the pop-in animation for each element should take.")]
    public float popInDuration = 0.3f;
    public AudioClip popInSound;
    [Tooltip("How much bigger the element gets before shrinking back to normal size (e.g., 1.2 is 20% bigger).")]
    public float popInOvershootScale = 1.2f;
    [Tooltip("Drag your dedicated 2D AudioSource object here.")]
    public AudioSource soundEffectSource;
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
        // We need to hide all the animated elements at the start.
        if (tutorialCompletePanel != null)
        {
            tutorialCompletePanel.SetActive(false);
        }

        // Hide each animated element and the main menu button initially.
        foreach (var element in animatedElements)
        {
            element.SetActive(false);
        }
        mainMenuButton.gameObject.SetActive(false);
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
            // Show the main panel background and pause the game.
            tutorialCompletePanel.SetActive(true);
            Time.timeScale = 0f;

            // Start the animation sequence!
            StartCoroutine(AnimatePanelSequence());
        }
    }
    private IEnumerator AnimatePanelSequence()
    {
        // Loop through each UI element you added to the list.
        foreach (var element in animatedElements)
        {
            // Start the pop-in animation for the current element.
            StartCoroutine(PopInElement(element));

            // Wait for the specified delay before moving to the next element.
            yield return new WaitForSecondsRealtime(delayBetweenAnimations);
        }

        // After all elements have animated, pop in the main menu button.
        StartCoroutine(PopInElement(mainMenuButton.gameObject));
    }
    private IEnumerator PopInElement(GameObject element)
    {
        if (popInSound != null && soundEffectSource != null)
        {
            // Use the AudioSource you provided to play the sound.
            soundEffectSource.PlayOneShot(popInSound);
        }
        else
        {
            // This warning helps if you forget to hook something up.
            if (popInSound != null) Debug.LogWarning("Pop-in sound is assigned, but the Sound Effect Source is missing!");
        }
        // 1. Set initial state: invisible and normal size.
        element.SetActive(true);
        element.transform.localScale = Vector3.one;
        CanvasGroup cg = element.GetComponent<CanvasGroup>();
        if (cg == null) cg = element.AddComponent<CanvasGroup>(); // Add CanvasGroup if it doesn't exist
        cg.alpha = 0;

        // 2. Animation loop
        float timer = 0f;
        while (timer < popInDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = timer / popInDuration;

            // Fade in the alpha
            cg.alpha = progress;

            // Scale up to the overshoot size, then back down to 1.
            // This uses a simple curve: goes up to overshootScale then back to 1.
            float scale;
            if (progress < 0.5f)
            {
                // First half: scale up
                scale = Mathf.Lerp(1f, popInOvershootScale, progress * 2);
            }
            else
            {
                // Second half: scale down
                scale = Mathf.Lerp(popInOvershootScale, 1f, (progress - 0.5f) * 2);
            }
            element.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        // 3. Ensure final state is perfect.
        cg.alpha = 1;
        element.transform.localScale = Vector3.one;
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
        // 1. Unpause the game.
        Time.timeScale = 1f;

        // Make sure the panel we are about to fade is visible.
        // This is important if your fade object is different from the panel itself.
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.gameObject.SetActive(true);
        }

        // 2. Fade In Logic (from 0 to 1)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            // THE FIX IS HERE: We just use (timer / fadeDuration) to go from 0 to 1.
            panelCanvasGroup.alpha = timer / fadeDuration;
            yield return null;
        }
        // Ensure it's fully opaque at the end.
        panelCanvasGroup.alpha = 1;

        // 3. Load the Lobby Scene using its index.
        Debug.Log("Fade complete. Loading Lobby Scene (Index 0).");
        SceneManager.LoadScene(0);
    }
}
