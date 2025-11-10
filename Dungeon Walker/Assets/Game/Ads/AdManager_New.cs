using UnityEngine;
using UnityEngine.Advertisements;
using System;
using System.Collections.Generic; // We need this for the list

public class AdManager_New : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener
{
    // --- Singleton Pattern ---
    // This makes it a single, persistent manager for the whole game.
    public static AdManager_New Instance { get; private set; }

    [Header("Ad Configuration")]
    [SerializeField] private string androidGameId;
    [SerializeField] private bool testMode = true;

    [Header("Ad Unit IDs")]
    [Tooltip("Add ALL the Rewarded Ad Unit IDs you use in your game here.")]
    [SerializeField] private List<string> rewardedAdUnitIds = new List<string>();

    // A simple flag to know if *any* rewarded ad is ready.
    // Your RewardedAdButton will handle checking the specific ad.
    public bool IsRewardedAdReady { get; private set; } = false;
    public static Action OnAnyRewardedAdLoaded;

    void Awake()
    {
        // --- Persistent Singleton Pattern ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // --- End of Pattern ---

        InitializeAds();
    }

    private void InitializeAds()
    {
        if (!Advertisement.isInitialized)
        {
            Advertisement.Initialize(androidGameId, testMode, this);
        }
        else
        {
            // If already initialized, just start loading our ads.
            LoadAllRewardedAds();
        }
    }

    // This is the new method that loads all ads from your list.
    private void LoadAllRewardedAds()
    {
        Debug.Log("[AdManager] Loading all specified rewarded ads...");
        foreach (var id in rewardedAdUnitIds)
        {
            Advertisement.Load(id, this);
        }
    }

    // This is the public method the button will call to load a new ad after one is used.
    public void LoadSpecificRewardedAd(string adUnitId)
    {
        Debug.Log($"[AdManager] Requesting to load a specific Rewarded Ad: {adUnitId}...");
        Advertisement.Load(adUnitId, this);
    }

    // --- SDK CALLBACKS ---

    public void OnInitializationComplete()
    {
        Debug.Log("[AdManager] SDK initialization complete.");
        LoadAllRewardedAds();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"[AdManager] SDK Initialization FAILED: {error} - {message}");
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        // If any of our rewarded ads load, we can consider ads to be "ready".
        if (rewardedAdUnitIds.Contains(placementId))
        {
            Debug.Log($"[AdManager] Rewarded Ad '{placementId}' successfully loaded.");
            IsRewardedAdReady = true;
            OnAnyRewardedAdLoaded?.Invoke();
        }
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"[AdManager] FAILED to load Ad '{placementId}': {error} - {message}");
        // Note: We don't set IsRewardedAdReady to false here, because another ad might still be ready.
        // The RewardedAdButton will handle its own state.
    }
}