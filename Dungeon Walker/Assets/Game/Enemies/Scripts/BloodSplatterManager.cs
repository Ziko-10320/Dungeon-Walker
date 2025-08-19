using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public enum SplatterType
{
    Hit,
    Death
}

[System.Serializable]
public class SplatterSettings
{
    public List<GameObject> prefabs; // Now a list to assign multiple splatters
    public float initialDisplayDuration = 0f; // For hit splatters, how long it stays fully visible
    public float fadeDuration = 2f;
    public float lifetime = 10f; // Total lifetime before destruction if no fade
    public bool overrideSize = false;
    public float minSize = 0.5f;
    public float maxSize = 1.0f;
    public GameObject specificDeathSplatterPrefab; // Dedicated slot for specific death splatter
    public int specificDeathSplatterLimit = 2; // Limit for specific death splatters
}

[System.Serializable]
public class EnemySplatterConfig
{
    public string enemyTag; // Tag to identify the enemy type
    public SplatterSettings hitSplatterSettings; // Changed to single SplatterSettings for hit
    public SplatterSettings deathSplatterSettings; // Specific setting for death splatter
}

public class BloodSplatterManager : MonoBehaviour
{
    public static BloodSplatterManager Instance { get; private set; }

    [Header("Global Splatter Settings")]
    public int minOrderInLayer = 4;
    public int maxOrderInLayer = 7;
    public bool randomRotation = true;
    public bool randomFlipX = true;
    public bool randomFlipY = false;

    [Header("Enemy Specific Splatter Configurations")]
    public List<EnemySplatterConfig> enemySplatterConfigs;

    private Dictionary<string, List<GameObject>> activeSpecificDeathSplatterPools = new Dictionary<string, List<GameObject>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CreateSplatter(Vector2 position, Quaternion rotation, SplatterType type, string enemyTag = "Default")
    {
        EnemySplatterConfig config = GetEnemySplatterConfig(enemyTag);
        if (config == null)
        {
            Debug.LogWarning($"No splatter configuration found for enemy tag: {enemyTag}. Using default settings if available.");
            return;
        }

        SplatterSettings currentSettings = null;
        GameObject selectedPrefab = null;

        if (type == SplatterType.Hit)
        {
            currentSettings = config.hitSplatterSettings;
            if (currentSettings != null && currentSettings.prefabs != null && currentSettings.prefabs.Count > 0)
            {
                selectedPrefab = currentSettings.prefabs[Random.Range(0, currentSettings.prefabs.Count)];
            }
        }
        else if (type == SplatterType.Death)
        {
            currentSettings = config.deathSplatterSettings;
            if (currentSettings != null)
            {
                if (currentSettings.specificDeathSplatterPrefab != null)
                {
                    selectedPrefab = currentSettings.specificDeathSplatterPrefab;

                    // Manage specific death splatter limit
                    if (!activeSpecificDeathSplatterPools.ContainsKey(enemyTag))
                    {
                        activeSpecificDeathSplatterPools[enemyTag] = new List<GameObject>();
                    }

                    List<GameObject> splatterPool = activeSpecificDeathSplatterPools[enemyTag];
                    splatterPool.RemoveAll(item => item == null); // Clean up null references

                    if (splatterPool.Count >= currentSettings.specificDeathSplatterLimit)
                    {
                        // Destroy the oldest specific death splatter to make room for a new one
                        if (splatterPool[0] != null) Destroy(splatterPool[0]);
                        splatterPool.RemoveAt(0);
                    }
                }
                else if (currentSettings.prefabs != null && currentSettings.prefabs.Count > 0)
                {
                    selectedPrefab = currentSettings.prefabs[Random.Range(0, currentSettings.prefabs.Count)];
                }
            }
        }

        if (selectedPrefab != null && currentSettings != null)
        {
            GameObject splatterGO = Instantiate(selectedPrefab, position, rotation);
            SpriteRenderer sr = splatterGO.GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                sr.sortingOrder = Random.Range(minOrderInLayer, maxOrderInLayer + 1);

                if (randomFlipX && Random.value < 0.5f)
                {
                    sr.flipX = true;
                }
                if (randomFlipY && Random.value < 0.5f)
                {
                    sr.flipY = true;
                }
            }

            if (randomRotation)
            {
                splatterGO.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
            }

            float randomSize = currentSettings.overrideSize ? Random.Range(currentSettings.minSize, currentSettings.maxSize) : 1f;
            splatterGO.transform.localScale = new Vector3(randomSize, randomSize, 1f);

            // Start the fading coroutine directly from the manager
            StartCoroutine(FadeOutAndDestroy(splatterGO, sr, currentSettings.initialDisplayDuration, currentSettings.fadeDuration, currentSettings.lifetime));

            if (type == SplatterType.Death && currentSettings.specificDeathSplatterPrefab != null)
            {
                activeSpecificDeathSplatterPools[enemyTag].Add(splatterGO);
            }
        }
        else
        {
            Debug.LogWarning($"No prefab or settings assigned for {type} splatter type for enemy tag: {enemyTag}.");
        }
    }

    private IEnumerator FadeOutAndDestroy(GameObject splatterGO, SpriteRenderer sr, float initialDisplayDuration, float fadeDuration, float totalLifetime)
    {
        // Ensure the GameObject and SpriteRenderer still exist before proceeding
        if (splatterGO == null || sr == null)
        {
            yield break;
        }

        // Wait for initial display duration
        yield return new WaitForSeconds(initialDisplayDuration);

        // Ensure the GameObject and SpriteRenderer still exist before fading
        if (splatterGO == null || sr == null)
        {
            yield break;
        }

        // Start fading
        float timer = 0f;
        Color startColor = sr.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (splatterGO == null || sr == null) yield break; // Check again inside loop
            sr.color = Color.Lerp(startColor, endColor, timer / fadeDuration);
            yield return null;
        }
        if (splatterGO != null && sr != null) sr.color = endColor; // Ensure it\"s fully transparent

        // Destroy after total lifetime (or immediately if fadeDuration + initialDisplayDuration >= totalLifetime)
        float remainingLifetime = totalLifetime - (initialDisplayDuration + fadeDuration);
        if (remainingLifetime > 0)
        {
            yield return new WaitForSeconds(remainingLifetime);
        }

        if (splatterGO != null) Destroy(splatterGO);
    }

    private EnemySplatterConfig GetEnemySplatterConfig(string enemyTag)
    {
        foreach (var config in enemySplatterConfigs)
        {
            if (config.enemyTag == enemyTag)
            {
                return config;
            }
        }
        return null; // Or return a default config
    }
}




