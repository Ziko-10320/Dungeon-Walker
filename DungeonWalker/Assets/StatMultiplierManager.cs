// StatMultiplierManager.cs

using UnityEngine;

public class StatMultiplierManager : MonoBehaviour
{
    public static StatMultiplierManager Instance { get; private set; }

    // --- The Multipliers ---
    // These are public so other scripts can read them.
    public float FleaMultiplier { get; private set; } = 1.0f;
    public float SprayerMultiplier { get; private set; } = 1.0f;
    public float InkMultiplier { get; private set; } = 1.0f;    
    public float FlyMultiplier { get; private set; } = 1.0f;
    // Add other enemy multipliers here as you create them (e.g., InkMultiplier, FlyMultiplier)

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// The WaveManager will call this at the start of a run to set the stats for this session.
    /// </summary>
    public void SetMultipliers(float fleaMultiplier, float sprayerMultiplier, float inkMultiplier, float flyMultiplier)
    {
        this.FleaMultiplier = fleaMultiplier;
        this.SprayerMultiplier = sprayerMultiplier;
        this.InkMultiplier = inkMultiplier;
        this.FlyMultiplier = flyMultiplier;
        Debug.Log($"[StatMultipliers] Set for this run -> Flea: x{fleaMultiplier}, Sprayer: x{sprayerMultiplier}");
    }
}
