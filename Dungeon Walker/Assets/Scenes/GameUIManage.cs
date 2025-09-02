using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    // ---- UPDATED: Now includes slots for the manager GameObjects ----
    [Header("Character Management")]
    [Tooltip("The visual prefab/GameObject for the Cat character.")]
    [SerializeField] private GameObject catCharacterObject;
    [Tooltip("The GameObject that holds the CatManager script.")]
    [SerializeField] private GameObject catManagerObject;

    [Tooltip("The visual prefab/GameObject for the Man character.")]
    [SerializeField] private GameObject manCharacterObject;
    [Tooltip("The GameObject that holds the ManManager script.")]
    [SerializeField] private GameObject manManagerObject;

    [Header("Game State")]
    public static bool isGamePaused = false;

    [Header("Death Panel UI")]
    [SerializeField] private GameObject deathPanel;

    [Header("Pause Panel UI")]
    [SerializeField] private GameObject pausePanel;
    // ... (other UI fields)

    // ---- UPDATED: Awake() now controls both characters and their managers ----
    void Awake()
    {
        // 1. Read the saved character choice from PlayerPrefs.
        string selectedCharacter = PlayerPrefs.GetString("SelectedCharacter", "Cat"); // Default to Cat

        // 2. Activate the correct character AND its manager, and disable the other pair.
        if (selectedCharacter == "Cat")
        {
            // Enable the Cat and its manager
            if (catCharacterObject != null) catCharacterObject.SetActive(true);
            if (catManagerObject != null) catManagerObject.SetActive(true);

            // Disable the Man and its manager
            if (manCharacterObject != null) manCharacterObject.SetActive(false);
            if (manManagerObject != null) manManagerObject.SetActive(false);

            Debug.Log("Activating Cat and CatManager.");
        }
        else if (selectedCharacter == "Man")
        {
            // Enable the Man and its manager
            if (manCharacterObject != null) manCharacterObject.SetActive(true);
            if (manManagerObject != null) manManagerObject.SetActive(true);

            // Disable the Cat and its manager
            if (catCharacterObject != null) catCharacterObject.SetActive(false);
            if (catManagerObject != null) catManagerObject.SetActive(false);

            Debug.Log("Activating Man and ManManager.");
        }
    }

    void Start()
    {
        // Hide UI panels
        if (deathPanel != null) deathPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        // Ensure game is running
        Time.timeScale = 1f;
        isGamePaused = false;
    }

    // ... (The rest of your script: Update, PauseGame, ResumeGame, etc. remains exactly the same)
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
        if (deathPanel != null) deathPanel.SetActive(true);
        Time.timeScale = 0f;
        isGamePaused = true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        isGamePaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        isGamePaused = false;
        SceneManager.LoadScene(0);
    }
}
