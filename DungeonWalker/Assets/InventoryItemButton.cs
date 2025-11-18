using UnityEngine;
using UnityEngine.UI;

public class InventoryItemButton : MonoBehaviour
{
    private object itemData; // Can now hold either a PowerUpData or a SkinData

    // Setup for Power-ups
    public void Setup(PowerUpData data)
    {
        this.itemData = data;
        GetComponent<Image>().sprite = data.icon;
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    // Setup for Skins
    public void Setup(SkinData data)
    {
        this.itemData = data;
        GetComponent<Image>().sprite = data.icon;
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        // Tell the InventoryUI what was clicked
        if (itemData is PowerUpData powerUp)
        {
            InventoryUI.Instance.OnItemClicked(powerUp);
        }
        else if (itemData is SkinData skin)
        {
            InventoryUI.Instance.OnItemClicked(skin);
        }
    }
}
