using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

    public List<WeaponConfig> weaponConfigs;
    public int killsToSwitch = 5;

    private int currentKills = 0;
    private WeaponConfig currentWeapon;
    private SpriteRenderer currentArmSpriteRenderer; // To track the currently active arm SpriteRenderer

    void Start()
    {
        InitializeWeapons();
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
                    if (go != null)
                    {
                        go.SetActive(false);
                    }
                }
            }
            if (config.weaponScripts != null)
            {
                foreach (var script in config.weaponScripts)
                {
                    if (script != null)
                    {
                        script.enabled = false;
                    }
                }
            }
            // Ensure the arm GameObject is activated if the user wants it, but disable its SpriteRenderer
            if (config.armGameObject != null)
            {
                // For consistent management, we disable the arm GameObject as well
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
        Debug.Log($"Kill count: {currentKills}/{killsToSwitch}");

        if (currentKills >= killsToSwitch)
        {
            SwitchWeapon();
            currentKills = 0;
        }
    }

    void SwitchWeapon()
    {
        Debug.Log($"Switching from weapon: {currentWeapon?.weaponName}");

        // Deactivate current weapon with enhanced cleanup
        DeactivateWeapon(currentWeapon);

        WeaponConfig newWeapon = GetRandomWeapon();
        // Prevent selecting the same weapon twice in a row
        while (newWeapon == currentWeapon && weaponConfigs.Count > 1)
        {
            newWeapon = GetRandomWeapon();
        }

        currentWeapon = newWeapon;
        ActivateWeapon(currentWeapon);

        Debug.Log($"Switched to weapon: {currentWeapon.weaponName}");
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

    /// <summary>
    /// Clean up any remaining weapon artifacts like ghost objects, projectiles, etc.
    /// </summary>
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
        GameObject[] bat2Objects = GameObject.FindObjectsOfType<GameObject>()
            .Where(go => go.name.Contains("Bat2") && go.GetComponent<Rigidbody2D>() != null)
            .ToArray();
        foreach (GameObject bat2 in bat2Objects)
        {
            Destroy(bat2);
        }
        if (bat2Objects.Length > 0)
        {
            Debug.Log($"Cleaned up {bat2Objects.Length} Bat2 objects during weapon switch");
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
        SwitchWeapon();
        currentKills = 0;
    }
}
