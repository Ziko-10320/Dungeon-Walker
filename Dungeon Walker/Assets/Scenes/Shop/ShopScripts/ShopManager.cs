using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Header("Catalog")]
    public List<ItemData> items;         // assign in Inspector
    public Transform gridContent;        // parent for item buttons
    public GameObject itemPrefab;        // prefab with icon/name/price/button

    [Header("Right Panel")]
    public GameObject panel;             // root panel
    public Image panelIcon;
    public TextMeshProUGUI panelName, panelDesc, panelPrice;
    public Button buyButton;
    public TextMeshProUGUI buyButtonLabel;

    [Header("Optional UI")]
    public TextMeshProUGUI coinsText;

    private GameObject selectedObj;
    private ItemData selectedData;

    void Start()
    {
        if (coinsText && PlayerWallet.Instance) PlayerWallet.Instance.SetCoinsText(coinsText);
        BuildGrid();
        HidePanelInstant();
    }

    void BuildGrid()
    {
        foreach (Transform child in gridContent) Destroy(child.gameObject);

        foreach (var item in items)
        {
            var go = Instantiate(itemPrefab, gridContent);
            var btn = go.GetComponent<Button>();
            var icon = go.transform.Find("Icon")?.GetComponent<Image>();
            var nameText = go.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            var priceText = go.transform.Find("Price")?.GetComponent<TextMeshProUGUI>();

            if (icon) icon.sprite = item.icon;
            if (nameText) nameText.text = item.displayName;
            if (priceText) priceText.text = item.price.ToString();

            btn.onClick.AddListener(() => OnItemClicked(go, item));
        }
    }

    void OnItemClicked(GameObject go, ItemData data)
    {
        // toggle deselect
        if (selectedObj == go)
        {
            Deselect();
            return;
        }

        if (selectedObj) Highlight(selectedObj, false);
        selectedObj = go;
        selectedData = data;
        Highlight(go, true);
        ShowPanel(data);
    }

    void Deselect()
    {
        if (selectedObj) Highlight(selectedObj, false);
        selectedObj = null; selectedData = null;
        HidePanel();
    }

    void Highlight(GameObject go, bool on)
    {
        var hl = go.transform.Find("Highlight");
        if (hl) hl.gameObject.SetActive(on);
    }

    void ShowPanel(ItemData data)
    {
        panel.SetActive(true);
        panelIcon.sprite = data.icon;
        panelName.text = data.displayName;
        panelDesc.text = data.description;
        panelPrice.text = data.price.ToString();

        bool owned = IsOwned(data);
        bool canAfford = PlayerWallet.Instance && PlayerWallet.Instance.Coins >= data.price;

        buyButton.interactable = (!owned || data.stackable) && canAfford;
        buyButtonLabel.text = (owned && !data.stackable) ? "Owned" : "Buy";
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => TryBuy(data));
    }

    void HidePanel() { panel.SetActive(false); }
    void HidePanelInstant() { panel.SetActive(false); }

    void TryBuy(ItemData data)
    {
        if (!PlayerWallet.Instance) return;
        if (IsOwned(data) && !data.stackable) return;
        if (!PlayerWallet.Instance.TrySpend(data.price)) return;

        if (data.stackable) AddCount(data, 1);
        else SetOwned(data, true);

        ShowPanel(data); // refresh UI
    }

    // --- simple save system ---
    bool IsOwned(ItemData d) => d.stackable ? PlayerPrefs.GetInt("COUNT_" + d.id, 0) > 0 : PlayerPrefs.GetInt("OWNED_" + d.id, 0) == 1;
    void SetOwned(ItemData d, bool owned) { PlayerPrefs.SetInt("OWNED_" + d.id, owned ? 1 : 0); PlayerPrefs.Save(); }
    int GetCount(ItemData d) => PlayerPrefs.GetInt("COUNT_" + d.id, 0);
    void AddCount(ItemData d, int add) { PlayerPrefs.SetInt("COUNT_" + d.id, GetCount(d) + add); PlayerPrefs.Save(); }
}
