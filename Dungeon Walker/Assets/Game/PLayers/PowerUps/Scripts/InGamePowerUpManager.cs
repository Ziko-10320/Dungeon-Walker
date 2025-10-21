using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class InGamePowerUpManager : MonoBehaviour
{
    [Header("In-Game Slot Settings")]
    private const int MAX_SLOTS = 2;
    public PowerUpData[] inGameSlots = new PowerUpData[MAX_SLOTS];
    private int nextSlotToReplace = 0;

    // --- THIS IS THE KEY CHANGE ---
    // Instead of a specific manager, we now look for the BASE manager.
    private BasePowerUpManager permanentPowerUpManager;

    private InventoryManager inventoryManager;

    [Header("UI References")]
    [SerializeField] private Image uiSlot1;
    [SerializeField] private Image uiSlot2;

    void Awake()
    {
        // --- THIS IS THE KEY CHANGE ---
        // This will now find EITHER PowerUpManager OR PowerUpManagerL3antix,
        // because both are a "BasePowerUpManager".
        permanentPowerUpManager = GetComponent<BasePowerUpManager>();
        if (permanentPowerUpManager == null)
        {
            Debug.LogError("FATAL: No PowerUpManager or PowerUpManagerL3antix found on this character!");
        }

        inventoryManager = InventoryManager.Instance;
    }

    void LateUpdate()
    {
        UpdateUI();
    }

    public void CollectPowerUp(PowerUpData newData)
    {
        // The "permanentPowerUpManager" variable will be null if it wasn't found in Awake.
        if (newData == null || permanentPowerUpManager == null) return;

        if (IsPowerUpAlreadyActive(newData.type))
        {
            Debug.Log($"Power-up '{newData.powerUpName}' is already active. Pickup ignored.");
            return;
        }

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (inGameSlots[i] == null)
            {
                EquipInSlot(newData, i);
                return;
            }
        }

        Debug.Log($"Slots are full. Replacing the oldest power-up in slot {nextSlotToReplace}.");
        EquipInSlot(newData, nextSlotToReplace);
        nextSlotToReplace = (nextSlotToReplace + 1) % MAX_SLOTS;
    }

    private void EquipInSlot(PowerUpData newData, int slotIndex)
    {
        if (inGameSlots[slotIndex] != null)
        {
            permanentPowerUpManager.RemovePersistentEffect(inGameSlots[slotIndex]);
        }

        inGameSlots[slotIndex] = newData;
        permanentPowerUpManager.ApplyPersistentEffect(newData);
    }

    public bool IsPowerUpAlreadyActive(PowerUpType type)
    {
        if (inventoryManager != null)
        {
            foreach (var powerUp in inventoryManager.equippedPowerUps)
            {
                if (powerUp != null && powerUp.type == type) return true;
            }
        }

        foreach (var powerUp in inGameSlots)
        {
            if (powerUp != null && powerUp.type == type) return true;
        }

        return false;
    }

    public void RemoveAllTemporaryPowerUps()
    {
        Debug.Log("Checkpoint reached. Removing all temporary power-ups.");
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (inGameSlots[i] != null && permanentPowerUpManager != null)
            {
                permanentPowerUpManager.RemovePersistentEffect(inGameSlots[i]);
                inGameSlots[i] = null;
            }
        }
        nextSlotToReplace = 0;
    }

    private void UpdateUI()
    {
        if (uiSlot1 == null || uiSlot2 == null) return;

        if (inGameSlots[0] != null)
        {
            uiSlot1.sprite = inGameSlots[0].icon;
            uiSlot1.enabled = true;
        }
        else
        {
            uiSlot1.enabled = false;
        }

        if (inGameSlots[1] != null)
        {
            uiSlot2.sprite = inGameSlots[1].icon;
            uiSlot2.enabled = true;
        }
        else
        {
            uiSlot2.enabled = false;
        }
    }
}
