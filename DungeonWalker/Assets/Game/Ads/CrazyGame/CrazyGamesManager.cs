using UnityEngine;
using UnityEngine.SceneManagement; // <-- Add this line!
using CrazyGames;

public class CrazyGamesManager : MonoBehaviour
{
    public static CrazyGamesManager Instance { get; private set; }
    public bool IsSDKInitialized { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("Initializing CrazyGames SDK...");
        CrazySDK.Init(() =>
        {
            Debug.Log("CrazyGames SDK Initialized successfully!");
            IsSDKInitialized = true;
        });
    }

    // --- FINAL, INDEPENDENT Mid-Game Ad Function ---
    public void ShowMidGameAdAndRestart() // The name is more descriptive now
    {
        if (!IsSDKInitialized)
        {
            Debug.LogWarning("SDK not ready, restarting immediately.");
            RestartTheGame();
            return;
        }

        Debug.Log("Requesting a mid-game ad before restart...");

        CrazySDK.Ad.RequestAd(
            CrazyAdType.Midgame,
            () => { Debug.Log("Mid-game ad started."); },
            (error) => {
                Debug.LogError("Mid-game ad failed: " + error.message + ". Restarting anyway.");
                RestartTheGame(); // Restart even if the ad fails
            },
            () => {
                Debug.Log("Mid-game ad finished. Restarting now.");
                RestartTheGame(); // Restart after the ad is finished
            }
        );
    }

    // --- FINAL, CORRECTED Rewarded Ad Function (This one is already correct) ---
    public void ShowRewardedAd(System.Action onRewardGranted)
    {
        if (!IsSDKInitialized)
        {
            Debug.LogWarning("SDK not ready, cannot show rewarded ad.");
            return;
        }

        Debug.Log("Requesting a rewarded ad...");
        CrazySDK.Ad.RequestAd(
            CrazyAdType.Rewarded,
            () => { Debug.Log("Rewarded ad started."); },
            (error) => { Debug.LogError("Rewarded ad failed: " + error.message); },
            () => {
                Debug.Log("Rewarded ad finished. Granting reward.");
                onRewardGranted?.Invoke();
            }
        );
    }

    // --- NEW HELPER FUNCTION ---
    private void RestartTheGame()
    {
        // This manager now handles the restart itself.
        Time.timeScale = 1f; // Ensure time is running before loading.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void ShowMidGameAdAndPerformAction(System.Action actionAfterAd)
    {
        if (!IsSDKInitialized)
        {
            Debug.LogWarning("SDK not ready, performing action immediately.");
            actionAfterAd?.Invoke(); // Immediately run the action if the SDK isn't ready
            return;
        }

        Debug.Log("Requesting a mid-game ad before performing an action...");

        CrazySDK.Ad.RequestAd(
            CrazyAdType.Midgame,
            () => { Debug.Log("Mid-game ad started."); },
            (error) => {
                Debug.LogError("Mid-game ad failed: " + error.message + ". Performing action anyway.");
                actionAfterAd?.Invoke(); // Run the action even if the ad fails
            },
            () => {
                Debug.Log("Mid-game ad finished. Performing action now.");
                actionAfterAd?.Invoke(); // Run the action after the ad is finished
            }
        );
    }
}
