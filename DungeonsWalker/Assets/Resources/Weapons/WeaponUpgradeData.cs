using UnityEngine;

// --- First, define the collapsible stat blocks ---

[System.Serializable] // This makes it show up in the Inspector
public class BatStats
{
    public float meleeAnticipation;
    public int meleeDamage;
    public float throwSpeed;
    public int throwDamage;
}

[System.Serializable]
public class BowStats
{
    public float bowChargeTime;
    public float bowMinDamage;
    public float bowMaxDamage;
}
[System.Serializable]
public class PistolStats
{
    public int pistolDamage;
    public float pistolFireRate;
    public float pistolBulletSpeed;
    public int pistolAmmoCapacity;
    public float pistolReloadSpeed;
}

[System.Serializable]
public class LauncherStats
{
    public int launcherDamage;
    public float launcherFireRate; // This will affect shootCooldown
    public float launcherRadius;
}


// --- Now, the main WeaponUpgradeData class uses these blocks ---

[System.Serializable]
public class WeaponUpgradeData
{
    public int upgradeCost;
    public PistolStats pistolStats;

    // By using these classes, the stats will be in collapsible groups!
    public BatStats batStats;
    public BowStats bowStats;
    public LauncherStats launcherStats;
}
