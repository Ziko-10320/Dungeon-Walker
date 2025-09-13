using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class InventoryItemButton : MonoBehaviour
{
    private PowerUpData powerUpData;

    // This function is called by the InventoryUI to set up the button
    public void Setup(PowerUpData data)
    {
        powerUpData = data;
        GetComponent<Image>().sprite = data.icon;
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    // When the button is clicked, it tells the main UI manager
    private void OnButtonClick()
    {
        InventoryUI.Instance.OnItemClicked(powerUpData);
    }
}
