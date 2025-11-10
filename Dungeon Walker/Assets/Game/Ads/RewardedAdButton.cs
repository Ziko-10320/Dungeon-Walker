using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Advertisements;
using UnityEngine.Events; // We still need this for the NEW reward type
using System; // We need this for the OLD Action

// THIS IS THE FINAL, HYBRID SCRIPT
public class RewardedAdButton : MonoBehaviour, IUnityAdsShowListener
{
    [Header("Ad Configuration")]
    [Tooltip("The Ad Unit ID for this specific button (e.g., Rewarded_Android_DoubleCoins).")]
    [SerializeField] private string adUnitId = "Rewarded_Android";

    [Header("Button Control")]
    [Tooltip("The actual UI Button that the player will click.")]
    [SerializeField] private Button buttonToControl;

    // --- THIS IS YOUR OLD, WORKING REWARD SYSTEM. IT IS BACK. ---
    // This is used for the main player revive.
    public static Action OnRewardGranted;

    [Header("OR: Use Unity Event for Other Rewards")]
    [Tooltip("FOR OTHER ADS LIKE 'DOUBLE COINS': Assign the reward function here.")]
    public UnityEvent OnRewardGranted_UnityEvent;


    void OnEnable()
    {
        if (buttonToControl == null) buttonToControl = GetComponent<Button>();
        if (buttonToControl == null)
        {
            Debug.LogError("RewardedAdButton: No button found or assigned!", this);
            return;
        }
        buttonToControl.onClick.RemoveAllListeners();
        buttonToControl.onClick.AddListener(ShowAd);
        AdManager_New.OnAnyRewardedAdLoaded += UpdateAdStatus;
        UpdateAdStatus();
    }
    void OnDisable()
    {
        // Unsubscribe from the event when the button is disabled to prevent errors.
        AdManager_New.OnAnyRewardedAdLoaded -= UpdateAdStatus;
    }
    public void UpdateAdStatus()
    {
        if (buttonToControl == null) return;
        AdManager_New adManager = FindObjectOfType<AdManager_New>();
        buttonToControl.interactable = (adManager != null && adManager.IsRewardedAdReady);
    }

    private void ShowAd()
    {
        Debug.Log($"[RewardedAdButton] Show Ad button clicked. Attempting to show '{adUnitId}'.");
        buttonToControl.interactable = false;
        Advertisement.Show(adUnitId, this);
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        if (placementId == adUnitId && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log($"[RewardedAdButton] Ad '{placementId}' completed successfully. Granting reward.");

            // --- THIS IS THE HYBRID REWARD LOGIC ---
            // If this button is for the main revive ad, use the old static Action.
            if (adUnitId == "Rewarded_Android")
            {
                Debug.Log("This is the Revive Ad. Invoking static OnRewardGranted Action.");
                OnRewardGranted?.Invoke();
            }
            else
            {
                // For any other ad (like Double Coins), use the new UnityEvent.
                Debug.Log("This is a different ad. Invoking the UnityEvent.");
                OnRewardGranted_UnityEvent?.Invoke();
            }
        }
        else
        {
            Debug.LogWarning($"[RewardedAdButton] Ad '{placementId}' was not completed. No reward given.");
        }
        Time.timeScale = 1f;
        AdManager_New.Instance?.LoadSpecificRewardedAd(adUnitId);
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"[RewardedAdButton] Failed to show Ad '{placementId}': {error} - {message}");
        Time.timeScale = 1f;
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        Debug.Log($"[RewardedAdButton] Ad '{placementId}' started.");
        Time.timeScale = 0f;
    }

    public void OnUnityAdsShowClick(string placementId) { }
}
