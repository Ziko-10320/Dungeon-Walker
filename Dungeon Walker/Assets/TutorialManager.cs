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
    [Header("Enemy Encounters")]
    [Tooltip("A list of all enemy encounters in the tutorial, in order.")]
    public List<EnemyEncounter> encounters;

    // --- Private Variables ---
    private const string TutorialCompletedKey = "TutorialCompleted";
    private bool tutorialFinished = false;
    private int currentEncounterIndex = 0; // To track which encounter we are on
    private GameObject spawnedEnemy;
    [Header("Tutorial Goal")]
    public int scoreToComplete = 1;
    [Header("UI Pop-In Animation")]
    [Tooltip("The list of UI elements to animate in sequence.")]
    public List<GameObject> animatedElements;
    [Tooltip("The time to wait between each element popping in.")]
    public float delayBetweenAnimations = 0.5f;
    [System.Serializable]
    public class EnemyEncounter
    {
        public string encounterName; // For you to identify it in the Inspector
        public Collider2D triggerZone;
        public GameObject barrierWalls;
        public GameObject enemyPrefab;
        public Transform enemySpawnPoint;
        [Tooltip("If true, the spawned enemy will deal 0 damage.")]
        public bool makeEnemyDealZeroDamage = false;
    }
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
        foreach (var encounter in encounters)
        {
            if (encounter.barrierWalls != null)
            {
                encounter.barrierWalls.SetActive(false);
            }
        }
        // Hide each animated element and the main menu button initially.
        foreach (var element in animatedElements)
        {
            element.SetActive(false);
        }
        mainMenuButton.gameObject.SetActive(false);
    }
    void Update()
    {
        // Check if there are still encounters left to complete.
        if (currentEncounterIndex < encounters.Count)
        {
            // Get the current encounter we're waiting for.
            EnemyEncounter currentEncounter = encounters[currentEncounterIndex];

            if (currentEncounter.triggerZone != null && existingCharacterObject != null)
            {
                Collider2D playerCollider = existingCharacterObject.GetComponent<Collider2D>();
                if (playerCollider != null && currentEncounter.triggerZone.IsTouching(playerCollider))
                {
                    // Start the current encounter.
                    StartEncounter(currentEncounter);
                }
            }
        }
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
    private void StartEncounter(EnemyEncounter encounter)
    {
        Debug.Log($"Starting encounter: {encounter.encounterName}");

        if (encounter.barrierWalls != null) encounter.barrierWalls.SetActive(true);

        if (encounter.enemyPrefab != null && encounter.enemySpawnPoint != null)
        {
            spawnedEnemy = Instantiate(encounter.enemyPrefab, encounter.enemySpawnPoint.position, encounter.enemySpawnPoint.rotation);

            // ----> THIS IS THE NEW, SMARTER CONNECTION LOGIC <----
            // Try to get FleaHealth script
            FleaHealth fleaHealth = spawnedEnemy.GetComponent<FleaHealth>();
            if (fleaHealth != null)
            {
                fleaHealth.SetTutorialManager(this); // Tell it to report back on death

                if (encounter.makeEnemyDealZeroDamage)
                {
                    // Get the Flea's attack script...
                    FleaChargeAttack fleaAttack = spawnedEnemy.GetComponent<FleaChargeAttack>();
                    if (fleaAttack != null)
                    {
                        // ...and call the new function we just made!
                        fleaAttack.SetTutorialMode();
                    }
                }
            }

            // Try to get FlyHealth script
            FlyHealth flyHealth = spawnedEnemy.GetComponent<FlyHealth>();
            if (flyHealth != null)
            {
                flyHealth.SetTutorialManager(this, encounter.makeEnemyDealZeroDamage);
                if (encounter.makeEnemyDealZeroDamage)
                {
                    FlyAttack flyAttack = spawnedEnemy.GetComponent<FlyAttack>();
                    if (flyAttack != null)
                    {
                        flyAttack.SetTutorialMode();
                    }
                }
            }
        }

        if (encounter.triggerZone != null) encounter.triggerZone.gameObject.SetActive(false);
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
    public void OnEnemyDefeated()
    {
        Debug.Log($"Enemy defeated for encounter: {encounters[currentEncounterIndex].encounterName}");

        // Get the current encounter and disable its walls.
        EnemyEncounter currentEncounter = encounters[currentEncounterIndex];
        if (currentEncounter.barrierWalls != null)
        {
            currentEncounter.barrierWalls.SetActive(false);
        }
        spawnedEnemy = null;

        // Move to the next encounter!
        currentEncounterIndex++;
        Debug.Log("Ready for next encounter.");
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
