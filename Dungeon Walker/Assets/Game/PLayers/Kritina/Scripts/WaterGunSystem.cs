using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class WaterGunSystem : MonoBehaviour
{
    [Header("COMPONENT REFERENCES")]
    [SerializeField] private GameObject Gun;
    [SerializeField] private GameObject Arm;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private Transform launcherAimPoint; // Point d'origine de la visée
    [SerializeField] private Transform minDistancePoint; // Transform pour visualiser la distance minimale

    [Header("PROJECTILE & EFFECTS")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private ParticleSystem destructionEffectPrefab;
    [SerializeField] private ParticleSystem muzzleFlashEffect;
    [SerializeField] private float bulletSpeed = 25f;
    [SerializeField] private float bulletLifetime = 3f;
    [SerializeField] private int bulletDamage = 15;
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] private LayerMask collisionLayers;

    [Header("AIMING & ROTATION (FROM ROBUST LAUNCHER)")]
    [Tooltip("Angle maximum de visée vers le haut")]
    [SerializeField] private float maxUpwardAngle = 80f;
    [Tooltip("Angle maximum de visée vers le bas")]
    [SerializeField] private float maxDownwardAngle = 80f;
    [Tooltip("Distance minimale pour que la visée s'active")]
    [SerializeField] private float minDistanceToAim = 0.8f;
    [Tooltip("Vitesse de rotation de l'arme (pour une rotation fluide)")]
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
        currentAmmo = maxAmmo;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // La séquence d'update la plus fiable, directement tirée du launcher
        HandleAiming();
        ApplyRotation();
        HandleShooting();
        UpdateMinDistancePointPosition(); // Mise à jour du point de visualisation
    }

    private void HandleAiming()
    {
        // 1. Mettre à jour la direction du joueur
        UpdatePlayerFacingDirection();

        // 2. Obtenir la position de la souris
        mouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // 3. Définir le point d'origine de la visée
        aimFromPosition = launcherAimPoint != null ? (Vector2)launcherAimPoint.position : (Vector2)Gun.transform.position;

        // 4. Calculer la direction et l'angle vers la souris
        Vector2 directionToMouse = (mouseWorldPosition - aimFromPosition);
        aimDirection = directionToMouse.normalized; // Stocker la direction normalisée

        // Si la souris est dans la zone morte, on ne met pas à jour les angles
        if (directionToMouse.magnitude < minDistanceToAim)
        {
            return;
        }

        // 5. Calculer l'angle en degrés
        float worldAngleToMouse = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;

        // 6. Brider l'angle avec la logique exacte du launcher
        float clampedWorldAngle = ClampWorldAngle(worldAngleToMouse);

        // 7. Définir les rotations finales en appliquant les offsets
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

    // LA LOGIQUE DE CLAMPING QUI MARCHE ENFIN
    private float ClampWorldAngle(float worldAngle)
    {
        // Normalise l'angle pour qu'il soit toujours entre -180 et 180
        worldAngle = (worldAngle + 180f) % 360f - 180f;

        if (isPlayerFacingRight)
        {
            return Mathf.Clamp(worldAngle, -maxDownwardAngle, maxUpwardAngle);
        }
        else
        {
            // Quand on est à gauche, on veut que l'angle soit entre (180 - maxUpwardAngle) et (180 + maxDownwardAngle)
            // On convertit l'angle de visée en son équivalent "gauche"
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

    private void HandleShooting()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame && !isReloading && currentAmmo < maxAmmo)
        {
            StartCoroutine(Reload());
            return;
        }

        if (Mouse.current.leftButton.isPressed && !isReloading && Time.time >= nextFireTime)
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

    private void Shoot()
    {
        if (muzzleFlashEffect != null) muzzleFlashEffect.Play();
        if (audioSource != null && shootSound != null) audioSource.PlayOneShot(shootSound);

        Quaternion shootRotation = Quaternion.Euler(0, 0, worldTrajectoryRotation);
        Vector2 shootDirection = shootRotation * Vector2.right;

        GameObject bulletInstance = Instantiate(bulletPrefab, bulletSpawnPoint.position, shootRotation);
        WaterBullet bulletScript = bulletInstance.AddComponent<WaterBullet>();
        bulletScript.Initialize(shootDirection, bulletSpeed, bulletLifetime, bulletDamage, damageableLayers, collisionLayers, destructionEffectPrefab);
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
    private AudioSource audioSource;

    public void Initialize(Vector2 dir, float spd, float life, int dmg, LayerMask dmgLayers, LayerMask colLayers, ParticleSystem destructionFx)
    {
        direction = dir.normalized;
        speed = spd;
        lifetime = life;
        damage = dmg;
        damageableLayers = dmgLayers;
        collisionLayers = colLayers;
        destructionEffectPrefab = destructionFx;

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

            if (audioSource != null && collisionSound != null) audioSource.PlayOneShot(collisionSound);

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


