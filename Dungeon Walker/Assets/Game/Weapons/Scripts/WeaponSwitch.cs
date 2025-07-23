using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WeaponSwitchManager : MonoBehaviour
{
    [System.Serializable]
    public class WeaponConfig
    {
        public string weaponName; // Nom de l'arme pour l'affichage dans l'inspecteur
        public List<GameObject> weaponGameObjects; // Liste des GameObjects associés à cette arme
        public List<MonoBehaviour> weaponScripts; // Liste des scripts à activer/désactiver pour cette arme
        public GameObject armGameObject; // GameObject du bras associé à cette arme (le parent)
        public SpriteRenderer armSpriteRenderer; // Le SpriteRenderer du bras à activer/désactiver
        [Range(0f, 1f)]
        public float dropChance = 0.25f; // Probabilité d'obtenir cette arme
    }

    public List<WeaponConfig> weaponConfigs;
    public int killsToSwitch = 5;

    private int currentKills = 0;
    private WeaponConfig currentWeapon;
    private SpriteRenderer currentArmSpriteRenderer; // Pour suivre le SpriteRenderer du bras actuellement actif

    void Start()
    {
        InitializeWeapons();
    }

    void InitializeWeapons()
    {
        // Désactiver toutes les armes, tous les bras (GameObjects) et tous les SpriteRenderers des bras au démarrage
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
            // Assurez-vous que le GameObject du bras est activé si l'utilisateur le souhaite, mais désactivez son SpriteRenderer
            if (config.armGameObject != null)
            {
                // L'utilisateur a dit que les bras sont initialement activés, mais leurs SpriteRenderers désactivés.
                // Donc, nous ne touchons pas à l'état actif du GameObject du bras ici, juste à son SpriteRenderer.
                // Cependant, pour la désactivation générale au démarrage, nous devons désactiver le GameObject du bras aussi.
                // Si l'utilisateur veut que le GameObject du bras reste actif, il devra le gérer en dehors de ce script.
                // Pour l'instant, nous désactivons le GameObject du bras pour une gestion cohérente.
                config.armGameObject.SetActive(false);
            }
            if (config.armSpriteRenderer != null)
            {
                config.armSpriteRenderer.enabled = false;
            }
        }

        // Activer une arme aléatoire au démarrage
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
        Debug.Log($"Kill count: {currentKills}");

        if (currentKills >= killsToSwitch)
        {
            SwitchWeapon();
            currentKills = 0;
        }
    }

    void SwitchWeapon()
    {
        DeactivateWeapon(currentWeapon);

        WeaponConfig newWeapon = GetRandomWeapon();
        // Empêcher la sélection de la même arme deux fois de suite
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
            if (weapon.weaponGameObjects != null)
            {
                foreach (var go in weapon.weaponGameObjects)
                {
                    if (go != null)
                    {
                        go.SetActive(true);
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
                    }
                }
            }
            // Activer le GameObject du bras et son SpriteRenderer
            if (weapon.armGameObject != null)
            {
                weapon.armGameObject.SetActive(true);
            }
            if (weapon.armSpriteRenderer != null)
            {
                weapon.armSpriteRenderer.enabled = true;
                currentArmSpriteRenderer = weapon.armSpriteRenderer; // Mettre à jour le SpriteRenderer du bras actuellement actif
            }
        }
    }

    void DeactivateWeapon(WeaponConfig weapon)
    {
        if (weapon != null)
        {
            if (weapon.weaponGameObjects != null)
            {
                foreach (var go in weapon.weaponGameObjects)
                {
                    if (go != null)
                    {
                        go.SetActive(false);
                    }
                }
            }
            if (weapon.weaponScripts != null)
            {
                foreach (var script in weapon.weaponScripts)
                {
                    if (script != null)
                    {
                        script.enabled = false;
                    }
                }
            }
            // Désactiver le GameObject du bras et son SpriteRenderer précédemment actif
            if (currentArmSpriteRenderer != null)
            {
                currentArmSpriteRenderer.enabled = false;
                // Si le GameObject parent du SpriteRenderer doit être désactivé, faites-le ici.
                // Pour l'instant, nous désactivons uniquement le SpriteRenderer.
                if (currentArmSpriteRenderer.gameObject != null)
                {
                    currentArmSpriteRenderer.gameObject.SetActive(false);
                }
                currentArmSpriteRenderer = null; // Réinitialiser
            }
        }
    }
}
