using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// This is a special class to define one possible drop and its chance.
[System.Serializable]
public class PowerUpDrop
{
    public PowerUpData powerUpData; // The power-up that can drop (e.g., Shield, SpeedBoost).
    [Tooltip("The chance 'weight' of this item. Higher is more common.")]
    public int weight;              // How likely it is to be chosen.
}

// This makes it so you can create this asset from the "Create" menu in Unity.
[CreateAssetMenu(fileName = "NewDropTable", menuName = "PowerUps/Power-Up Drop Table")]
public class PowerUpDropTable : ScriptableObject
{
    [Header("Possible Drops")]
    [Tooltip("The list of all power-ups that can drop from this table.")]
    public List<PowerUpDrop> possibleDrops;

    /// <summary>
    /// This method calculates and returns a random power-up based on the weights.
    /// </summary>
    public PowerUpData GetRandomDrop()
    {
        List<PowerUpDrop> availableDrops = new List<PowerUpDrop>(possibleDrops);

        // Try to find the player's inventory.
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory != null)
        {
            // If we found the inventory, get a list of power-up types the player has permanently equipped.
            var equippedTypes = inventory.equippedPowerUps
                                    .Where(p => p != null)
                                    .Select(p => p.type);

            // Remove any drops from our temporary list if the player already has them.
            availableDrops.RemoveAll(drop => equippedTypes.Contains(drop.powerUpData.type));
        }

        // If the filtered list is now empty, it means the player has everything. Stop here.
        if (availableDrops.Count == 0)
        {
            return null;
        }
        if (possibleDrops == null || possibleDrops.Count == 0)
        {
            return null; // No drops in this table.
        }
        int totalWeight = availableDrops.Sum(drop => drop.weight);

        // Roll a random number between 1 and the total weight.
        int randomRoll = Random.Range(1, totalWeight + 1);

        // Loop through the drops to find which one was chosen.
        foreach (var drop in availableDrops)
        {
            if (randomRoll <= drop.weight)
            {
                // This is the chosen one.
                return drop.powerUpData;
            }
            // If not, subtract its weight and check the next one.
            randomRoll -= drop.weight;
        }

        return null; // Should never happen, but it's a safe fallback.
    }
}
