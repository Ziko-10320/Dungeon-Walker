using UnityEngine;
using UnityEngine.U2D.Animation;
using System.Collections.Generic; // We need this for using Lists.

// This script goes on the root Player GameObject.
public class PlayerSkinController : MonoBehaviour
{
    [Header("SKIN CONFIGURATION")]
    [Tooltip("The names of your skins. Must EXACTLY match the Label names in your Sprite Library Asset (e.g., 'Default', 'Bee', 'Robot').")]
    public List<string> skinNames = new List<string>();

    // A list to hold all the SpriteResolver components of the character's body parts.
    private List<SpriteResolver> spriteResolvers = new List<SpriteResolver>();
    private string currentSkinName = "Default";
    // --- CORE UNITY FUNCTIONS ---
    public string GetCurrentSkinName()
    {
        return currentSkinName;
    }
    void Awake()
    {
        // Find all SpriteResolver components (this part is the same)
        this.GetComponentsInChildren<SpriteResolver>(true, spriteResolvers);

        // --- THIS IS THE NEW, DIRECT LOGIC ---

        // 1. We know this script is for the Cat, so we tell it directly.
        string characterType = "Cat";

        // 2. Construct the correct key to look for in PlayerPrefs.
        string equippedSkinKey = "EquippedSkin_" + characterType; // This will become "EquippedSkin_Cat"

        // 3. Load the equipped skin label from PlayerPrefs. Default to "Default".
        string savedSkinLabel = PlayerPrefs.GetString(equippedSkinKey, "Default");

        // 4. Apply the skin!
        Debug.Log("Applying skin '" + savedSkinLabel + "' to " + characterType);
        ApplySkin(savedSkinLabel);
    }

    // --- THE SKIN APPLYING LOGIC ---

    // This is the main function that does all the work.
    public void ApplySkin(string skinName)
    {
        currentSkinName = skinName;
        // Safety check: if the list of resolvers is empty, something is wrong.
        if (spriteResolvers.Count == 0)
        {
            Debug.LogError("No SpriteResolvers found on the character. Make sure your body parts have SpriteResolver components.");
            return;
        }

        // Go through every SpriteResolver we found on the character.
        foreach (var resolver in spriteResolvers)
        {
            // This is the magic line.
            // It tells the resolver to change its Label to the new skin name.
            // The Category remains untouched, which is exactly what we want.
            resolver.SetCategoryAndLabel(resolver.GetCategory(), skinName);
        }

        Debug.Log("Applied skin: " + skinName);
    }

    // --- INSPECTOR "TRIGGER" FUNCTIONS ---

    [ContextMenu("Select Skin: Default")]
    private void SelectSkin_Default()
    {
        string skinName = "Default";
        PlayerPrefs.SetString("SelectedSkinName", skinName);
        ApplySkin(skinName);
    }

    [ContextMenu("Select Skin: Bee")]
    private void SelectSkin_Bee()
    {
        string skinName = "Bee";
        PlayerPrefs.SetString("SelectedSkinName", skinName);
        ApplySkin(skinName);
    }

    [ContextMenu("Select Skin: Mexican")]
    private void SelectSkin_Mexican()
    {
        string skinName = "Mexican";
        PlayerPrefs.SetString("SelectedSkinName", skinName);
        ApplySkin(skinName);
    }
    [ContextMenu("Select Skin: Beezy")]
    private void SelectSkin_Beezy()
    {
        string skinName = "Beezy";
        PlayerPrefs.SetString("SelectedSkinName", skinName);
        ApplySkin(skinName);
    }
    [ContextMenu("Select Skin: MilkMan")]
    private void SelectSkin_MilkMan()
    {
        string skinName = "MilkMan";
        PlayerPrefs.SetString("SelectedSkinName", skinName);
        ApplySkin(skinName);
    }
    [ContextMenu("Select Skin: Monkey")]
    private void SelectSkin_Monkey()
    {
        string skinName = "Monkey";
        PlayerPrefs.SetString("SelectedSkinName", skinName);
        ApplySkin(skinName);
    }

    // Add more functions here for your other skins. Just copy, paste, and change the name.
    // For example:
    // [ContextMenu("Select Skin: Ninja")]
    // private void SelectSkin_Ninja()
    // {
    //     string skinName = "Ninja";
    //     PlayerPrefs.SetString("SelectedSkinName", skinName);
    //     ApplySkin(skinName);
    // }
}
