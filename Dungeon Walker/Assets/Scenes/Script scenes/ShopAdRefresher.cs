// In the new script: ShopAdRefresher.cs

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ShopAdRefresher : MonoBehaviour
{
    [Header("Button To Refresh")]
    [Tooltip("Drag the 'Free Coins' button here.")]
    [SerializeField] private Button adButtonToRefresh;

    // This is the coroutine that will run in the background.
    private Coroutine refreshLoop;

    // When the Shop Panel is enabled...
    void OnEnable()
    {
        Debug.Log("[ShopAdRefresher] Shop panel is open. Starting ad refresh loop.");
        // ...start the watchdog loop.
        if (refreshLoop != null)
        {
            StopCoroutine(refreshLoop);
        }
        refreshLoop = StartCoroutine(RefreshAdStatusLoop());
    }

    // When the Shop Panel is disabled...
    void OnDisable()
    {
        Debug.Log("[ShopAdRefresher] Shop panel is closed. Stopping ad refresh loop.");
        // ...stop the watchdog loop to save performance.
        if (refreshLoop != null)
        {
            StopCoroutine(refreshLoop);
            refreshLoop = null;
        }
    }

    private IEnumerator RefreshAdStatusLoop()
    {
        // This loop will run forever, as long as this script is active.
        while (true)
        {
            // Find the RewardedAdButton component on our button.
            RewardedAdButton adButtonScript = adButtonToRefresh.GetComponent<RewardedAdButton>();
            if (adButtonScript != null)
            {
                // Manually tell the button to update its status.
                // This will check with the AdManager and enable/disable the button correctly.
                adButtonScript.UpdateAdStatus();
            }

            // If the button is STILL not interactable, it means the ad is not ready.
            // We should tell the AdManager to try loading it again.
            if (!adButtonToRefresh.interactable && AdManager_New.Instance != null)
            {
                // This is a safe way to try reloading.
                // We get the adUnitId directly from the button's script.
                string adId = adButtonScript.GetAdUnitId();
                if (!string.IsNullOrEmpty(adId))
                {
                    AdManager_New.Instance.LoadSpecificRewardedAd(adId);
                }
            }

            // Wait for a few seconds before checking again.
            yield return new WaitForSecondsRealtime(5f); // Check every 5 seconds.
        }
    }
}
