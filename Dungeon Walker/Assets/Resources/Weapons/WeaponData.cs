using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/WeaponData")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public Sprite weaponIcon;

    // This is the list of all 5 upgrade levels for this weapon
    public List<WeaponUpgradeData> upgradeLevels = new List<WeaponUpgradeData>(5);
}
