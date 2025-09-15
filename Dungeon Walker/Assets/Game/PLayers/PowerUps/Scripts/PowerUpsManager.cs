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
    [Header("Shield Animation")]
    [SerializeField] public Animator shieldAnimator;
    [SerializeField] public GameObject shieldObject;

    [Header("SoapTrail PowerUp")]
    public GameObject[] soapTrailObjects;
    public Transform soapDamagePoint;
    public Vector2 soapDamageSize = new Vector2(1f, 0.5f);
    void Awake()
    {
        // Find components. This is correct.
        superMeter = FindObjectOfType<PlayerSuperMeter>();
        if (superMeter == null) Debug.LogError("PowerUpManager: No PlayerSuperMeter found in scene!");
        if (playerMovement == null) playerMovement = GetComponent<KritinaMovement>();
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();

        // Store the original speed before any modifications.
        originalMoveSpeed = playerMovement.moveSpeed;

        if (shieldObject != null)
            shieldObject.SetActive(false);
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
                // Instead of infinite invincibility:
                playerHealth.ActivateShield(Mathf.RoundToInt(data.effectValue));
                playerHealth.RestoreShieldToMax();
                if (shieldObject != null)
                    shieldObject.SetActive(true);

                if (shieldAnimator != null)
                {
                    shieldAnimator.SetTrigger("StartShield");
                }

                Debug.Log("Persistent Effect Applied: Shield 🛡️ with HP: " + data.effectValue);
                
                break;

            case PowerUpType.InstantHeal:
                playerHealth.FullHeal();
                Debug.Log("Instant Effect Applied: Full Heal ❤️");
                break;

            case PowerUpType.InstantSuper:
                superMeter.ForceGiveSuper();
                Debug.Log("Instant Effect Applied: Full Super ⚡");
                break;

            case PowerUpType.SoapTrail:
                {
                    SoapTrailDamage soap = GetComponent<SoapTrailDamage>();
                    if (soap != null)
                    {
                        // Assign references
                        soap.soapTrailObjects = soapTrailObjects;
                        soap.damagePoint = soapDamagePoint;
                        soap.damageSize = soapDamageSize;
                        soap.damage = (int)data.effectValue;
                        soap.player = GetComponent<KritinaMovement>();

                        // --- FORCE ENABLE ALL TRAILS ONCE WHEN EQUIPPED ---
                        foreach (var trail in soapTrailObjects)
                        {
                            if (trail != null)
                            {
                                trail.SetActive(true);

                                var ps = trail.GetComponent<ParticleSystem>();
                                if (ps != null && !ps.isPlaying)
                                    ps.Play();
                            }
                        }
                    }

                    Debug.Log("Persistent Effect Applied: SoapTrail 🧼 (All trails enabled!)");
                }
                break;

        }
    }
    public void OnShieldAnimationEnd()
    {
        if (shieldObject != null)
            shieldObject.SetActive(false);
    }
    public bool HasPowerUp(PowerUpType type)
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null) return false;

        foreach (PowerUpData equippedItem in inventory.equippedPowerUps)
        {
            if (equippedItem != null && equippedItem.type == type)
                return true;
        }
        return false;
    }
}
