using System.Collections;
using UnityEngine;

public enum CoinType { Golden, Silver, Bronze }

public class ExplosiveCoinsPowerUp : MonoBehaviour
{
    [Header("Spawn / Chance")]
    [Range(0f, 1f)] public float spawnChance = 0.2f;
    public Transform spawnPoint;
    [Header("Audio")]
    [SerializeField] private AudioClip coinSpawnSound;
    [Range(0f, 1f)]
    [SerializeField] private float coinSpawnVolume = 1f;

    [SerializeField] private AudioClip goldenExplosionSound;
    [Range(0f, 1f)]
    [SerializeField] private float goldenExplosionVolume = 1f;

    [SerializeField] private AudioClip silverExplosionSound;
    [Range(0f, 1f)]
    [SerializeField] private float silverExplosionVolume = 1f;

    [SerializeField] private AudioClip bronzeExplosionSound;
    [Range(0f, 1f)]
    [SerializeField] private float bronzeExplosionVolume = 1f;
    [Header("Single launch force (controls diagonal launch)")]
    public float launchForce = 6f;
    public float splitForceMultiplier = 1.3f;

    [Header("Timing & explosion")]
    public float splitDelay = 0.25f;
    public float explosionDuration = 1f;
    public float explosionRadius = 2f;
    public float damageTickInterval = 0.1f;

    [Header("Layers")]
    public LayerMask enemyLayer;
    public LayerMask playerLayer;
    public LayerMask groundLayer;

    [Header("Prefabs")]
    public GameObject goldenCoinPrefab;
    public GameObject silverCoinPrefab;
    public GameObject bronzeCoinPrefab;

    [Header("Damage values")]
    public float goldenDamage = 30f;
    public float silverDamage = 20f;
    public float bronzeDamage = 10f;

    [Header("Particles")]
    public ParticleSystem[] goldenExplosionParticles;
    public ParticleSystem[] silverExplosionParticles;
    public ParticleSystem[] bronzeExplosionParticles;
    public float coinRotationSpeed = 360f;
    // --- Spawn logic ---
    public void TrySpawnCoin()
    {
        if (spawnPoint == null) return;
        if (Random.value <= spawnChance)
        {
            SpawnCoin(goldenCoinPrefab, CoinType.Golden, spawnPoint.position);
        }
    }

    private void SpawnCoin(GameObject prefab, CoinType type, Vector3 pos)
    {
        if (prefab == null) return;
        if (coinSpawnSound != null)
        {
            AudioSource.PlayClipAtPoint(coinSpawnSound, pos, coinSpawnVolume);
        }
        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        var cb = go.AddComponent<CoinBehaviour>();
        float initialForce = (type == CoinType.Golden) ? launchForce * splitForceMultiplier : launchForce;
        cb.Initialize(this, type, initialForce);
    }

    public void SpawnSplitCoins(GameObject prefab, CoinType type, Vector3 pos)
    {
        if (prefab == null) return;
        if (coinSpawnSound != null)
        {
            AudioSource.PlayClipAtPoint(coinSpawnSound, pos, coinSpawnVolume);
        }
        for (int i = 0; i < 2; i++)
        {
            GameObject go = Instantiate(prefab, pos, Quaternion.identity);
            var cb = go.AddComponent<CoinBehaviour>();
            float force = launchForce * splitForceMultiplier;
            int bias = (i == 0) ? -1 : 1;
            cb.Initialize(this, type, force, bias);
        }
    }

    public void DamageInArea(Vector2 pos, float radius, float damage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius, enemyLayer | playerLayer);
        foreach (var col in hits)
        {
            if (col == null) continue;
            if (col.TryGetComponent(out FleaHealth flea)) flea.TakeDamage((int)damage, Vector2.zero);
            if (col.TryGetComponent(out FleaHealthV2 fleaV2)) fleaV2.TakeDamage((int)damage, Vector2.zero);
            if (col.TryGetComponent(out FlyHealth fly)) fly.TakeDamage((int)damage, Vector2.zero);
            if (col.TryGetComponent(out SprayerHealth sprayer)) sprayer.TakeDamage((int)damage, Vector2.zero);
            if (col.TryGetComponent(out InkHealth ink)) ink.TakeDamage((int)damage, Vector2.zero);
            if (col.TryGetComponent(out RatKingHealth rat)) rat.TakeDamage((int)damage);
            if (col.TryGetComponent(out PlayerHealth player)) player.TakeDamage((int)damage, 0f, Vector2.zero);
        }
    }

    // ---------------- Inner Coin Behaviour ----------------
    private class CoinBehaviour : MonoBehaviour
    {
        public Rigidbody2D rb;
        private CircleCollider2D mainCollider;
        private CircleCollider2D sensorCollider;
        private Animator anim;

        private ExplosiveCoinsPowerUp manager;
        private CoinType type;

        private bool exploded = false;
        private bool landed = false;
        private bool explosionTriggered = false;

        public void Initialize(ExplosiveCoinsPowerUp manager, CoinType type, float forceMagnitude, int horizontalBias = 0)
        {
            this.manager = manager;
            this.type = type;

            rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D[] existing = GetComponents<CircleCollider2D>();

            // main collider
            mainCollider = null;
            foreach (var c in existing) if (!c.isTrigger) { mainCollider = c; break; }
            if (mainCollider == null) mainCollider = gameObject.AddComponent<CircleCollider2D>();
            mainCollider.enabled = false;

            // sensor collider
            sensorCollider = null;
            foreach (var c in existing) if (c.isTrigger && sensorCollider == null) sensorCollider = c;
            if (sensorCollider == null)
            {
                sensorCollider = gameObject.AddComponent<CircleCollider2D>();
                sensorCollider.isTrigger = true;
                sensorCollider.radius = Mathf.Max(mainCollider.radius * 0.9f, 0.2f);
            }
            sensorCollider.enabled = true;

            anim = GetComponent<Animator>();
            if (anim != null) anim.SetBool("isRolling", true);

            ApplyDiagonalVelocity(forceMagnitude, horizontalBias);
            rb.angularVelocity = manager.coinRotationSpeed;
        }

        private void ApplyDiagonalVelocity(float magnitude, int horizontalBias)
        {
            float angleDeg = Random.Range(30f, 60f);
            float angleRad = angleDeg * Mathf.Deg2Rad;
            int side = horizontalBias == 0 ? (Random.value < 0.5f ? -1 : 1) : (horizontalBias < 0 ? -1 : 1);
            Vector2 dir = new Vector2(Mathf.Cos(angleRad) * side, Mathf.Sin(angleRad));
            rb.velocity = dir * magnitude;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            int otherLayer = other.gameObject.layer;

            // --- LAND ON GROUND ---
            if (!landed && ((manager.groundLayer.value & (1 << otherLayer)) != 0))
            {
                landed = true;
                mainCollider.enabled = true;
                sensorCollider.enabled = false;

                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;

                if (!exploded) TriggerExplosion();
                Destroy(gameObject);
                return;
            }

            // --- AIR COLLISION (split) ---
            if (!exploded && ((manager.enemyLayer.value & (1 << otherLayer)) != 0 ||
                              (manager.playerLayer.value & (1 << otherLayer)) != 0))
            {
                exploded = true;
                StartCoroutine(AirCollisionSequence());
                return;
            }
        }

        private IEnumerator AirCollisionSequence()
        {
            if (!exploded) exploded = true;
            TriggerExplosion();
            yield return new WaitForSeconds(manager.splitDelay);

            if (type == CoinType.Golden)
                manager.SpawnSplitCoins(manager.silverCoinPrefab, CoinType.Silver, transform.position);
            else if (type == CoinType.Silver)
                manager.SpawnSplitCoins(manager.bronzeCoinPrefab, CoinType.Bronze, transform.position);

            Destroy(gameObject);
        }

        private void TriggerExplosion()
        {
            if (explosionTriggered) return;
            explosionTriggered = true;

            ParticleSystem[] parts = null;
            float dmg = 0f;
            AudioClip explosionSound = null;
            float explosionVolume = 1f;
            switch (type)
            {
                case CoinType.Golden: parts = manager.goldenExplosionParticles; dmg = manager.goldenDamage; explosionSound = manager.goldenExplosionSound;
                    explosionVolume = manager.goldenExplosionVolume; break;
                case CoinType.Silver: parts = manager.silverExplosionParticles; dmg = manager.silverDamage; explosionSound = manager.silverExplosionSound;
                    explosionVolume = manager.silverExplosionVolume; break;
                case CoinType.Bronze: parts = manager.bronzeExplosionParticles; explosionSound = manager.bronzeExplosionSound;
                    explosionVolume = manager.bronzeExplosionVolume; dmg = manager.bronzeDamage; break;
            }
            if (explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(explosionSound, transform.position, explosionVolume);
            }
            if (parts != null)
            {
                foreach (var ps in parts)
                {
                    if (ps == null) continue;
                    var inst = Instantiate(ps, transform.position, Quaternion.identity);
                    inst.Play();
                    Destroy(inst.gameObject, 3f);
                }
            }

            StartCoroutine(ExplosionDamageCoroutine(dmg));
        }

        private IEnumerator ExplosionDamageCoroutine(float damage)
        {
            float elapsed = 0f;
            while (elapsed < manager.explosionDuration)
            {
                manager.DamageInArea(transform.position, manager.explosionRadius, damage);
                elapsed += manager.damageTickInterval;
                yield return new WaitForSeconds(manager.damageTickInterval);
            }
        }
    }
}

