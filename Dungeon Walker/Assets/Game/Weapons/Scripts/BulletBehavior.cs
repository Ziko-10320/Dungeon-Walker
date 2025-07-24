using UnityEngine;

public class BulletBehavior : MonoBehaviour
{
    [Header("Bullet Stats")]
    [SerializeField] private int bulletDamage = 10;
    [SerializeField] private float bulletSpeed = 15f;
    [SerializeField] private float lifetime = 3f; // Durée de vie de la balle

    [Header("Effects & Collision")]
    [SerializeField] private LayerMask collisionLayers; // Layers qui déclenchent la collision
    [SerializeField] private GameObject waterExplosionPrefab; // Le PREFAB de ton effet de particule

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private bool hasHit = false; // Sécurité pour éviter les doubles exécutions

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // On lance le timer de destruction au cas où la balle ne touche rien
        Destroy(gameObject, lifetime);
    }

    // Méthode appelée par le WaterGunSystem pour donner la direction initiale
    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;

        // Applique la vélocité
        if (rb != null)
        {
            rb.velocity = moveDirection * bulletSpeed;
        }

        // Fait pivoter le sprite de la balle pour qu'il soit aligné avec sa direction
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // Gère les collisions avec des triggers
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            HandleImpact(other.gameObject, transform.position);
        }
    }

    // Gère les collisions physiques
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & collisionLayers) != 0)
        {
            HandleImpact(collision.gameObject, collision.contacts[0].point);
        }
    }

    /// <summary>
    /// Méthode centrale qui gère l'impact, les dégâts et la destruction.
    /// </summary>
    private void HandleImpact(GameObject hitObject, Vector2 impactPoint)
    {
        // Si on a déjà touché quelque chose, on sort pour éviter les bugs
        if (hasHit) return;
        hasHit = true;

        // --- Logique de dégâts ---
        FleaHealth enemyHealth = hitObject.GetComponent<FleaHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(bulletDamage, moveDirection);
        }

        SprayerHealth SprayerHealth = hitObject.GetComponent<SprayerHealth>();
        if (SprayerHealth != null)
        {
            SprayerHealth.TakeDamage(bulletDamage, moveDirection);
        }

        FlyHealth flyHealth = hitObject.GetComponent<FlyHealth>();
        if (flyHealth != null)
        {
            flyHealth.TakeDamage(bulletDamage, moveDirection);
        }

        InkHealth inkHealth = hitObject.GetComponent<InkHealth>();
        if (inkHealth != null)
        {
            inkHealth.TakeDamage(bulletDamage, moveDirection);
        }
        // --- LA LOGIQUE DE PARTICULE INSPIRÉE DU BOWSYSTEM ---

        // 1. On instancie la particule à l'endroit de l'impact
        if (waterExplosionPrefab != null)
        {
            // On crée une instance du prefab de particule
            GameObject particleInstance = Instantiate(waterExplosionPrefab, impactPoint, Quaternion.identity);

            // On récupère le composant ParticleSystem de cette nouvelle instance
            ParticleSystem ps = particleInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // On s'assure qu'elle joue
                ps.Play();

                // On détruit l'objet de la particule SEULEMENT après la fin de son animation
                // C'est la ligne la plus importante.
                Destroy(particleInstance, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                // Si le prefab n'a pas de ParticleSystem, on le détruit après un court délai par sécurité
                Destroy(particleInstance, 2f);
            }
        }

        // 2. On détruit la balle elle-même, maintenant que la particule est lancée et autonome
        Destroy(gameObject);
    }
}
