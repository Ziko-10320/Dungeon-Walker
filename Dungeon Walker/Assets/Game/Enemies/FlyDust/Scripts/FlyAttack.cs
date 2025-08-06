using UnityEngine;
using System.Collections;

public class FlyAttack : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public GameObject dustProjectilePrefab;
    public Transform projectileSpawnPoint;
    public GameObject dustExplosionEffect;

    [Header("Attack Settings")]
    public float attackIntervalMin = 2f;
    public float attackIntervalMax = 5f;

    [Header("Player Targeting")]
    public Vector2 playerTargetOffset = Vector2.zero;

    [Header("Projectile Settings")]
    public float projectileSpeed = 10f;
    public int projectileDamage = 10;
    public float projectileLifetime = 3f;
    public LayerMask groundLayer;
    public LayerMask playerLayer;
    public float playerKnockbackForce = 5f;
    public Vector2 playerKnockbackDirection = Vector2.up;

    [Header("Explosion Settings")]
    public float explosionRadius = 2f;
    public int explosionDamage = 15;

    [Header("Double Attack Settings")]
    [Range(0f, 1f)] public float doubleAttackChance = 0.3f;
    public float delayBetweenProjectiles = 0.2f;
    public Vector2 secondProjectileOffset = new Vector2(0f, 1f);

    [Header("Attack Sound Settings")]
    public AudioClip attackSoundClip;
    [Range(0f, 1f)] public float attackSoundVolume = 0.7f; // Volume slider added here

    private AudioSource audioSource;
    private Animator animator;
    private float nextAttackTime;
    private Vector2 lastKnownPlayerPosition;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = attackSoundVolume; // Set initial volume
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("FlyAttack: Animator not found on this GameObject. Please add one.");
            enabled = false;
        }
      
        if (dustProjectilePrefab == null)
        {
            Debug.LogError("FlyAttack: Dust Projectile Prefab not assigned. Please assign the DustProjectile prefab in the Inspector.");
            enabled = false;
        }
        if (projectileSpawnPoint == null)
        {
            Debug.LogError("FlyAttack: Projectile Spawn Point not assigned. Please create an empty GameObject as a child and assign it.");
            enabled = false;
        }
        if (dustExplosionEffect == null)
        {
            Debug.LogWarning("FlyAttack: Dust Explosion Effect not assigned. No explosion effect will play on projectile destruction.");
        }
        SetNextAttackTime();
    }

    void Update()
    {
        if (playerTransform != null)
        {
            lastKnownPlayerPosition = (Vector2)playerTransform.position + playerTargetOffset;
        }
        if (Time.time >= nextAttackTime)
        {
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
            SetNextAttackTime();
        }
    }

    public void ThrowDust()
    {
        Debug.Log("ThrowDust method called.");
        if (dustProjectilePrefab == null || projectileSpawnPoint == null || playerTransform == null)
        {
            Debug.LogError("FlyAttack: Missing references for ThrowDust.");
            return;
        }

        // Play attack sound with adjustable volume
        if (attackSoundClip != null && audioSource != null)
        {
            audioSource.volume = attackSoundVolume;
            audioSource.PlayOneShot(attackSoundClip);
        }

        StartCoroutine(ThrowDustCoroutine());
    }

    private IEnumerator ThrowDustCoroutine()
    {
        InstantiateAndInitializeProjectile(lastKnownPlayerPosition);
        if (Random.value < doubleAttackChance)
        {
            yield return new WaitForSeconds(delayBetweenProjectiles);
            InstantiateAndInitializeProjectile(lastKnownPlayerPosition + secondProjectileOffset);
        }
    }

    private void InstantiateAndInitializeProjectile(Vector2 targetPlayerPosition)
    {
        Vector2 predictedPlayerPosition = targetPlayerPosition;
        Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            float distanceToPlayer = Vector2.Distance(projectileSpawnPoint.position, targetPlayerPosition);
            float timeToReachPlayer = distanceToPlayer / projectileSpeed;
            predictedPlayerPosition += playerRb.velocity * timeToReachPlayer;
        }

        GameObject dust = Instantiate(dustProjectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
        Rigidbody2D dustRb = dust.GetComponent<Rigidbody2D>();
        if (dustRb == null)
        {
            dustRb = dust.AddComponent<Rigidbody2D>();
            dustRb.gravityScale = 0;
            dustRb.isKinematic = true;
        }

        Collider2D dustCollider = dust.GetComponent<Collider2D>();
        if (dustCollider == null)
        {
            CapsuleCollider2D capsule = dust.AddComponent<CapsuleCollider2D>();
            capsule.isTrigger = true;
        }
        else
        {
            dustCollider.isTrigger = true;
        }

        ProjectileController projectileController = dust.AddComponent<ProjectileController>();
        projectileController.InitializeProjectile(
            predictedPlayerPosition,
            projectileSpeed,
            projectileDamage,
            projectileLifetime,
            groundLayer,
            playerLayer,
            dustExplosionEffect,
            playerKnockbackForce,
            playerKnockbackDirection,
            explosionRadius,
            explosionDamage
        );
    }

    void SetNextAttackTime()
    {
        nextAttackTime = Time.time + Random.Range(attackIntervalMin, attackIntervalMax);
    }

    // Internal class to manage projectile behavior
    public class ProjectileController : MonoBehaviour
    {
        private float speed;
        private int damage;
        private float lifetime;
        private LayerMask groundLayer;
        private LayerMask playerLayer;
        private GameObject explosionEffect;
        private float knockbackForce;
        private Vector2 knockbackDirection;
        private float explosionRadius;
        private int explosionDamage;
        private Vector2 targetPosition;
        private Rigidbody2D rb;
        private bool hasBeenDestroyed = false;

        public void InitializeProjectile(
            Vector2 targetPos,
            float projSpeed,
            int projDamage,
            float projLifetime,
            LayerMask gLayer,
            LayerMask pLayer,
            GameObject expEffect,
            float kbForce,
            Vector2 kbDirection,
            float expRadius,
            int expDamage)
        {
            targetPosition = targetPos;
            speed = projSpeed;
            damage = projDamage;
            lifetime = projLifetime;
            groundLayer = gLayer;
            playerLayer = pLayer;
            explosionEffect = expEffect;
            knockbackForce = kbForce;
            knockbackDirection = kbDirection;
            explosionRadius = expRadius;
            explosionDamage = expDamage;
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError("ProjectileController: Rigidbody2D not found.");
                enabled = false;
                return;
            }

            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            rb.velocity = direction * speed;
            StartCoroutine(LifetimeCountdown());
        }

        private IEnumerator LifetimeCountdown()
        {
            yield return new WaitForSeconds(lifetime);
            if (!hasBeenDestroyed)
            {
                DestroyProjectile(transform.position, true);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (hasBeenDestroyed) return;

            if (((1 << other.gameObject.layer) & groundLayer) != 0)
            {
                DestroyProjectile(transform.position, true);
            }
            else if (((1 << other.gameObject.layer) & playerLayer) != 0)
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage, knockbackForce, knockbackDirection);
                }
                else
                {
                    Debug.LogWarning("PlayerHealth not found on player object.");
                }
                DestroyProjectile(transform.position, false);
            }
        }

        public void DestroyProjectile(Vector2 explosionPosition, bool triggerExplosion)
        {
            if (hasBeenDestroyed) return;
            hasBeenDestroyed = true;

            if (triggerExplosion)
            {
                Collider2D[] hitColliders = Physics2D.OverlapCircleAll(explosionPosition, explosionRadius, playerLayer);
                foreach (Collider2D hitCollider in hitColliders)
                {
                    PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(explosionDamage, knockbackForce, knockbackDirection);
                    }
                }
            }

            if (explosionEffect != null)
            {
                GameObject explosionInstance = Instantiate(explosionEffect, explosionPosition, Quaternion.identity);
                ParticleSystem ps = explosionInstance.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                    Destroy(explosionInstance, ps.main.duration + ps.main.startLifetime.constantMax + 0.1f);
                }
                else
                {
                    Destroy(explosionInstance, 3f);
                }
            }

            Destroy(gameObject);
        }

        void OnDestroy()
        {
            hasBeenDestroyed = true;
        }
    }
}