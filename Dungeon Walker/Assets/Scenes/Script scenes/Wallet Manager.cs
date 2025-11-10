using UnityEngine;
using UnityEngine.Events;
public class WalletManager : MonoBehaviour
{
    // Singleton instance - this makes it globally accessible
    public static WalletManager Instance { get; private set; }

    // The current number of coins the player has.
    public int CurrentCoins { get; private set; }

    // A key to save and load the coin data from PlayerPrefs.
    private const string CoinsSaveKey = "PlayerTotalCoins";
    public UnityEvent<int> OnCoinsChanged;
    // This function is called when the script instance is being loaded.
    private void Awake()
    {
        // --- Singleton Pattern Implementation ---
        // If an instance already exists and it's not this one, destroy this one.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        // Otherwise, set this as the instance.
        Instance = this;

        // --- Make it Persistent ---
        // Don't destroy this GameObject when loading a new scene.
        DontDestroyOnLoad(gameObject);

        // --- Load Data ---
        // Load the player's coin balance from storage when the game starts.
        LoadCoins();
    }
    private void Start() // <-- ADD A Start() METHOD
    {
        // When the game starts, fire the event once to make sure all UI is up-to-date.
        OnCoinsChanged?.Invoke(CurrentCoins);
    }
    /// <summary>
    /// Loads the coin balance from PlayerPrefs. If no data is found, it defaults to 0.
    /// </summary>
    private void LoadCoins()
    {
        CurrentCoins = PlayerPrefs.GetInt(CoinsSaveKey, 0);
    }

    /// <summary>
    /// Saves the current coin balance to PlayerPrefs.
    /// </summary>
    private void SaveCoins()
    {
        PlayerPrefs.SetInt(CoinsSaveKey, CurrentCoins);
        PlayerPrefs.Save(); // Immediately writes data to disk.
    }

    /// <summary>
    /// Adds a specified amount of coins to the wallet.
    /// </summary>
    /// <param name="amountToAdd">The number of coins to add. Must be positive.</param>
    public void AddCoins(int amountToAdd)
    {
        if (amountToAdd <= 0)
        {
            Debug.LogWarning("Cannot add a zero or negative amount of coins.");
            return;
        }

        CurrentCoins += amountToAdd;
        SaveCoins(); // Save the new balance immediately.
        Debug.Log(amountToAdd + " coins added. New balance: " + CurrentCoins);

        OnCoinsChanged?.Invoke(CurrentCoins);
    }
    public void AddFiveCoinsFromAd()
    {
        // We call your existing, safe AddCoins method.
        AddCoins(5);
        Debug.Log("5 coins awarded from watching a rewarded ad!");
    }
    /// <summary>
    /// Attempts to spend a specified amount of coins.
    /// </summary>
    /// <param name="amountToSpend">The number of coins to spend.</param>
    /// <returns>True if the purchase was successful, false if not enough coins.</returns>
    public bool SpendCoins(int amountToSpend)
    {
        if (amountToSpend <= 0)
        {
            Debug.LogWarning("Cannot spend a zero or negative amount of coins.");
            return false;
        }

        if (CurrentCoins >= amountToSpend)
        {
            CurrentCoins -= amountToSpend;
            SaveCoins(); // Save the new balance immediately.
            Debug.Log(amountToSpend + " coins spent. New balance: " + CurrentCoins);
            OnCoinsChanged?.Invoke(CurrentCoins);
            return true; // Purchase successful
        }
        else
        {
            Debug.Log("Not enough coins to spend " + amountToSpend + ". Current balance: " + CurrentCoins);
            return false; // Purchase failed
        }
    }
}
