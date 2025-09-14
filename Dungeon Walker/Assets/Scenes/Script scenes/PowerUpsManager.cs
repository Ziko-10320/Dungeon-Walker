using UnityEngine;
using System.Collections;

public class PowerUpManager : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private KritinaMovement playerMovement;
    [SerializeField] private PlayerHealth playerHealth;
    private PlayerSuperMeter superMeter;

    private float originalMoveSpeed;

    [Header("Particle Systems")]
    public ParticleSystem speedBoostParticles;
    public ParticleSystem ShiledParticules;

    void Awake()
    {
        // Find components. This is correct.
        superMeter = FindObjectOfType<PlayerSuperMeter>();
        if (superMeter == null) Debug.LogError("PowerUpManager: No PlayerSuperMeter found in scene!");
        if (playerMovement == null) playerMovement = GetComponent<KritinaMovement>();
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();

        // Store the original speed before any modifications.
        originalMoveSpeed = playerMovement.moveSpeed;
    }

    // We use a coroutine to ensure this runs AFTER all other Start() methods.
    IEnumerator Start()
    {
        // 1. Wait for the end of the very first frame.
        // This gives all other scripts time to complete their initial setup.
        yield return new WaitForEndOfFrame();

        // 2. Find the InventoryManager instance.
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null)
        {
            Debug.LogError("PowerUpManager could not find InventoryManager.Instance! The system will not work.");
            yield break; // Stop the coroutine here.
        }

        Debug.Log("Applying persistent power-up effects for this level...");

        // 3. Loop through the player's equipped items.
        foreach (PowerUpData equippedItem in inventory.equippedPowerUps)
        {
            if (equippedItem != null)
            {
                ApplyPersistentEffect(equippedItem);
            }
        }
    }

    private void ApplyPersistentEffect(PowerUpData data)
    {
        switch (data.type)
        {
            case PowerUpType.SpeedBoost:
                // We apply the multiplier to the ORIGINAL speed we saved in Awake.
                playerMovement.moveSpeed = originalMoveSpeed * data.speedMultiplier;
                Debug.Log("Persistent Effect Applied: SpeedBoost 🚀 New Speed: " + playerMovement.moveSpeed);
                if (speedBoostParticles != null) speedBoostParticles.Play();
                break;

            case PowerUpType.Shield:
                playerHealth.isInvincible = true;
                Debug.Log("Persistent Effect Applied: Shield 🛡️");
                if (ShiledParticules != null) ShiledParticules.Play();
                break;

            case PowerUpType.InstantHeal:
                playerHealth.FullHeal();
                Debug.Log("Instant Effect Applied: Full Heal ❤️");
                break;

            case PowerUpType.InstantSuper:
                superMeter.ForceGiveSuper();
                Debug.Log("Instant Effect Applied: Full Super ⚡");
                break;
        }
    }
}
