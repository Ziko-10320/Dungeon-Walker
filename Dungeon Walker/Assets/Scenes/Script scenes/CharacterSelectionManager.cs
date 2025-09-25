using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Scene Transition")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    [Header("UI Elements")]
    [SerializeField] private GameObject startGameButton;
    // ---- NEW: Add references for the map selection UI ----
    [SerializeField] private GameObject mapSelectionGroup; // An empty GameObject holding your map buttons
    [Header("Selection Animation")]
    [Tooltip("How fast the selection animation plays.")]
    [SerializeField] private float animationSpeed = 0.2f;
    [Tooltip("The scale of the selected button (e.g., 1.1).")]
    [SerializeField] private float selectedScale = 1.1f;
    [Tooltip("The scale of the unselected buttons (e.g., 0.9).")]
    [SerializeField] private float deselectedScale = 0.9f;
    [Tooltip("The opacity of the unselected buttons (0 = invisible, 1 = fully visible).")]
    [SerializeField] private float deselectedAlpha = 0.6f;
    // ---- NEW: Variables to store the player's choices ----
    private string selectedCharacter;
    private string selectedMapSceneName; // We'll store the scene name to load
    [Header("Button References")]
    [SerializeField] private Button catButton;
    [SerializeField] private Button manButton;
    [SerializeField] private Button map1Button;
    [SerializeField] private Button map2Button;
    void Start()
    {
        // Hide both the start button and the map selection buttons at the beginning.
        if (startGameButton != null) startGameButton.SetActive(false);
        if (mapSelectionGroup != null) mapSelectionGroup.SetActive(false);
        StartCoroutine(FadeIn());
    }

    // --- CHARACTER SELECTION ---
    // These functions now also reveal the map selection buttons.
    public void SelectCat()
    {
        selectedCharacter = "Cat";
        PlayerPrefs.SetString("SelectedCharacter", selectedCharacter);
        Debug.Log("Character selected: Cat");
        AnimateSelection(catButton, manButton);
        // ---- NEW: Show the map buttons ----
        ShowMapSelection();
    }

    public void SelectMan()
    {
        selectedCharacter = "Man";
        PlayerPrefs.SetString("SelectedCharacter", selectedCharacter);
        Debug.Log("Character selected: Man");
        AnimateSelection(manButton, catButton);
        // ---- NEW: Show the map buttons ----
        ShowMapSelection();
    }

    // ---- NEW: A function to show the map selection UI ----
    private void ShowMapSelection()
    {
        if (mapSelectionGroup != null)
        {
            mapSelectionGroup.SetActive(true);
        }
    }

    // --- MAP SELECTION ---
    // ---- NEW: Functions for your new map buttons ----

    // This function will be called by your "Map 1" button.
    public void SelectMap1()
    {
        // IMPORTANT: Replace "SampleScene" with the actual name of your first map scene.
        selectedMapSceneName = "SampleScene";
        Debug.Log("Map selected: " + selectedMapSceneName);
        AnimateSelection(map1Button, map2Button);
        // Now that a map is also chosen, show the start button.
        ShowStartButton();
    }

    // This function will be called by your "Map 2" button.
    public void SelectMap2()
    {
        // IMPORTANT: Replace "NewMapScene" with the actual name of your second map scene.
        selectedMapSceneName = "MapII";
        Debug.Log("Map selected: " + selectedMapSceneName);
        AnimateSelection(map2Button, map1Button);
        // Now that a map is also chosen, show the start button.
        ShowStartButton();
    }
    private void AnimateSelection(Button selected, Button deselected)
    {
        // Start the animation coroutines for both buttons
        StartCoroutine(AnimateButton(selected, selectedScale, 1.0f));
        StartCoroutine(AnimateButton(deselected, deselectedScale, deselectedAlpha));
    }

    private IEnumerator AnimateButton(Button button, float targetScale, float targetAlpha)
    {
        // Get the components we need to animate
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();

        // Get the starting values
        Vector3 startScale = rectTransform.localScale;
        float startAlpha = canvasGroup.alpha;

        float timer = 0f;
        while (timer < animationSpeed)
        {
            timer += Time.deltaTime;
            float progress = timer / animationSpeed;

            // Smoothly interpolate (Lerp) between the start and target values
            rectTransform.localScale = Vector3.Lerp(startScale, Vector3.one * targetScale, progress);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);

            yield return null; // Wait for the next frame
        }

        // Ensure the final values are set exactly
        rectTransform.localScale = Vector3.one * targetScale;
        canvasGroup.alpha = targetAlpha;
    }

    // This function now gets called after a map is selected.
    private void ShowStartButton()
    {
        if (startGameButton != null)
        {
            startGameButton.SetActive(true);
        }
    }

    // --- GAME START & NAVIGATION ---

    // ---- UPDATED: The StartGame function is now much smarter ----
    public void StartGame()
    {
        // We only proceed if both a character and a map have been selected.
        if (!string.IsNullOrEmpty(selectedCharacter) && !string.IsNullOrEmpty(selectedMapSceneName))
        {
            // Save the final choices before loading the scene.
            PlayerPrefs.Save();
            // Load the scene the player chose!
            StartCoroutine(FadeAndLoadScene(selectedMapSceneName));
        }
        else
        {
            Debug.LogError("Cannot start game! Either character or map is not selected.");
        }
    }

    // This function remains the same.
    public void GoToMainMenu()
    {
        StartCoroutine(FadeAndLoadScene("start scene"));
    }
    private IEnumerator FadeIn()
    {
        // This coroutine fades the screen from black to clear
        fadeCanvasGroup.alpha = 1; // Start fully faded
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = 1 - (timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 0;
    }

    public IEnumerator FadeAndLoadScene(string sceneName)
    {
        // This coroutine fades the screen to black and then loads the scene
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = timer / fadeDuration;
            yield return null;
        }
        fadeCanvasGroup.alpha = 1;

        SceneManager.LoadScene(sceneName);
    }
}
