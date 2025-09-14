using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class InventoryItemButton : MonoBehaviour
{
    // --- NEW: Add a direct reference to the Image component ---
    [SerializeField] private Image iconImage;

    private PowerUpData powerUpData;

    // This function is called by the InventoryUI to set up the button
    public void Setup(PowerUpData data)
    {
        powerUpData = data;

        // --- Use the direct reference to set the sprite ---
        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
        }
        else
        {
            Debug.LogError("Icon Image is not assigned on the InventoryItemButton script!");
        }

        // Add a listener to the button's click event
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    // When the button is clicked, it tells the main UI manager
    private void OnButtonClick()
    {
        // This part is correct and doesn't need to change
        InventoryUI.Instance.OnItemClicked(powerUpData);
    }

    // --- NEW: A helper function for when the script is first added in the editor ---
    private void OnValidate()
    {
        // This is a handy trick. It tries to automatically find the Image component
        // on the same GameObject when you add the script in the Unity Editor.
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }
    }
}
