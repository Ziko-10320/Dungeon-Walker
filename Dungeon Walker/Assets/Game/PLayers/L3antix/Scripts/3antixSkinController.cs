using UnityEngine;
using UnityEngine.U2D.Animation;
using System.Collections.Generic; // We need this for using Lists.

// This script goes on the root Player GameObject.
public class L3antixSkinController : MonoBehaviour
{
    [Header("SKIN CONFIGURATION")]
    [Tooltip("The names of your skins. Must EXACTLY match the Label names in your Sprite Library Asset (e.g., 'Default', 'Bee', 'Robot').")]
    public List<string> skinNames = new List<string>();

    // A list to hold all the SpriteResolver components of the character's body parts.
    private List<SpriteResolver> spriteResolvers = new List<SpriteResolver>();

    // --- CORE UNITY FUNCTIONS ---

    void Awake()
    {
        // Find all SpriteResolver components in this object AND all its children, and add them to our list.
        // The 'true' means it will include inactive objects.
        this.GetComponentsInChildren<SpriteResolver>(true, spriteResolvers);

        // Load the last selected skin name from PlayerPrefs. If none is saved, use the first name in our list.
        string savedSkinName = PlayerPrefs.GetString("SelectedSkinName", skinNames[0]);
        ApplySkin(savedSkinName);
    }

    // --- THE SKIN APPLYING LOGIC ---

    // This is the main function that does all the work.
    public void ApplySkin(string skinName)
    {
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

    [ContextMenu("Select Skin: Rock")]
    private void SelectSkin_Rock()
    {
        string skinName = "Rock";
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
    [ContextMenu("Select Skin: Suit")]
    private void SelectSkin_Suit()
    {
        string skinName = "Suit";
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
