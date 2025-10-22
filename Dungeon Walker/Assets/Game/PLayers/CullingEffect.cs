using UnityEngine;
using System.Collections;

/// <summary>
/// A robust and optimized system to control the spawning of visual effects.
/// It only allows effects to spawn if they are within a specified radius of the player.
/// </summary>
public class EffectCullingSystem : MonoBehaviour
{
    // Singleton for easy access
    public static EffectCullingSystem Instance { get; private set; }

    [Header("Culling Settings")]
    [Tooltip("The radius of the circle. Effects outside this circle will not be spawned.")]
    [SerializeField] private float effectCullingRadius = 30f;
    [Tooltip("The central point for the culling radius.")]
    [SerializeField] private Transform effectOriginPoint;

    [Header("Managed Effects")]
    [Tooltip("A list of effect prefabs that this system should manage. Only these effects will be culled.")]
    [SerializeField] private GameObject[] managedEffectPrefabs;
    [SerializeField] private GameObject[] managedEnemyPrefabs; 

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Find the origin point at the start of the game.
        if (effectOriginPoint == null)
        {
            GameObject originObj = GameObject.FindGameObjectWithTag("Origin");
            if (originObj != null)
            {
                effectOriginPoint = originObj.transform;
            }
            else
            {
                Debug.LogWarning("EffectCullingSystem: Could not find a GameObject with the tag 'Origin'. Culling will be disabled.");
            }
        }
    }

    /// <summary>
    /// The main public method. Other scripts will call this to spawn an effect.
    /// It checks if the effect is managed and if it's in range before spawning.
    /// </summary>
    public GameObject SpawnEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation)
    {
        // Failsafe checks
        if (effectPrefab == null || ObjectPoolManager.Instance == null)
        {
            return null;
        }

        // --- CULLING LOGIC ---
        // 1. Check if this effect is one we are supposed to manage.
        bool isManaged = false;
        foreach (var managedPrefab in managedEffectPrefabs)
        {
            if (managedPrefab == effectPrefab)
            {
                isManaged = true;
                break;
            }
        }

        // 2. If it's not a managed effect, just spawn it normally.
        if (!isManaged)
        {
            return ObjectPoolManager.Instance.SpawnFromPool(effectPrefab, position, rotation);
        }

        // 3. If it IS a managed effect, check if it's in range.
        if (IsPositionInRadius(position))
        {
            // If in range, spawn it from the pool.
            return ObjectPoolManager.Instance.SpawnFromPool(effectPrefab, position, rotation);
        }

        // 4. If it's a managed effect but it's out of range, do nothing.
        return null;
    }

    public bool IsPositionInRadius(Vector3 position)
    {
        if (effectOriginPoint == null) return true; // Failsafe: if no origin, allow spawning.
        return (effectOriginPoint.position - position).sqrMagnitude <= effectCullingRadius * effectCullingRadius;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = (effectOriginPoint != null) ? effectOriginPoint.position : transform.position;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(center, effectCullingRadius);
    }
}
