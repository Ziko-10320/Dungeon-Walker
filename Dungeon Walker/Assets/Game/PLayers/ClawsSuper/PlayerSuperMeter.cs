using UnityEngine;
using UnityEngine.Events;

public class PlayerSuperMeter : MonoBehaviour
{
    [Header("Super Settings")]
    [Tooltip("Damage needed to earn 1 super charge")]
    public int superDamageThreshold = 1000; // editable in Inspector

    [Header("Events")]
    public UnityEvent onSuperReady;
    public UnityEvent onSuperUsed;

    private int currentDamageDealt = 0;
    private bool hasCharge = false;

    /// <summary>
    /// Adds damage to the super meter.
    /// </summary>
    public void AddDamage(int amount)
    {
        if (hasCharge) return; // already full, wait for use

        currentDamageDealt += amount;
        if (currentDamageDealt >= superDamageThreshold)
        {
            hasCharge = true;
            currentDamageDealt = superDamageThreshold; // clamp
            Debug.Log("✅ Super Ready!");
            onSuperReady?.Invoke();
        }
    }

    /// <summary>
    /// Returns normalized fill (0 to 1).
    /// </summary>
    public float GetProgressNormalized()
    {
        if (hasCharge) return 1f;
        if (superDamageThreshold <= 0) return 0f;
        return Mathf.Clamp01((float)currentDamageDealt / superDamageThreshold);
    }

    public bool HasSuperCharge() => hasCharge;

    public void UseSuper()
    {
        if (!hasCharge) return;
        hasCharge = false;
        currentDamageDealt = 0;
        Debug.Log("🔥 Super used!");
        onSuperUsed?.Invoke();
    }

    public void ForceGiveSuper()
    {
        currentDamageDealt = superDamageThreshold; // instantly fill
        hasCharge = true;
        onSuperReady?.Invoke();
    }
}
