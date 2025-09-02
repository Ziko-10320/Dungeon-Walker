using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Required for accessing Button components

public class CharacterSelectionManager : MonoBehaviour
{
    // ---- NEW ----
    // Add a reference to your "Start Game" button.
    [Header("UI Elements")]
    [Tooltip("The button that starts the game. It will be hidden until a character is chosen.")]
    [SerializeField] private GameObject startGameButton;

    // The Start() function is called once when the scene loads.
    void Start()
    {
        // ---- NEW ----
        // Ensure the "Start Game" button is hidden at the very beginning.
        if (startGameButton != null)
        {
            startGameButton.SetActive(false);
        }
    }

    // This function is called by your "Select Cat" button.
    public void SelectCat()
    {
        PlayerPrefs.SetString("SelectedCharacter", "Cat");
        PlayerPrefs.Save();
        Debug.Log("Cat selected");

        // ---- NEW ----
        // After a choice is made, show the "Start Game" button.
        ShowStartButton();
    }

    // This function is called by your "Select Man" button.
    public void SelectMan()
    {
        PlayerPrefs.SetString("SelectedCharacter", "Man");
        PlayerPrefs.Save();
        Debug.Log("Man selected");

        // ---- NEW ----
        // After a choice is made, show the "Start Game" button.
        ShowStartButton();
    }

    // ---- NEW ----
    // A helper function to enable the start button.
    private void ShowStartButton()
    {
        if (startGameButton != null && !startGameButton.activeSelf)
        {
            startGameButton.SetActive(true);
            Debug.Log("Start button is now visible.");
        }
    }

    // This function is called by the "Start Game" button.
    public void StartGame()
    {
        // This part remains the same. It loads your game scene.
        // Make sure the scene name or index is correct!
        SceneManager.LoadScene("SampleScene"); // Or SceneManager.LoadScene(2);
    }
}
