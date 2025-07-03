using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehavior : MonoBehaviour
{
    // --- NEW: Added a damage variable ---
    [SerializeField] private int bulletDamage = 10; // How much damage the bullet deals

    [SerializeField] private float BulletSpeed = 15f;
    [SerializeField] private float destroyTime = 3f;
    [SerializeField] private LayerMask collisionLayers; // Your original comment in Arabic
    [SerializeField] private GameObject waterExplosionParticleSystem; // Your original comment in Arabic

    private Rigidbody2D rb;
    private Vector2 moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        SetDestroyTime();
        SetStraightVelocity();
    }

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void SetStraightVelocity()
    {
        rb.velocity = moveDirection * BulletSpeed;
    }

    private void SetDestroyTime()
    {
        Destroy(gameObject, destroyTime);
    }

    // Your original comment in Arabic
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Your original comment in Arabic
        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            // --- THIS IS THE NEW DAMAGE LOGIC ---
            // Try to get the FleaHealth component from the object we hit
            FleaHealth enemyHealth = other.GetComponent<FleaHealth>();

            // If the enemyHealth component exists, deal damage to it
            if (enemyHealth != null)
            {
                // Call the TakeDamage method on the enemy's health script
                // We pass the bullet's damage and its direction for knockback
                enemyHealth.TakeDamage(bulletDamage, moveDirection);
            }
            // --- END OF NEW DAMAGE LOGIC ---

            // Your original comment in Arabic
            // Your original comment in Arabic
            Destroy(gameObject);
        }
    }

    // Your original comment in Arabic
    // Your original comment in Arabic
    private void OnDestroy()
    {
        // Your original comment in Arabic
        if (waterExplosionParticleSystem != null)
        {
            // Your original comment in Arabic
            // Your original comment in Arabic
            Instantiate(waterExplosionParticleSystem, transform.position, Quaternion.identity);
        }
    }

    // Your original comment in Arabic

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & collisionLayers) != 0)
        {
            // --- THIS IS THE NEW DAMAGE LOGIC (for non-trigger colliders) ---
            FleaHealth enemyHealth = collision.gameObject.GetComponent<FleaHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(bulletDamage, moveDirection);
            }
            // --- END OF NEW DAMAGE LOGIC ---

            // Your original comment in Arabic
            // Your original comment in Arabic
            Destroy(gameObject);
        }
    }
}
