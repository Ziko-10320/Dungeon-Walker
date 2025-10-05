using FirstGearGames.SmoothCameraShaker;
using Photon.Pun;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
public class MachineGunSystem : MonoBehaviour, IPunObservable, IPoolable
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
    public Joystick aimJoystick;
    private bool isShootingWithJoystick = false;
    [Header("PROJECTILE & EFFECTS")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private ParticleSystem destructionEffectPrefab;
    [SerializeField] private ParticleSystem muzzleFlashEffect;
    [SerializeField] private float bulletSpeed = 25f;
    [SerializeField] private float bulletLifetime = 3f;
    [SerializeField] private int bulletDamage = 15;
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] public LayerMask collisionLayers;
    [SerializeField] public LayerMask enemyLayers;
    private PhotonView playerView;

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
    [Header("Input Settings")]
    public bool PcInput = true;
    public bool MobileInput = false;

    [Header("LAUNCHER CALIBRATION")]
    [Tooltip("Offset de rotation pour l\"arme quand le joueur regarde à DROITE")]
    public float launcherRotationOffsetRight = 0f;
    [Tooltip("Offset de rotation pour l\"arme quand le joueur regarde à GAUCHE")]
    public float launcherRotationOffsetLeft = 0f;
    [Tooltip("Offset de rotation pour la TRAJECTOIRE quand le joueur regarde à DROITE")]
    public float trajectoryRotationOffsetRight = 0f;
    [Tooltip("Offset de rotation pour la TRAJECTOIRE quand le joueur regarde à GAUCHE")]
    public float trajectoryRotationOffsetLeft = 0f;
    public bool enableAimStabilization = true;
    public bool independentLauncherRotation = true;
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

    private Vector2 mouseScreenPosition; // Mouse screen position
   
    private Vector2 stabilizedMouseWorldPosition; // Stabilized mouse world position

    private PhotonView view;
    private PlayerSyncManager syncManager;
    private bool isOnlineMode = false;

    [Header("Object Pooling Settings")]
    [SerializeField] private int bulletPoolSize = 50; // More bullets for a machine gun
    [SerializeField] private int effectPoolSize = 30;
    void Awake()
    {
        view = GetComponentInParent<PhotonView>();
        syncManager = GetComponentInParent<PlayerSyncManager>();

        if (view != null && transform.root.CompareTag("OnlinePlayer"))
        {
            isOnlineMode = true;
            Debug.Log("MachineGunSystem: Online Mode Detected.");
        }
        else
        {
            isOnlineMode = false;
            Debug.Log("MachineGunSystem: Offline Mode Detected.");
        }
       

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
    public void CreatePools()
    {
        if (ObjectPoolManager.Instance != null)
        {
            if (bulletPrefab != null) ObjectPoolManager.Instance.CreatePool(bulletPrefab, bulletPoolSize);
            if (destructionEffectPrefab != null) ObjectPoolManager.Instance.CreatePool(destructionEffectPrefab.gameObject, effectPoolSize);
        }
    }
    void Update()
    {
        if (isOnlineMode && !view.IsMine)
        {
            // If we are online and this isn't our character, do nothing.
            // The PlayerSyncManager will handle rotation.
            return;
        }
        HandleInputAndShooting();
        ApplyRotation();
        
        UpdateMinDistancePointPosition();
        HandleOverheat();
    }
    private void HandleInputAndShooting()
    {
        if (playerView != null && !playerView.IsMine)
        {
            return; // If this is an online character that isn't mine, do nothing.
        }
        bool isAiming = false;
        bool isShooting = false;

        // --- MOBILE JOYSTICK LOGIC ---
        if (MobileInput)
        {
            if (aimJoystick != null && aimJoystick.Direction.sqrMagnitude > 0.1f)
            {
                // For mobile, aiming and shooting are tied to the joystick being active
                isAiming = true;
                isShooting = true;
                isShootingWithJoystick = true;

                // Calculate a world aim position based on the joystick's direction
                Vector3 joystickDirection = new Vector3(aimJoystick.Direction.x, aimJoystick.Direction.y, 0);
                mouseWorldPosition = launcherAimPoint.position + joystickDirection * 10f;
            }
            else
            {
                // If the joystick is released, stop shooting
                isShootingWithJoystick = false;
            }
        }

        // --- PC MOUSE LOGIC (Fallback or Primary) ---
        // Only run PC input if it's enabled AND the mobile joystick isn't currently being used
        if (PcInput && !isShootingWithJoystick)
        {
            isAiming = true; // With a mouse, you are always aiming
            isShooting = Mouse.current.leftButton.isPressed; // Shooting is holding the left mouse button

            // Get the mouse position in the world
            mouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        }

        // --- AIMING LOGIC (runs if either input is active) ---
        if (isAiming)
        {
            stabilizedMouseWorldPosition = mouseWorldPosition;
            UpdatePlayerFacingDirection();
            CalculateAimDirection();
        }

        // --- OVERHEAT & SHOOTING LOGIC ---
        if (isOverheated)
        {
            // If overheated, start the cooldown timer
            overheatCooldownTimer -= Time.deltaTime;
            if (overheatCooldownTimer <= 0f)
            {
                isOverheated = false;
                currentOverheatValue = 0f; // Reset heat when cooldown is done
            }
            return; // Stop here if overheated, no shooting allowed
        }

        if (isShooting)
        {
            // Increase heat while shooting
            currentOverheatValue += Time.deltaTime;
            currentOverheatValue = Mathf.Min(currentOverheatValue, maxOverheatTime);

            // Check if we can fire
            if (Time.time >= nextFireTime)
            {
                // Don't shoot if the aim is inside the dead zone
                if (Vector2.Distance(mouseWorldPosition, aimFromPosition) < minDistanceToAim) return;

                Shoot();
                SpawnEmptyBullet();
                nextFireTime = Time.time + fireRate;
            }
        }
        else
        {
            // If not shooting, cool down the weapon
            currentOverheatValue -= Time.deltaTime * (maxOverheatTime / overheatCoolDownTime);
            currentOverheatValue = Mathf.Max(0f, currentOverheatValue);
        }
    }




    private void UpdatePlayerFacingDirection()
    {
        if (playerTransform != null)
        {
            KritinaMovement playerMovement = playerTransform.GetComponent<KritinaMovement>();
            if (playerMovement != null)
            {
                isPlayerFacingRight = playerMovement.isFacingRight;
            }
            else
            {
                isPlayerFacingRight = playerTransform.localScale.x > 0;
            }
        }
    }

    private void CalculateAimDirection()
    {
        aimFromPosition = launcherAimPoint != null ? launcherAimPoint.position : Gun.transform.position;
        Vector2 directionToMouse = (stabilizedMouseWorldPosition - aimFromPosition);
        float distanceToMouse = directionToMouse.magnitude;

        if (distanceToMouse < minDistanceToAim)
        {
            return;
        }

        float worldAngleToMouse = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
        float clampedWorldAngle = ClampWorldAngle(worldAngleToMouse);

        // Set world space rotations
        worldArmRotation = clampedWorldAngle;

        // Apply launcher offsets
        float currentLauncherOffset = isPlayerFacingRight ? launcherRotationOffsetRight : launcherRotationOffsetLeft;
        worldLauncherRotation = clampedWorldAngle + currentLauncherOffset;

        // Apply trajectory offsets (this controls the green line - where projectiles actually go)
        float currentTrajectoryOffset = isPlayerFacingRight ? trajectoryRotationOffsetRight : trajectoryRotationOffsetLeft;
        worldTrajectoryRotation = clampedWorldAngle + currentTrajectoryOffset;

        aimDirection = directionToMouse.normalized;
    }

    private float ClampWorldAngle(float worldAngle)
    {
        while (worldAngle > 180f) worldAngle -= 360f;
        while (worldAngle < -180f) worldAngle += 360f;

        if (worldAngle >= -maxDownwardAngle && worldAngle <= maxUpwardAngle)
        {
            return Mathf.Clamp(worldAngle, -maxDownwardAngle, maxUpwardAngle);
        }
        else if (worldAngle > 90f && worldAngle < 270f)
        {
            float leftUpLimit = 180f - maxDownwardAngle;
            float leftDownLimit = 180f + maxDownwardAngle;
            return Mathf.Clamp(worldAngle, leftUpLimit, leftDownLimit);
        }

        return worldAngle;
    }

    private void ApplyRotation()
    {
        Quaternion armWorldRotation = Quaternion.Euler(0, 0, worldArmRotation);
        Quaternion launcherWorldRotation = Quaternion.Euler(0, 0, worldLauncherRotation);
        Quaternion trajectoryWorldRotation = Quaternion.Euler(0, 0, worldTrajectoryRotation);

        if (useInstantRotation)
        {
            Arm.transform.rotation = armWorldRotation;
            if (independentLauncherRotation)
            {
                Gun.transform.rotation = launcherWorldRotation;
            }
            else
            {
                Gun.transform.rotation = armWorldRotation;
            }

           
        }
        else
        {
            Arm.transform.rotation = Quaternion.Lerp(Arm.transform.rotation, armWorldRotation, rotationSpeed * Time.deltaTime);
            if (independentLauncherRotation)
            {
                Gun.transform.rotation = Quaternion.Lerp(Gun.transform.rotation, launcherWorldRotation, rotationSpeed * Time.deltaTime);
            }
            else
            {
                Gun.transform.rotation = Quaternion.Lerp(Gun.transform.rotation, armWorldRotation, rotationSpeed * Time.deltaTime);
            }

           
        }
    }
   

    private void Shoot()
{
        // Local effects are always played immediately for responsiveness.
        if (muzzleFlashEffect != null && bulletSpawnPoint != null)
        {
            // 1. Instantiate the prefab at the bullet spawn point's position and rotation.
            ParticleSystem flashInstance = Instantiate(muzzleFlashEffect, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

            // 2. (Optional but good practice) Destroy the effect after it has played.
            // This prevents your Hierarchy from filling up with thousands of old effects.
            Destroy(flashInstance.gameObject, flashInstance.main.duration);
        }
        else
        {
            if (muzzleFlashEffect == null) Debug.LogWarning("MuzzleFlashEffect prefab is not assigned in the Inspector!");
            if (bulletSpawnPoint == null) Debug.LogWarning("BulletSpawnPoint is not assigned in the Inspector!");
        } 
        if (audioSource != null && shootSound != null) audioSource.PlayOneShot(shootSound, shootSoundVolume);

    // Calculate spawn parameters once.
    float spread = Random.Range(-maxSpreadAngle / 2f, maxSpreadAngle / 2f);
    Quaternion shootRotation = Quaternion.Euler(0, 0, worldTrajectoryRotation + spread);
    Vector2 randomSpawnOffset = Random.insideUnitCircle * spawnAreaRadius;
    Vector3 spawnPosition = bulletSpawnPoint.position + (Vector3)randomSpawnOffset;
    Vector2 shootDirection = shootRotation * Vector2.right;

    // --- THE MODE-AWARE LOGIC ---
    if (isOnlineMode)
    {
            // --- ONLINE ---
            // 1. The shooter creates their own "real" bullet locally.
            GameObject myBullet = ObjectPoolManager.Instance.SpawnFromPool(bulletPrefab, spawnPosition, shootRotation);
            InitializeBullet(myBullet, shootDirection, true); // true = isReal

        // 2. The shooter tells everyone else to create a "visual" bullet via an RPC.
        // This RPC must be on your PlayerSyncManager.
        if (syncManager != null)
        {
                view.RPC("RPC_SpawnVisualBullet", RpcTarget.Others, spawnPosition, shootRotation, shootDirection);
            }
    }
    else
    {
            // --- OFFLINE ---
            GameObject myBullet = ObjectPoolManager.Instance.SpawnFromPool(bulletPrefab, spawnPosition, shootRotation);
            InitializeBullet(myBullet, shootDirection, true); // true = isReal
    }
}
    [PunRPC]
    public void RPC_SpawnVisualBullet(Vector3 position, Quaternion rotation, Vector2 direction)
    {
        // This runs on all other clients.
        GameObject visualBullet = ObjectPoolManager.Instance.SpawnFromPool(bulletPrefab, position, rotation);
        InitializeBullet(visualBullet, direction, false); // false = isReal
    }
    private void InitializeBullet(GameObject bullet, Vector2 direction, bool isReal)
    {
        BulletComponent bulletComponent = bullet.AddComponent<BulletComponent>();
        bulletComponent.machineGunSystem = this; // Give it a reference back to this script
        bulletComponent.isReal = isReal;
        bulletComponent.ResetHitFlag();
        bulletComponent.Initialize(direction, bulletSpeed, bulletLifetime, bulletDamage, damageableLayers, collisionLayers, enemyLayers, destructionEffectPrefab, bulletCollisionSound, bulletCollisionSoundVolume);
    }
    public void TriggerNetworkedDestruction(Vector3 position)
    {
        if (isOnlineMode)
        {
            // ONLINE: We tell the PlayerSyncManager to send an RPC to EVERYONE.
            if (syncManager != null && destructionEffectPrefab != null)
            {
                // We send the NAME of the prefab. It must be in a "Resources" folder.
                syncManager.InstantiateEffect(destructionEffectPrefab.name, position, Quaternion.identity);
            }
        }
        else
        {
            // --- OFFLINE: THIS IS THE DEFINITIVE FIX ---
            if (destructionEffectPrefab != null)
            {
                // 1. Instantiate the ParticleSystem itself, not its GameObject.
                ParticleSystem effectInstance = Instantiate(destructionEffectPrefab, position, Quaternion.identity);

                // 2. Explicitly tell it to play.
                effectInstance.Play();
            }
        }
    }
   
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We are the owner. We send our arm and gun rotation.
            stream.SendNext(Arm.transform.rotation);
            stream.SendNext(Gun.transform.rotation);
        }
        else
        {
            // We are a remote client. We receive and apply the rotation.
            this.Arm.transform.rotation = (Quaternion)stream.ReceiveNext();
            this.Gun.transform.rotation = (Quaternion)stream.ReceiveNext();
        }
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
    public void TriggerDestructionEffect(Vector3 position)
    {
        if (destructionEffectPrefab != null && ObjectPoolManager.Instance != null)
        {
            // Get an effect from the pool. The PoolableParticle script will handle the rest.
            ObjectPoolManager.Instance.SpawnFromPool(destructionEffectPrefab.gameObject, position, Quaternion.identity);
        }

        // Play the collision sound at the impact point
        if (audioSource != null && bulletCollisionSound != null)
        {
            AudioSource.PlayClipAtPoint(bulletCollisionSound, position, bulletCollisionSoundVolume);
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
    public MachineGunSystem machineGunSystem;
    public bool isReal;
    private bool hasHit = false;

    public void Initialize(Vector2 dir, float spd, float life, int dmg, LayerMask dmgLayers, LayerMask colLayers, LayerMask enemyLyrs, ParticleSystem destructionFx, AudioClip colSound, float colSoundVolume)
    {
        // --- THIS IS THE CRITICAL FIX ---
        // We must assign the incoming parameters to the class-level variables.
        this.direction = dir.normalized;
        this.speed = spd;
        this.lifetime = life;
        this.damage = dmg;
        this.damageableLayers = dmgLayers;
        this.collisionLayers = colLayers;
        this.enemyLayers = enemyLyrs;
        this.destructionEffectPrefab = destructionFx;
        this.collisionSound = colSound;
        this.collisionSoundVolume = colSoundVolume;
        // --- END OF FIX ---

        // The rest of the function was already correct.
        rb = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        // We no longer set the velocity here, as FixedUpdate will handle it.

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        StartCoroutine(DeactivateAfterTime(life));

    }
    void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }

    void FixedUpdate()
    {
        if (rb != null) rb.velocity = direction * speed;
    }

    public void ResetHitFlag()
    {
        this.hasHit = false;
    }
    private void HandleCollision(GameObject collidedObject)
    {
        // If this bullet has already processed a hit, do nothing.
        if (hasHit) return;

        // Check what we hit
        bool hitEnemy = ((1 << collidedObject.layer) & enemyLayers) != 0;
        bool hitWall = ((1 << collidedObject.layer) & collisionLayers) != 0;

        // If we didn't hit an enemy OR a wall, ignore the collision and continue flying.
        if (!hitEnemy && !hitWall)
        {
            return;
        }

        // --- If we are here, it means we hit a valid target (enemy or wall) ---

        // Mark this bullet as "hit" to prevent any further collision checks.
        hasHit = true;

        // --- LOGIC FOR THE "REAL" BULLET (THE ONE THAT DEALS DAMAGE) ---
        if (isReal)
        {
            // 1. If it hit an enemy, deal damage.
            if (hitEnemy)
            {
                FleaHealth fleaHealth = collidedObject.GetComponent<FleaHealth>();
                if (fleaHealth != null)
                {
                    fleaHealth.TakeDamage(damage, transform.right);
                    if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                        L3antixSuperMeter.Instance.AddDamage(damage);

                    if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                        PlayerSuperMeter.Instance.AddDamage(damage);

                }

                SprayerHealth sprayerHealth = collidedObject.GetComponent<SprayerHealth>();
                if (sprayerHealth != null)
                {
                    sprayerHealth.TakeDamage(damage, transform.right);
                    if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                        L3antixSuperMeter.Instance.AddDamage(damage);

                    if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                        PlayerSuperMeter.Instance.AddDamage(damage);
                }

                FlyHealth flyHealth = collidedObject.GetComponent<FlyHealth>();
                if (flyHealth != null)
                {
                    flyHealth.TakeDamage(damage, transform.right);
                    if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                        L3antixSuperMeter.Instance.AddDamage(damage);

                    if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                        PlayerSuperMeter.Instance.AddDamage(damage);
                }

                InkHealth inkHealth = collidedObject.GetComponent<InkHealth>();
                if (inkHealth != null)
                {
                    inkHealth.TakeDamage(damage, transform.right, 1f);
                    if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                        L3antixSuperMeter.Instance.AddDamage(damage);

                    if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                        PlayerSuperMeter.Instance.AddDamage(damage);

                }
                RatKingHealth ratKingHealth = collidedObject.GetComponent<RatKingHealth>();
                if (ratKingHealth != null)
                {
                     ratKingHealth.TakeDamage(damage);
                        if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                            L3antixSuperMeter.Instance.AddDamage(damage);

                        if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                            PlayerSuperMeter.Instance.AddDamage(damage);   
                }

                    BarrelExplosion barrelExplosion = collidedObject.GetComponent<BarrelExplosion>();
                    if (barrelExplosion != null)
                    {
                        barrelExplosion.TakeDamage(damage);
                    }
                }

                machineGunSystem.TriggerDestructionEffect(transform.position);

            // 3. The "real" bullet destroys itself.
            gameObject.SetActive(false);

        }
        else
            {
            // --- LOGIC FOR THE "VISUAL" BULLET ---
            // Visual copies just destroy themselves silently. They don't deal damage or create effects.
            gameObject.SetActive(false);

        }
    }
    private System.Collections.IEnumerator DeactivateAfterTime(float delay)
    {
        yield return new WaitForSeconds(delay);
        // This check prevents errors if the bullet was already disabled by a collision.
        if (gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
    }
}

    // Interface for damageable objects
    public interface IDamageable
    {
        void TakeDamage(int damage);
    }



