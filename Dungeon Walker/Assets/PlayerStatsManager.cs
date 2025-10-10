using UnityEngine;
using TMPro; // For TextMeshPro UI elements

public class PlayerStatsManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static PlayerStatsManager Instance { get; private set; }
    [HideInInspector] public bool newHighScoreAchieved = false;
    [HideInInspector] public bool newMostKillsAchieved = false;
    private const string HighScoreKey = "HighScore";
    private const string MostKillsKey = "MostKills";
    // --- Stats to Track ---
    public int coinsGathered = 0;
    public int enemiesKilled = 0;
    public int finalScore = 0;

    void Awake()
    {
        // Implement the singleton pattern to ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // This is crucial! It keeps the stats when the scene reloads.
        }
        else
        {
            Destroy(gameObject); // Destroy any duplicate instances
        }
    }

    // --- Public Methods to Update Stats ---

    public void AddCoins(int amount)
    {
        coinsGathered += amount;
    }

    public void AddKill()
    {
        enemiesKilled++;
    }

    public void SetFinalScore(int score)
    {
        finalScore = score;
    }
    public void CheckAndSaveHighScores()
    {
        // Load the old records from the device
        int savedHighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        int savedMostKills = PlayerPrefs.GetInt(MostKillsKey, 0);

        // Reset the flags before checking
        newHighScoreAchieved = false;
        newMostKillsAchieved = false;

        // Check if the current score is a new high score
        if (finalScore > savedHighScore)
        {
            newHighScoreAchieved = true;
            PlayerPrefs.SetInt(HighScoreKey, finalScore);
        }

        // Check if the current kills are a new record
        if (enemiesKilled > savedMostKills)
        {
            newMostKillsAchieved = true;
            PlayerPrefs.SetInt(MostKillsKey, enemiesKilled);
        }

        // If we set any new record, save the data permanently
        if (newHighScoreAchieved || newMostKillsAchieved)
        {
            PlayerPrefs.Save();
        }
    }
    // This method will be called when the game restarts to clear old stats
    public void ResetStats()
    {
        coinsGathered = 0;
        enemiesKilled = 0;
        finalScore = 0;
        // --- ADD THIS ---
        newHighScoreAchieved = false;
        newMostKillsAchieved = false;
        // --- END ADDITION ---
    }
}
