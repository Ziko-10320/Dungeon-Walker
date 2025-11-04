using UnityEngine;
using UnityEngine.EventSystems;

public class BulletBehavior : MonoBehaviour
{
    // --- PUBLIC STATS (Set by WaterGunSystem) ---
    public int bulletDamage;
    public float bulletSpeed;
    public LayerMask collisionLayers;
    public GameObject waterExplosionPrefab;
    public AudioClip collisionSound; // The sound to play on impact.
    [Range(0f, 1f)]
    public float collisionVolume = 1f;
    // --- INTERNAL COMPONENTS & STATE ---
    private Rigidbody2D rb;
    private bool canCollide;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // The bullet starts asleep and unable to collide.
        rb.simulated = false;
        canCollide = false;
    }

    // This is called by the gun to shoot the bullet.
    public void Fire(Vector2 direction)
    {
        // 1. WAKE UP: Enable physics simulation.
        rb.simulated = true;

        // 2. FIRE: Set the velocity.
        rb.velocity = direction * bulletSpeed;

        // 3. READY: Explicitly allow collisions.
        canCollide = true;
    }

    // --- COLLISION HANDLING ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        // We only run the impact logic if collisions are allowed.
        if (canCollide && ((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            HandleImpact(other.gameObject, transform.position);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (canCollide && ((1 << collision.gameObject.layer) & collisionLayers) != 0)
        {
            HandleImpact(collision.gameObject, collision.contacts[0].point);
        }
    }
    public void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || Camera.main == null) return;

        // Create a clean, independent object for the sound
        GameObject soundPlayerObject = new GameObject("BulletImpact_FORCE_PLAY_SOUND");

        // --- THIS IS THE CRITICAL FIX for volume issues ---
        // Position it directly on the camera to guarantee it's heard at full volume
        soundPlayerObject.transform.position = Camera.main.transform.position;

        // Add and aggressively configure the AudioSource
        AudioSource tempAudioSource = soundPlayerObject.AddComponent<AudioSource>();

        tempAudioSource.clip = clip;

        // --- CRITICAL OVERRIDES ---
        tempAudioSource.volume = volume;
        tempAudioSource.spatialBlend = 0.0f;              // Force 2D sound
        tempAudioSource.priority = 0;                     // Highest priority
        tempAudioSource.bypassEffects = true;             // Ignore mixers
        tempAudioSource.bypassListenerEffects = true;     // Ignore listener effects
        tempAudioSource.bypassReverbZones = true;         // Ignore reverb zones

        // Play the sound and schedule its destruction
        tempAudioSource.Play();
        Destroy(soundPlayerObject, clip.length);
    }
    private void HandleImpact(GameObject hitObject, Vector2 impactPoint)
    {
        // 1. LOCK: Immediately prevent any other collision calls.
        canCollide = false;
        PlaySound(collisionSound, collisionVolume);
        FleaHealth enemyHealth = hitObject.GetComponent<FleaHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(bulletDamage, rb.velocity.normalized);
            if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                L3antixSuperMeter.Instance.AddDamage(bulletDamage);

            if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                PlayerSuperMeter.Instance.AddDamage(bulletDamage);
        }

        SprayerHealth SprayerHealth = hitObject.GetComponent<SprayerHealth>();
        if (SprayerHealth != null)
        {
            SprayerHealth.TakeDamage(bulletDamage, rb.velocity.normalized);
            if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                L3antixSuperMeter.Instance.AddDamage(bulletDamage);

            if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                PlayerSuperMeter.Instance.AddDamage(bulletDamage);
        }

        FleaHealthV2 FleaHealthV2 = hitObject.GetComponent<FleaHealthV2>();
        if (FleaHealthV2 != null)
        {
            FleaHealthV2.TakeDamage(bulletDamage, rb.velocity.normalized);
            if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                L3antixSuperMeter.Instance.AddDamage(bulletDamage);

            if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                PlayerSuperMeter.Instance.AddDamage(bulletDamage);
        }

        FlyHealth flyHealth = hitObject.GetComponent<FlyHealth>();
        if (flyHealth != null)
        {
            flyHealth.TakeDamage(bulletDamage, rb.velocity.normalized);
            if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                L3antixSuperMeter.Instance.AddDamage(bulletDamage);

            if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                PlayerSuperMeter.Instance.AddDamage(bulletDamage);
        }

        InkHealth inkHealth = hitObject.GetComponent<InkHealth>();
        if (inkHealth != null)
        {
            inkHealth.TakeDamage(bulletDamage, rb.velocity.normalized);
            if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                L3antixSuperMeter.Instance.AddDamage(bulletDamage);

            if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                PlayerSuperMeter.Instance.AddDamage(bulletDamage);
        }

        RatKingHealth RatKingHealth = hitObject.GetComponent<RatKingHealth>();
        if (RatKingHealth != null)
        {
            RatKingHealth.TakeDamage(bulletDamage);
            if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                L3antixSuperMeter.Instance.AddDamage(bulletDamage);

            if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                PlayerSuperMeter.Instance.AddDamage(bulletDamage);
        }
        CheeseProjectile CheeseProjectile = hitObject.GetComponent<CheeseProjectile>();
        if (CheeseProjectile != null)
        {
            CheeseProjectile.TakeDamage(bulletDamage, rb.velocity.normalized);
            
        }
        DestructibleObject DestructibleObject = hitObject.GetComponent<DestructibleObject>();
        if (DestructibleObject != null)
        {
            DestructibleObject.TakeDamage(bulletDamage);
        } 
        // 3. SPAWN EFFECT: Ask the pool for a particle effect.
        if (waterExplosionPrefab != null && ObjectPoolManager.Instance != null)
        {
            // The effect will now handle its own lifecycle thanks to PoolableParticle.cs
            ObjectPoolManager.Instance.SpawnFromPool(waterExplosionPrefab, impactPoint, Quaternion.identity);
        }

        // 4. GO TO SLEEP: Deactivate this bullet to return it to the pool.
        gameObject.SetActive(false);
    }

    // OnDisable is called when SetActive(false) happens.
    void OnDisable()
    {
        // This is our GUARANTEED cleanup routine.
        // Stop all physics movement immediately.
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false; // Put the Rigidbody to sleep.
        }
        // Ensure collisions are off until the next Fire() call.
        canCollide = false;
    }
}
