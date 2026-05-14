using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// THIS IS THE NEW, SIMPLIFIED SCRIPT FOR CRAZYGAMES
public class RewardedAdButtonCrazy : MonoBehaviour
{
    [Header("Button Control")]
    [Tooltip("The actual UI Button that the player will click.")]
    [SerializeField] private Button buttonToControl;

    [Header("Reward")]
    [Tooltip("Assign the reward function here (e.g., GameUIManager.RewardDoubleCoins).")]
    public UnityEvent OnRewardGranted;

    void Awake()
    {
        // Find the button if it's not assigned
        if (buttonToControl == null)
        {
            buttonToControl = GetComponent<Button>();
        }
    }

    void OnEnable()
    {
        if (buttonToControl == null)
        {
            Debug.LogError("RewardedAdButton: No button found or assigned!", this);
            this.enabled = false;
            return;
        }

        // Always make the button interactable. CrazyGames handles the "ad not ready" case internally.
        buttonToControl.interactable = true;

        // Set up the button click listener
        buttonToControl.onClick.RemoveAllListeners();
        buttonToControl.onClick.AddListener(ShowCrazyGamesRewardedAd);
    }

    private void ShowCrazyGamesRewardedAd()
    {
        // When the button is clicked, we tell our new manager to show an ad.
        // We pass our 'OnRewardGranted' event directly to the manager.
        // The manager will then invoke it for us upon successful completion.
        CrazyGamesManager.Instance.ShowRewardedAd(() => {
            OnRewardGranted?.Invoke();
        });
    }
}
