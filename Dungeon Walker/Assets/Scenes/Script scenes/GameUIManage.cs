using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    // ---- NEW: A toggle for easy testing ----
    [Header("Developer Settings")]
    [Tooltip("If true, the game will use the characters enabled in the Hierarchy instead of PlayerPrefs. FOR TESTING ONLY.")]
    [SerializeField] private bool developerMode = false;

    [Header("Character Management")]
    [SerializeField] private GameObject catCharacterObject;
    [SerializeField] private GameObject catManagerObject;
    [SerializeField] private GameObject manCharacterObject;
    [SerializeField] private GameObject manManagerObject;

    [Header("Camera Control")]
    [SerializeField] private CameraFollowMouseHorizontal cameraFollowScript;
    [SerializeField] private Transform catFollowTarget;
    [SerializeField] private Transform manFollowTarget;

    [Header("Game State")]
    public static bool isGamePaused = false;

    [Header("Death Panel UI")]
    [SerializeField] private GameObject deathPanel;

    [Header("Pause Panel UI")]
    [SerializeField] private GameObject pausePanel;

    void Awake()
    {
        // ---- NEW: Check for Developer Mode at the start ----
        // If this is true, we skip all the PlayerPrefs logic and use what's active in the scene.
        if (developerMode)
        {
            Debug.LogWarning("DEVELOPER MODE is ON. Using characters enabled in the scene hierarchy.");

            // We still need to tell the camera who to follow based on the active character.
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
            // Exit the function early so the PlayerPrefs code below doesn't run.
            return;
        }

        // ---- Your existing PlayerPrefs logic ----
        // This code will only run if 'developerMode' is false.
        string selectedCharacter = PlayerPrefs.GetString("SelectedCharacter", "Cat");

        if (selectedCharacter == "Cat")
        {
            // Enable Cat objects
            if (catCharacterObject != null) catCharacterObject.SetActive(true);
            if (catManagerObject != null) catManagerObject.SetActive(true);
            // Disable Man objects
            if (manCharacterObject != null) manCharacterObject.SetActive(false);
            if (manManagerObject != null) manManagerObject.SetActive(false);
            // Set camera target
            if (cameraFollowScript != null && catFollowTarget != null)
            {
                cameraFollowScript.SetTarget(catFollowTarget);
            }
            Debug.Log("Activating Cat, CatManager, and setting camera target.");
        }
        else if (selectedCharacter == "Man")
        {
            // Enable Man objects
            if (manCharacterObject != null) manCharacterObject.SetActive(true);
            if (manManagerObject != null) manManagerObject.SetActive(true);
            // Disable Cat objects
            if (catCharacterObject != null) catCharacterObject.SetActive(false);
            if (catManagerObject != null) catManagerObject.SetActive(false);
            // Set camera target
            if (cameraFollowScript != null && manFollowTarget != null)
            {
                cameraFollowScript.SetTarget(manFollowTarget);
            }
            Debug.Log("Activating Man, ManManager, and setting camera target.");
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
