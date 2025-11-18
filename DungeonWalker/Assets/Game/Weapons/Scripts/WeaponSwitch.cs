using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using TMPro; // ---- NEW ----: Import the TextMeshPro namespace
using UnityEngine.UI; // ---- NEW ----: Import the UI namespace
using Photon.Pun;
public class WeaponSwitchManager : MonoBehaviour
{
    [System.Serializable]
    public class WeaponConfig
    {
        public string weaponName; // Name of the weapon for display in the inspector
        public List<GameObject> weaponGameObjects; // List of GameObjects associated with this weapon
        public List<MonoBehaviour> weaponScripts; // List of scripts to enable/disable for this weapon
        public GameObject armGameObject; // GameObject of the arm associated with this weapon (the parent)
        public SpriteRenderer armSpriteRenderer; // The SpriteRenderer of the arm to enable/disable
        public Sprite weaponUIIcon; // ---- NEW ----: Sprite for the weapon's UI icon
        [Range(0f, 1f)]
        public float dropChance = 0.25f; // Probability of getting this weapon
    }

    [Header("Weapon Configuration")]
    public List<WeaponConfig> weaponConfigs;
    public int killsToSwitch = 5;
    [SerializeField] private float switchDelay = 0.2f;

    [Header("UI Settings")] // ---- NEW ----: Header for UI elements
    public TextMeshProUGUI killsLeftText; // ---- NEW ----: Reference to the UI Text element
    public Image weaponIconUI; // ---- NEW ----: Reference to the UI Image element for weapon icon

    private int currentKills = 0;
    private WeaponConfig currentWeapon;
    private SpriteRenderer currentArmSpriteRenderer;
    private bool isSwitchingWeapon = false;
    private List<GameObject> activeGhosts = new List<GameObject>();
    private List<GameObject> activeBat2Objects = new List<GameObject>();
    private List<GameObject> activeThrowSlashObjects = new List<GameObject>();
    void Start()
    {
        InitializeWeapons();
        UpdateKillsUI(); // ---- NEW ----: Update the UI on start
    }

    void InitializeWeapons()
    {
        // Disable all weapons, all arms (GameObjects) and all arm SpriteRenderers at startup
        foreach (var config in weaponConfigs)
        {
            if (config.weaponGameObjects != null)
            {
                foreach (var go in config.weaponGameObjects)
                {
                    if (go != null) go.SetActive(false);
                }
            }
            if (config.weaponScripts != null)
            {
                foreach (var script in config.weaponScripts)
                {
                    if (script != null) script.enabled = false;
                }
            }
            if (config.armGameObject != null)
            {
                config.armGameObject.SetActive(false);
            }
            if (config.armSpriteRenderer != null)
            {
                config.armSpriteRenderer.enabled = false;
            }
        }

        // Activate a random weapon at startup
        if (weaponConfigs.Count > 0)
        {
            currentWeapon = GetRandomWeapon();
            ActivateWeapon(currentWeapon);
            Debug.Log($"Initial weapon: {currentWeapon.weaponName}");
        }
    }

    public void OnEnemyKilled()
    {
        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.AddKill();
        }
        currentKills++;
        UpdateKillsUI();
        Debug.Log($"Kill count: {currentKills}/{killsToSwitch}");

        if (currentKills >= killsToSwitch && !isSwitchingWeapon)
        {
            StartCoroutine(SwitchWeaponWithDelay());
            currentKills = 0;
        }
    }
    public void SwitchWeaponManually()
    {
        // On vérifie si on n'est pas déjà en train de changer d'arme pour éviter les bugs.
        if (!isSwitchingWeapon)
        {
            Debug.Log("Manual weapon switch triggered by UI button.");

            // On réinitialise le compteur pour que le cycle soit cohérent.
            currentKills = 0;

            StartCoroutine(SwitchWeaponWithDelay());
        }
    }

    // ---- NEW ----: A new method dedicated to updating the UI text
    void UpdateKillsUI()
    {
        if (killsLeftText != null)
        {
            int killsRemaining = killsToSwitch - currentKills;
            killsLeftText.text = killsRemaining.ToString();
        }
        else
        {
            Debug.LogWarning("Kills Left Text UI element is not assigned in the inspector!");
        }
    }

    private IEnumerator SwitchWeaponWithDelay()
    {
        isSwitchingWeapon = true;
        Debug.Log($"Initiating weapon switch from: {currentWeapon?.weaponName}");

        yield return new WaitForSeconds(switchDelay);

        // ÉTAPE 1 : DÉSACTIVER L'ARME ACTUELLE (C'est ici que les visuels sont cachés)
        DeactivateWeapon(currentWeapon);

        // ÉTAPE 2 : TROUVER UNE NOUVELLE ARME
        WeaponConfig newWeapon = GetRandomWeapon();
        while (newWeapon == currentWeapon && weaponConfigs.Count > 1)
        {
            newWeapon = GetRandomWeapon();
        }

        // ÉTAPE 3 : ACTIVER LA NOUVELLE ARME (C'est ici que les nouveaux visuels apparaissent)
        currentWeapon = newWeapon;
        ActivateWeapon(currentWeapon);

        // ÉTAPE 4 : METTRE À JOUR L'INTERFACE UTILISATEUR
        UpdateKillsUI();

        Debug.Log($"Switched to weapon: {currentWeapon.weaponName}");
        isSwitchingWeapon = false;
    }
    public void RegisterSpawnedObject(GameObject obj)
    {
        if (obj.CompareTag("Ghost"))
        {
            activeGhosts.Add(obj);
        }
        else if (obj.name.Contains("Bat2"))
        {
            activeBat2Objects.Add(obj);
        }
        else if (obj.name.Contains("ThrowSlash"))
        {
            activeThrowSlashObjects.Add(obj);
        }
    }
    WeaponConfig GetRandomWeapon()
    {
        float totalChance = weaponConfigs.Sum(config => config.dropChance);
        float randomValue = Random.Range(0f, totalChance);

        float cumulativeChance = 0f;
        foreach (var config in weaponConfigs)
        {
            cumulativeChance += config.dropChance;
            if (randomValue <= cumulativeChance)
            {
                return config;
            }
        }
        return weaponConfigs.Last(); // Fallback
    }

    void ActivateWeapon(WeaponConfig weapon)
    {
        if (weapon != null)
        {
            Debug.Log($"Activating weapon: {weapon.weaponName}");

            if (weapon.weaponGameObjects != null)
            {
                foreach (var go in weapon.weaponGameObjects)
                {
                    if (go != null)
                    {
                        go.SetActive(true);
                        Debug.Log($"Activated weapon GameObject: {go.name}");
                    }
                }
            }
            if (weapon.weaponScripts != null)
            {
                foreach (var script in weapon.weaponScripts)
                {
                    if (script != null)
                    {
                        script.enabled = true;
                        Debug.Log($"Enabled weapon script: {script.GetType().Name}");
                    }
                }
            }
            // Activate the arm GameObject and its SpriteRenderer
            if (weapon.armGameObject != null)
            {
                weapon.armGameObject.SetActive(true);
                Debug.Log($"Activated arm GameObject: {weapon.armGameObject.name}");
            }
            if (weapon.armSpriteRenderer != null)
            {
                weapon.armSpriteRenderer.enabled = true;
                currentArmSpriteRenderer = weapon.armSpriteRenderer; // Update the currently active arm SpriteRenderer
                Debug.Log($"Enabled arm SpriteRenderer: {weapon.armSpriteRenderer.name}");
            }

            // ---- NEW ----: Update the UI Image with the weapon's icon
            if (weaponIconUI != null && weapon.weaponUIIcon != null)
            {
                weaponIconUI.sprite = weapon.weaponUIIcon;
                weaponIconUI.enabled = true; // Ensure the image is visible
            }
            else if (weaponIconUI != null)
            {
                weaponIconUI.enabled = false; // Hide the image if no icon is assigned
            }
        }
    }

    void DeactivateWeapon(WeaponConfig weapon)
    {
        if (weapon != null)
        {
            Debug.Log($"Deactivating weapon: {weapon.weaponName}");

            // Enhanced cleanup for weapon scripts - call specific cleanup methods if they exist
            if (weapon.weaponScripts != null)
            {
                foreach (var script in weapon.weaponScripts)
                {
                    if (script != null)
                    {
                        // Special handling for BatAttackSystem to ensure proper cleanup
                        if (script is BatAttackSystem batSystem)
                        {
                            Debug.Log("Performing special cleanup for BatAttackSystem");
                            // The OnDisable method will handle the cleanup automatically
                        }

                        script.enabled = false;
                        Debug.Log($"Disabled weapon script: {script.GetType().Name}");
                    }
                }
            }

            if (weapon.weaponGameObjects != null)
            {
                foreach (var go in weapon.weaponGameObjects)
                {
                    if (go != null)
                    {
                        go.SetActive(false);
                        Debug.Log($"Deactivated weapon GameObject: {go.name}");
                    }
                }
            }

            // Disable the arm GameObject and its SpriteRenderer that was previously active
            if (currentArmSpriteRenderer != null)
            {
                currentArmSpriteRenderer.enabled = false;
                Debug.Log($"Disabled arm SpriteRenderer: {currentArmSpriteRenderer.name}");

                // If the GameObject parent of the SpriteRenderer should be disabled, do it here.
                if (currentArmSpriteRenderer.gameObject != null)
                {
                    currentArmSpriteRenderer.gameObject.SetActive(false);
                    Debug.Log($"Deactivated arm GameObject: {currentArmSpriteRenderer.gameObject.name}");
                }
                currentArmSpriteRenderer = null; // Reset
            }

            // Additional cleanup: Destroy any remaining ghost objects or other artifacts
            CleanupWeaponArtifacts();
        }
    }
    // In WeaponSwitchManager.cs, replace your entire CleanupWeaponArtifacts method with this new version

    void CleanupWeaponArtifacts()
    {
        // Clean up ghost objects from our list
        foreach (GameObject ghost in activeGhosts)
        {
            if (ghost != null)
            {
                Destroy(ghost);
            }
        }
        activeGhosts.Clear(); // Clear the list for the next cycle
        Debug.Log("Cleaned up ghost objects during weapon switch");

        // Clean up Bat2 objects from our list
        foreach (GameObject bat2 in activeBat2Objects)
        {
            if (bat2 != null)
            {
                // The check to avoid destroying the current weapon is no longer needed
                // if you only register projectiles/temporary bats.
                Destroy(bat2);
            }
        }
        activeBat2Objects.Clear();
        Debug.Log("Cleaned up Bat2 objects during weapon switch");

        // Clean up ThrowSlash objects from our list
        foreach (GameObject throwSlash in activeThrowSlashObjects)
        {
            if (throwSlash != null)
            {
                Destroy(throwSlash);
            }
        }
        activeThrowSlashObjects.Clear();
        Debug.Log("Cleaned up ThrowSlash objects during weapon switch");
    }


    public string GetCurrentWeaponName()
    {
        return currentWeapon?.weaponName ?? "None";
    }

    /// <summary>
    /// Public method to force a weapon switch (for testing purposes)
    /// </summary>
    public void ForceWeaponSwitch()
    {
        if (!isSwitchingWeapon)
        {
            StartCoroutine(SwitchWeaponWithDelay());
            currentKills = 0;
        }
    }
}

