using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Needed for advanced list operations

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public List<PowerUpData> ownedPowerUps = new List<PowerUpData>();
    public PowerUpData[] equippedPowerUps = new PowerUpData[3];
    public List<string> ownedSkins = new List<string>();
    private const string OwnedSaveKey = "PlayerInventory_Owned";
    private const string EquippedSaveKey = "PlayerInventory_Equipped";
    private const string OwnedSkinsSaveKey = "PlayerInventory_OwnedSkins";
    public bool isThirdSlotUnlocked = false; // Tracks the state
    private const string ThirdSlotSaveKey = "PlayerInventory_ThirdSlotUnlocked";
    public bool isSecondSlotUnlocked = false;
    private const string SecondSlotSaveKey = "PlayerInventory_SecondSlotUnlocked";


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadInventory();
    }
    public void AddOwnedSkin(string uniqueID)
    {
        if (!ownedSkins.Contains(uniqueID))
        {
            ownedSkins.Add(uniqueID);
            SaveInventory();
        }
    }
    public void UnlockSecondSlot()
    {
        isSecondSlotUnlocked = true;
        SaveInventory();
        Debug.Log("Second power-up slot has been permanently unlocked!");
    }
    public void UnlockThirdSlot()
    {
        isThirdSlotUnlocked = true;
        SaveInventory(); // Save the change immediately
        Debug.Log("Third power-up slot has been permanently unlocked!");
    }
    public bool IsSkinOwned(string uniqueID)
    {
        return ownedSkins.Contains(uniqueID);
    }

    // This function equips a skin for a specific character
    public void EquipSkin(SkinData skin)
    {
        if (skin == null) return;
        // Check for ownership using the NEW unique ID
        if (!IsSkinOwned(skin.GetUniqueID())) return;

        // The key is still specific to the character
        string equippedSkinKey = "EquippedSkin_" + skin.character.ToString();
        // We still save the LABEL, because that's what the SpriteResolver needs
        PlayerPrefs.SetString(equippedSkinKey, skin.spriteLibraryLabel);

        Debug.Log("Equipped " + skin.skinName + " for " + skin.character.ToString());
        PlayerPrefs.Save();
    }
    public void UnequipSkin(CharacterType character)
    {
        string equippedSkinKey = "EquippedSkin_" + character.ToString();
        PlayerPrefs.SetString(equippedSkinKey, "Default"); // Revert to the default skin

        Debug.Log("Unequipped skin for " + character.ToString() + ". Reverting to Default.");
        PlayerPrefs.Save();
    }
    public void AddOwnedPowerUp(PowerUpData powerUp)
    {
        if (!ownedPowerUps.Contains(powerUp))
        {
            ownedPowerUps.Add(powerUp);
            SaveInventory();
        }
    }

    // --- THIS FUNCTION WAS MISSING ---
    public void EquipPowerUp(PowerUpData powerUp, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equippedPowerUps.Length) return;

        // If item is already equipped in another slot, unequip it from there first
        for (int i = 0; i < equippedPowerUps.Length; i++)
        {
            if (equippedPowerUps[i] == powerUp)
            {
                equippedPowerUps[i] = null;
            }
        }

        equippedPowerUps[slotIndex] = powerUp;
        Debug.Log("Equipped " + powerUp.powerUpName + " in slot " + slotIndex);
        SaveInventory(); // Save after equipping
    }

    // --- THIS FUNCTION WAS MISSING ---
    public void UnequipPowerUp(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equippedPowerUps.Length) return;

        if (equippedPowerUps[slotIndex] != null)
        {
            Debug.Log("Unequipped " + equippedPowerUps[slotIndex].powerUpName + " from slot " + slotIndex);
            equippedPowerUps[slotIndex] = null;
            SaveInventory(); // Save after unequipping
        }
    }

    private void SaveInventory()
    {
        // --- Save Owned Power-Ups (This part is unchanged) ---
        List<string> ownedPowerUpNames = ownedPowerUps.Select(p => p.name).ToList();
        string ownedPowerUpsSaveData = string.Join(",", ownedPowerUpNames);
        PlayerPrefs.SetString(OwnedSaveKey, ownedPowerUpsSaveData);

        // --- Save Equipped Power-Ups (This part is unchanged) ---
        string[] equippedPowerUpNames = new string[equippedPowerUps.Length];
        for (int i = 0; i < equippedPowerUps.Length; i++)
        {
            equippedPowerUpNames[i] = (equippedPowerUps[i] != null) ? equippedPowerUps[i].name : "null";
        }
        string equippedPowerUpsSaveData = string.Join(",", equippedPowerUpNames);
        PlayerPrefs.SetString(EquippedSaveKey, equippedPowerUpsSaveData);

        // --- NEW: Save Owned Skins ---
        string ownedSkinsSaveData = string.Join(",", ownedSkins);
        PlayerPrefs.SetString(OwnedSkinsSaveKey, ownedSkinsSaveData);
        PlayerPrefs.SetInt(SecondSlotSaveKey, isSecondSlotUnlocked ? 1 : 0);
        // -----------------------------
        PlayerPrefs.SetInt(ThirdSlotSaveKey, isThirdSlotUnlocked ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("Inventory Saved (Power-ups and Skins).");
    }

    private void LoadInventory()
    {
        // --- Load Owned Power-Ups (This part is unchanged) ---
        string ownedPowerUpsSaveData = PlayerPrefs.GetString(OwnedSaveKey, "");
        if (!string.IsNullOrEmpty(ownedPowerUpsSaveData))
        {
            ownedPowerUps.Clear();
            string[] ownedNames = ownedPowerUpsSaveData.Split(',');
            foreach (string name in ownedNames)
            {
                PowerUpData powerUp = Resources.Load<PowerUpData>("PowerUps/" + name);
                if (powerUp != null) ownedPowerUps.Add(powerUp);
            }
        }

        // --- Load Equipped Power-Ups (This part is unchanged) ---
        string equippedPowerUpsSaveData = PlayerPrefs.GetString(EquippedSaveKey, "");
        if (!string.IsNullOrEmpty(equippedPowerUpsSaveData))
        {
            string[] equippedNames = equippedPowerUpsSaveData.Split(',');
            for (int i = 0; i < equippedPowerUps.Length; i++)
            {
                if (equippedNames[i] != "null")
                {
                    PowerUpData powerUp = Resources.Load<PowerUpData>("PowerUps/" + equippedNames[i]);
                    if (powerUp != null) equippedPowerUps[i] = powerUp;
                }
                else
                {
                    equippedPowerUps[i] = null;
                }
            }
        }
        isSecondSlotUnlocked = PlayerPrefs.GetInt(SecondSlotSaveKey, 0) == 1;
        // --- NEW: Load Owned Skins ---
        string ownedSkinsSaveData = PlayerPrefs.GetString(OwnedSkinsSaveKey, "");
        if (!string.IsNullOrEmpty(ownedSkinsSaveData))
        {
            ownedSkins = ownedSkinsSaveData.Split(',').ToList();
        }
        // -----------------------------
        isThirdSlotUnlocked = PlayerPrefs.GetInt(ThirdSlotSaveKey, 0) == 1;
        Debug.Log("Inventory Loaded (Power-ups and Skins).");
    }
}
