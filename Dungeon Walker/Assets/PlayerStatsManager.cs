using UnityEngine;
using TMPro; // For TextMeshPro UI elements

public class PlayerStatsManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static PlayerStatsManager Instance { get; private set; }

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

    // This method will be called when the game restarts to clear old stats
    public void ResetStats()
    {
        coinsGathered = 0;
        enemiesKilled = 0;
        finalScore = 0;
    }
}
