using UnityEngine;

public class DestructibleObject : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("The total health of the object. How many hits it can take.")]
    [SerializeField] private int maxHealth = 50;
    private int currentHealth;

    [Header("Destruction Effect")]
    [Tooltip("The particle effect prefab to spawn when the object is destroyed.")]
    [SerializeField] private GameObject destructionParticlesPrefab;
    

    [Header("Sound Effect")]
    [Tooltip("The sound to play when the object takes damage.")]
    [SerializeField] private AudioClip hitSound;
    [Tooltip("The sound to play when the object is destroyed.")]
    [SerializeField] private AudioClip destructionSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.8f;

    // This method is called automatically when the object is enabled (or at the start of the game).
    void OnEnable()
    {
        // Reset the health every time the object is spawned (important for object pooling).
        currentHealth = maxHealth;
    }

    // This is the main public method that other scripts (like player attacks) will call.
    public void TakeDamage(int damageAmount)
    {
        // If already destroyed, do nothing.
        if (currentHealth <= 0) return;

        // Reduce health.
        currentHealth -= damageAmount;
        Debug.Log($"{gameObject.name} took {damageAmount} damage. Health is now {currentHealth}.");

        // Check if the object is now destroyed.
        if (currentHealth <= 0)
        {
            DestroyObject();
        }
        else
        {
            // If not destroyed, just play the hit sound.
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position, soundVolume);
            }
        }
    }

    private void DestroyObject()
    {
        Debug.Log($"{gameObject.name} has been destroyed!");

        // --- SPAWN THE DESTRUCTION EFFECT ---
        // Determine the spawn position. Use the specific point if it exists, otherwise use the object's center.
        Vector3 spawnPosition = transform.position;

        if (destructionParticlesPrefab != null)
        {
            // Check if we are using an object pooler.
            if (ObjectPoolManager.Instance != null)
            {
                // If yes, spawn the effect from the pool.
                ObjectPoolManager.Instance.SpawnFromPool(destructionParticlesPrefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                // If no pooler, just instantiate it normally.
                Instantiate(destructionParticlesPrefab, spawnPosition, Quaternion.identity);
            }
        }

        // --- PLAY THE DESTRUCTION SOUND ---
        if (destructionSound != null)
        {
            AudioSource.PlayClipAtPoint(destructionSound, spawnPosition, soundVolume);
        }

        // --- FINALLY, HIDE OR DESTROY THE OBJECT ---
        // Using SetActive(false) is better than Destroy() because it allows for object pooling.
        gameObject.SetActive(false);
    }
}
