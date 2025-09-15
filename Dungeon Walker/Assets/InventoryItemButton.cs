using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class InventoryItemButton : MonoBehaviour
{
    private PowerUpData powerUpData; // Keep a reference to the data

    public void Setup(PowerUpData data)
    {
        powerUpData = data; // Store the data

        // Set the icon
        GetComponent<Image>().sprite = data.icon;

        // --- THIS IS THE LINE I WRONGLY REMOVED. THIS MAKES THE BUTTON WORK AGAIN. ---
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    // This function tells the InventoryUI to equip the item.
    private void OnButtonClick()
    {
        // Safety check
        if (powerUpData != null)
        {
            InventoryUI.Instance.OnItemClicked(powerUpData);
        }
    }
}
