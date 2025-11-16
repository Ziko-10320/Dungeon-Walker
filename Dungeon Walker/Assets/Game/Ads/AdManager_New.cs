// In AdManager_New.cs

using UnityEngine;
using UnityEngine.Advertisements;
using System;
using System.Collections.Generic;

public class AdManager_New : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener
{
    public static AdManager_New Instance { get; private set; }

    [Header("Ad Configuration")]
    [SerializeField] private string androidGameId;
    [SerializeField] private bool testMode = false;

    [Header("Ad Unit IDs")]
    [SerializeField] private List<string> rewardedAdUnitIds = new List<string>();

    // --- THIS IS THE NEW, SMARTER SYSTEM ---
    // A Dictionary to track the ready status of EACH ad unit.
    private Dictionary<string, bool> adStatus = new Dictionary<string, bool>();
    // --- END OF NEW SYSTEM ---

    public static Action OnAnyRewardedAdLoaded;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeAds();
    }

    private void InitializeAds()
    {
        // Initialize the status dictionary
        foreach (var id in rewardedAdUnitIds)
        {
            adStatus[id] = false;
        }

        if (!Advertisement.isInitialized)
        {
            Advertisement.Initialize(androidGameId, testMode, this);
        }
        else
        {
            LoadAllRewardedAds();
        }
    }

    private void LoadAllRewardedAds()
    {
        foreach (var id in rewardedAdUnitIds)
        {
            adStatus[id] = false; // Mark as not ready while loading
            Advertisement.Load(id, this);
        }
    }

    public void LoadSpecificRewardedAd(string adUnitId)
    {
        if (rewardedAdUnitIds.Contains(adUnitId))
        {
            adStatus[adUnitId] = false; // Mark as not ready
            Advertisement.Load(adUnitId, this);
        }
    }

    // --- THIS IS THE NEW PUBLIC METHOD THE BUTTON WILL USE ---
    public bool IsAdReady(string adUnitId)
    {
        return adStatus.ContainsKey(adUnitId) && adStatus[adUnitId];
    }
    // --- END OF NEW METHOD ---

    public void OnInitializationComplete()
    {
        LoadAllRewardedAds();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message) { /* ... */ }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        if (adStatus.ContainsKey(placementId))
        {
            Debug.Log($"[AdManager] Ad '{placementId}' is now ready.");
            adStatus[placementId] = true; // Mark this specific ad as ready
            OnAnyRewardedAdLoaded?.Invoke();
        }
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        if (adStatus.ContainsKey(placementId))
        {
            Debug.LogError($"[AdManager] FAILED to load Ad '{placementId}'.");
            adStatus[placementId] = false; // Mark this specific ad as not ready
        }
    }
}
