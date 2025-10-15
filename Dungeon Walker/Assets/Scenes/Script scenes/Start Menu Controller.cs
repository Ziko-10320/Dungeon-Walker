using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    [Header("Scene Transition")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    // ----> ADD THIS LINE <----
    // The key we use to check if the tutorial was completed.
    private const string TutorialCompletedKey = "TutorialCompleted";

    // This is your existing function for the single-player "Start" button
    public void OnStartClick()
    {
        // 1. Check PlayerPrefs to see if the tutorial has been completed.
        bool tutorialCompleted = PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1;

        if (tutorialCompleted)
        {
            // Player has already completed the tutorial.
            // Load the Character Selection scene (Scene Index 1) as normal.
            Debug.Log("Tutorial complete. Loading Character Selection Scene (Index 1).");
            StartCoroutine(FadeAndLoadScene(1));
        }
        else
        {
            // This is the player's first time!
            // We need to force them into the tutorial.
            Debug.Log("First time launch. Loading Tutorial Scene (Index 4).");
            StartCoroutine(FadeAndLoadScene(4)); // <-- THE FIX IS HERE
        }
    }


    // This is your existing function for the "Online" button
    public void OnOnlineClick()
    {
        StartCoroutine(FadeAndLoadScene(3));
    }

    // This is your existing function for the "Exit" button
    public void OnExitClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    // ----> ADD THIS DEV TOOL FUNCTION <----
    // You can hook this up to a secret button or call it from the Inspector to test.
    [ContextMenu("DEV: Reset Tutorial Progress")]
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(TutorialCompletedKey);
        PlayerPrefs.Save();
        Debug.LogWarning("TUTORIAL PROGRESS HAS BEEN RESET. Next 'Start' click will launch the tutorial.");
    }

    public IEnumerator FadeAndLoadScene(int sceneIndex)
    {
        // Fade Out
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = timer / fadeDuration;
            yield return null;
        }
        fadeCanvasGroup.alpha = 1;

        // Load Scene
        SceneManager.LoadScene(sceneIndex);
    }
}
