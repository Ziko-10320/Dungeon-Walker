using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Needed for advanced list operations

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public List<PowerUpData> ownedPowerUps = new List<PowerUpData>();
    public PowerUpData[] equippedPowerUps = new PowerUpData[2];

    private const string OwnedSaveKey = "PlayerInventory_Owned";
    private const string EquippedSaveKey = "PlayerInventory_Equipped";

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
        // --- Save Owned Items ---
        List<string> ownedNames = ownedPowerUps.Select(p => p.name).ToList();
        string ownedSaveData = string.Join(",", ownedNames);
        PlayerPrefs.SetString(OwnedSaveKey, ownedSaveData);

        // --- Save Equipped Items ---
        string[] equippedNames = new string[equippedPowerUps.Length];
        for (int i = 0; i < equippedPowerUps.Length; i++)
        {
            equippedNames[i] = (equippedPowerUps[i] != null) ? equippedPowerUps[i].name : "null";
        }
        string equippedSaveData = string.Join(",", equippedNames);
        PlayerPrefs.SetString(EquippedSaveKey, equippedSaveData);

        PlayerPrefs.Save();
        Debug.Log("Inventory Saved.");
    }

    private void LoadInventory()
    {
        // --- Load Owned Items ---
        string ownedSaveData = PlayerPrefs.GetString(OwnedSaveKey, "");
        if (!string.IsNullOrEmpty(ownedSaveData))
        {
            ownedPowerUps.Clear();
            string[] ownedNames = ownedSaveData.Split(',');
            foreach (string name in ownedNames)
            {
                PowerUpData powerUp = Resources.Load<PowerUpData>("PowerUps/" + name);
                if (powerUp != null) ownedPowerUps.Add(powerUp);
            }
        }

        // --- Load Equipped Items ---
        string equippedSaveData = PlayerPrefs.GetString(EquippedSaveKey, "");
        if (!string.IsNullOrEmpty(equippedSaveData))
        {
            string[] equippedNames = equippedSaveData.Split(',');
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
        Debug.Log("Inventory Loaded.");
    }
}
