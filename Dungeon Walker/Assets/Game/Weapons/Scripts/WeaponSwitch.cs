using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using TMPro; // ---- NEW ----: Import the TextMeshPro namespace

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
        [Range(0f, 1f)]
        public float dropChance = 0.25f; // Probability of getting this weapon
    }

    [Header("Weapon Configuration")]
    public List<WeaponConfig> weaponConfigs;
    public int killsToSwitch = 5;
    [SerializeField] private float switchDelay = 0.2f;

    [Header("UI Settings")] // ---- NEW ----: Header for UI elements
    public TextMeshProUGUI killsLeftText; // ---- NEW ----: Reference to the UI Text element

    private int currentKills = 0;
    private WeaponConfig currentWeapon;
    private SpriteRenderer currentArmSpriteRenderer;
    private bool isSwitchingWeapon = false;

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
        currentKills++;
        UpdateKillsUI(); // ---- NEW ----: Update the UI every time an enemy is killed
        Debug.Log($"Kill count: {currentKills}/{killsToSwitch}");

        if (currentKills >= killsToSwitch && !isSwitchingWeapon)
        {
            StartCoroutine(SwitchWeaponWithDelay());
            currentKills = 0;
            // ---- NEW ----: We will update the UI again after the switch completes
        }
    }

    // ---- NEW ----: A new method dedicated to updating the UI text
    void UpdateKillsUI()
    {
        if (killsLeftText != null)
        {
            int killsRemaining = killsToSwitch - currentKills;
            killsLeftText.text = $"Kills to Switch: {killsRemaining}";
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

        DeactivateWeapon(currentWeapon);

        WeaponConfig newWeapon = GetRandomWeapon();
        while (newWeapon == currentWeapon && weaponConfigs.Count > 1)
        {
            newWeapon = GetRandomWeapon();
        }

        currentWeapon = newWeapon;
        ActivateWeapon(currentWeapon);

        UpdateKillsUI(); // ---- NEW ----: Update the UI after the switch is complete to reset the counter display

        Debug.Log($"Switched to weapon: {currentWeapon.weaponName}");
        isSwitchingWeapon = false;
    }

    // ... (The rest of your script remains the same)
    // GetRandomWeapon, ActivateWeapon, DeactivateWeapon, CleanupWeaponArtifacts, etc.
    // I have omitted the rest of the script for brevity as no other changes are needed.
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
    void CleanupWeaponArtifacts()
    {
        // Clean up ghost objects
        GameObject[] ghosts = GameObject.FindGameObjectsWithTag("Ghost");
        foreach (GameObject ghost in ghosts)
        {
            Destroy(ghost);
        }
        if (ghosts.Length > 0)
        {
            Debug.Log($"Cleaned up {ghosts.Length} ghost objects during weapon switch");
        }

        // Clean up any Bat2 objects that might be left in the scene
        // BUT EXCLUDE the player's current weapon bat
        GameObject[] bat2Objects = GameObject.FindObjectsOfType<GameObject>()
            .Where(go => go.name.Contains("Bat2") && go.GetComponent<Rigidbody2D>() != null)
            .ToArray();

        foreach (GameObject bat2 in bat2Objects)
        {
            // Check if this bat2 is part of the current weapon - if so, DON'T destroy it
            // This assumes 'currentWeapon' is correctly set and 'weaponGameObjects' contains the bat's GameObject.
            bool isCurrentWeaponBat = false;
            if (currentWeapon != null && currentWeapon.weaponGameObjects != null)
            {
                foreach (GameObject weaponGO in currentWeapon.weaponGameObjects)
                {
                    if (weaponGO == bat2)
                    {
                        isCurrentWeaponBat = true;
                        break;
                    }
                }
            }

            if (!isCurrentWeaponBat)
            {
                Destroy(bat2);
            }
            else
            {
                Debug.Log($"Skipping destruction of current weapon bat: {bat2.name}");
            }
        }
        if (bat2Objects.Length > 0)
        {
            Debug.Log($"Cleaned up Bat2 objects during weapon switch (excluding current weapon)");
        }

        // Clean up any ThrowSlash objects that might be left in the scene
        GameObject[] throwSlashObjects = GameObject.FindObjectsOfType<GameObject>()
            .Where(go => go.name.Contains("ThrowSlash"))
            .ToArray();
        foreach (GameObject throwSlash in throwSlashObjects)
        {
            Destroy(throwSlash);
        }
        if (throwSlashObjects.Length > 0)
        {
            Debug.Log($"Cleaned up {throwSlashObjects.Length} ThrowSlash objects during weapon switch");
        }
    }


    /// <summary>
    /// Public method to get the current weapon name for debugging
    /// </summary>
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
