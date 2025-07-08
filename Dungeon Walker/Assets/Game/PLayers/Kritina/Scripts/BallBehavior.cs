using UnityEngine;
using System.Collections;

public class StableDamageExplodingBall : MonoBehaviour
{
    [Header("Ball Destruction System")]
    [Tooltip("Main explosion particle system prefab")]
    public GameObject explosionPrefab;
    [Tooltip("Additional explosion particle system 1")]
    public GameObject explosionPrefab2;
    [Tooltip("Additional explosion particle system 2")]
    public GameObject explosionPrefab3;
    [Tooltip("Additional explosion particle system 3")]
    public GameObject explosionPrefab4;

    [Header("Color-Specific Explosions")]
    public GameObject greenExplosionPrefab;
    public GameObject orangeExplosionPrefab;
    public GameObject blueExplosionPrefab;

    [Tooltip("Layer mask for collision destruction")]
    public LayerMask destructionLayers = -1;
    [Tooltip("Time before ball auto-destructs (seconds)")]
    public float ballLifetime = 3f;
    [Tooltip("Enable collision-based destruction")]
    public bool enableCollisionDestruction = true;
    [Tooltip("Enable time-based destruction")]
    public bool enableTimeDestruction = true;
    [Tooltip("Instant destruction (no fade-out)")]
    public bool instantDestruction = true;
    [Tooltip("Show destruction debug info")]
    public bool showDestructionDebug = false;

    [Header("Ball Damage System")]
    [Tooltip("Damage dealt by this ball to enemies")]
    public float ballDamage = 25f;
    [Tooltip("Layer mask for enemies that can take damage")]
    public LayerMask enemyLayers = -1;
    [Tooltip("Enable damage system")]
    public bool enableDamageSystem = true;
    [Tooltip("Damage enemies on collision")]
    public bool damageOnCollision = true;
    [Tooltip("Damage enemies on explosion")]
    public bool damageOnExplosion = true;
    [Tooltip("Explosion damage radius")]
    public float explosionDamageRadius = 2f;
    [Tooltip("Explosion damage multiplier")]
    public float explosionDamageMultiplier = 1.5f;
    [Tooltip("Show damage debug info")]
    public bool showDamageDebug = false;

    [Header("Explosion Effects")]
    [Tooltip("Scale of the main explosion")]
    public float explosionScale = 1f;
    [Tooltip("Scale of additional explosions")]
    public float additionalExplosionsScale = 0.8f;
    [Tooltip("Delay between additional explosions")]
    public float explosionDelay = 0.1f;
    [Tooltip("Random offset for additional explosions")]
    public float explosionRandomOffset = 0.5f;
    [Tooltip("Duration before explosion effects are destroyed")]
    public float explosionDuration = 3f;
    [Tooltip("Show explosion debug info")]
    public bool showExplosionDebug = false;

    [Header("Sound Effects")]
    [Tooltip("Main explosion sound")]
    public AudioClip explosionSound;
    [Tooltip("Additional explosion sounds")]
    public AudioClip[] additionalExplosionSounds;
    [Tooltip("Volume of explosion sounds")]
    [Range(0f, 1f)]
    public float explosionVolume = 0.8f;
    [Tooltip("Enable sound effects")]
    public bool enableSoundEffects = true;

    // Private variables
    private bool hasExploded = false;
    private bool hasBeenDestroyed = false;
    private SpriteRenderer ballRenderer;
    private Collider2D ballCollider;
    private Rigidbody2D ballRigidbody;
    private float spawnTime;

    // New variable to store ball type
    public string ballType = "";

    void Start()
    {
        // Get components
        ballRenderer = GetComponent<SpriteRenderer>();
        ballCollider = GetComponent<Collider2D>();
        ballRigidbody = GetComponent<Rigidbody2D>();

        // Record spawn time
        spawnTime = Time.time;

        // Start auto-destruction timer if enabled
        if (enableTimeDestruction)
        {
            StartCoroutine(AutoDestructAfterTime());
        }

        if (showDestructionDebug)
        {
            Debug.Log($"Ball spawned at {spawnTime}, will auto-destruct in {ballLifetime}s");
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasBeenDestroyed)
        {
            return;
        }

        // Check if collision should trigger destruction
        bool shouldDestroy = false;

        if (enableCollisionDestruction)
        {
            // Check if the collided object is in the destruction layers
            if (((1 << collision.gameObject.layer) & destructionLayers) != 0)
            {
                shouldDestroy = true;

                if (showDestructionDebug)
                {
                    Debug.Log($"Ball collided with destruction layer: {LayerMask.LayerToName(collision.gameObject.layer)}");
                }
            }
        }

        // Handle damage on collision
        if (enableDamageSystem && damageOnCollision)
        {
            HandleCollisionDamage(collision.gameObject, collision.contacts[0].point);
        }

        // Destroy ball if collision triggered destruction
        if (shouldDestroy)
        {
            DestroyBall(collision.contacts[0].point);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenDestroyed)
        {
            return;
        }

        // Check if trigger should cause destruction
        bool shouldDestroy = false;

        if (enableCollisionDestruction)
        {
            // Check if the triggered object is in the destruction layers
            if (((1 << other.gameObject.layer) & destructionLayers) != 0)
            {
                shouldDestroy = true;

                if (showDestructionDebug)
                {
                    Debug.Log($"Ball triggered with destruction layer: {LayerMask.LayerToName(other.gameObject.layer)}");
                }
            }
        }

        // Handle damage on trigger
        if (enableDamageSystem && damageOnCollision)
        {
            HandleTriggerDamage(other.gameObject, transform.position);
        }

        // Destroy ball if trigger caused destruction
        if (shouldDestroy)
        {
            DestroyBall(transform.position);
        }
    }

    private void HandleCollisionDamage(GameObject target, Vector2 impactPoint)
    {
        // Check if target is an enemy
        if (((1 << target.layer) & enemyLayers) != 0)
        {
            // Try to get FleaHealth component
            FleaHealth enemyHealth = target.GetComponent<FleaHealth>();
            if (enemyHealth != null)
            {
                // Calculate attack direction (from ball to enemy)
                Vector2 attackDirection = (target.transform.position - transform.position).normalized;

                // Deal damage
                enemyHealth.TakeDamage((int)ballDamage, attackDirection);

                if (showDamageDebug)
                {
                    Debug.Log($"Ball dealt {ballDamage} damage to {target.name} at {impactPoint}");
                }
            }
            else if (showDamageDebug)
            {
                Debug.LogWarning($"Enemy {target.name} doesn't have FleaHealth component!");
            }
        }
    }

    private void HandleTriggerDamage(GameObject target, Vector2 impactPoint)
    {
        // Check if target is an enemy
        if (((1 << target.layer) & enemyLayers) != 0)
        {
            // Try to get FleaHealth component
            FleaHealth enemyHealth = target.GetComponent<FleaHealth>();
            if (enemyHealth != null)
            {
                // Calculate attack direction (from ball to enemy)
                Vector2 attackDirection = (target.transform.position - transform.position).normalized;

                // Deal damage
                enemyHealth.TakeDamage((int)ballDamage, attackDirection);

                if (showDamageDebug)
                {
                    Debug.Log($"Ball dealt {ballDamage} trigger damage to {target.name} at {impactPoint}");
                }
            }
            else if (showDamageDebug)
            {
                Debug.LogWarning($"Enemy {target.name} doesn't have FleaHealth component!");
            }
        }
    }

    private void HandleExplosionDamage(Vector2 explosionCenter)
    {
        if (!enableDamageSystem || !damageOnExplosion)
        {
            return;
        }

        // Find all enemies within explosion radius
        Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(explosionCenter, explosionDamageRadius, enemyLayers);

        foreach (Collider2D enemyCollider in enemiesInRange)
        {
            FleaHealth enemyHealth = enemyCollider.GetComponent<FleaHealth>();
            if (enemyHealth != null)
            {
                // Calculate distance-based damage
                float distance = Vector2.Distance(explosionCenter, enemyCollider.transform.position);
                float damageMultiplier = 1f - (distance / explosionDamageRadius);
                float explosionDamage = ballDamage * explosionDamageMultiplier * damageMultiplier;

                // Calculate attack direction (from explosion center to enemy)
                Vector2 attackDirection = (enemyCollider.transform.position - (Vector3)explosionCenter).normalized;

                // Deal explosion damage
                enemyHealth.TakeDamage((int)explosionDamage, attackDirection);

                if (showDamageDebug)
                {
                    Debug.Log($"Explosion dealt {explosionDamage:F1} damage to {enemyCollider.name} (distance: {distance:F2})");
                }
            }
        }

        if (showDamageDebug)
        {
            Debug.Log($"Explosion damaged {enemiesInRange.Length} enemies within {explosionDamageRadius} units");
        }
    }

    private IEnumerator AutoDestructAfterTime()
    {
        yield return new WaitForSeconds(ballLifetime);

        if (!hasBeenDestroyed)
        {
            if (showDestructionDebug)
            {
                Debug.Log($"Ball auto-destructed after {ballLifetime}s");
            }

            DestroyBall(transform.position);
        }
    }

    public void DestroyBall(Vector2 explosionPosition)
    {
        if (hasBeenDestroyed)
        {
            return;
        }

        hasBeenDestroyed = true;

        if (showDestructionDebug)
        {
            Debug.Log($"Destroying ball at {explosionPosition}");
        }

        // Handle explosion damage first
        HandleExplosionDamage(explosionPosition);

        // Instant destruction - hide ball immediately
        if (instantDestruction)
        {
            HideBallInstantly();
        }

        // Create explosion effects
        CreateExplosionEffects(explosionPosition);

        // Play explosion sounds
        if (enableSoundEffects)
        {
            PlayExplosionSounds(explosionPosition);
        }

        // Destroy the ball GameObject
        if (instantDestruction)
        {
            // Destroy immediately for instant destruction
            Destroy(gameObject);
        }
        else
        {
            // Destroy after a short delay for non-instant destruction
            Destroy(gameObject, 0.1f);
        }
    }

    private void HideBallInstantly()
    {
        // Disable renderer immediately
        if (ballRenderer != null)
        {
            ballRenderer.enabled = false;
        }

        // Disable collider to prevent further collisions
        if (ballCollider != null)
        {
            ballCollider.enabled = false;
        }

        // Stop rigidbody movement
        if (ballRigidbody != null)
        {
            ballRigidbody.velocity = Vector2.zero;
            ballRigidbody.angularVelocity = 0f;
            ballRigidbody.simulated = false;
        }

        if (showDestructionDebug)
        {
            Debug.Log("Ball hidden instantly");
        }
    }

    private void CreateExplosionEffects(Vector2 explosionPosition)
    {
        if (hasExploded)
        {
            return;
        }

        hasExploded = true;

        GameObject selectedExplosionPrefab = null;

        switch (ballType)
        {
            case "GreenBall":
                selectedExplosionPrefab = greenExplosionPrefab;
                break;
            case "OrangeBall":
                selectedExplosionPrefab = orangeExplosionPrefab;
                break;
            case "BlueBall":
                selectedExplosionPrefab = blueExplosionPrefab;
                break;
            default:
                selectedExplosionPrefab = explosionPrefab; // Fallback to generic explosion
                break;
        }

        // Create main explosion
        if (selectedExplosionPrefab != null)
        {
            CreateSingleExplosion(selectedExplosionPrefab, explosionPosition, explosionScale, 0f);
        }

        // Create additional explosions with delays (using generic ones for now, can be extended)
        if (explosionPrefab2 != null)
        {
            StartCoroutine(CreateDelayedExplosion(explosionPrefab2, explosionPosition, additionalExplosionsScale, explosionDelay));
        }

        if (explosionPrefab3 != null)
        {
            StartCoroutine(CreateDelayedExplosion(explosionPrefab3, explosionPosition, additionalExplosionsScale, explosionDelay * 2f));
        }

        if (explosionPrefab4 != null)
        {
            StartCoroutine(CreateDelayedExplosion(explosionPrefab4, explosionPosition, additionalExplosionsScale, explosionDelay * 3f));
        }

        if (showExplosionDebug)
        {
            Debug.Log($"Created explosion effects at {explosionPosition}");
        }
    }

    private void CreateSingleExplosion(GameObject explosionPrefab, Vector2 position, float scale, float delay)
    {
        if (explosionPrefab == null)
        {
            return;
        }

        // Add random offset for additional explosions
        Vector2 finalPosition = position;
        if (delay > 0f && explosionRandomOffset > 0f)
        {
            Vector2 randomOffset = Random.insideUnitCircle * explosionRandomOffset;
            finalPosition += randomOffset;
        }

        // Instantiate explosion
        GameObject explosion = Instantiate(explosionPrefab, finalPosition, Quaternion.identity);

        // Scale explosion
        explosion.transform.localScale = Vector3.one * scale;

        // Get particle system and play it
        ParticleSystem particles = explosion.GetComponent<ParticleSystem>();
        if (particles != null)
        {
            particles.Play();
        }

        // Auto-destroy explosion after duration
        Destroy(explosion, explosionDuration);

        if (showExplosionDebug)
        {
            Debug.Log($"Created explosion at {finalPosition} with scale {scale}");
        }
    }

    private IEnumerator CreateDelayedExplosion(GameObject explosionPrefab, Vector2 position, float scale, float delay)
    {
        yield return new WaitForSeconds(delay);
        CreateSingleExplosion(explosionPrefab, position, scale, delay);
    }

    private void PlayExplosionSounds(Vector2 explosionPosition)
    {
        // Play main explosion sound
        if (explosionSound != null)
        {
            PlaySoundAtPosition(explosionSound, explosionPosition, 0f);
        }

        // Play additional explosion sounds with delays
        if (additionalExplosionSounds != null && additionalExplosionSounds.Length > 0)
        {
            for (int i = 0; i < additionalExplosionSounds.Length; i++)
            {
                if (additionalExplosionSounds[i] != null)
                {
                    float soundDelay = explosionDelay * (i + 1);
                    StartCoroutine(PlayDelayedSound(additionalExplosionSounds[i], explosionPosition, soundDelay));
                }
            }
        }
    }

    private void PlaySoundAtPosition(AudioClip clip, Vector2 position, float delay)
    {
        if (clip == null)
        {
            return;
        }

        // Create temporary audio source
        GameObject audioObject = new GameObject("ExplosionAudio");
        audioObject.transform.position = position;

        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = explosionVolume;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.Play();

        // Destroy audio object after clip finishes
        Destroy(audioObject, clip.length + 0.1f);

        if (showExplosionDebug)
        {
            Debug.Log($"Playing explosion sound at {position}");
        }
    }

    private IEnumerator PlayDelayedSound(AudioClip clip, Vector2 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlaySoundAtPosition(clip, position, delay);
    }

    // Public methods for external control
    public void ForceDestroy()
    {
        DestroyBall(transform.position);
    }

    public void SetInstantDestruction(bool instant)
    {
        instantDestruction = instant;
        if (showDestructionDebug)
        {
            Debug.Log($"Instant destruction set to: {instant}");
        }
    }

    public void SetBallDamage(float damage)
    {
        ballDamage = damage;
        if (showDamageDebug)
        {
            Debug.Log($"Ball damage set to: {damage}");
        }
    }

    public void SetExplosionDamageRadius(float radius)
    {
        explosionDamageRadius = radius;
        if (showDamageDebug)
        {
            Debug.Log($"Explosion damage radius set to: {radius}");
        }
    }

    public void SetExplosionDamageMultiplier(float multiplier)
    {
        explosionDamageMultiplier = multiplier;
        if (showDamageDebug)
        {
            Debug.Log($"Explosion damage multiplier set to: {multiplier}");
        }
    }

    public void SetEnableDamageSystem(bool enabled)
    {
        enableDamageSystem = enabled;
        if (showDamageDebug)
        {
            Debug.Log($"Damage system set to: {enabled}");
        }
    }

    public void SetDamageOnCollision(bool enabled)
    {
        damageOnCollision = enabled;
        if (showDamageDebug)
        {
            Debug.Log($"Damage on collision set to: {enabled}");
        }
    }

    public void SetDamageOnExplosion(bool enabled)
    {
        damageOnExplosion = enabled;
        if (showDamageDebug)
        {
            Debug.Log($"Damage on explosion set to: {enabled}");
        }
    }

    public void SetExplosionDelay(float delay)
    {
        explosionDelay = delay;
        if (showExplosionDebug)
        {
            Debug.Log($"Explosion delay set to: {delay}");
        }
    }

    public void SetExplosionRandomOffset(float offset)
    {
        explosionRandomOffset = offset;
        if (showExplosionDebug)
        {
            Debug.Log($"Explosion random offset set to: {offset}");
        }
    }

    public void SetExplosionScale(float scale)
    {
        explosionScale = scale;
        if (showExplosionDebug)
        {
            Debug.Log($"Explosion scale set to: {scale}");
        }
    }

    public void SetAdditionalExplosionsScale(float scale)
    {
        additionalExplosionsScale = scale;
        if (showExplosionDebug)
        {
            Debug.Log($"Additional explosions scale set to: {scale}");
        }
    }

    public void SetExplosionPrefabs(GameObject main, GameObject additional1, GameObject additional2, GameObject additional3)
    {
        explosionPrefab = main;
        explosionPrefab2 = additional1;
        explosionPrefab3 = additional2;
        explosionPrefab4 = additional3;

        if (showExplosionDebug)
        {
            Debug.Log("Explosion prefabs updated");
        }
    }

    public void SetBallLifetime(float lifetime)
    {
        ballLifetime = lifetime;
        if (showDestructionDebug)
        {
            Debug.Log($"Ball lifetime set to: {lifetime}s");
        }
    }

    public void SetEnableCollisionDestruction(bool enabled)
    {
        enableCollisionDestruction = enabled;
        if (showDestructionDebug)
        {
            Debug.Log($"Collision destruction set to: {enabled}");
        }
    }

    public void SetEnableTimeDestruction(bool enabled)
    {
        enableTimeDestruction = enabled;
        if (showDestructionDebug)
        {
            Debug.Log($"Time destruction set to: {enabled}");
        }
    }

    public void SetDestructionLayers(LayerMask layers)
    {
        destructionLayers = layers;
        if (showDestructionDebug)
        {
            Debug.Log($"Destruction layers updated");
        }
    }

    public void SetEnemyLayers(LayerMask layers)
    {
        enemyLayers = layers;
        if (showDamageDebug)
        {
            Debug.Log($"Enemy layers updated");
        }
    }

    // Getter methods for external access
    public float GetBallDamage()
    {
        return ballDamage;
    }

    public float GetExplosionDamageRadius()
    {
        return explosionDamageRadius;
    }

    public bool IsDestroyed()
    {
        return hasBeenDestroyed;
    }

    public bool HasExploded()
    {
        return hasExploded;
    }

    public float GetTimeAlive()
    {
        return Time.time - spawnTime;
    }

    public float GetTimeUntilAutoDestruct()
    {
        if (!enableTimeDestruction)
        {
            return -1f; // No auto-destruct
        }

        float timeRemaining = ballLifetime - GetTimeAlive();
        return Mathf.Max(0f, timeRemaining);
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (enableDamageSystem && damageOnExplosion)
        {
            // Draw explosion damage radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionDamageRadius);

            // Draw explosion random offset range
            if (explosionRandomOffset > 0f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, explosionRandomOffset);
            }
        }

        // Draw ball trajectory if it has a rigidbody
        if (ballRigidbody != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, ballRigidbody.velocity.normalized * 2f);
        }
    }
}

