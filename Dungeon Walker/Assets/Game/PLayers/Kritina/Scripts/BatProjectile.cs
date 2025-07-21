using UnityEngine;

public class BatProjectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 20;
    public LayerMask enemyLayers;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("BatProjectile requires a Rigidbody2D component.");
        }
    }

    public void Initialize(Vector2 direction, float throwSpeed, int batDamage, LayerMask enemies, LayerMask ground)
    {
        speed = throwSpeed;
        damage = batDamage;
        enemyLayers = enemies;
        groundLayer = ground;
        rb.velocity = direction.normalized * speed;
        hasHit = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // Check if it hit an enemy
        if (((1 << other.gameObject.layer) & enemyLayers) != 0)
        {
            // Apply damage to enemy (similar to ApplyDamage in BatAttackSystem)
            // You'll need to adapt this part based on your enemy health scripts
            Debug.Log($"Bat hit enemy: {other.name}");
            // Example: other.GetComponent<EnemyHealth>()?.TakeDamage(damage);
            StopBat();
        }
        // Check if it hit the ground
        else if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            Debug.Log("Bat hit ground.");
            StopBat();
        }
    }

    void StopBat()
    {
        hasHit = true;
        rb.velocity = Vector2.zero;
        rb.isKinematic = true; // Stop physics movement
        // Optionally, disable collider or set trigger to allow player to pick up
        // GetComponent<Collider2D>().isTrigger = true; // Or adjust as needed for pickup
    }

    // Call this from BatAttackSystem when player picks up the bat
    public void PickUp()
    {
        Destroy(gameObject);
    }
}
