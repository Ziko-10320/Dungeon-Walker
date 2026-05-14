using UnityEngine;
using System.Collections.Generic;

public class PlayerPowerLevelManager : MonoBehaviour
{
    public static PlayerPowerLevelManager Instance { get; private set; }

    private const string PPLSaveKey = "PlayerPowerLevel";

    // --- PPL SCORING SYSTEM ---
    private const int PPL_PER_WEAPON_LEVEL = 5;
    private const int PPL_SLOT_2_UNLOCKED = 10;
    private const int PPL_SLOT_3_UNLOCKED = 15;

    // We use a Dictionary to store the PPL value for each power-up type.
    private Dictionary<PowerUpType, int> powerUpPPLValues = new Dictionary<PowerUpType, int>()
    {
        // High-Impact Power-Ups
        { PowerUpType.ReviveUpgraded, 10 },
        { PowerUpType.ShieldUpgraded, 3 },
        { PowerUpType.SoulLink, 10 },

        // Medium-Impact Power-Ups
        { PowerUpType.Revive, 5 },
        { PowerUpType.Shield, 3 },
        { PowerUpType.SpeedBoost2, 10 },
        { PowerUpType.BeePowerUp, 10 },

        // Low-Impact Power-Ups
        { PowerUpType.SpeedBoost, 3 },
        { PowerUpType.Invisibility, 5 },
        { PowerUpType.ExplosiveCoins, 10 },
        { PowerUpType.SoapTrail, 3 },
        { PowerUpType.AcidTrail, 3}
    };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Calculates the Player Power Level based on all permanent upgrades and saves it.
    /// Call this method whenever a permanent upgrade is purchased.
    /// </summary>
    public void CalculateAndSavePPL()
    {
        int totalPPL = 0;

        // 1. Calculate PPL from Weapon Upgrades
        if (InventoryManager.Instance != null)
        {
            foreach (var weapon in InventoryManager.Instance.weaponLevels)
            {
                // weapon.Value is the level of the weapon.
                totalPPL += weapon.Value * PPL_PER_WEAPON_LEVEL;
            }
        }

        // 2. Calculate PPL from Owned Permanent Power-Ups
        if (InventoryManager.Instance != null)
        {
            foreach (PowerUpData powerUp in InventoryManager.Instance.ownedPowerUps)
            {
                if (powerUp != null && powerUpPPLValues.ContainsKey(powerUp.type))
                {
                    totalPPL += powerUpPPLValues[powerUp.type];
                }
            }
        }

        // 3. Calculate PPL from Unlocked Slots
        if (InventoryManager.Instance != null)
        {
            if (InventoryManager.Instance.isSecondSlotUnlocked)
            {
                totalPPL += PPL_SLOT_2_UNLOCKED;
            }
            if (InventoryManager.Instance.isThirdSlotUnlocked)
            {
                totalPPL += PPL_SLOT_3_UNLOCKED;
            }
        }

        // 4. Save the final PPL value
        PlayerPrefs.SetInt(PPLSaveKey, totalPPL);
        PlayerPrefs.Save();

        Debug.Log($"[PlayerPowerLevelManager] New PPL Calculated and Saved: {totalPPL}");
    }

    /// <summary>
    /// Gets the currently saved Player Power Level.
    /// </summary>
    public int GetPPL()
    {
        return PlayerPrefs.GetInt(PPLSaveKey, 0); // Default to 0 if not found
    }
}
