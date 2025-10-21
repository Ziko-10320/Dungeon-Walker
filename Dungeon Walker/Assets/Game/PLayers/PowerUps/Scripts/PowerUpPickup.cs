using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    private PowerUpData powerUpData;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// Called by the enemy that drops this pickup to configure it.
    /// </summary>
    public void Initialize(PowerUpData data)
    {
        this.powerUpData = data;
        if (spriteRenderer != null && data != null)
        {
            spriteRenderer.sprite = data.icon; // Set the visual icon.
        }
    }

    // When the player enters the trigger...
    void OnTriggerEnter2D(Collider2D other)
    {
        // ...check if it's the player.
        if (other.CompareTag("Player"))
        {
            // Find the player's InGamePowerUpManager.
            InGamePowerUpManager manager = other.GetComponent<InGamePowerUpManager>();
            if (manager != null)
            {
                // Tell the manager to collect this power-up.
                manager.CollectPowerUp(powerUpData);
            }

            // Destroy the pickup object.
            Destroy(gameObject);
        }
    }
}
