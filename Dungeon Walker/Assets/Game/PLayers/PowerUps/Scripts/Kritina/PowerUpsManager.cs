using Photon.Realtime;
using System.Collections;
using System.Linq;
using UnityEngine;

public class PowerUpManager : BasePowerUpManager
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
    public static PowerUpManager Instance { get; private set; }
    private InGamePowerUpManager tempManager;

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
        tempManager = GetComponent<InGamePowerUpManager>();
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


    public override void ApplyPersistentEffect(PowerUpData data)
    {
        
        switch (data.type)
        {
            case PowerUpType.SpeedBoost:
            case PowerUpType.SpeedBoost2:
                RecalculateSpeed();
                break;

            case PowerUpType.Shield:
                // Instead of activating directly, we add it to the player's shield queue.
                playerHealth.AddShieldToQueue(PowerUpType.Shield);
                Debug.Log("Persistent Effect Applied: Added Normal Shield to queue.");
                break;

            case PowerUpType.ShieldUpgraded:
                // Same for the upgraded shield.
                playerHealth.AddShieldToQueue(PowerUpType.ShieldUpgraded);
                Debug.Log("Persistent Effect Applied: Added Upgraded Shield to queue.");
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
                UpdateSoulLinkStatus(true, Mathf.Clamp01(data.effectValue));
                break;



        }
    }

    public override void RemovePersistentEffect(PowerUpData data)
    {
        if (data == null) return;
        Debug.Log($"Removing persistent effect: {data.powerUpName}");

        switch (data.type)
        {
            case PowerUpType.SpeedBoost:
            case PowerUpType.SpeedBoost2:
                // We need to wait a frame before recalculating, because the item is still in the list when this is called.
                // A coroutine is perfect for this.
                StartCoroutine(RecalculateSpeedAfterFrame());
                break;

            case PowerUpType.Shield:
            case PowerUpType.ShieldUpgraded:
                // This case is now handled by the PlayerHealth queue logic.
                // When a temporary shield is removed, we don't need to do anything extra here,
                // as the PlayerHealth script will manage activating the next shield in line if needed.
                // We can leave this empty or add a log.
                Debug.Log("A shield power-up was removed. PlayerHealth will manage the queue.");
                break;



            case PowerUpType.SoapTrail:
                SoapTrailDamage soap = GetComponent<SoapTrailDamage>();
                if (soap != null)
                {
                    // We need to call a new method on the SoapTrailDamage script to hide everything.
                    soap.DisablePowerUp();
                }
                break;

            case PowerUpType.AcidTrail:
                AcidTrailDamage acid = GetComponent<AcidTrailDamage>();
                if (acid != null)
                {
                    // We do the same for the acid trail.
                    acid.DisablePowerUp();
                }
                break;

            case PowerUpType.Revive:
                ReviveSystem revive = GetComponent<ReviveSystem>();
                if (revive != null) revive.hasRevivePowerUp = false;
                break;

            case PowerUpType.ReviveUpgraded:
                ReviveUpgradedSystem reviveUp = GetComponent<ReviveUpgradedSystem>();
                if (reviveUp != null) reviveUp.hasReviveUpgradedPowerUp = false;
                break;

            case PowerUpType.Invisibility:
                PlayerInvisibility invis = GetComponent<PlayerInvisibility>();
                if (invis != null) invis.DeactivateInvisibility();
                break;

            case PowerUpType.ExplosiveCoins:
                ExplosiveCoinsPowerUp explosive = GetComponent<ExplosiveCoinsPowerUp>();
                if (explosive != null) explosive.enabled = false;
                break;

            case PowerUpType.BeePowerUp:
                BeePowerUp bee = GetComponent<BeePowerUp>();
                if (bee != null) bee.DisableBeePowerUp();
                break;

            case PowerUpType.SoulLink:
                UpdateSoulLinkStatus(false, 0f);
                break;


            case PowerUpType.InstantHeal:
            case PowerUpType.InstantSuper:
                break;
        }
    }

    private void UpdateSoulLinkStatus(bool isEnabled, float chance)
    {
        SoulLinkEquipped = isEnabled;
        SoulLinkEnemy[] enemies = FindObjectsOfType<SoulLinkEnemy>();

        foreach (SoulLinkEnemy e in enemies)
        {
            if (e != null)
            {
                e.linkChance = isEnabled ? chance : 0f;
            }
        }

        if (isEnabled)
        {
            Debug.Log("SoulLink status UPDATED: ENABLED with chance: " + chance);
        }
        else
        {
            Debug.Log("SoulLink status UPDATED: DISABLED.");
        }
    }
    private void RecalculateSpeed()
    {
        // Start with the base speed.
        float currentMultiplier = 1.0f;

        // --- Find all active power-ups from both managers ---
        var allActivePowerUps = new System.Collections.Generic.List<PowerUpData>();
        if (InventoryManager.Instance != null)
        {
            allActivePowerUps.AddRange(InventoryManager.Instance.equippedPowerUps.Where(p => p != null));
        }
        // NOTE: This assumes your InGamePowerUpManager has a public list named 'inGameSlots' or similar.
        // Based on your script, it's `inGameSlots`.
        InGamePowerUpManager tempManager = FindObjectOfType<InGamePowerUpManager>();
        if (tempManager != null)
        {
            // We need to access the array of temporary powerups. Let's make it public.
            // I will assume you will make `private PowerUpData[] inGameSlots` public for this to work.
            // If you can't, let me know and I'll find another way.
            allActivePowerUps.AddRange(tempManager.inGameSlots.Where(p => p != null));
        }

        // --- Calculate the combined multiplier ---
        bool hasSpeedBoost1 = false;
        bool hasSpeedBoost2 = false;

        foreach (var powerUp in allActivePowerUps)
        {
            if (powerUp.type == PowerUpType.SpeedBoost)
            {
                // Add the bonus from the multiplier. (e.g., 1.5x multiplier adds 0.5)
                currentMultiplier += powerUp.speedMultiplier - 1.0f;
                hasSpeedBoost1 = true;
            }
            else if (powerUp.type == PowerUpType.SpeedBoost2)
            {
                currentMultiplier += powerUp.speedMultiplier - 1.0f;
                hasSpeedBoost2 = true;
            }
        }

        // Apply the final calculated speed.
        playerMovement.moveSpeed = originalMoveSpeed * currentMultiplier;

        // --- Update particle effects ---
        if (hasSpeedBoost1 && !speedBoostParticles.isPlaying) speedBoostParticles.Play();
        if (!hasSpeedBoost1 && speedBoostParticles.isPlaying) speedBoostParticles.Stop();

        if (hasSpeedBoost2 && !speedBoostParticles2.isPlaying) speedBoostParticles2.Play();
        if (!hasSpeedBoost2 && speedBoostParticles2.isPlaying) speedBoostParticles2.Stop();

        Debug.Log($"Speed Recalculated. New Multiplier: {currentMultiplier}, New Speed: {playerMovement.moveSpeed}");
    }
    private IEnumerator RecalculateSpeedAfterFrame()
    {
        // Wait until the end of the current frame.
        yield return new WaitForEndOfFrame();
        // By now, the power-up has been removed from the list, so we can safely recalculate.
        RecalculateSpeed();
    }

    public void OnShieldAnimationEnd()
    {
        if (shieldObject != null)
            shieldObject.SetActive(false);
    }
    public bool HasPowerUp(PowerUpType type)
    {
        // First, check the permanent inventory (this is already fast).
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory != null && inventory.equippedPowerUps.Any(p => p != null && p.type == type))
        {
            return true;
        }

        // --- THE OPTIMIZATION ---
        // Use the stored reference to the temporary manager. No searching!
        if (tempManager != null && tempManager.IsPowerUpAlreadyActive(type))
        {
            return true;
        }
        // --- END OF OPTIMIZATION ---

        return false;
    }


}
