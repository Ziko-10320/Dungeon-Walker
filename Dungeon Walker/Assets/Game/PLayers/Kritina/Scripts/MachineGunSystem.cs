using FirstGearGames.SmoothCameraShaker;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MachineGunSystem : MonoBehaviour
{
    [Header("COMPONENT REFERENCES")]
    [SerializeField] private GameObject Gun;
    [SerializeField] private GameObject Arm;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private Transform launcherAimPoint; // Point d\"origine de la visée
    [SerializeField] private Transform minDistancePoint; // Transform pour visualiser la distance minimale
    [SerializeField] private Transform pivotPoint; // Nouveau point de pivot pour la rotation du Gun
    [SerializeField] private Transform emptyBulletSpawnPoint; // Point de spawn des douilles vides
    [SerializeField] private List<SpriteRenderer> gunSpriteRenderers; // SpriteRenderers pour le changement de couleur

    [Header("PROJECTILE & EFFECTS")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private ParticleSystem destructionEffectPrefab;
    [SerializeField] private ParticleSystem muzzleFlashEffect;
    [SerializeField] private float bulletSpeed = 25f;
    [SerializeField] private float bulletLifetime = 3f;
    [SerializeField] private int bulletDamage = 15;
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private LayerMask enemyLayers;

    [Header("AIMING & ROTATION (FROM ROBUST LAUNCHER)")]
    [Tooltip("Angle maximum de visée vers le haut")]
    [SerializeField] private float maxUpwardAngle = 80f;
    [Tooltip("Angle maximum de visée vers le bas")]
    [SerializeField] private float maxDownwardAngle = 80f;
    [Tooltip("Distance minimale pour que la visée s\"active")]
    [SerializeField] private float minDistanceToAim = 0.8f;
    [Tooltip("Vitesse de rotation de l\"arme (pour une rotation fluide)")]
    [SerializeField] private float rotationSpeed = 25f;
    [Tooltip("Utiliser une rotation instantanée pour une réactivité maximale")]
    [SerializeField] private bool useInstantRotation = true;

    [Header("LAUNCHER CALIBRATION")]
    [Tooltip("Offset de rotation pour l\"arme quand le joueur regarde à DROITE")]
    public float launcherRotationOffsetRight = 0f;
    [Tooltip("Offset de rotation pour l\"arme quand le joueur regarde à GAUCHE")]
    public float launcherRotationOffsetLeft = 0f;
    [Tooltip("Offset de rotation pour la TRAJECTOIRE quand le joueur regarde à DROITE")]
    public float trajectoryRotationOffsetRight = 0f;
    [Tooltip("Offset de rotation pour la TRAJECTOIRE quand le joueur regarde à GAUCHE")]
    public float trajectoryRotationOffsetLeft = 0f;

    [Header("FIRING & AMMO SYSTEM")]
    [SerializeField] private float fireRate = 0.2f;

    [Header("MACHINE GUN SPECIFIC")]
    [Tooltip("Angle maximum de dispersion des balles (en degrés)")]
    [SerializeField] private float maxSpreadAngle = 10f;
    [Tooltip("Rayon de la zone de spawn aléatoire des balles autour du bulletSpawnPoint")]
    [SerializeField] private float spawnAreaRadius = 0.1f;
    [SerializeField] private GameObject emptyBulletPrefab; // Prefab de la douille vide
    [SerializeField] private float emptyBulletForce = 2f; // Force appliquée à la douille vide
    [SerializeField] private float emptyBulletLifetime = 2f; // Durée de vie de la douille vide
    [SerializeField] private float emptyBulletSpawnAreaRadius = 0.05f; // Rayon de la zone de spawn aléatoire des douilles vides

    [Header("OVERHEAT SYSTEM")]
    [Tooltip("Temps maximum de tir continu avant surchauffe")]
    [SerializeField] private float maxOverheatTime = 7f;
    [Tooltip("Temps nécessaire pour que l\"arme refroidisse complètement après surchauffe")]
    [SerializeField] private float overheatCoolDownTime = 7f;
    [Tooltip("Pourcentage de surchauffe à partir duquel la couleur de l\"arme commence à changer (0.0 - 1.0)")]
    [SerializeField, Range(0f, 1f)] private float overheatColorThreshold = 0.6f;
    [Tooltip("Système de particules de fumée à déclencher en cas de surchauffe")]
    [SerializeField] private ParticleSystem smokeParticleSystem;
    [Tooltip("Son joué lorsque l\"arme surchauffe")]
    [SerializeField] private AudioClip overheatAudioClip;
    [SerializeField, Range(0f, 1f)] private float overheatAudioVolume = 1f;
    [Tooltip("Référence au script CameraShake de FirstGearGames")]
    public ShakeData cameraShakeOver; // Pour le Camera Shake


    [Header("AUDIO")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField, Range(0f, 1f)] private float shootSoundVolume = 1f; // Volume for shoot sound
    [SerializeField] private AudioClip bulletCollisionSound; // New sound slot for bullet collision
    [SerializeField, Range(0f, 1f)] private float bulletCollisionSoundVolume = 1f; // Volume for bullet collision sound
    private AudioSource audioSource;

    // --- CORE AIMING VARIABLES (FROM ROBUST LAUNCHER) ---
    private Vector2 mouseWorldPosition;
    private Vector2 aimFromPosition;
    private Vector2 aimDirection;
    private bool isPlayerFacingRight = true;
    private float worldArmRotation = 0f;
    private float worldLauncherRotation = 0f;
    private float worldTrajectoryRotation = 0f;

    // Overheat variables
    private float nextFireTime = 0f;
    private float currentOverheatValue = 0f;
    private bool isOverheated = false;
    private Color originalGunColor;
    private bool playedOverheatSound = false;
    private bool playedOverheatShake = false;
    private float overheatCooldownTimer = 0f; // New timer for cooldown

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (gunSpriteRenderers != null && gunSpriteRenderers.Count > 0)
        {
            originalGunColor = gunSpriteRenderers[0].color; // Store the original color of the first renderer
        }
    }

    void Update()
    {
        HandleAiming();
        ApplyRotation();
        HandleShooting();
        UpdateMinDistancePointPosition();
        HandleOverheat();
    }

    private void HandleAiming()
    {
        UpdatePlayerFacingDirection();
        mouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        aimFromPosition = launcherAimPoint != null ? (Vector2)launcherAimPoint.position : (Vector2)Gun.transform.position;
        Vector2 directionToMouse = (mouseWorldPosition - aimFromPosition);
        aimDirection = directionToMouse.normalized;

        if (directionToMouse.magnitude < minDistanceToAim)
        {
            return;
        }

        float worldAngleToMouse = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
        float clampedWorldAngle = ClampWorldAngle(worldAngleToMouse);

        worldArmRotation = clampedWorldAngle;
        float currentLauncherOffset = isPlayerFacingRight ? launcherRotationOffsetRight : launcherRotationOffsetLeft;
        worldLauncherRotation = clampedWorldAngle + currentLauncherOffset;
        float currentTrajectoryOffset = isPlayerFacingRight ? trajectoryRotationOffsetRight : trajectoryRotationOffsetLeft;
        worldTrajectoryRotation = clampedWorldAngle + currentTrajectoryOffset;
    }

    private void UpdatePlayerFacingDirection()
    {
        if (playerTransform != null)
        {
            isPlayerFacingRight = playerTransform.localScale.x > 0;
        }
    }

    private float ClampWorldAngle(float worldAngle)
    {
        worldAngle = (worldAngle + 180f) % 360f - 180f;

        if (isPlayerFacingRight)
        {
            return Mathf.Clamp(worldAngle, -maxDownwardAngle, maxUpwardAngle);
        }
        else
        {
            float leftEquivAngle = 180 + worldAngle;
            leftEquivAngle = Mathf.Clamp(leftEquivAngle, 180 - maxUpwardAngle, 180 + maxDownwardAngle);
            return leftEquivAngle - 180;
        }
    }

    private void ApplyRotation()
    {
        if (Vector2.Distance(mouseWorldPosition, aimFromPosition) < minDistanceToAim)
        {
            return;
        }

        Quaternion armTargetRotation = Quaternion.Euler(0, 0, worldArmRotation);
        Quaternion gunTargetRotation = Quaternion.Euler(0, 0, worldLauncherRotation);

        if (useInstantRotation)
        {
            Arm.transform.rotation = armTargetRotation;
            if (Gun != null && pivotPoint != null)
            {
                Gun.transform.RotateAround(pivotPoint.position, Vector3.forward, worldLauncherRotation - Gun.transform.rotation.eulerAngles.z);
            }
            else if (Gun != null)
            {
                Gun.transform.rotation = gunTargetRotation;
            }
        }
        else
        {
            Arm.transform.rotation = Quaternion.Slerp(Arm.transform.rotation, armTargetRotation, rotationSpeed * Time.deltaTime);
            if (Gun != null && pivotPoint != null)
            {
                float currentZ = Gun.transform.rotation.eulerAngles.z;
                float targetZ = worldLauncherRotation;
                float angleDiff = Mathf.DeltaAngle(currentZ, targetZ);
                Gun.transform.RotateAround(pivotPoint.position, Vector3.forward, angleDiff * rotationSpeed * Time.deltaTime);
            }
            else if (Gun != null)
            {
                Gun.transform.rotation = Quaternion.Slerp(Gun.transform.rotation, gunTargetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void HandleShooting()
    {
        // If currently overheated, prevent shooting and handle cooldown
        if (isOverheated)
        {
            overheatCooldownTimer -= Time.deltaTime;
            if (overheatCooldownTimer <= 0f)
            {
                isOverheated = false; // Cooldown finished, no longer overheated
                currentOverheatValue = 0f; // Reset overheat value
            }
            return; // Prevent shooting while overheated
        }

        if (Mouse.current.leftButton.isPressed)
        {
            // Augmenter la valeur de surchauffe si le bouton est pressé et que l\'arme n\'est pas surchauffée
            currentOverheatValue += Time.deltaTime;
            currentOverheatValue = Mathf.Min(currentOverheatValue, maxOverheatTime); // Capped at max

            // Tirer seulement si l\'arme n\'est pas surchauffée et que le temps de tir est écoulé
            if (Time.time >= nextFireTime)
            {
                if (Vector2.Distance(mouseWorldPosition, aimFromPosition) < minDistanceToAim) return;

                Shoot();
                SpawnEmptyBullet();
                nextFireTime = Time.time + fireRate;
            }
        }
        else // Le bouton de la souris n\'est PAS pressé
        {
            // Diminuer la valeur de surchauffe
            currentOverheatValue -= Time.deltaTime * (maxOverheatTime / overheatCoolDownTime);
            currentOverheatValue = Mathf.Max(0f, currentOverheatValue); // S\'assurer qu\'elle ne descend pas en dessous de 0
        }
    }

    private void Shoot()
    {
        if (muzzleFlashEffect != null) muzzleFlashEffect.Play();
        if (audioSource != null && shootSound != null) audioSource.PlayOneShot(shootSound, shootSoundVolume);

        float spread = Random.Range(-maxSpreadAngle / 2f, maxSpreadAngle / 2f);
        Quaternion shootRotation = Quaternion.Euler(0, 0, worldTrajectoryRotation + spread);
        Vector2 shootDirection = shootRotation * Vector2.right;

        Vector2 randomSpawnOffset = Random.insideUnitCircle * spawnAreaRadius;
        Vector3 spawnPosition = bulletSpawnPoint.position + (Vector3)randomSpawnOffset;

        GameObject bulletInstance = Instantiate(bulletPrefab, spawnPosition, shootRotation);

        // Add BulletComponent and initialize it
        BulletComponent bulletComponent = bulletInstance.AddComponent<BulletComponent>();
        bulletComponent.Initialize(shootDirection, bulletSpeed, bulletLifetime, bulletDamage, damageableLayers, collisionLayers, enemyLayers, destructionEffectPrefab, bulletCollisionSound, bulletCollisionSoundVolume);
    }

    private void SpawnEmptyBullet()
    {
        if (emptyBulletPrefab == null || emptyBulletSpawnPoint == null) return;

        Vector2 randomSpawnOffset = Random.insideUnitCircle * emptyBulletSpawnAreaRadius;
        Vector3 spawnPosition = emptyBulletSpawnPoint.position + (Vector3)randomSpawnOffset;

        GameObject emptyBulletInstance = Instantiate(emptyBulletPrefab, spawnPosition, Quaternion.identity);
        Rigidbody2D rb = emptyBulletInstance.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 forceDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(0.5f, 1f)).normalized;
            rb.AddForce(forceDirection * emptyBulletForce, ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(-100f, 100f));
        }
        Destroy(emptyBulletInstance, emptyBulletLifetime);
    }

    private void HandleOverheat()
    {
        float overheatProgress = currentOverheatValue / maxOverheatTime;

        // Handle color change
        if (gunSpriteRenderers != null && gunSpriteRenderers.Count > 0)
        {
            if (overheatProgress >= overheatColorThreshold)
            {
                float colorLerpFactor = Mathf.InverseLerp(overheatColorThreshold, 1f, overheatProgress);
                Color targetColor = Color.Lerp(originalGunColor, Color.red, colorLerpFactor);
                foreach (SpriteRenderer sr in gunSpriteRenderers)
                {
                    sr.color = targetColor;
                }
            }
            else
            {
                foreach (SpriteRenderer sr in gunSpriteRenderers)
                {
                    sr.color = originalGunColor;
                }
            }
        }

        // Handle smoke particles
        if (smokeParticleSystem != null)
        {
            if (overheatProgress >= 0.9f && !smokeParticleSystem.isPlaying)
            {
                smokeParticleSystem.Play();
            }
            else if (overheatProgress < 0.9f && smokeParticleSystem.isPlaying)
            {
                smokeParticleSystem.Stop();
            }
        }

        // Update current overheat state
        bool wasOverheatedBeforeThisFrame = isOverheated;
        isOverheated = currentOverheatValue >= maxOverheatTime;

        // Trigger sound and shake only once when it *becomes* overheated
        if (isOverheated && !wasOverheatedBeforeThisFrame)
        {
            if (audioSource != null && overheatAudioClip != null)
            {
                audioSource.PlayOneShot(overheatAudioClip, overheatAudioVolume);
            }
            CameraShakerHandler.Shake(cameraShakeOver);
            overheatCooldownTimer = overheatCoolDownTime; // Start cooldown timer
        }

        // If it was overheated and now cooled down
        if (wasOverheatedBeforeThisFrame && !isOverheated)
        {
            // Reset flags when cooling down from an overheated state
            playedOverheatSound = false;
            playedOverheatShake = false;
            if (smokeParticleSystem != null) smokeParticleSystem.Stop();
        }
    }

    private void UpdateMinDistancePointPosition()
    {
        if (minDistancePoint == null) return;
        minDistancePoint.position = aimFromPosition;
    }

    public bool IsOverheated() => isOverheated;
    public float GetCurrentOverheatValue() => currentOverheatValue;
    public float GetMaxOverheatTime() => maxOverheatTime;

    private void OnDrawGizmos()
    {
        Vector2 origin = launcherAimPoint != null ? (Vector2)launcherAimPoint.position : (Gun != null ? (Vector2)Gun.transform.position : Vector2.zero);
        if (origin == Vector2.zero) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, mouseWorldPosition);
        Gizmos.color = (Vector2.Distance(mouseWorldPosition, origin) < minDistanceToAim) ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(origin, minDistanceToAim);

        Gizmos.color = Color.magenta;
        if (bulletSpawnPoint != null)
        {
            Gizmos.DrawWireSphere(bulletSpawnPoint.position, spawnAreaRadius);
        }

        Gizmos.color = Color.gray;
        if (emptyBulletSpawnPoint != null)
        {
            Gizmos.DrawWireSphere(emptyBulletSpawnPoint.position, emptyBulletSpawnAreaRadius);
        }

        if (bulletSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Quaternion shootRotation = Quaternion.Euler(0, 0, worldTrajectoryRotation);
            Gizmos.DrawRay(bulletSpawnPoint.position, shootRotation * Vector2.right * 3f);
        }
    }
}

// Nested class for bullet behavior
public class BulletComponent : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float lifetime;
    private int damage;
    private LayerMask damageableLayers;
    private LayerMask collisionLayers;
    private LayerMask enemyLayers;
    private ParticleSystem destructionEffectPrefab;
    private AudioClip collisionSound;
    private float collisionSoundVolume;
    private Rigidbody2D rb;
    private AudioSource audioSource;

    public void Initialize(Vector2 dir, float spd, float life, int dmg, LayerMask dmgLayers, LayerMask colLayers, LayerMask enemyLyrs, ParticleSystem destructionFx, AudioClip colSound, float colSoundVolume)
    {
        direction = dir.normalized;
        speed = spd;
        lifetime = life;
        damage = dmg;
        damageableLayers = dmgLayers;
        collisionLayers = colLayers;
        enemyLayers = enemyLyrs;
        destructionEffectPrefab = destructionFx;
        collisionSound = colSound;
        collisionSoundVolume = colSoundVolume;

        rb = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (GetComponent<Collider2D>() == null)
        {
            var collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.1f;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        if (rb != null) rb.velocity = direction * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }

    private void HandleCollision(GameObject collidedObject)
    {
        bool shouldDestroy = false;

        if ((((1 << collidedObject.layer) & collisionLayers) != 0))
        {
            shouldDestroy = true;
        }

        if ((((1 << collidedObject.layer) & enemyLayers) != 0))
        {
            Vector2 attackDirection = (collidedObject.transform.position - transform.position).normalized;

            // Damage enemies
            // Original code had specific health components. Re-adding them.
            // Assuming these classes (FleaHealth, SprayerHealth, FlyHealth, InkHealth) exist in your project.
            FleaHealth fleaHealth = collidedObject.GetComponent<FleaHealth>();
            if (fleaHealth != null)
            {
                fleaHealth.TakeDamage(damage, attackDirection);
            }

            SprayerHealth sprayerHealth = collidedObject.GetComponent<SprayerHealth>();
            if (sprayerHealth != null)
            {
                sprayerHealth.TakeDamage(damage, attackDirection);
            }

            FlyHealth flyHealth = collidedObject.GetComponent<FlyHealth>();
            if (flyHealth != null)
            {
                flyHealth.TakeDamage(damage, attackDirection);
            }

            InkHealth inkHealth = collidedObject.GetComponent<InkHealth>();
            if (inkHealth != null)
            {
                inkHealth.TakeDamage(damage, attackDirection, 1f);
            }

            if (audioSource != null && collisionSound != null) audioSource.PlayOneShot(collisionSound, collisionSoundVolume);
            shouldDestroy = true;
        }
        else if ((((1 << collidedObject.layer) & damageableLayers) != 0))
        {
            shouldDestroy = true;
        }

        if (shouldDestroy)
        {
            if (destructionEffectPrefab != null)
            {
                ParticleSystem newParticleSystem = Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
                newParticleSystem.Play();
                Destroy(newParticleSystem.gameObject, newParticleSystem.main.duration);
            }
            Destroy(gameObject);
        }
    }
}

// Interface for damageable objects
public interface IDamageable
{
    void TakeDamage(int damage);
}


