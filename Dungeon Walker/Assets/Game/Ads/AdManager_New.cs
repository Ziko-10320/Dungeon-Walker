using UnityEngine;
using UnityEngine.Advertisements;
using System;

public class AdManager_New : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener
{
    // This is a scene-specific instance.
    public static AdManager_New Instance { get; private set; }

    [Header("Unity Ads - Android")]
    [SerializeField] private string androidGameId;
    private const string ANDROID_REWARDED_ID = "Rewarded_Android";

    [Header("Settings")]
    [SerializeField] private bool testMode = true;

    // We use a static variable for the ad state so it can persist briefly between scenes.
    private static bool isAdReady = false;
    public bool IsRewardedAdReady => isAdReady;

    void Awake()
    {
        // If another instance exists in this scene, destroy this one.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("[AdManager_New] Scene-specific instance created.");

        // Initialize ads every time a scene with this manager loads.
        InitializeAds();
    }

    private void InitializeAds()
    {
        // The SDK itself is static and persists. We just need to re-initialize our listeners.
        if (!Advertisement.isInitialized)
        {
            Debug.Log("[AdManager_New] SDK not initialized. Initializing now...");
            Advertisement.Initialize(androidGameId, testMode, this);
        }
        else
        {
            Debug.Log("[AdManager_New] SDK was already initialized. Just loading an ad.");
            LoadRewardedAd();
        }
    }

    public void LoadRewardedAd()
    {
        Debug.Log("[AdManager_New] Requesting to load a Rewarded Ad...");
        isAdReady = false;
        Advertisement.Load(ANDROID_REWARDED_ID, this);
    }

    // --- SDK CALLBACKS ---

    public void OnInitializationComplete()
    {
        Debug.Log("[AdManager_New] SDK initialization complete.");
        LoadRewardedAd();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"[AdManager_New] SDK Initialization FAILED: {error} - {message}");
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        if (placementId == ANDROID_REWARDED_ID)
        {
            Debug.Log("[AdManager_New] Rewarded Ad successfully loaded.");
            isAdReady = true;
        }
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"[AdManager_New] FAILED to load Ad {placementId}: {error} - {message}");
        isAdReady = false;
    }
}
