using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class WaterGunSystem : MonoBehaviour, IPunObservable, IPoolable
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
    [SerializeField] public ParticleSystem destructionEffectPrefab;
    [SerializeField] private ParticleSystem muzzleFlashEffect;
    [SerializeField] public float bulletSpeed = 25f;
    [SerializeField] public float bulletLifetime = 3f;
    [SerializeField] public int bulletDamage = 15;
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] public LayerMask collisionLayers;
    [Header("Weapon Upgrade Data")]
    [SerializeField] private WeaponData weaponData;
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

    private PhotonView view;
    private bool isOnlineMode = false;

    [SerializeField] private int bulletPoolSize = 20;
    [SerializeField] private int effectPoolSize = 20;

    [Header("UI REFERENCES")]
    [Tooltip("The TextMeshPro UI element that will display the ammo count.")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [Tooltip("The empty Transform on the player that the ammo UI should follow.")]
    [SerializeField] private Transform ammoUiFollowPoint;
    [Tooltip("The main UI Canvas for positioning calculations.")]
    [SerializeField] private Canvas uiCanvas;
    void Awake()
    {
        view = GetComponentInParent<PhotonView>();
        if (view != null && transform.root.CompareTag("OnlinePlayer"))
        {
            isOnlineMode = true;
        }
        currentAmmo = maxAmmo;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        ApplyWeaponUpgrades();
        UpdateAmmoUI();
        FindCanvas();
    }
    void OnEnable()
    {
        // When the weapon is switched to, immediately update and show the UI.
        UpdateAmmoUI();
    }

    // This method is called automatically when the script is disabled
    void OnDisable()
    {
        // When the weapon is switched away from, hide the ammo UI.
        if (ammoText != null)
        {
            ammoText.gameObject.SetActive(false);
        }

        // Also, if we were in the middle of reloading, cancel it.
        isReloading = false;
        StopAllCoroutines();
    }
    private void UpdateAmmoUI()
    {
        if (ammoText == null) return; // Safety check

        // --- THIS IS THE NEW LOGIC ---
        if (isReloading)
        {
            // If we are reloading, show the special text.
            ammoText.text = "-- / --";
        }
        else
        {
            // Otherwise, show the normal "current/max" ammo count.
            ammoText.text = $"{currentAmmo} / {maxAmmo}";
        }
        // --- END OF NEW LOGIC ---

        // This part remains the same: only show the UI if the script is active.
        bool shouldBeVisible = this.enabled;
        if (ammoText.gameObject.activeSelf != shouldBeVisible)
        {
            ammoText.gameObject.SetActive(shouldBeVisible);
        }
    }

    // METHOD 2: The position update logic (copied from your BowSystem)
    private void UpdateAmmoUIPosition()
    {
        // If any references are missing, or the text is inactive, do nothing.
        if (ammoText == null || !ammoText.gameObject.activeInHierarchy || ammoUiFollowPoint == null || uiCanvas == null)
        {
            return;
        }

        // Convert the world position of our follow point to a screen position.
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(ammoUiFollowPoint.position);

        // Convert the screen point into a local position within the canvas.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)uiCanvas.transform,
            screenPoint,
            uiCanvas.worldCamera, // Use the canvas's camera
            out Vector2 localPosition
        );

        // Apply the correctly converted local position.
        ammoText.transform.localPosition = localPosition;
    }

    // METHOD 3: A helper to find the canvas if it's not assigned
    private void FindCanvas()
    {
        if (uiCanvas == null)
        {
            uiCanvas = FindObjectOfType<Canvas>();
            if (uiCanvas == null)
            {
                Debug.LogError("WaterGunSystem: No UI Canvas found in the scene!");
            }
        }
    }
    public void CreatePools()
    {
        if (ObjectPoolManager.Instance != null)
        {
            if (bulletPrefab != null) ObjectPoolManager.Instance.CreatePool(bulletPrefab, bulletPoolSize);
            if (destructionEffectPrefab != null) ObjectPoolManager.Instance.CreatePool(destructionEffectPrefab.gameObject, effectPoolSize);
        }
    }
    private void ApplyWeaponUpgrades()
    {
        if (weaponData == null)
        {
            Debug.Log("No WeaponData assigned to WaterGunSystem. Using default stats.");
            return;
        }

        int currentLevel = InventoryManager.Instance.GetWeaponLevel(weaponData.name);

        if (currentLevel > 0)
        {
            Debug.Log("Applying upgrades for " + weaponData.weaponName + " at Level " + currentLevel);

            WeaponUpgradeData currentUpgrade = weaponData.upgradeLevels[currentLevel - 1];

            // --- OVERRIDE THE PISTOL'S STATS ---
            this.bulletDamage = currentUpgrade.pistolStats.pistolDamage;
            this.fireRate = currentUpgrade.pistolStats.pistolFireRate;
            this.bulletSpeed = currentUpgrade.pistolStats.pistolBulletSpeed;
            this.maxAmmo = currentUpgrade.pistolStats.pistolAmmoCapacity;
            this.reloadTime = currentUpgrade.pistolStats.pistolReloadSpeed;

            // --- IMPORTANT: Reset ammo to the new max ---
            this.currentAmmo = this.maxAmmo;

            Debug.Log("New Stats -> Damage: " + this.bulletDamage + ", Fire Rate: " + this.fireRate + ", Capacity: " + this.maxAmmo);
        }
        else
        {
            Debug.Log(weaponData.weaponName + " is Level 0. Using default stats.");
        }
    }
    void Update()
    {
        if (isOnlineMode && !view.IsMine)
        {
            // La synchronisation via OnPhotonSerializeView s'occupera de la rotation.
            return;
        }
        // La séquence d"update la plus fiable, directement tirée du launcher
        HandleInputAndShooting();
        ApplyRotation();

        UpdateMinDistancePointPosition(); // Mise à jour du point de visualisation
        UpdateAmmoUI();
        UpdateAmmoUIPosition();
    }

    private void HandleInputAndShooting()
    {
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

    private void Shoot()
    {
        // 1. Calculate direction and rotation
        Quaternion shootRotation = Quaternion.Euler(0, 0, worldTrajectoryRotation);
        Vector2 shootDirection = shootRotation * Vector2.right;

        // 2. Play local effects for immediate feedback
        if (muzzleFlashEffect != null) muzzleFlashEffect.Play();
        if (audioSource != null && shootSound != null) audioSource.PlayOneShot(shootSound, shootSoundVolume);

        // 3. Create the "real" bullet that deals damage.
        GameObject myBullet = ObjectPoolManager.Instance.SpawnFromPool(bulletPrefab, bulletSpawnPoint.position, shootRotation);
        if (myBullet == null) return; // Safety check

        // 2. Get the correct script component: BulletBehavior.
        BulletBehavior myBulletScript = myBullet.GetComponent<BulletBehavior>();
        if (myBulletScript != null)
        {
            // Set stats
            myBulletScript.bulletSpeed = this.bulletSpeed;
            myBulletScript.bulletDamage = this.bulletDamage;
            myBulletScript.collisionLayers = this.collisionLayers;
            myBulletScript.waterExplosionPrefab = this.destructionEffectPrefab.gameObject;

            // Call the new Fire method
            myBulletScript.Fire(shootDirection);
        }

        // 4. If we are online, we send a message to everyone else.
        if (isOnlineMode)
        {
            view.RPC("RPC_SpawnVisualBullet", RpcTarget.Others, bulletSpawnPoint.position, shootDirection);
        }
    }



    // --- AJOUT : LA FONCTION DE SYNCHRONISATION POUR LA ROTATION DE L'ARME ---
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Le propriétaire envoie la rotation de l'arme et du bras.
            stream.SendNext(Arm.transform.rotation);
            stream.SendNext(Gun.transform.rotation);
        }
        else
        {
            // Les autres reçoivent et appliquent la rotation.
            Arm.transform.rotation = (Quaternion)stream.ReceiveNext();
            Gun.transform.rotation = (Quaternion)stream.ReceiveNext();
        }
    }
    private IEnumerator Reload()
    {
        if (isReloading) yield break;
        isReloading = true;
        Debug.Log("Reloading...");
        UpdateAmmoUI();
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
        UpdateAmmoUI();
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
    private float lifetime;
    public float speed;
    public int damage;
    public LayerMask damageableLayers;
    public LayerMask collisionLayers;
    public ParticleSystem destructionEffectPrefab;
    public AudioClip collisionSound;
    public float collisionSoundVolume;
    private Rigidbody2D rb;
    private bool hasHit = false;

    private Coroutine deactivateCoroutine;

    [Header("AUDIO")]
    private AudioSource audioSource;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    void OnEnable()
    {
        hasHit = false;
    }
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

        if (deactivateCoroutine != null)
        {
            StopCoroutine(deactivateCoroutine);
        }
        deactivateCoroutine = StartCoroutine(DeactivateAfterTime(lifetime));
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
                FindObjectOfType<SuperMoveController>().superMeter.AddDamage(damage);
            }

            var sprayerHealth = other.GetComponent<SprayerHealth>();
            if (sprayerHealth != null)
            {
                Vector2 attackDirection = (other.transform.position - transform.position).normalized;
                sprayerHealth.TakeDamage(damage, attackDirection);
                FindObjectOfType<SuperMoveController>().superMeter.AddDamage(damage);
            }
            var FleaHealthV2 = other.GetComponent<FleaHealthV2>();
            if (FleaHealthV2 != null)
            {
                Vector2 attackDirection = (other.transform.position - transform.position).normalized;
                FleaHealthV2.TakeDamage(damage, attackDirection);
                FindObjectOfType<SuperMoveController>().superMeter.AddDamage(damage);
            }
            var flyHealth = other.GetComponent<FlyHealth>();
            if (flyHealth != null)
            {
                Vector2 attackDirection = (other.transform.position - transform.position).normalized;
                flyHealth.TakeDamage(damage, attackDirection);
                FindObjectOfType<SuperMoveController>().superMeter.AddDamage(damage);
            }

            // NEW: Handle InkHealth
            var inkHealth = other.GetComponent<InkHealth>();
            if (inkHealth != null)
            {
                Vector2 attackDirection = (other.transform.position - transform.position).normalized;
                // Assuming InkHealth.TakeDamage takes damage, attackDirection, and knockbackForce
                // You might need to adjust the knockbackForce value (e.g., 1f) based on your game design.
                inkHealth.TakeDamage(damage, attackDirection, 1f);
                FindObjectOfType<SuperMoveController>().superMeter.AddDamage(damage);
            }

            var RatKingHealth = other.GetComponent<RatKingHealth>();
            if (RatKingHealth != null)
            {
                Vector2 attackDirection = (other.transform.position - transform.position).normalized;
                // Assuming InkHealth.TakeDamage takes damage, attackDirection, and knockbackForce
                // You might need to adjust the knockbackForce value (e.g., 1f) based on your game design.
                RatKingHealth.TakeDamage(damage);
                FindObjectOfType<SuperMoveController>().superMeter.AddDamage(damage);
            }

          

            if (audioSource != null && collisionSound != null) audioSource.PlayOneShot(collisionSound, collisionSoundVolume);

            HandleDestruction();
        }
    }

    private void HandleDestruction()
    {
        if (destructionEffectPrefab != null)
        {
            // Instantiate the effect...
            ParticleSystem effectInstance = Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
          
        }

        // Stop the lifetime timer if it's running.
        if (deactivateCoroutine != null)
        {
            StopCoroutine(deactivateCoroutine);
            deactivateCoroutine = null;
        }
        // Deactivate the bullet so it can be reused.
        gameObject.SetActive(false);
    }

    private IEnumerator DeactivateAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        gameObject.SetActive(false);
    }
    public void Fire(Vector2 direction, float life)
    {
        // Make sure the Rigidbody is simulated
        rb.simulated = true;
        // Apply velocity directly
        rb.velocity = direction * speed;
        // Start the deactivation timer
        StartCoroutine(DeactivateAfterTime(life));
    }

    // --- ADD THIS SECOND METHOD ---
    void OnDisable()
    {
        // Stop all timers when the bullet is returned to the pool
        StopAllCoroutines();
        // Stop all movement and turn off physics until it's needed again
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
    }

}

