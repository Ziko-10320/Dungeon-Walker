using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class InGamePowerUpManager : MonoBehaviour
{
    [Header("In-Game Slot Settings")]
    private const int MAX_SLOTS = 2;
    public PowerUpData[] inGameSlots = new PowerUpData[MAX_SLOTS];
    private int nextSlotToReplace = 0;
    public static InGamePowerUpManager Instance { get; private set; }
    // --- THIS IS THE KEY CHANGE ---
    // Instead of a specific manager, we now look for the BASE manager.
    private BasePowerUpManager permanentPowerUpManager;

    private InventoryManager inventoryManager;

    [Header("UI References")]
    [SerializeField] private Image uiSlot1;
    [SerializeField] private Image uiSlot2;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // Optional: If another one exists, destroy this one to enforce the singleton pattern.
            Destroy(gameObject);
            return;
        }
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
        // --- THIS IS THE NEW, SELF-CONTAINED LOGIC ---

        // Loop through our two temporary slots every frame.
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            // Get the power-up in the current slot.
            PowerUpData powerUp = inGameSlots[i];

            // If there's no power-up in this slot, just continue to the next one.
            if (powerUp == null)
            {
                continue;
            }

            // Now, we check if this power-up is "finished".
            bool isFinished = false;
            switch (powerUp.type)
            {
                case PowerUpType.Invisibility:
                    // Check if the player has an invisibility component and if it's NOT currently active.
                    var invis = GetComponent<PlayerInvisibility>();
                    var invis3antix = GetComponent<PlayerInvisibility3antix>();
                    if ((invis != null && !invis.IsInvisible()) || (invis3antix != null && !invis3antix.IsInvisible()))
                    {
                        isFinished = true;
                    }
                    break;

                case PowerUpType.Shield:
                case PowerUpType.ShieldUpgraded:
                    // Check if the player has a health component and if its shield is gone.
                    var health = GetComponent<PlayerHealth>();
                    var health3antix = GetComponent<L3antixHealth>();
                    if ((health != null && !health.HasShield) || (health3antix != null && !health3antix.HasShield))
                    {
                        isFinished = true;
                    }
                    break;

                case PowerUpType.Revive:
                    // Check if the player has a revive component and if it has been used.
                    var revive = GetComponent<ReviveSystem>();
                    if (revive != null && revive.hasUsedRevive)
                    {
                        isFinished = true;
                    }
                    break;

                case PowerUpType.ReviveUpgraded:
                    // Check for the upgraded revive component.
                    var reviveUp = GetComponent<ReviveUpgradedSystem>();
                    if (reviveUp != null && reviveUp.HasUsedRevive)
                    {
                        isFinished = true;
                    }
                    break;
            }

            // If we determined that the power-up in this slot is finished...
            if (isFinished)
            {
                Debug.Log($"Detected that temporary power-up '{powerUp.powerUpName}' has been used. Removing from slot {i}.");
                // ...remove it from the slot.
                inGameSlots[i] = null;
            }
        }

        // Finally, update the UI based on the current state of the slots.
        UpdateUI();
    }
    public void ReportPowerUpFinished(PowerUpType finishedType)
    {
        Debug.Log($"Report received: Power-up '{finishedType}' has finished.");
        // Loop through our temporary slots.
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            // If we find the power-up that just finished...
            if (inGameSlots[i] != null && inGameSlots[i].type == finishedType)
            {
                Debug.Log($"Found and removed '{finishedType}' from temporary slot {i}.");
                // We don't need to call RemovePersistentEffect here, because the effect
                // has already ended (e.g., invisibility wore off, shield broke).
                // We just need to clear it from the UI.
                inGameSlots[i] = null;

                // We found it, so we can stop looking.
                return;
            }
        }
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
            if (inGameSlots[i] != null)
            {
                // --- THIS IS THE FIX ---
                // We only care about cleaning up shields.
                if (inGameSlots[i].type == PowerUpType.Shield || inGameSlots[i].type == PowerUpType.ShieldUpgraded)
                {
                    // Find the correct health script on the player and call our new "kill switch".
                    PlayerHealth pHealth = GetComponent<PlayerHealth>();
                    if (pHealth != null)
                    {
                        pHealth.ForceRemoveShield(inGameSlots[i].type);
                    }

                    L3antixHealth l3Health = GetComponent<L3antixHealth>();
                    if (l3Health != null)
                    {
                        l3Health.ForceRemoveShield(inGameSlots[i].type);
                    }
                }
                // For other temporary power-ups, we can still call the old method if needed,
                // but for shields, we use our new, safe method.
                else if (permanentPowerUpManager != null)
                {
                    permanentPowerUpManager.RemovePersistentEffect(inGameSlots[i]);
                }
                // --- END OF FIX ---

                // Finally, clear the slot.
                inGameSlots[i] = null;
            }
        }
        nextSlotToReplace = 0;
        UpdateUI(); // Force the UI to update after clearing the slots.
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
