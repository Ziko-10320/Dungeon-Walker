using Photon.Realtime;
using System.Collections;
using System.Collections.Generic; // Added for List support
using System.Linq; // Added for .Any() and .Where()
using UnityEngine;

public class PowerUpManagerL3antix : BasePowerUpManager
{
    // --- ADDED: Instance for easy access ---
    public static PowerUpManagerL3antix Instance { get; private set; }

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
    public GameObject upgradedShieldObject;
    public Animator upgradedShieldAnimator;

    [Header("Upgraded Shield Destruction Effect")]
    public GameObject destructionShieldPrefab;
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
        // --- ADDED: Set up the Instance ---
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        superMeter = FindObjectOfType<PlayerSuperMeter>();
        if (superMeter == null) Debug.LogError("PowerUpManager: No PlayerSuperMeter found in scene!");
        if (L3antixMovement == null) L3antixMovement = GetComponent<L3antixMovement>();
        if (L3antixHealth == null) L3antixHealth = GetComponent<L3antixHealth>();

        originalMoveSpeed = L3antixMovement.moveSpeed;

        if (shieldObject != null)
            shieldObject.SetActive(false);
        ReviveSystemL3antix = FindObjectOfType<ReviveSystemL3antix>();
    }

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
            // --- UPDATED: Stacking Speed Boost Logic ---
            case PowerUpType.SpeedBoost:
            case PowerUpType.SpeedBoost2:
                RecalculateSpeed();
                break;

            case PowerUpType.Shield:
                // Instead of activating directly, we add it to the player's shield queue.
                L3antixHealth.AddShieldToQueue(PowerUpType.Shield);
                Debug.Log("Persistent Effect Applied: Added Normal Shield to queue.");
                break;

            case PowerUpType.ShieldUpgraded:
                // Same for the upgraded shield.
                L3antixHealth.AddShieldToQueue(PowerUpType.ShieldUpgraded);
                Debug.Log("Persistent Effect Applied: Added Upgraded Shield to queue.");
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
                SoapTrailDamageL3antix soap = GetComponent<SoapTrailDamageL3antix>();
                if (soap != null)
                {
                    soap.soapTrailObjects = soapTrailObjects;
                    soap.damagePoint = soapDamagePoint;
                    soap.damageSize = soapDamageSize;
                    soap.damage = (int)data.effectValue;
                    soap.player = L3antixMovement;
                    foreach (var trail in soap.soapTrailObjects) { if (trail != null) trail.SetActive(true); }
                    foreach (var visual in soap.soapVisuals) { if (visual != null && !visual.gameObject.activeSelf) visual.gameObject.SetActive(true); }
                    soap.enabled = true;
                    Debug.Log("Persistent Effect Applied: SoapTrail 🧼");
                }
                break;

            case PowerUpType.AcidTrail:
                AcidTrailDamageL3antix acid = GetComponent<AcidTrailDamageL3antix>();
                if (acid != null)
                {
                    acid.acidTrailObjects = acidTrailObjects;
                    acid.damagePoint = acidDamagePoint;
                    acid.damageSize = acidDamageSize;
                    acid.damage = (int)data.effectValue;
                    acid.player = L3antixMovement;
                    foreach (var trail in acid.acidTrailObjects) { if (trail != null) trail.SetActive(true); }
                    foreach (var visual in acid.acidVisuals) { if (visual != null && !visual.gameObject.activeSelf) visual.gameObject.SetActive(true); }
                    acid.enabled = true;
                    Debug.Log("Persistent Effect Applied: AcidTrail 🧪");
                }
                break;

            case PowerUpType.Revive:
                if (ReviveSystemL3antix != null)
                {
                    ReviveSystemL3antix.hasRevivePowerUp = true;
                    ReviveSystemL3antix.hasUsedRevive = false;
                    Debug.Log("Revive powerup equipped.");
                }
                break;

            case PowerUpType.ReviveUpgraded:
                ReviveUpgradedSystemL3antix reviveUp = GetComponent<ReviveUpgradedSystemL3antix>();
                if (reviveUp != null)
                {
                    reviveUp.EquipReviveUpgraded();
                    Debug.Log("Persistent Effect Applied: RevivePlus equipped");
                }
                break;

            case PowerUpType.Invisibility:
                PlayerInvisibility3antix invis = GetComponent<PlayerInvisibility3antix>();
                if (invis != null)
                {
                    if (data.effectValue > 0) invis.invisibilityDuration = data.effectValue;
                    invis.ActivateInvisibility();
                    Debug.Log("Persistent Effect Applied: Invisibility 👻");
                }
                break;

            case PowerUpType.ExplosiveCoins:
                ExplosiveCoinsPowerUpL3antix explosive = GetComponent<ExplosiveCoinsPowerUpL3antix>();
                if (explosive != null)
                {
                    if (data.effectValue > 0) explosive.spawnChance = Mathf.Clamp01(data.effectValue);
                    explosive.enabled = true;
                    Debug.Log("Persistent Effect Applied: Explosive Coins 💰💥");
                }
                break;

            case PowerUpType.BeePowerUp:
                BeePowerUpL3antix bee = GetComponent<BeePowerUpL3antix>();
                if (bee != null)
                {
                    if (data.effectValue > 0) bee.damage = data.effectValue;
                    bee.EnableBeePowerUp();
                    Debug.Log("Persistent Effect Applied: Bee Swarm 🐝");
                }
                break;

            // --- UPDATED: Soul Link Logic ---
            case PowerUpType.SoulLink:
                UpdateSoulLinkStatus(true, Mathf.Clamp01(data.effectValue));
                break;
        }
    }

    // --- ADDED: The entire missing RemovePersistentEffect method ---
    public override void RemovePersistentEffect(PowerUpData data)
    {
        if (data == null) return;
        Debug.Log($"Removing persistent effect: {data.powerUpName}");

        switch (data.type)
        {
            case PowerUpType.SpeedBoost:
            case PowerUpType.SpeedBoost2:
                StartCoroutine(RecalculateSpeedAfterFrame());
                break;

            case PowerUpType.Shield:
            case PowerUpType.ShieldUpgraded:
                if (L3antixHealth != null) L3antixHealth.DamageShield(99999);
                break;

            case PowerUpType.SoapTrail:
                SoapTrailDamageL3antix soap = GetComponent<SoapTrailDamageL3antix>();
                if (soap != null) soap.DisablePowerUp();
                break;

            case PowerUpType.AcidTrail:
                AcidTrailDamageL3antix acid = GetComponent<AcidTrailDamageL3antix>();
                if (acid != null) acid.DisablePowerUp();
                break;

            case PowerUpType.Revive:
                if (ReviveSystemL3antix != null) ReviveSystemL3antix.hasRevivePowerUp = false;
                break;

            case PowerUpType.ReviveUpgraded:
                ReviveUpgradedSystemL3antix reviveUp = GetComponent<ReviveUpgradedSystemL3antix>();
                if (reviveUp != null) reviveUp.hasReviveUpgradedPowerUp = false;
                break;

            case PowerUpType.Invisibility:
                PlayerInvisibility3antix invis = GetComponent<PlayerInvisibility3antix>();
                if (invis != null) invis.DeactivateInvisibility();
                break;

            case PowerUpType.ExplosiveCoins:
                ExplosiveCoinsPowerUpL3antix explosive = GetComponent<ExplosiveCoinsPowerUpL3antix>();
                if (explosive != null) explosive.enabled = false;
                break;

            case PowerUpType.BeePowerUp:
                BeePowerUpL3antix bee = GetComponent<BeePowerUpL3antix>();
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

    // --- ADDED: Stacking speed boost logic ---
    private void RecalculateSpeed()
    {
        float currentMultiplier = 1.0f;
        var allActivePowerUps = new List<PowerUpData>();

        if (InventoryManager.Instance != null)
        {
            allActivePowerUps.AddRange(InventoryManager.Instance.equippedPowerUps.Where(p => p != null));
        }
        InGamePowerUpManager tempManager = FindObjectOfType<InGamePowerUpManager>();
        if (tempManager != null)
        {
            allActivePowerUps.AddRange(tempManager.inGameSlots.Where(p => p != null));
        }

        bool hasSpeedBoost1 = false;
        bool hasSpeedBoost2 = false;

        foreach (var powerUp in allActivePowerUps)
        {
            if (powerUp.type == PowerUpType.SpeedBoost)
            {
                currentMultiplier += powerUp.speedMultiplier - 1.0f;
                hasSpeedBoost1 = true;
            }
            else if (powerUp.type == PowerUpType.SpeedBoost2)
            {
                currentMultiplier += powerUp.speedMultiplier - 1.0f;
                hasSpeedBoost2 = true;
            }
        }

        L3antixMovement.moveSpeed = originalMoveSpeed * currentMultiplier;

        if (speedBoostParticles != null) { if (hasSpeedBoost1 && !speedBoostParticles.isPlaying) speedBoostParticles.Play(); if (!hasSpeedBoost1 && speedBoostParticles.isPlaying) speedBoostParticles.Stop(); }
        if (speedBoostParticles2 != null) { if (hasSpeedBoost2 && !speedBoostParticles2.isPlaying) speedBoostParticles2.Play(); if (!hasSpeedBoost2 && speedBoostParticles2.isPlaying) speedBoostParticles2.Stop(); }

        Debug.Log($"L3antix Speed Recalculated. New Multiplier: {currentMultiplier}, New Speed: {L3antixMovement.moveSpeed}");
    }

    private IEnumerator RecalculateSpeedAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        RecalculateSpeed();
    }

    // --- ADDED: Soul Link update logic ---
    private void UpdateSoulLinkStatus(bool isEnabled, float chance)
    {
        SoulLinkEquipped = isEnabled;
        SoulLinkEnemy[] enemies = FindObjectsOfType<SoulLinkEnemy>();
        foreach (SoulLinkEnemy e in enemies)
        {
            if (e != null) e.linkChance = isEnabled ? chance : 0f;
        }
        Debug.Log($"SoulLink status UPDATED: {(isEnabled ? "ENABLED" : "DISABLED")}");
    }

    public void OnShieldAnimationEnd()
    {
        if (shieldObject != null)
            shieldObject.SetActive(false);
    }

    // --- UPDATED: The correct HasPowerUp check ---
    public bool HasPowerUp(PowerUpType type)
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory != null && inventory.equippedPowerUps.Any(p => p != null && p.type == type))
        {
            return true;
        }
        InGamePowerUpManager tempManager = FindObjectOfType<InGamePowerUpManager>();
        if (tempManager != null && tempManager.IsPowerUpAlreadyActive(type))
        {
            return true;
        }
        return false;
    }
}
