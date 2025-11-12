// In RewardedAdButton.cs

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Advertisements;
using UnityEngine.Events;

// THIS IS THE FINAL, CORRECT, SIMPLIFIED SCRIPT
public class RewardedAdButton : MonoBehaviour, IUnityAdsShowListener
{
    [Header("Ad Configuration")]
    [Tooltip("The Ad Unit ID for this specific button (e.g., Rewarded_Android_DoubleCoins).")]
    [SerializeField] private string adUnitId; // Default is now empty

    [Header("Button Control")]
    [Tooltip("The actual UI Button that the player will click.")]
    [SerializeField] private Button buttonToControl;

    [Header("Reward")]
    [Tooltip("Assign the reward function here (e.g., GameUIManager.RewardDoubleCoins).")]
    public UnityEvent OnRewardGranted; // We ONLY use this.

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
        AdManager_New.OnAnyRewardedAdLoaded -= UpdateAdStatus;
    }

    public void UpdateAdStatus()
    {
        if (buttonToControl == null) return;

        if (AdManager_New.Instance != null)
        {
            buttonToControl.interactable = AdManager_New.Instance.IsAdReady(adUnitId);
        }
        else
        {
            buttonToControl.interactable = false;
        }
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
            Debug.Log($"[RewardedAdButton] Ad '{placementId}' completed. Invoking the UnityEvent reward.");
            OnRewardGranted?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[RewardedAdButton] Ad '{placementId}' was not completed. No reward given.");
        }

        Time.timeScale = 1f;
        AdManager_New.Instance?.LoadSpecificRewardedAd(adUnitId);
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message) { Time.timeScale = 1f; }
    public void OnUnityAdsShowStart(string placementId) { Time.timeScale = 0f; }
    public void OnUnityAdsShowClick(string placementId) { }
}
