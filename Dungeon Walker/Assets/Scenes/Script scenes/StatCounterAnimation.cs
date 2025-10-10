using UnityEngine;
using System.Collections;
using TMPro; // For TextMeshPro

public class StatCounterAnimation : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI coinsText;
    [Header("High Score UI")]
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI mostKillsText;
    [Header("Animation Settings")]
    [Tooltip("The total duration for each number to count up, in seconds.")]
    [SerializeField] private float countDuration = 0.75f;
    [Tooltip("The short delay between each stat appearing (e.g., after score finishes, wait a bit before kills start).")]
    [SerializeField] private float delayBetweenStats = 0.2f;

    [Header("Sound Effects (Optional)")]
    [SerializeField] private AudioSource tickSound; // An audio source that can play the counting sound
    [SerializeField] private AudioClip finishSound; // A sound to play when a counter finishes

    // Private variables to manage the animation state
    private Coroutine animationCoroutine;
    private bool isAnimating = false;

    void Update()
    {
        // Check for skip input only while the animation is running
        if (isAnimating && Input.GetMouseButtonDown(0)) // 0 is the left mouse button
        {
            SkipAnimation();
        }
    }

    /// <summary>
    /// Public method to start the entire stat animation sequence.
    /// </summary>
    public void StartAnimation(int finalScore, int finalKills, int finalCoins)
    {
        // Stop any previous animations and reset the state
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        // Start the new animation sequence
        animationCoroutine = StartCoroutine(AnimateAllStats(finalScore, finalKills, finalCoins));
    }

    /// <summary>
    /// The main coroutine that controls the sequence: Score -> Kills -> Coins.
    /// </summary>
    private IEnumerator AnimateAllStats(int finalScore, int finalKills, int finalCoins)
    {
        isAnimating = true;

        // --- 1. Handle Score Display ---
        scoreText.gameObject.SetActive(true);
        highScoreText.gameObject.SetActive(true);
        // Check if it's a new high score
        if (PlayerStatsManager.Instance.newHighScoreAchieved)
        {
            highScoreText.text = "New Record!";
            // Animate the score because it's a new record
            yield return StartCoroutine(CountUp(scoreText, "Score: ", finalScore));
            PlayFinishSound();
        }
        else
        {
            // Not a new record, so just display the numbers instantly
            scoreText.text = "Score: " + finalScore;
            highScoreText.text = "Best: " + PlayerPrefs.GetInt("HighScore", 0);
        }
        yield return new WaitForSeconds(delayBetweenStats);

        // --- 2. Handle Kills Display ---
        killsText.gameObject.SetActive(true);
        mostKillsText.gameObject.SetActive(true);
        // Check if it's a new most kills record
        if (PlayerStatsManager.Instance.newMostKillsAchieved)
        {
            mostKillsText.text = "New Record!";
            // Animate the kills because it's a new record
            yield return StartCoroutine(CountUp(killsText, "Kills: ", finalKills));
            PlayFinishSound();
        }
        else
        {
            // Not a new record, so just display the numbers instantly
            killsText.text = "Kills: " + finalKills;
            mostKillsText.text = "Best: " + PlayerPrefs.GetInt("MostKills", 0);
        }
        yield return new WaitForSeconds(delayBetweenStats);

        // --- 3. Animate Coins (always animates) ---
        coinsText.gameObject.SetActive(true);
        coinsText.text = "Coins Gathered: 0";
        yield return StartCoroutine(CountUp(coinsText, "Coins Gathered: ", finalCoins));
        PlayFinishSound();

        isAnimating = false;
        animationCoroutine = null;
    }

    /// <summary>
    /// A reusable coroutine that animates a single TextMeshPro element from 0 to a target value.
    /// </summary>
    private IEnumerator CountUp(TextMeshProUGUI textElement, string prefix, int targetValue)
    {
        if (tickSound != null) tickSound.Play();

        float timer = 0f;
        int currentValue = 0;

        while (timer < countDuration)
        {
            timer += Time.unscaledDeltaTime; // Use unscaled time as the game is paused
            // Calculate the current value based on the animation progress
            currentValue = (int)Mathf.Lerp(0, targetValue, timer / countDuration);
            textElement.text = prefix + currentValue;
            yield return null; // Wait for the next frame
        }

        // Ensure the final value is exact
        textElement.text = prefix + targetValue;
        if (tickSound != null) tickSound.Stop();
    }

    /// <summary>
    /// Skips the animation and immediately displays the final values.
    /// </summary>
    public void SkipAnimation()
    {
        if (!isAnimating) return;

        Debug.Log("Skipping stat animation!");

        // Stop all running coroutines on this script
        StopAllCoroutines();
        if (tickSound != null) tickSound.Stop();

        // Get the final stats directly from the manager
        int score = PlayerStatsManager.Instance.finalScore;
        int kills = PlayerStatsManager.Instance.enemiesKilled;
        int coins = PlayerStatsManager.Instance.coinsGathered;

        // Set the text to the final values instantly
        scoreText.gameObject.SetActive(true);
        scoreText.text = "Final Score: " + score;

        killsText.gameObject.SetActive(true);
        killsText.text = "Enemies Killed: " + kills;

        coinsText.gameObject.SetActive(true);
        coinsText.text = "Coins Gathered: " + coins;

        // Reset animation state
        isAnimating = false;
        animationCoroutine = null;
    }

    /// <summary>
    /// Resets the text elements to be hidden, ready for the next time the panel is shown.
    /// </summary>
    public void ResetUI()
    {
        scoreText.gameObject.SetActive(false);
        killsText.gameObject.SetActive(false);
        coinsText.gameObject.SetActive(false);
        // --- ADD THIS ---
        if (highScoreText != null) highScoreText.gameObject.SetActive(false);
        if (mostKillsText != null) mostKillsText.gameObject.SetActive(false);
        // --- END ADDITION ---
    }

    private void PlayFinishSound()
    {
        if (finishSound != null && tickSound != null)
        {
            // Play the finish sound as a one-shot so it doesn't interrupt the ticking sound
            tickSound.PlayOneShot(finishSound);
        }
    }
}
