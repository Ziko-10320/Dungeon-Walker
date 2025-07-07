using UnityEngine;

public class BallBehaviorSystem : MonoBehaviour, IProjectileBehavior
{
    [Header("Ball Settings")]
    [SerializeField] private float destroyTime = 5f; // Time after which the ball is destroyed
    [SerializeField] private LayerMask collisionLayers; // Layers the ball will collide with
    [SerializeField] private GameObject impactParticleSystem; // Particle system for impact (e.g., explosion)

    private Rigidbody2D rb;
    private Vector2 launchDirection; // Stored for potential use in damage calculation

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // This method is called by the LauncherSystem to launch the ball
    public void Launch(Vector2 direction, float speed)
    {
        launchDirection = direction.normalized; // Store normalized direction
        if (rb != null)
        {
            rb.velocity = launchDirection * speed;
        }
        else
        {
            Debug.LogError("BallBehavior: Rigidbody2D not found on " + gameObject.name + ". Cannot launch.", this);
        }

        // Start self-destruction timer
        Destroy(gameObject, destroyTime);
    }

    // Handles collision with other objects
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the collided object's layer is in our collisionLayers
        if (((1 << collision.gameObject.layer) & collisionLayers) != 0)
        {
            // Optional: Deal damage to enemy
            FleaHealth enemyHealth = collision.gameObject.GetComponent<FleaHealth>();
            if (enemyHealth != null)
            {
                // You'll need to define bulletDamage in BallBehavior or pass it from LauncherSystem
                // For now, let's use a placeholder damage value
                int ballDamage = 20; // Example damage
                enemyHealth.TakeDamage(ballDamage, launchDirection); // Pass stored launchDirection for knockback
            }

            // Play impact particle system
            if (impactParticleSystem != null)
            {
                Instantiate(impactParticleSystem, transform.position, Quaternion.identity);
            }

            // Destroy the ball on impact
            Destroy(gameObject);
        }
    }

    // Handles trigger collision (if your collider is set to Is Trigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            FleaHealth enemyHealth = other.GetComponent<FleaHealth>();
            if (enemyHealth != null)
            {
                int ballDamage = 20; // Example damage
                enemyHealth.TakeDamage(ballDamage, launchDirection);
            }

            if (impactParticleSystem != null)
            {
                Instantiate(impactParticleSystem, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }

    // OnDestroy is called when the GameObject is destroyed (either by timer or collision)
    private void OnDestroy()
    {
        // If you want impact particles to play even if destroyed by time,
        // ensure impactParticleSystem is instantiated here.
        // If impact particles should ONLY play on collision, remove this.
        // For now, it's only in OnCollisionEnter2D/OnTriggerEnter2D.
    }
}
