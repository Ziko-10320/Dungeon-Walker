// In RewardedAdButton.cs

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Advertisements;
using System;

[RequireComponent(typeof(Button))]
public class RewardedAdButton : MonoBehaviour, IUnityAdsShowListener
{
    private Button watchAdButton;
    private const string ANDROID_REWARDED_ID = "Rewarded_Android";

    public static Action OnRewardGranted; // Event to grant the reward

    void Awake()
    {
        watchAdButton = GetComponent<Button>();
        watchAdButton.onClick.AddListener(ShowAd); // Add listener via code
    }

    void OnEnable()
    {
        // Find the current scene's AdManager and check readiness
        AdManager_New adManager = FindObjectOfType<AdManager_New>();
        if (adManager != null && adManager.IsRewardedAdReady)
        {
            watchAdButton.interactable = true;
        }
        else
        {
            watchAdButton.interactable = false;
        }
    }

    private void ShowAd()
    {
        Debug.Log("[RewardedAdButton] Show Ad button clicked.");
        watchAdButton.interactable = false; // Prevent spamming
        Advertisement.Show(ANDROID_REWARDED_ID, this);
    }

    // --- SHOW LISTENER IMPLEMENTATION ---

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        if (placementId == ANDROID_REWARDED_ID && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log("[RewardedAdButton] Ad completed. Granting reward.");
            OnRewardGranted?.Invoke();
        }
        Time.timeScale = 1f;
        // Find the scene's ad manager and tell it to load the next ad.
        AdManager_New adManager = FindObjectOfType<AdManager_New>();
        if (adManager != null)
        {
            adManager.LoadRewardedAd();
        }
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message) { Time.timeScale = 1f; }
    public void OnUnityAdsShowStart(string placementId) { Time.timeScale = 0f; } // Pause game
    public void OnUnityAdsShowClick(string placementId) { }
}
