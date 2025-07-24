using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A robust singleton system for handling enemy kill notifications.
/// This ensures that weapon switching works reliably regardless of script execution order or timing issues.
/// </summary>
public class EnemyKillNotifier : MonoBehaviour
{
    private static EnemyKillNotifier instance;
    private static readonly object lockObject = new object();

    [Header("Debug Settings")]
    public bool enableDebugLogs = true;

    // List of all registered WeaponSwitchManagers
    private List<WeaponSwitchManager> registeredManagers = new List<WeaponSwitchManager>();

    // Singleton instance
    public static EnemyKillNotifier Instance
    {
        get
        {
            if (instance == null)
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        // Try to find existing instance
                        instance = FindObjectOfType<EnemyKillNotifier>();

                        if (instance == null)
                        {
                            // Create new instance
                            GameObject go = new GameObject("EnemyKillNotifier");
                            instance = go.AddComponent<EnemyKillNotifier>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        // Ensure singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            if (enableDebugLogs)
                Debug.Log("EnemyKillNotifier singleton created and set to persist across scenes.");
        }
        else if (instance != this)
        {
            if (enableDebugLogs)
                Debug.Log("Duplicate EnemyKillNotifier found, destroying duplicate.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Register a WeaponSwitchManager to receive kill notifications
    /// </summary>
    /// <param name="manager">The WeaponSwitchManager to register</param>
    public void RegisterWeaponSwitchManager(WeaponSwitchManager manager)
    {
        if (manager != null && !registeredManagers.Contains(manager))
        {
            registeredManagers.Add(manager);
            if (enableDebugLogs)
                Debug.Log($"WeaponSwitchManager registered with EnemyKillNotifier. Total registered: {registeredManagers.Count}");
        }
    }

    /// <summary>
    /// Unregister a WeaponSwitchManager from receiving kill notifications
    /// </summary>
    /// <param name="manager">The WeaponSwitchManager to unregister</param>
    public void UnregisterWeaponSwitchManager(WeaponSwitchManager manager)
    {
        if (manager != null && registeredManagers.Contains(manager))
        {
            registeredManagers.Remove(manager);
            if (enableDebugLogs)
                Debug.Log($"WeaponSwitchManager unregistered from EnemyKillNotifier. Total registered: {registeredManagers.Count}");
        }
    }

    /// <summary>
    /// Notify all registered WeaponSwitchManagers that an enemy was killed
    /// </summary>
    /// <param name="enemyName">Name of the enemy that was killed (for debugging)</param>
    /// <param name="enemyType">Type of enemy that was killed (for debugging)</param>
    public void NotifyEnemyKilled(string enemyName = "Unknown", string enemyType = "Unknown")
    {
        if (enableDebugLogs)
            Debug.Log($"EnemyKillNotifier: {enemyType} enemy \'{enemyName}\' was killed. Notifying {registeredManagers.Count} registered managers.");

        // Clean up any null references first
        registeredManagers.RemoveAll(manager => manager == null);

        // If no managers are registered, try to find one automatically
        if (registeredManagers.Count == 0)
        {
            WeaponSwitchManager foundManager = FindObjectOfType<WeaponSwitchManager>();
            if (foundManager != null)
            {
                RegisterWeaponSwitchManager(foundManager);
                if (enableDebugLogs)
                    Debug.Log("No registered managers found, but automatically found and registered a WeaponSwitchManager in the scene.");
            }
        }

        // Notify all registered managers
        int successfulNotifications = 0;
        foreach (var manager in registeredManagers)
        {
            if (manager != null)
            {
                try
                {
                    manager.OnEnemyKilled();
                    successfulNotifications++;
                    if (enableDebugLogs)
                        Debug.Log($"Successfully notified WeaponSwitchManager: {manager.name}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error notifying WeaponSwitchManager {manager.name}: {e.Message}");
                }
            }
        }

        if (enableDebugLogs)
            Debug.Log($"EnemyKillNotifier: Successfully notified {successfulNotifications} out of {registeredManagers.Count} managers.");

        // If no successful notifications, log a warning
        if (successfulNotifications == 0)
        {
            Debug.LogWarning("EnemyKillNotifier: No WeaponSwitchManagers were successfully notified! Weapon switching may not work.");
        }
    }

    /// <summary>
    /// Get the number of registered WeaponSwitchManagers
    /// </summary>
    /// <returns>Number of registered managers</returns>
    public int GetRegisteredManagerCount()
    {
        // Clean up null references
        registeredManagers.RemoveAll(manager => manager == null);
        return registeredManagers.Count;
    }

    /// <summary>
    /// Force cleanup of null references in the registered managers list
    /// </summary>
    public void CleanupNullReferences()
    {
        int beforeCount = registeredManagers.Count;
        registeredManagers.RemoveAll(manager => manager == null);
        int afterCount = registeredManagers.Count;

        if (enableDebugLogs && beforeCount != afterCount)
            Debug.Log($"EnemyKillNotifier: Cleaned up {beforeCount - afterCount} null references. Active managers: {afterCount}");
    }
}

