using UnityEngine;
using Photon.Pun;

public class BulletBehavior : MonoBehaviour
{
    [Header("Bullet Stats")]
    [SerializeField] private int bulletDamage = 10;
    [SerializeField] private float bulletSpeed = 15f;
    [SerializeField] private float lifetime = 3f;

    [Header("Effects & Collision")]
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private GameObject waterExplosionPrefab;

    private Rigidbody2D rb;
    private PhotonView view;
    private bool hasHit = false;
    private Vector2 moveDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        view = GetComponent<PhotonView>();
        Destroy(gameObject, lifetime);
    }

    public void Initialize(Vector2 direction)
    {
        this.moveDirection = direction.normalized;
        if (rb != null)
        {
            rb.velocity = this.moveDirection * bulletSpeed;
        }
        float angle = Mathf.Atan2(this.moveDirection.y, this.moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // --- COLLISION DETECTION ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the layer we hit is in our collision mask.
        if ((collisionLayers.value & (1 << other.gameObject.layer)) > 0)
        {
            HandleImpact(other.gameObject, transform.position);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the layer we hit is in our collision mask.
        if ((collisionLayers.value & (1 << collision.gameObject.layer)) > 0)
        {
            HandleImpact(collision.gameObject, collision.contacts[0].point);
        }
    }

    // --- THIS IS THE NEW, SIMPLIFIED IMPACT LOGIC ---
    private void HandleImpact(GameObject hitObject, Vector3 impactPoint)
    {
        // If we've already processed a hit, do nothing.
        if (hasHit) return;
        hasHit = true;

        // --- STEP 1: Play the visual effect ---
        // This is safe for both online and offline.
        if (waterExplosionPrefab != null)
        {
            GameObject particleInstance = Instantiate(waterExplosionPrefab, impactPoint, Quaternion.identity);
            Destroy(particleInstance, 2f);
        }

        // --- STEP 2: Handle Damage and Destruction ---
        if (view != null) // --- ONLINE MODE ---
        {
            // The online logic is working, so we trust it.
            // Only the Master Client should deal damage and destroy the object.
            if (PhotonNetwork.IsMasterClient)
            {
                DealDamage(hitObject);
                PhotonNetwork.Destroy(gameObject);
            }
        }
        else // --- OFFLINE MODE (THE FIX) ---
        {
            // In single-player, we are the authority.
            // We deal damage AND destroy the bullet immediately.
            DealDamage(hitObject);
            Destroy(gameObject);
        }
    }

    // --- HELPER: Deals damage to a target ---
    private void DealDamage(GameObject target)
    {
        // This function is now guaranteed to be called correctly in both modes.
        FleaHealth enemyHealth = target.GetComponent<FleaHealth>();
        if (enemyHealth != null) enemyHealth.TakeDamage(bulletDamage, moveDirection);

        SprayerHealth sprayerHealth = target.GetComponent<SprayerHealth>();
        if (sprayerHealth != null) sprayerHealth.TakeDamage(bulletDamage, moveDirection);

        FlyHealth flyHealth = target.GetComponent<FlyHealth>();
        if (flyHealth != null) flyHealth.TakeDamage(bulletDamage, moveDirection);

        InkHealth inkHealth = target.GetComponent<InkHealth>();
        if (inkHealth != null) inkHealth.TakeDamage(bulletDamage, moveDirection);

        RatKingHealth ratKingHealth = target.GetComponent<RatKingHealth>();
        if (ratKingHealth != null) ratKingHealth.TakeDamage(bulletDamage);
    }
}
