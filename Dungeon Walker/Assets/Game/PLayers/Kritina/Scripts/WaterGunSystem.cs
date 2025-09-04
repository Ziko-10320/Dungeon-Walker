using System.Collections;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
public class WaterGunSystem : MonoBehaviour
{
    [Header("COMPONENT REFERENCES")]
    [SerializeField] private GameObject Gun;
    [SerializeField] private GameObject Arm;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private Transform launcherAimPoint; // Point d"origine de la visée
    [SerializeField] private Transform minDistancePoint; // Transform pour visualiser la distance minimale
    public Joystick aimJoystick;
    [Header("PROJECTILE & EFFECTS")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private ParticleSystem destructionEffectPrefab;
    [SerializeField] private ParticleSystem muzzleFlashEffect;
    [SerializeField] private float bulletSpeed = 25f;
    [SerializeField] private float bulletLifetime = 3f;
    [SerializeField] private int bulletDamage = 15;
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] private LayerMask collisionLayers;
    private PhotonView playerView;

    [Header("AIMING & ROTATION (FROM ROBUST LAUNCHER)")]
    [Tooltip("Angle maximum de visée vers le haut")]
    [SerializeField] private float maxUpwardAngle = 80f;
    [Tooltip("Angle maximum de visée vers le bas")]
    [SerializeField] private float maxDownwardAngle = 80f;
    [Tooltip("Distance minimale pour que la visée sactive")]
    [SerializeField] private float minDistanceToAim = 0.8f;
    [Tooltip("Vitesse de rotation de l'arme(pour une rotation fluide)")]
    [SerializeField] private float rotationSpeed = 25f;
    [Tooltip("Utiliser une rotation instantanée pour une réactivité maximale")]
    [SerializeField] private bool useInstantRotation = true;

    [Header("LAUNCHER CALIBRATION")]
    [Tooltip("Offset de rotation pour l'arme quand le joueur regarde à DROITE")]
    public float launcherRotationOffsetRight = 0f;
    [Tooltip("Offset de rotation pour l'arme quand le joueur regarde à GAUCHE")]
    public float launcherRotationOffsetLeft = 0f;
    [Tooltip("Offset de rotation pour la TRAJECTOIRE quand le joueur regarde à DROITE")]
    public float trajectoryRotationOffsetRight = 0f;
    [Tooltip("Offset de rotation pour la TRAJECTOIRE quand le joueur regarde à GAUCHE")]
    public float trajectoryRotationOffsetLeft = 0f;

    [Header("FIRING & AMMO SYSTEM")]
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private float reloadTime = 1.5f;

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

    // --- AMMO & STATE VARIABLES ---
    private int currentAmmo;
    private bool isReloading = false;
    private float nextFireTime = 0f;

    void Awake()
    {
        playerView = GetComponentInParent<PhotonView>();

        currentAmmo = maxAmmo;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // La séquence d"update la plus fiable, directement tirée du launcher
        HandleInputAndShooting();
        ApplyRotation();
        
        UpdateMinDistancePointPosition(); // Mise à jour du point de visualisation
    }

    private void HandleInputAndShooting()
    {
        if (playerView != null && !playerView.IsMine)
        {
            return; // If this is an online character that isn't mine, do nothing.
        }
        bool isAiming = false;
        bool isShooting = false;

        if (aimJoystick != null && aimJoystick.Direction.sqrMagnitude > 0.1f)
        {
            // --- MODE MOBILE ---
            isAiming = true;
            isShooting = true; // Tir automatique
            Vector3 joystickDirection = new Vector3(aimJoystick.Direction.x, aimJoystick.Direction.y, 0);
            mouseWorldPosition = launcherAimPoint.position + joystickDirection * 10f;
        }
        else
        {
            // --- MODE PC (SOURIS) ---
            isAiming = true;
            isShooting = Mouse.current.leftButton.isPressed;
            mouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        }

        // --- LOGIQUE DE VISÉE ---
        if (isAiming)
        {
            UpdatePlayerFacingDirection();
            aimFromPosition = launcherAimPoint != null ? (Vector2)launcherAimPoint.position : (Vector2)Gun.transform.position;
            Vector2 directionToMouse = (mouseWorldPosition - aimFromPosition);
            aimDirection = directionToMouse.normalized;
            if (directionToMouse.magnitude >= minDistanceToAim)
            {
                float worldAngleToMouse = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
                float clampedWorldAngle = ClampWorldAngle(worldAngleToMouse);
                worldArmRotation = clampedWorldAngle;
                float currentLauncherOffset = isPlayerFacingRight ? launcherRotationOffsetRight : launcherRotationOffsetLeft;
                worldLauncherRotation = clampedWorldAngle + currentLauncherOffset;
                float currentTrajectoryOffset = isPlayerFacingRight ? trajectoryRotationOffsetRight : trajectoryRotationOffsetLeft;
                worldTrajectoryRotation = clampedWorldAngle + currentTrajectoryOffset;
            }
        }

        // --- LOGIQUE DE TIR ---
        if (Keyboard.current.rKey.wasPressedThisFrame && !isReloading && currentAmmo < maxAmmo)
        {
            StartCoroutine(Reload());
            return;
        }

        if (isShooting && !isReloading && Time.time >= nextFireTime)
        {
            if (Vector2.Distance(mouseWorldPosition, aimFromPosition) < minDistanceToAim) return;

            if (currentAmmo > 0)
            {
                Shoot();
                currentAmmo--;
                nextFireTime = Time.time + fireRate;
                if (currentAmmo <= 0) StartCoroutine(Reload());
            }
            else
            {
                StartCoroutine(Reload());
            }
        }
    }
    private void UpdatePlayerFacingDirection()

    {
        if (playerTransform != null)
        {
            isPlayerFacingRight = playerTransform.localScale.x > 0;
        }
    }

    // LA LOGIQUE DE CLAMPING QUI MARCHE ENFIN
    private float ClampWorldAngle(float worldAngle)
    {
        // Normalise l"angle pour qu"il soit toujours entre -180 et 180
        worldAngle = (worldAngle + 180f) % 360f - 180f;

        if (isPlayerFacingRight)
        {
            return Mathf.Clamp(worldAngle, -maxDownwardAngle, maxUpwardAngle);
        }
        else
        {
            // Quand on est à gauche, on veut que l"angle soit entre (180 - maxUpwardAngle) et (180 + maxDownwardAngle)
            // On convertit l"angle de visée en son équivalent "gauche"
            float leftEquivAngle = 180 + worldAngle;
            leftEquivAngle = Mathf.Clamp(leftEquivAngle, 180 - maxUpwardAngle, 180 + maxDownwardAngle);
            // On le reconvertit en son équivalent "world"
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
            Gun.transform.rotation = gunTargetRotation;
        }
        else
        {
            Arm.transform.rotation = Quaternion.Slerp(Arm.transform.rotation, armTargetRotation, rotationSpeed * Time.deltaTime);
            Gun.transform.rotation = Quaternion.Slerp(Gun.transform.rotation, gunTargetRotation, rotationSpeed * Time.deltaTime);
        }
    }



    private void Shoot() // Or SpawnNextBall(), ShootArrow(), etc.
    {
        // Calculate the rotation once.
        Quaternion shootRotation = Quaternion.Euler(0, 0, worldTrajectoryRotation); // Or whatever your rotation variable is

        if (playerView != null) // --- ONLINE MODE ---
        {
            // This part is working perfectly, so we don't touch it.
            playerView.RPC("RPC_FireWeapon", RpcTarget.All, bulletPrefab.name, bulletSpawnPoint.position, shootRotation);
        }
        else // --- OFFLINE MODE (THE FIX) ---
        {
            // 1. Create the bullet locally, just like before.
            GameObject bulletInstance = Instantiate(bulletPrefab, bulletSpawnPoint.position, shootRotation);

            // 2. Get the BulletBehavior script from the new bullet.
            BulletBehavior bulletBehavior = bulletInstance.GetComponent<BulletBehavior>();

            // 3. THIS IS THE MISSING LINE. We must manually initialize the bullet in offline mode.
            if (bulletBehavior != null)
            {
                bulletBehavior.Initialize(shootRotation * Vector3.right);
            }
        }

        // Play sounds and effects locally for both modes.
        if (muzzleFlashEffect != null) muzzleFlashEffect.Play();
        if (audioSource != null && shootSound != null) audioSource.PlayOneShot(shootSound, shootSoundVolume);
    }
    private IEnumerator Reload()
    {
        if (isReloading) yield break;
        isReloading = true;
        Debug.Log("Reloading...");
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
        Debug.Log("Reload complete!");
    }

    private void UpdateMinDistancePointPosition()
    {
        if (minDistancePoint == null) return;
        minDistancePoint.position = aimFromPosition + (aimDirection.normalized * minDistanceToAim);
    }

    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmo;
    public bool IsReloading() => isReloading;

    private void OnDrawGizmos()
    {
        Vector2 origin = launcherAimPoint != null ? (Vector2)launcherAimPoint.position : (Gun != null ? (Vector2)Gun.transform.position : Vector2.zero);
        if (origin == Vector2.zero) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, mouseWorldPosition);
        Gizmos.color = (Vector2.Distance(mouseWorldPosition, origin) < minDistanceToAim) ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(origin, minDistanceToAim);

        if (bulletSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Quaternion shootRotation = Quaternion.Euler(0, 0, worldTrajectoryRotation);
            Gizmos.DrawRay(bulletSpawnPoint.position, shootRotation * Vector2.right * 3f);
        }
    }
}

public class WaterBullet : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float lifetime;
    private int damage;
    private LayerMask damageableLayers;
    private LayerMask collisionLayers;
    private ParticleSystem destructionEffectPrefab;
    private Rigidbody2D rb;
    private bool hasHit = false;

    [Header("AUDIO")]
    [SerializeField] private AudioClip collisionSound;
    [SerializeField, Range(0f, 1f)] private float collisionSoundVolume = 1f; // Volume for collision sound
    private AudioSource audioSource;

    public void Initialize(Vector2 dir, float spd, float life, int dmg, LayerMask dmgLayers, LayerMask colLayers, ParticleSystem destructionFx, AudioClip colSound, float colSoundVolume)
    {
        direction = dir.normalized;
        speed = spd;
        lifetime = life;
        damage = dmg;
        damageableLayers = dmgLayers;
        collisionLayers = colLayers;
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
        if (hasHit) return;

        if ((((1 << other.gameObject.layer) & damageableLayers) != 0) || (((1 << other.gameObject.layer) & collisionLayers) != 0))
        {
            hasHit = true;

            var enemyHealth = other.GetComponent<FleaHealth>();
            if (enemyHealth != null)
            {
                Vector2 attackDirection = (other.transform.position - transform.position).normalized;
                enemyHealth.TakeDamage(damage, attackDirection);
            }

            var sprayerHealth = other.GetComponent<SprayerHealth>();
            if (sprayerHealth != null)
            {
                Vector2 attackDirection = (other.transform.position - transform.position).normalized;
                sprayerHealth.TakeDamage(damage, attackDirection);
            }

            var flyHealth = other.GetComponent<FlyHealth>();
            if (flyHealth != null)
            {
                Vector2 attackDirection = (other.transform.position - transform.position).normalized;
                flyHealth.TakeDamage(damage, attackDirection);
            }

            // NEW: Handle InkHealth
            var inkHealth = other.GetComponent<InkHealth>();
            if (inkHealth != null)
            {
                Vector2 attackDirection = (other.transform.position - transform.position).normalized;
                // Assuming InkHealth.TakeDamage takes damage, attackDirection, and knockbackForce
                // You might need to adjust the knockbackForce value (e.g., 1f) based on your game design.
                inkHealth.TakeDamage(damage, attackDirection, 1f);
            }

            var RatKingHealth = other.GetComponent<RatKingHealth>();
            if (RatKingHealth != null)
            {
                Vector2 attackDirection = (other.transform.position - transform.position).normalized;
                // Assuming InkHealth.TakeDamage takes damage, attackDirection, and knockbackForce
                // You might need to adjust the knockbackForce value (e.g., 1f) based on your game design.
                RatKingHealth.TakeDamage(damage);
            }

            var BarrelExplosion = other.GetComponent<BarrelExplosion>();
            if (BarrelExplosion != null)
            {
                Vector2 attackDirection = (other.transform.position - transform.position).normalized;
                // Assuming InkHealth.TakeDamage takes damage, attackDirection, and knockbackForce
                // You might need to adjust the knockbackForce value (e.g., 1f) based on your game design.
                BarrelExplosion.TakeDamage(damage);
            }

            if (audioSource != null && collisionSound != null) audioSource.PlayOneShot(collisionSound, collisionSoundVolume);

            HandleDestruction();
        }
    }

    private void HandleDestruction()
    {
        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}

