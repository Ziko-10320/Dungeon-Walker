using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject startGameButton;
    // ---- NEW: Add references for the map selection UI ----
    [SerializeField] private GameObject mapSelectionGroup; // An empty GameObject holding your map buttons

    // ---- NEW: Variables to store the player's choices ----
    private string selectedCharacter;
    private string selectedMapSceneName; // We'll store the scene name to load

    void Start()
    {
        // Hide both the start button and the map selection buttons at the beginning.
        if (startGameButton != null) startGameButton.SetActive(false);
        if (mapSelectionGroup != null) mapSelectionGroup.SetActive(false);
    }

    // --- CHARACTER SELECTION ---
    // These functions now also reveal the map selection buttons.
    public void SelectCat()
    {
        selectedCharacter = "Cat";
        PlayerPrefs.SetString("SelectedCharacter", selectedCharacter);
        Debug.Log("Character selected: Cat");

        // ---- NEW: Show the map buttons ----
        ShowMapSelection();
    }

    public void SelectMan()
    {
        selectedCharacter = "Man";
        PlayerPrefs.SetString("SelectedCharacter", selectedCharacter);
        Debug.Log("Character selected: Man");

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

        // Now that a map is also chosen, show the start button.
        ShowStartButton();
    }

    // This function will be called by your "Map 2" button.
    public void SelectMap2()
    {
        // IMPORTANT: Replace "NewMapScene" with the actual name of your second map scene.
        selectedMapSceneName = "MapII";
        Debug.Log("Map selected: " + selectedMapSceneName);

        // Now that a map is also chosen, show the start button.
        ShowStartButton();
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
            SceneManager.LoadScene(selectedMapSceneName);
        }
        else
        {
            Debug.LogError("Cannot start game! Either character or map is not selected.");
        }
    }

    // This function remains the same.
    public void GoToMainMenu()
    {
        SceneManager.LoadScene(0);
    }
}
