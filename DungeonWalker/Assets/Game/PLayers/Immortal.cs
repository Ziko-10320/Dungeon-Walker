using UnityEngine;

public class Immortal : MonoBehaviour
{
    // This is the static "slot" for our one true instance.
    // 'static' means this variable belongs to the CLASS, not to any one object.
    public static Immortal Instance { get; private set; }

    // This function is called by Unity as soon as the object is created,
    // even before Start().
    void Awake()
    {
        // --- THE SINGLETON LOGIC ---

        // 1. Check if the 'Instance' slot is already filled.
        if (Instance != null && Instance != this)
        {
            // If it is, it means another "Immortal" object already exists.
            // This current object must be a duplicate.
            Debug.LogWarning("A duplicate instance of the Immortal object was found. Destroying the duplicate.");

            // Destroy this duplicate GameObject immediately.
            Destroy(gameObject);
        }
        else
        {
            // If the 'Instance' slot is empty, it means we are the FIRST one.

            // 2. We claim the slot for ourselves.
            Instance = this;

            // 3. We make ourselves permanent. This is the magic command.
            // This object will now survive scene loads.
            DontDestroyOnLoad(gameObject);
        }
    }
}
