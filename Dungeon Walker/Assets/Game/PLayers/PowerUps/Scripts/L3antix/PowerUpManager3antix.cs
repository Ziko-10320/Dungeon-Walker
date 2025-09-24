using Photon.Realtime;
using System.Collections;
using UnityEngine;

public class PowerUpManagerL3antix : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private L3antixMovement L3antixMovement;
    [SerializeField] private L3antixHealth L3antixHealth;
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

    private ReviveSystemL3antix ReviveSystemL3antix;
    public static bool SoulLinkEquipped = false;

    void Awake()
    {
        // Find components. This is correct.
        superMeter = FindObjectOfType<PlayerSuperMeter>();
        if (superMeter == null) Debug.LogError("PowerUpManager: No PlayerSuperMeter found in scene!");
        if (L3antixMovement == null) L3antixMovement = GetComponent<L3antixMovement>();
        if (L3antixHealth == null) L3antixHealth = GetComponent<L3antixHealth>();

        // Store the original speed before any modifications.
        originalMoveSpeed = L3antixMovement.moveSpeed;

        if (shieldObject != null)
            shieldObject.SetActive(false);
        ReviveSystemL3antix = FindObjectOfType<ReviveSystemL3antix>();
    }

    // We use a coroutine to ensure this runs AFTER all other Start() methods.
    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null)
        {
            Debug.LogError("PowerUpManager could not find InventoryManager.Instance! The system will not work.");
            yield break;
        }

        Debug.Log("Applying persistent power-up effects for this level...");

        // First apply all power-ups
        foreach (PowerUpData equippedItem in inventory.equippedPowerUps)
        {
            if (equippedItem != null)
            {
                ApplyPersistentEffect(equippedItem);
            }
        }

        // ✅ Handle SoulLink once, after all power-ups are processed
        PowerUpData soulLinkData = null;
        foreach (PowerUpData equippedItem in inventory.equippedPowerUps)
        {
            if (equippedItem != null && equippedItem.type == PowerUpType.SoulLink)
            {
                soulLinkData = equippedItem;
                break;
            }
        }

        SoulLinkEnemy[] enemies = FindObjectsOfType<SoulLinkEnemy>();
        if (soulLinkData != null)
        {
            SoulLinkEquipped = true;
            float chance = Mathf.Clamp01(soulLinkData.effectValue);

            foreach (SoulLinkEnemy e in enemies)
            {
                if (e != null)
                    e.linkChance = chance;
            }

            Debug.Log("Persistent Effect Applied: SoulLink 🔮 Chance: " + soulLinkData.effectValue);
        }
        else
        {
            SoulLinkEquipped = false;
            foreach (SoulLinkEnemy e in enemies)
            {
                if (e != null)
                    e.linkChance = 0f;
            }
            Debug.Log("SoulLink not equipped. Enemies cannot link.");
        }
    }


    public void ApplyPersistentEffect(PowerUpData data)
    {

        switch (data.type)
        {
            case PowerUpType.SpeedBoost:
                // We apply the multiplier to the ORIGINAL speed we saved in Awake.
                L3antixMovement.moveSpeed = originalMoveSpeed * data.speedMultiplier;
                Debug.Log("Persistent Effect Applied: SpeedBoost 🚀 New Speed: " + L3antixMovement.moveSpeed);
                if (speedBoostParticles != null) speedBoostParticles.Play();
                break;
            case PowerUpType.SpeedBoost2:
                // Same logic but with its own multiplier and particles
                L3antixMovement.moveSpeed = originalMoveSpeed * data.speedMultiplier;
                Debug.Log("Persistent Effect Applied: SpeedBoost2 ⚡ New Speed: " + L3antixMovement.moveSpeed);
                if (speedBoostParticles2 != null) speedBoostParticles2.Play();
                break;

            case PowerUpType.Shield:
                // Instead of infinite invincibility:
                L3antixHealth.ActivateShield(Mathf.RoundToInt(data.effectValue));
                L3antixHealth.RestoreShieldToMax();
                if (shieldObject != null)
                    shieldObject.SetActive(true);

                if (shieldAnimator != null)
                {
                    shieldAnimator.SetTrigger("StartShield");
                }

                Debug.Log("Persistent Effect Applied: Shield 🛡️ with HP: " + data.effectValue);

                break;
            case PowerUpType.ShieldUpgraded:
                L3antixHealth.ActivateShield(L3antixHealth.upgradedShieldMaxHealth, true);
                L3antixHealth.RestoreShieldToMax();

                if (upgradedShieldObject != null)
                    upgradedShieldObject.SetActive(true);

                if (upgradedShieldAnimator != null)
                    upgradedShieldAnimator.SetTrigger("StartShield");

                Debug.Log("Persistent Effect Applied: Upgraded Shield 🛡️ with HP: " + L3antixHealth.upgradedShieldMaxHealth);
                break;
            case PowerUpType.InstantHeal:
                L3antixHealth.FullHeal();
                Debug.Log("Instant Effect Applied: Full Heal ❤️");
                break;

            case PowerUpType.InstantSuper:
                superMeter.ForceGiveSuper();
                Debug.Log("Instant Effect Applied: Full Super ⚡");
                break;

            case PowerUpType.SoapTrail:
                {
                    SoapTrailDamageL3antix soap = GetComponent<SoapTrailDamageL3antix>();
                    if (soap != null)
                    {
                        // Assign references
                        soap.soapTrailObjects = soapTrailObjects;
                        soap.damagePoint = soapDamagePoint;
                        soap.damageSize = soapDamageSize;
                        soap.damage = (int)data.effectValue;
                        soap.player = L3antixMovement;

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
                    AcidTrailDamageL3antix acid = GetComponent<AcidTrailDamageL3antix>();
                    if (acid != null)
                    {
                        // Assign references
                        acid.acidTrailObjects = acidTrailObjects;
                        acid.damagePoint = acidDamagePoint;
                        acid.damageSize = acidDamageSize;
                        acid.damage = (int)data.effectValue;
                        acid.player = L3antixMovement;

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
                ReviveSystemL3antix revive = GetComponent<ReviveSystemL3antix>();
                if (revive != null)
                {
                    revive.hasRevivePowerUp = true;
                    revive.hasUsedRevive = false; // optional: reset on equip
                    Debug.Log("Revive powerup equipped.");
                }
                break;

            case PowerUpType.ReviveUpgraded:
                {
                    ReviveUpgradedSystemL3antix reviveUp = GetComponent<ReviveUpgradedSystemL3antix>();
                    if (reviveUp != null)
                    {
                        reviveUp.EquipReviveUpgraded();
                        Debug.Log("Persistent Effect Applied: RevivePlus equipped");
                    }
                }
                break;

            case PowerUpType.Invisibility:
                {
                    PlayerInvisibility3antix invis = GetComponent<PlayerInvisibility3antix>();
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
                    BeePowerUpL3antix bee = GetComponent<BeePowerUpL3antix>();
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
