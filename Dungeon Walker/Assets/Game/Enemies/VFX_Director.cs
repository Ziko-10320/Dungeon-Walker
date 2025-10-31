// VFX_Director.cs (Final, Resilient Version)

using UnityEngine;
using System.Collections.Generic;

// This class does not need to be modified.
[System.Serializable]
public class VFX_Entry
{
    public string effectName;
    public GameObject effectPrefab;
}

public class VFX_Director : MonoBehaviour
{
    public static VFX_Director Instance { get; private set; }

    [Header("VFX Library")]
    public List<VFX_Entry> vfxLibrary;

    [Header("Pooling Settings")]
    public int defaultPoolSize = 10;

    // We no longer initialize the dictionary in Awake.
    private Dictionary<string, GameObject> vfxDictionary;

    // --- NEW ---
    // A flag to know if we have successfully created our pools.
    private bool arePoolsInitialized = false;

    void Awake()
    {
        // Standard Singleton setup. This part is correct.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // This is the "lazy initializer". It creates the pools if they don't exist.
    private void EnsurePoolsAreInitialized()
    {
        // If we've already initialized, or if the ObjectPoolManager doesn't exist yet, do nothing.
        if (arePoolsInitialized || ObjectPoolManager.Instance == null)
        {
            return;
        }

        // --- Run the setup logic ---
        vfxDictionary = new Dictionary<string, GameObject>();
        Debug.Log("--- VFX Director: ObjectPoolManager found. Initializing VFX pools... ---");

        foreach (VFX_Entry entry in vfxLibrary)
        {
            if (entry.effectPrefab == null || string.IsNullOrEmpty(entry.effectName)) continue;

            vfxDictionary[entry.effectName] = entry.effectPrefab;
            ObjectPoolManager.Instance.CreatePool(entry.effectPrefab, defaultPoolSize);
        }

        Debug.Log($"--- VFX Director is ready. {vfxDictionary.Count} pools created. ---");

        // --- Set the flag to true so we don't do this again unless needed. ---
        arePoolsInitialized = true;
    }

    // --- We add one new public method to be called when you restart your game ---
    public void ResetInitializationFlag()
    {
        Debug.Log("VFX Director: Initialization flag has been reset. Will re-initialize pools on next PlayEffect call.");
        arePoolsInitialized = false;
    }

    // --- THE MASTER PLAY FUNCTION (Now with smart checking) ---
    public void PlayEffect(string effectName, Vector3 position)
    {
        // --- THIS IS THE MAGIC ---
        // Before we do anything, we make sure the pools are ready.
        // If they aren't, this method will create them.
        EnsurePoolsAreInitialized();

        // The rest of the function is the same, but now it's guaranteed to work.
        if (ObjectPoolManager.Instance == null || string.IsNullOrEmpty(effectName) || vfxDictionary == null) return;

        if (vfxDictionary.TryGetValue(effectName, out GameObject effectPrefab))
        {
            ObjectPoolManager.Instance.SpawnFromPool(effectPrefab, position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"VFX Director: Tried to play an effect named '{effectName}', but it was not found in the library.");
        }
    }
}
