using UnityEngine;
using UnityEngine.Events;

public class L3antixSuperMeter : MonoBehaviour
{
    [Header("Super Settings")]
    public int superDamageThreshold = 1000;

    [Header("Events")]
    public UnityEvent onSuperReady;
    public UnityEvent onSuperUsed;

    private int currentDamageDealt = 0;
    private bool hasCharge = false;
    public static L3antixSuperMeter Instance;
    void Awake()
    {
        Instance = this;
    }
    public void AddDamage(int amount)
    {
        if (hasCharge) return;

        currentDamageDealt += amount;
        if (currentDamageDealt >= superDamageThreshold)
        {
            hasCharge = true;
            currentDamageDealt = superDamageThreshold;
            Debug.Log("✅ 3antix Super Ready!");
            onSuperReady?.Invoke();
        }
    }

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
        Debug.Log("🔥 3antix Super used!");
        onSuperUsed?.Invoke();
    }
}
