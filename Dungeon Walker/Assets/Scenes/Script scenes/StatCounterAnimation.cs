using UnityEngine;
using System.Collections;
using TMPro;

public class StatCounterAnimation : MonoBehaviour
{
    // All your variable declarations are correct.
    [Header("Session Stat UI")]
    [SerializeField] private TextMeshProUGUI scoreLabelText;
    [SerializeField] private TextMeshProUGUI scoreValueText;
    [SerializeField] private TextMeshProUGUI killsLabelText;
    [SerializeField] private TextMeshProUGUI killsValueText;
    [SerializeField] private TextMeshProUGUI coinsLabelText;
    [SerializeField] private TextMeshProUGUI coinsValueText;

    [Header("High Score UI")]
    [SerializeField] private TextMeshProUGUI highScoreLabelText; // e.g., "High Score:" or "New Record!"
    [SerializeField] private TextMeshProUGUI highScoreValueText; // The number for the high score
    [SerializeField] private TextMeshProUGUI mostKillsLabelText; // e.g., "Most Kills:" or "New Record!"
    [SerializeField] private TextMeshProUGUI mostKillsValueText; // The number for most kills

    // ... (Your Animation Settings and Sound Effects variables are unchanged)
    [Header("Animation Settings")]
    [SerializeField] private float countDuration = 0.75f;
    [SerializeField] private float delayBetweenStats = 0.2f;
    [Header("Sound Effects (Optional)")]
    [SerializeField] private AudioSource tickSound;
    [SerializeField] private AudioClip finishSound;


    private Coroutine animationCoroutine;
    private bool isAnimating = false;

    // StartAnimation and Update methods are unchanged.
    public void StartAnimation(int finalScore, int finalKills, int finalCoins)
    {
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateAllStats(finalScore, finalKills, finalCoins));
    }

    void Update()
    {
        if (isAnimating && Input.GetMouseButtonDown(0)) SkipAnimation();
    }

    // --- THIS IS THE NEW, SIMPLIFIED CORE LOGIC ---
    private IEnumerator AnimateAllStats(int finalScore, int finalKills, int finalCoins)
    {
        isAnimating = true;
        ResetUI(); // Start with a clean slate, everything is hidden.

        // --- 1. Handle Score ---
        // Make score labels and values visible
        scoreLabelText.gameObject.SetActive(true);
        scoreValueText.gameObject.SetActive(true);
        highScoreLabelText.gameObject.SetActive(true);
        highScoreValueText.gameObject.SetActive(true);

        // Set the text for the labels
        scoreLabelText.text = "Score:";
        if (PlayerStatsManager.Instance.newHighScoreAchieved)
        {
            highScoreLabelText.text = "New Record!";
            highScoreValueText.text = finalScore.ToString();
        }
        else
        {
            highScoreLabelText.text = "High Score:";
            highScoreValueText.text = PlayerPrefs.GetInt("HighScore", 0).ToString();
        }

        // Animate the score value
        yield return StartCoroutine(CountUp(scoreValueText, finalScore));
        PlayFinishSound();
        yield return new WaitForSeconds(delayBetweenStats);

        // --- 2. Handle Kills ---
        // Now, make the kills labels and values visible
        killsLabelText.gameObject.SetActive(true);
        killsValueText.gameObject.SetActive(true);
        mostKillsLabelText.gameObject.SetActive(true);
        mostKillsValueText.gameObject.SetActive(true);

        // Set the text for the labels
        killsLabelText.text = "Enemies Killed:"; // <--- UPDATED TEXT
        if (PlayerStatsManager.Instance.newMostKillsAchieved)
        {
            mostKillsLabelText.text = "New Record!";
            mostKillsValueText.text = finalKills.ToString();
        }
        else
        {
            mostKillsLabelText.text = "Most Kills:";
            mostKillsValueText.text = PlayerPrefs.GetInt("MostKills", 0).ToString();
        }

        // Animate the kills value
        yield return StartCoroutine(CountUp(killsValueText, finalKills));
        PlayFinishSound();
        yield return new WaitForSeconds(delayBetweenStats);

        // --- 3. Handle Coins ---
        // Finally, make the coins labels and values visible
        coinsLabelText.gameObject.SetActive(true);
        coinsValueText.gameObject.SetActive(true);

        // Set the text for the label
        coinsLabelText.text = "Coins Gathered:"; // <--- UPDATED TEXT

        // Animate the coins value
        yield return StartCoroutine(CountUp(coinsValueText, finalCoins));
        PlayFinishSound();

        isAnimating = false;
        animationCoroutine = null;
    }

    // The CountUp method is unchanged and correct.
    private IEnumerator CountUp(TextMeshProUGUI valueText, int targetValue)
    {
        valueText.text = "0"; // Start from 0
        if (tickSound != null) tickSound.Play();
        float timer = 0f;
        while (timer < countDuration)
        {
            timer += Time.unscaledDeltaTime;
            int currentValue = (int)Mathf.Lerp(0, targetValue, timer / countDuration);
            valueText.text = currentValue.ToString();
            yield return null;
        }
        valueText.text = targetValue.ToString();
        if (tickSound != null) tickSound.Stop();
    }

    // The SkipAnimation method is simplified to just call the final state logic.
    public void SkipAnimation()
    {
        if (!isAnimating) return;
        StopAllCoroutines();
        if (tickSound != null) tickSound.Stop();

        // This instantly shows the final result
        AnimateAllStats(
            PlayerStatsManager.Instance.finalScore,
            PlayerStatsManager.Instance.enemiesKilled,
            PlayerStatsManager.Instance.coinsGathered
        );
        // We call the coroutine but since it has no yields in the instant-display path, it finishes in one frame.
        // To be safe, let's just set the final values directly.

        StopAllCoroutines(); // Stop it again just in case

        scoreValueText.text = PlayerStatsManager.Instance.finalScore.ToString();
        killsValueText.text = PlayerStatsManager.Instance.enemiesKilled.ToString();
        coinsValueText.text = PlayerStatsManager.Instance.coinsGathered.ToString();

        isAnimating = false;
    }

    // A helper method to turn everything on or off.
    private void SetAllActive(bool isActive)
    {
        scoreLabelText.gameObject.SetActive(isActive);
        scoreValueText.gameObject.SetActive(isActive);
        killsLabelText.gameObject.SetActive(isActive);
        killsValueText.gameObject.SetActive(isActive);
        coinsLabelText.gameObject.SetActive(isActive);
        coinsValueText.gameObject.SetActive(isActive);
        highScoreLabelText.gameObject.SetActive(isActive);
        highScoreValueText.gameObject.SetActive(isActive);
        mostKillsLabelText.gameObject.SetActive(isActive);
        mostKillsValueText.gameObject.SetActive(isActive);
    }

    public void ResetUI()
    {
        SetAllActive(false);
    }

    // The PlayFinishSound method is unchanged.
    private void PlayFinishSound()
    {
        if (finishSound != null && tickSound != null)
        {
            tickSound.PlayOneShot(finishSound);
        }
    }
}
