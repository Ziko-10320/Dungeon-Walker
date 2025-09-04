using UnityEngine;
using TMPro;

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    [SerializeField] private int coins = 0;
    [SerializeField] private string prefsKey = "WALLET_COINS";
    [SerializeField] private TextMeshProUGUI coinsText; // optional link

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        coins = PlayerPrefs.GetInt(prefsKey, 0);
        RefreshUI();
    }

    public int Coins => coins;

    public void AddCoins(int amount) { coins = Mathf.Max(0, coins + amount); Save(); }
    public bool TrySpend(int amount)
    {
        if (coins < amount) return false;
        coins -= amount; Save(); return true;
    }

    void Save()
    {
        PlayerPrefs.SetInt(prefsKey, coins);
        PlayerPrefs.Save();
        RefreshUI();
    }

    public void SetCoinsText(TextMeshProUGUI text) { coinsText = text; RefreshUI(); }
    void RefreshUI() { if (coinsText) coinsText.text = coins.ToString(); }
}
