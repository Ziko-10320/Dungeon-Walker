using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The class name is the same, no changes needed here.
public class GameUIManager : MonoBehaviour
{
    [Header("Character Management")]
    [Tooltip("The visual prefab/GameObject for the Cat character.")]
    [SerializeField] private GameObject catCharacterObject;
    [Tooltip("The GameObject that holds the CatManager script.")]
    [SerializeField] private GameObject catManagerObject;

    [Tooltip("The visual prefab/GameObject for the Man character.")]
    [SerializeField] private GameObject manCharacterObject;
    [Tooltip("The GameObject that holds the ManManager script.")]
    [SerializeField] private GameObject manManagerObject;

    // ---- NEW: Fields for Custom Camera Control ----
    [Header("Camera Control")]
    [Tooltip("The CameraFollowMouseHorizontal script attached to your main camera.")]
    [SerializeField] private CameraFollowMouseHorizontal cameraFollowScript; // Reference to YOUR camera script
    [Tooltip("The Transform the camera should follow for the Cat (CMM).")]
    [SerializeField] private Transform catFollowTarget;
    [Tooltip("The Transform the camera should follow for the Man (CMMX).")]
    [SerializeField] private Transform manFollowTarget;

    [Header("Game State")]
    public static bool isGamePaused = false;

    [Header("Death Panel UI")]
    [SerializeField] private GameObject deathPanel;

    [Header("Pause Panel UI")]
    [SerializeField] private GameObject pausePanel;
    // ... (other UI fields)

    // ---- UPDATED: Awake() now controls characters, managers, AND the camera target ----
    void Awake()
    {
        // 1. Read the saved character choice from PlayerPrefs.
        string selectedCharacter = PlayerPrefs.GetString("SelectedCharacter", "Cat");

        // 2. Activate the correct character, manager, and set the camera target.
        if (selectedCharacter == "Cat")
        {
            // Enable the Cat and its manager
            if (catCharacterObject != null) catCharacterObject.SetActive(true);
            if (catManagerObject != null) catManagerObject.SetActive(true);

            // Disable the Man and its manager
            if (manCharacterObject != null) manCharacterObject.SetActive(false);
            if (manManagerObject != null) manManagerObject.SetActive(false);

            // ---- NEW: Set camera to follow the Cat's target ----
            if (cameraFollowScript != null && catFollowTarget != null)
            {
                cameraFollowScript.SetTarget(catFollowTarget);
            }

            Debug.Log("Activating Cat, CatManager, and setting camera target.");
        }
        else if (selectedCharacter == "Man")
        {
            // Enable the Man and its manager
            if (manCharacterObject != null) manCharacterObject.SetActive(true);
            if (manManagerObject != null) manManagerObject.SetActive(true);

            // Disable the Cat and its manager
            if (catCharacterObject != null) catCharacterObject.SetActive(false);
            if (catManagerObject != null) catManagerObject.SetActive(false);

            // ---- NEW: Set camera to follow the Man's target ----
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

    // The rest of your script (Update, PauseGame, ResumeGame, etc.) remains exactly the same.
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
