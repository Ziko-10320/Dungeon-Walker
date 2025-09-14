using UnityEngine;
using System.Collections;

public class PowerUpManager : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private KritinaMovement playerMovement;
    [SerializeField] private PlayerHealth playerHealth;
    private PlayerSuperMeter superMeter;

    [Header("Assigned PowerUps")]
    [SerializeField] private PowerUpData[] powerUps;

    private float originalMoveSpeed;
    public ParticleSystem speedBoostParticles;
    public ParticleSystem ShiledParticules;
    void Awake()
    {
        superMeter = FindObjectOfType<PlayerSuperMeter>();
        if (superMeter == null)
        {
            Debug.LogError("PowerUpManager: No PlayerSuperMeter found in scene!");
        }

        if (playerMovement == null)
            playerMovement = GetComponent<KritinaMovement>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        originalMoveSpeed = playerMovement.moveSpeed;
    }

    void Start()
    {
        // Apply all power-ups that are enabled
        foreach (var powerUp in powerUps)
        {
            if (powerUp.enabledByDefault)
            {
                ApplyPowerUp(powerUp);
            }
        }
    }

    private void ApplyPowerUp(PowerUpData data)
    {
        switch (data.type)
        {
            case PowerUpType.SpeedBoost:
                playerMovement.moveSpeed = originalMoveSpeed * data.speedMultiplier;
                Debug.Log("SpeedBoost active forever 🚀");

                // 🔹 Keep particles looping while active
                if (speedBoostParticles != null)
                {
                    var mainModule = speedBoostParticles.main;
                    mainModule.loop = true;
                    speedBoostParticles.Play();
                }
                break;

            case PowerUpType.InstantHeal:
                playerHealth.FullHeal();
                Debug.Log("Instant Heal applied at game start ❤️");
                break;

            case PowerUpType.InstantSuper:
                superMeter.ForceGiveSuper();
                Debug.Log("Instant Super applied ⚡");
                break;

            case PowerUpType.Shield:
                playerHealth.isInvincible = true;
                if (ShiledParticules != null)
                {
                    var mainModule = ShiledParticules.main;
                    mainModule.loop = true;
                    ShiledParticules.Play();
                }
                Debug.Log("Shield is permanently ON 🛡️");
                break;
        }
    }
}

