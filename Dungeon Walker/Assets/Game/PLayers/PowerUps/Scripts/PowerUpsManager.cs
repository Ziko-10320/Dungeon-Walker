using Photon.Realtime;
using System.Collections;
using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private KritinaMovement playerMovement;
    [SerializeField] private PlayerHealth playerHealth;
    private PlayerSuperMeter superMeter;

    private float originalMoveSpeed;

    [Header("Particle Systems")]
    public ParticleSystem speedBoostParticles;
    public ParticleSystem speedBoostParticles2;
    [Header("Shield Animation")]
    [SerializeField] public Animator shieldAnimator;
    [SerializeField] public GameObject shieldObject;

    [Header("Upgraded Shield References")]
    public GameObject upgradedShieldObject;   // 🆕 Upgraded Shield object
    public Animator upgradedShieldAnimator;

    [Header("Upgraded Shield Destruction Effect")]
    public GameObject destructionShieldPrefab;   // prefab with particle system
    public Transform[] destructionSpawnPoints;

    [Header("SoapTrail PowerUp")]
    public GameObject[] soapTrailObjects;
    public Transform soapDamagePoint;
    public Vector2 soapDamageSize = new Vector2(1f, 0.5f);

    [Header("AcidTrail PowerUp")]
    public GameObject[] acidTrailObjects;
    public Transform acidDamagePoint;
    public Vector2 acidDamageSize = new Vector2(1f, 0.5f);

    private ReviveSystem reviveSystem;
    public static bool SoulLinkEquipped = false;

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
        reviveSystem = FindObjectOfType<ReviveSystem>();
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

    public void ApplyPersistentEffect(PowerUpData data)
    {
        if (data.type != PowerUpType.SoulLink)
        {
            SoulLinkEquipped = false;
            SoulLinkEnemy[] allEnemies = FindObjectsOfType<SoulLinkEnemy>();
            foreach (SoulLinkEnemy e in allEnemies)
            {
                if (e != null)
                    e.linkChance = 0f;
            }
        }
        switch (data.type)
        {
            case PowerUpType.SpeedBoost:
                // We apply the multiplier to the ORIGINAL speed we saved in Awake.
                playerMovement.moveSpeed = originalMoveSpeed * data.speedMultiplier;
                Debug.Log("Persistent Effect Applied: SpeedBoost 🚀 New Speed: " + playerMovement.moveSpeed);
                if (speedBoostParticles != null) speedBoostParticles.Play();
                break;
            case PowerUpType.SpeedBoost2:
                // Same logic but with its own multiplier and particles
                playerMovement.moveSpeed = originalMoveSpeed * data.speedMultiplier;
                Debug.Log("Persistent Effect Applied: SpeedBoost2 ⚡ New Speed: " + playerMovement.moveSpeed);
                if (speedBoostParticles2 != null) speedBoostParticles2.Play();
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
            case PowerUpType.ShieldUpgraded:
                playerHealth.ActivateShield(playerHealth.upgradedShieldMaxHealth, true);
                playerHealth.RestoreShieldToMax();

                if (upgradedShieldObject != null)
                    upgradedShieldObject.SetActive(true);

                if (upgradedShieldAnimator != null)
                    upgradedShieldAnimator.SetTrigger("StartShield");

                Debug.Log("Persistent Effect Applied: Upgraded Shield 🛡️ with HP: " + playerHealth.upgradedShieldMaxHealth);
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
                        soap.player = playerMovement;

                        // --- FORCE ENABLE ALL TRAILS ONCE ---
                        foreach (var trail in soapTrailObjects)
                        {
                            if (trail != null)
                                trail.SetActive(true);
                        }

                        // --- ENABLE permanent SoapVisuals ---
                        foreach (var visual in soap.soapVisuals)
                        {
                            if (visual != null && !visual.gameObject.activeSelf)
                                visual.gameObject.SetActive(true);
                        }

                        soap.enabled = true;
                    }
                    else
                    {
                        Debug.LogError("No SoapTrailDamage found on player!");
                    }

                    Debug.Log("Persistent Effect Applied: SoapTrail 🧼 (Visuals enabled + trails active!)");
                }
                break;

            case PowerUpType.AcidTrail:
                {
                    AcidTrailDamage acid = GetComponent<AcidTrailDamage>();
                    if (acid != null)
                    {
                        // Assign references
                        acid.acidTrailObjects = acidTrailObjects;
                        acid.damagePoint = acidDamagePoint;
                        acid.damageSize = acidDamageSize;
                        acid.damage = (int)data.effectValue;
                        acid.player = playerMovement;

                        // --- FORCE ENABLE ALL TRAILS ONCE ---
                        foreach (var trail in acidTrailObjects)
                        {
                            if (trail != null)
                                trail.SetActive(true);
                        }

                        // --- ENABLE permanent SoapVisuals ---
                        foreach (var visual in acid.acidVisuals)
                        {
                            if (visual != null && !visual.gameObject.activeSelf)
                                visual.gameObject.SetActive(true);
                        }

                        acid.enabled = true;
                    }
                    else
                    {
                        Debug.LogError("No SoapTrailDamage found on player!");
                    }

                    Debug.Log("Persistent Effect Applied: SoapTrail 🧼 (Visuals enabled + trails active!)");
                }
                break;
            case PowerUpType.Revive:
                ReviveSystem revive = GetComponent<ReviveSystem>();
                if (revive != null)
                {
                    revive.hasRevivePowerUp = true;
                    revive.hasUsedRevive = false; // optional: reset on equip
                    Debug.Log("Revive powerup equipped.");
                }
                break;

            case PowerUpType.ReviveUpgraded:
                {
                    ReviveUpgradedSystem reviveUp = GetComponent<ReviveUpgradedSystem>();
                    if (reviveUp != null)
                    {
                        reviveUp.EquipReviveUpgraded();
                        Debug.Log("Persistent Effect Applied: RevivePlus equipped");
                    }
                }
                break;

            case PowerUpType.Invisibility:
                {
                    PlayerInvisibility invis = GetComponent<PlayerInvisibility>();
                    if (invis != null)
                    {
                        // You can use effectValue if you want to override default duration
                        if (data.effectValue > 0)
                            invis.invisibilityDuration = data.effectValue;

                        invis.ActivateInvisibility();
                        Debug.Log("Persistent Effect Applied: Invisibility 👻 Duration: " + invis.invisibilityDuration);
                    }
                    else
                    {
                        Debug.LogError("No PlayerInvisibility component found on player!");
                    }
                }
                break;
            case PowerUpType.ExplosiveCoins:
                {
                    ExplosiveCoinsPowerUp explosive = GetComponent<ExplosiveCoinsPowerUp>();
                    if (explosive != null)
                    {
                        if (data.effectValue > 0)
                            explosive.spawnChance = Mathf.Clamp01(data.effectValue);

                        explosive.enabled = true; // Only enabled if equipped
                        Debug.Log("Persistent Effect Applied: Explosive Coins 💰💥 Chance: " + explosive.spawnChance);
                    }
                    else
                    {
                        Debug.LogError("No ExplosiveCoinsPowerUp component found on player!");
                    }
                }
                break;
            case PowerUpType.BeePowerUp:
                {
                    BeePowerUp bee = GetComponent<BeePowerUp>();
                    if (bee != null)
                    {
                        if (data.effectValue > 0)
                            bee.damage = data.effectValue; // override damage if defined in PowerUpData

                        bee.EnableBeePowerUp();
                        Debug.Log("Persistent Effect Applied: Bee Swarm 🐝 Active! Damage: " + bee.damage);
                    }
                    else
                    {
                        Debug.LogError("No BeePowerUp component found on player!");
                    }
                } 
                break;
            case PowerUpType.SoulLink:
                {
                    // Player equipped the power-up
                    float chance = Mathf.Clamp01(data.effectValue);
                    SoulLinkEquipped = true;

                    SoulLinkEnemy[] enemies = FindObjectsOfType<SoulLinkEnemy>();
                    foreach (SoulLinkEnemy e in enemies)
                    {
                        if (e != null)
                            e.linkChance = chance; // only now they can link
                    }
                    Debug.Log("Persistent Effect Applied: SoulLink 🔮 Chance: " + data.effectValue);
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
