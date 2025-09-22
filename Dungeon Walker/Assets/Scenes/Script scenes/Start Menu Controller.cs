using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    [Header("Scene Transition")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    // This is your existing function for the single-player "Start" button
    public void OnStartClick()
    {
        StartCoroutine(FadeAndLoadScene(1));
    }
   
    // This is your existing function for the "Online" button
    public void OnOnlineClick()
    {
        StartCoroutine(FadeAndLoadScene(3));
    }

    // --- THE OnShopClick() FUNCTION IS NO LONGER NEEDED HERE ---

    // This is your existing function for the "Exit" button
    public void OnExitClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
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