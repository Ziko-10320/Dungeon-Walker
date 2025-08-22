using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // ---- NEW ----: Required for the Image component

// The class name is updated to reflect its broader role.
public class GameUIManager : MonoBehaviour
{
    [Header("Game State")]
    public static bool isGamePaused = false; // ---- NEW ----: To track pause state

    [Header("Death Panel UI")]
    [Tooltip("The panel that appears when the player dies.")]
    [SerializeField] private GameObject deathPanel; // This was your 'restartPanel'

    // ---- NEW ----: All of these fields are for the new Pause Panel
    [Header("Pause Panel UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Image resumeButtonIcon;
    [SerializeField] private Image restartButtonIcon_Pause;
    [SerializeField] private Image menuButtonIcon_Pause;

    void Start()
    {
        // Hide both panels at the start
        if (deathPanel != null) deathPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false); // ---- NEW ----

        // Ensure the game is running
        Time.timeScale = 1f;
        isGamePaused = false;
    }

    // ---- NEW ----: This whole Update function is new
    void Update()
    {
        // Listen for the 'P' key to toggle the pause menu
        if (Input.GetKeyDown(KeyCode.P))
        {
            // Don't allow pausing if the death screen is already up
            if (deathPanel != null && deathPanel.activeInHierarchy)
            {
                return;
            }

            if (isGamePaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // ---- NEW ----: Method to handle pausing the game
    public void PauseGame()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f;
        isGamePaused = true;
    }

    // ---- NEW ----: Method to handle resuming the game
    public void ResumeGame()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
        isGamePaused = false;
    }

    // This is your existing method for the death screen. Let's rename it for clarity.
    public void ShowDeathScreen()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }
        Time.timeScale = 0f;
        isGamePaused = true; // The game is also paused on death
    }

    // This is your existing method. It works for both panels.
    public void RestartGame()
    {
        Time.timeScale = 1f;
        isGamePaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // This is your existing method. It works for both panels.
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        isGamePaused = false;
        SceneManager.LoadScene(0);
    }
}
