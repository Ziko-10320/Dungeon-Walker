using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using Photon.Pun;
public class BatAttackSystem : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Animator playerAnimator; // Reference to the player\"s Animator
    [SerializeField] private string anticipationTriggerName = "Anticipation"; // Name of the Anticipation Trigger in the Animator
    [SerializeField] private string attackTriggerName = "BatAttack"; // Name of the Attack Trigger in the Animator
    [SerializeField] private string upwardAttackTriggerName = "UpwardAttack"; // Name of the Upward Attack Trigger in the Animator
    [SerializeField] private string throwBatTriggerName = "ThrowBat"; // Name of the Throw Bat Trigger in the Animator
    [SerializeField] private float anticipationDuration = 1.0f; // Duration of the anticipation animation
    [SerializeField] private float attackCooldown = 1.0f; // Cooldown duration for the attack (in seconds)
    [SerializeField] private int damage = 20; // Damage amount dealt by the attack
    [Header("Mobile Controls")]
    [Header("Input Control")]
    public bool MobileInput = true; // Set to true to enable mobile input, false to disable
    public bool PcInput = false;    // Set to true to enable PC input, false to disable
    private PhotonView playerView;
    [Tooltip("Faites glisser le joystick d'attaque de la batte ici.")]
    public Joystick attackJoystick;
    public Joystick runningJoystick;
    [Tooltip("Seuil pour différencier un 'tap' d'une 'visée'.")]
    [SerializeField] private float joystickAimThreshold = 0.5f;
    [Tooltip("Temps maximum en secondes pour qu'un contact soit considéré comme un 'tap'.")]
    [SerializeField] private float joystickTapTime = 0.2f;

    private bool isJoystickHeld = false;
    private float joystickHoldTime = 0f;
    [Header("Throw Slash Settings")]
    [SerializeField] private GameObject throwSlashPrefab; // Prefab of the ThrowSlash projectile
    [SerializeField] private Transform throwSlashSpawnPointRight; // Point where the ThrowSlash is spawned when facing right
    [SerializeField] private Transform throwSlashSpawnPointLeft; // Point where the ThrowSlash is spawned when facing left
    [SerializeField] private float throwSlashSpeed = 15f; // Speed of the thrown ThrowSlash
    [SerializeField] private int throwSlashDamage = 20; // Damage dealt by ThrowSlash
    [SerializeField] private GameObject bat2Prefab; // Prefab of the Bat2 to spawn on ground hit
    [SerializeField] public float aimVerticalOffsetRightCursor = 0f; // Vertical offset for aim when cursor is to the right of the player
    [SerializeField] public float aimVerticalOffsetLeftCursor = 0f; // Vertical offset for aim when cursor is to the left of the player

    [Header("Bat Pickup Settings")]
    [SerializeField] private float batPickupRange = 1.5f; // Range within which player can pick up the Bat2

    [Header("Damage Area Settings")]
    [SerializeField] private Transform attackPoint; // Origin point of the normal attack (usually in front of the player)
    [SerializeField] private Transform upwardAttackPoint; // Origin point of the upward attack (usually above the player)
    [SerializeField] private float attackRange = 0.5f; // Radius of the attack area (circle)
    [SerializeField] private LayerMask enemyLayers; // Layers of enemies that can receive damage
    [SerializeField] private LayerMask groundLayer; // Layer mask for ground collision

    [Header("Direction Detection Settings")]
    [SerializeField] private float upwardZoneMinY = 0.5f; // Minimum Y-coordinate for upward attack zone (relative to player)
    [SerializeField] private float upwardZoneMaxY = 5.0f; // Maximum Y-coordinate for upward attack zone (relative to player)
    [SerializeField] private float normalZoneMaxY = 0.4f; // Maximum Y-coordinate for normal attack zone (relative to player)
    [SerializeField] private bool showDirectionDebug = false; // Show debug information for mouse direction

    [Header("Audio Settings")]
    [SerializeField] public AudioSource audioSource; // Reference to the AudioSource component
    [SerializeField] private AudioClip attackSound; // Sound played when performing normal attack
    [SerializeField] private AudioClip upwardAttackSound; // Sound played when performing upward attack
    [SerializeField] private AudioClip throwBatSound; // Sound played when throwing the bat
    [SerializeField] private AudioClip throwSlashHitEnemySound; // Sound played when ThrowSlash hits an enemy
    [SerializeField] private AudioClip batHitEnemySound; // New: Sound played when bat hits an enemy

    [Header("Visual Settings")]
    [SerializeField] private GameObject playerBatVisual; // Visual representation of the bat on the player (to hide when thrown)
    [SerializeField] private SpriteRenderer playerBatSpriteRenderer;

    [Header("Bat Protection Settings")]
   
    [SerializeField] private Transform batParent; // Le parent de la batte (l'épaule/bras du joueur)
    private GameObject _currentBatInstance;

    [Header("Ghost Effect Settings")]
    [SerializeField] private List<SpriteRenderer> ghostTargets = new List<SpriteRenderer>(); // SpriteRenderers that will have ghost effect
    [SerializeField] private float ghostInterval = 0.1f; // Time between ghost spawns during anticipation
    [SerializeField] private float ghostDuration = 0.3f; // How long each ghost copy lasts
    [SerializeField] private Color ghostColor = new Color(1f, 1f, 1f, 0.3f); // Color of the ghost effect
    [SerializeField] private Material ghostMaterial; // Optional custom material for ghosts

    [Header("Camera Effects Settings")]
    [SerializeField] private CameraEffects cameraEffects; // Reference to camera effects component
    [SerializeField] private bool enableCameraEffects = true; // Toggle camera effects on/off

    [Header("Knockback Settings")]
    [SerializeField] private float defaultKnockbackForce = 5f; // Default knockback force for enemies
    [SerializeField] private float fleaKnockbackForce = 8f; // Specific knockback for Flea
    [SerializeField] private float inkKnockbackForce = 5f; // Specific knockback for Ink
    [SerializeField] private float flyKnockbackForce = 7f; // Specific knockback for Fly
    [SerializeField] private float sprayerKnockbackForce = 6f; // Specific knockback for Sprayer

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheck; // Point for ground detection
    [SerializeField] private float groundCheckRadius = 0.2f; // Radius of the ground check circle
    [SerializeField] private LayerMask whatIsGround; // Layer mask for ground

    [Header("Bat Pointer Settings")]
    [SerializeField] private RectTransform batPointerRectTransform; // The RectTransform of the UI pointer
    [SerializeField] private float minPointerSize = 0.5f; // Minimum scale of the pointer when close
    [SerializeField] private float maxPointerSize = 1.5f; // Maximum scale of the pointer when far
    [SerializeField] private float maxDistanceForScaling = 20f; // Distance at which pointer reaches max size
    [SerializeField] private float edgeOffset = 50f; // Offset from the screen edge for the pointer

    [Header("Volume Control Settings")]
    [SerializeField] private AudioMixer masterMixer; // Reference to the Master Audio Mixer
    [SerializeField] private Slider masterVolumeSlider; // UI Slider for Master Volume
    [SerializeField] private Slider musicVolumeSlider; // UI Slider for Music Volume
    [SerializeField] private Slider sfxVolumeSlider; // UI Slider for SFX Volume
    private GameObject activeThrowSlash; // Reference to the currently flying ThrowSlash projectile
    private List<Collider2D> hitEnemies = new List<Collider2D>();
    // Private variables
    private float nextAttackTime = 0f;
    private bool canDealDamage = false;
    private bool isAnticipating = false;
    private Coroutine ghostEffectCoroutine;
    private Camera playerCamera;
    private bool isUpwardAttack = false;
    private bool hasBat = true;
    private GameObject spawnedBat2; // Reference to the currently spawned Bat2 on the ground
    private Vector3 lastMousePosition;
    private bool isGrounded;

    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    // Static list to track all active Bat2 objects for cleanup
    private static List<GameObject> activeBat2Objects = new List<GameObject>();

    // Static list to track all active ghost objects for cleanup
    private static List<GameObject> activeGhostObjects = new List<GameObject>();

    void OnDisable()
    {
        Debug.Log("BatAttackSystem OnDisable called - performing comprehensive cleanup");

        // Clean up all active Bat2 objects
        CleanupAllBat2Objects();

        // Clean up all ghost objects
        CleanupAllGhostObjects();

        // Stop all coroutines to prevent MissingReferenceException
        StopAllCoroutines();

        // Ensure the player\"s bat visual is active when the script is disabled
        // This prepares it for when the bat weapon is re-enabled or picked up
        if (playerBatSpriteRenderer != null)
        {
            playerBatSpriteRenderer.enabled = true; // Ensure bat is visible when script is disabled
            Debug.Log("playerBatSpriteRenderer set to enabled in OnDisable (preparing for re-enable).");
        }

        // Reset other states
        isAnticipating = false;
        Time.timeScale = 1.0f; // Ensure time scale is reset
        Debug.Log("BatAttackSystem state reset in OnDisable.");
    }

    void OnEnable()
    {
        Debug.Log("BatAttackSystem OnEnable called - resetting to fresh state");
        EnsureBatExists();
        ResetBatSystemState();
    }

    void Awake()
    {
        // Crée la batte dès le début pour s'assurer qu'elle existe.
        EnsureBatExists();
        playerView = GetComponentInParent<PhotonView>();
    }
    void Start()
    {
        EnsureBatExists();
        InitializeComponents();
        ResetBatSystemState();
        LoadVolumeSettings();
        SetupVolumeSliders();
    }

    void Update()
    {
        CheckGround();
        HandleInput();
        if (playerView != null && !playerView.IsMine)
        {
            return; // If this is an online character that isn't mine, do nothing.
        }
        if (Input.GetButtonDown("Fire1"))
        {
            
           
        }
        if (hasBat)
        {
            EnsureBatExists();
        }

        if (!hasBat)
        {
            // Check for flying ThrowSlash collision with walls
            if (activeThrowSlash != null)
            {
                CheckThrowSlashCollision();
            }

            // Check for spawned bat pickup
            if (spawnedBat2 != null)
            {
                float distanceToBat2 = Vector2.Distance(transform.position, spawnedBat2.transform.position);
                if (distanceToBat2 <= batPickupRange)
                {
                    PickUpBat2();
                }
                CheckAndAdjustBat2Position();
                UpdateBatPointer(); // Update pointer when bat is thrown
            }
        }
        else if (batPointerRectTransform != null)
        {
            batPointerRectTransform.gameObject.SetActive(false); // Hide pointer if no bat is thrown
        }
    }
    private void HandleInput()
    {
        // Exit if we are already in an attack, don't have the bat, or are on cooldown.
        if (isAnticipating || !hasBat || Time.time < nextAttackTime)
        {
            return;
        }

        // --- MOBILE JOYSTICK INPUT ---
               if (MobileInput && attackJoystick != null && attackJoystick.gameObject.activeInHierarchy)

        {
            // Check if the attack joystick is being touched
            if (attackJoystick.Direction.sqrMagnitude > 0.01f)
            {
                if (!isJoystickHeld)
                {
                    // This is the first frame the joystick is held down
                    isJoystickHeld = true;
                    joystickHoldTime = 0f;
                }
                // Increment the hold timer
                joystickHoldTime += Time.deltaTime;
            }
            // Check if the attack joystick was just released
            else if (isJoystickHeld)
            {
                isJoystickHeld = false; // Mark as released

                // Check if the release was a THROW (held long enough and dragged far enough)
                if (joystickHoldTime > joystickTapTime || attackJoystick.Direction.magnitude > joystickAimThreshold)
                {
                    // --- THROW ATTACK ---
                    // Calculate a simulated mouse position based on the joystick's last direction
                    Vector3 joystickScreenPos = new Vector3(Screen.width / 2, Screen.height / 2, 0) + (Vector3)attackJoystick.Direction * 200f;
                    lastMousePosition = joystickScreenPos;
                    StartAnticipationAndThrowSlash();
                }
                // Otherwise, it was a quick TAP for a MELEE attack
                else
                {
                    // --- MELEE ATTACK ---
                    bool isAimingUp = false;
                    // Check if the RUNNING joystick is being held upwards
                    if (runningJoystick != null && runningJoystick.Direction.y > 0.7f) // Using 0.7 as a strong upward threshold
                    {
                        isAimingUp = true;
                    }
                    StartAnticipationAttack(isAimingUp);
                }

                // Reset the timer
                joystickHoldTime = 0f;
            }
        }

        // --- PC MOUSE & KEYBOARD INPUT (remains as a fallback) ---
        if (PcInput && Input.GetMouseButtonDown(0))

        {
            // Melee Attack
            bool shouldPerformUpwardAttack = ShouldPerformUpwardAttack();
            StartAnticipationAttack(shouldPerformUpwardAttack);
        }

        if (PcInput && Input.GetMouseButtonDown(1))

        {
            // Throw Attack
            lastMousePosition = Input.mousePosition;
            StartAnticipationAndThrowSlash();
        }
    }


    private void InitializeComponents()
    {
        if (cameraEffects == null)
        {
            cameraEffects = Camera.main?.GetComponent<CameraEffects>();
            if (cameraEffects == null)
            {
                enableCameraEffects = false;
            }
        }

        playerCamera = Camera.main;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Animator>();
        }

        if (groundCheck == null)
        {
            groundCheck = transform;
        }

        if (batPointerRectTransform != null)
        {
            batPointerRectTransform.gameObject.SetActive(false); // Initially hide the pointer
        }
    }

    public void EnsureBatExists()
    {
        // Étape 1: On ne cherche la batte que si notre référence est vide.
        // C'est plus performant que de chercher à chaque fois.
        if (_currentBatInstance == null)
        {
            Debug.Log("Référence de la batte est nulle, tentative de recherche avec le tag 'PlayerWeaponBat'...");
            _currentBatInstance = GameObject.FindGameObjectWithTag("PlayerWeaponBat");
        }

        // Étape 2: LA VÉRIFICATION CRUCIALE.
        // Si, après la recherche, la référence est TOUJOURS nulle, on arrête TOUT.
        // C'est ce qui empêche le NullReferenceException.
        if (_currentBatInstance == null)
        {
            // On affiche une erreur claire pour le débogage et on quitte la fonction.
            Debug.LogError("ÉCHEC DE LA RECHERCHE : Impossible de trouver un GameObject actif avec le tag 'PlayerWeaponBat'. Le script ne peut pas continuer.");
            return; // Quitte la fonction pour éviter le crash.
        }

        // Étape 3: Si on arrive ici, c'est que _currentBatInstance a été trouvé avec succès.
        // On peut maintenant assigner les autres variables en toute sécurité.
        if (playerBatVisual == null)
        {
            playerBatVisual = _currentBatInstance;
        }
        if (playerBatSpriteRenderer == null)
        {
            playerBatSpriteRenderer = _currentBatInstance.GetComponent<SpriteRenderer>();
        }

        // On s'assure que la batte est visuellement active.
        if (!_currentBatInstance.activeSelf)
        {
            _currentBatInstance.SetActive(true);
        }
    }
    private void ResetBatSystemState()
    {
        hasBat = true;
        isAnticipating = false;
        spawnedBat2 = null; // Clear reference to Bat2
        Time.timeScale = 1.0f;

        if (playerBatSpriteRenderer != null)
        {
            playerBatSpriteRenderer.enabled = true; // Ensure bat is visible on reset
        }
        Debug.Log("BatAttackSystem state fully reset.");
    }
    private void CheckThrowSlashCollision()
    {
        if (activeThrowSlash == null) return;

        Collider2D slashCollider = activeThrowSlash.GetComponent<Collider2D>();
        if (slashCollider == null) return;

        // Check for enemies first
        Collider2D[] overlappingEnemies = Physics2D.OverlapBoxAll(
            activeThrowSlash.transform.position,
            slashCollider.bounds.size,
            activeThrowSlash.transform.eulerAngles.z,
            enemyLayers
        );

        foreach (Collider2D enemyCollider in overlappingEnemies)
        {
            if (!hitEnemies.Contains(enemyCollider))
            {
                // Add null check for enemy GameObject before accessing its transform
                if (enemyCollider == null || enemyCollider.gameObject == null) continue;

                hitEnemies.Add(enemyCollider);

                // Calculate knockback direction from the ThrowSlash position to the enemy
                Vector2 knockbackDirection = ((Vector2)(enemyCollider.transform.position - activeThrowSlash.transform.position)).normalized;

                // Use TryGetComponent for specific health scripts, similar to ApplyDamageAtPoint
                if (enemyCollider.TryGetComponent<FleaHealth>(out var fleaHealth) && fleaHealth != null)
                {
                    fleaHealth.TakeDamage(throwSlashDamage, knockbackDirection, fleaKnockbackForce);
                }
                else if (enemyCollider.TryGetComponent<InkHealth>(out var inkHealth) && inkHealth != null)
                {
                    inkHealth.TakeDamage(throwSlashDamage, knockbackDirection, inkKnockbackForce);
                }
                else if (enemyCollider.TryGetComponent<FlyHealth>(out var flyHealth) && flyHealth != null)
                {
                    flyHealth.TakeDamage(throwSlashDamage, knockbackDirection, flyKnockbackForce);
                }
                else if (enemyCollider.TryGetComponent<SprayerHealth>(out var sprayerHealth) && sprayerHealth != null)
                {
                    sprayerHealth.TakeDamage(throwSlashDamage, knockbackDirection, sprayerKnockbackForce);
                }
                else if (enemyCollider.TryGetComponent<RatKingHealth>(out var RatKingHealth) && RatKingHealth != null)
                {
                    RatKingHealth.TakeDamage(throwSlashDamage);
                }
                else if (enemyCollider.TryGetComponent<BarrelExplosion>(out var barrelExplosion) && barrelExplosion != null)
                {
                    barrelExplosion.TakeDamage(throwSlashDamage);
                }
                else
                {
                    Debug.LogWarning($"No recognized health script found on {enemyCollider.name}. Damage applied without specific knockback.");
                }

                // Play bat hit enemy sound
                if (audioSource != null && batHitEnemySound != null)
                {
                    audioSource.PlayOneShot(batHitEnemySound);
                }
            }
        }

        // Check for walls (keep your existing wall collision logic)
        Collider2D groundCollider = Physics2D.OverlapBox(
            activeThrowSlash.transform.position,
            slashCollider.bounds.size,
            activeThrowSlash.transform.eulerAngles.z,
            groundLayer
        );

        if (groundCollider != null)
        {
            HandleThrowSlashHit(activeThrowSlash.transform.position);
            return;
        }
    }
    private void HandleThrowSlashHit(Vector3 hitPosition)
    {
        if (activeThrowSlash == null) return;

        // Stop the ThrowSlash movement
        Rigidbody2D slashRb = activeThrowSlash.GetComponent<Rigidbody2D>();
        if (slashRb != null)
        {
            slashRb.velocity = Vector2.zero;
            slashRb.isKinematic = true;
        }

        // Spawn the pickupable bat (bat2Prefab) at the hit position
        if (bat2Prefab != null)
        {
            // Adjust spawn position slightly above the hit point to prevent sinking
            Vector3 spawnPosition = hitPosition;
            spawnPosition.y += 0.1f;

            GameObject newBat2 = Instantiate(bat2Prefab, spawnPosition, Quaternion.identity);
            SetSpawnedBat2(newBat2);

            // Add to tracking list
            activeBat2Objects.Add(newBat2);
        }

        // Destroy the ThrowSlash projectile
        Destroy(activeThrowSlash);
        activeThrowSlash = null;
    }
    private void CleanupAllBat2Objects()
    {
        // Clean up active ThrowSlash if it exists
        if (activeThrowSlash != null)
        {
            Destroy(activeThrowSlash);
            activeThrowSlash = null;
        }
        // Clean up the current spawned Bat2 if it exists and is not already destroyed
        if (spawnedBat2 != null)
        {
            Destroy(spawnedBat2);
            spawnedBat2 = null;
        }

        // Clean up all tracked Bat2 objects
        for (int i = activeBat2Objects.Count - 1; i >= 0; i--)
        {
            if (activeBat2Objects[i] != null)
            {
                Destroy(activeBat2Objects[i]);
            }
        }
        activeBat2Objects.Clear();

        // Find and destroy any remaining Bat2 objects in the scene by name pattern
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            // Check if the object\"s name contains "Bat2" and it\"s not the player\"s visual bat
            if (obj != playerBatVisual && obj.name.Contains("Bat2") && obj.GetComponent<Rigidbody2D>() != null)
            {
                Destroy(obj);
            }
        }

        Debug.Log("All Bat2 objects cleaned up");
    }

    private void CleanupAllGhostObjects()
    {
        // Stop ghost effect coroutine if it\"s running
        if (ghostEffectCoroutine != null)
        {
            StopCoroutine(ghostEffectCoroutine);
            ghostEffectCoroutine = null;
        }

        // Clean up all tracked ghost objects
        for (int i = activeGhostObjects.Count - 1; i >= 0; i--)
        {
            if (activeGhostObjects[i] != null)
            {
                Destroy(activeGhostObjects[i]);
            }
        }
        activeGhostObjects.Clear();

        // Find and destroy any remaining ghost objects by tag and name pattern
        GameObject[] remainingGhostsByTag = GameObject.FindGameObjectsWithTag("Ghost");
        foreach (GameObject ghost in remainingGhostsByTag)
        {
            Destroy(ghost);
        }

        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("Ghost_"))
            {
                Destroy(obj);
            }
        }

        Debug.Log("All ghost objects cleaned up");
    }

    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
    }

    private void CheckAndAdjustBat2Position()
    {
        if (spawnedBat2 == null) return;

        Rigidbody2D bat2Rb = spawnedBat2.GetComponent<Rigidbody2D>();
        if (bat2Rb == null) return;

        // Use a small raycast distance to detect ground directly below the bat
        float raycastDistance = 0.2f; // Adjusted for more precise ground detection
        // Offset the bat slightly above the ground to prevent it from sinking
        float verticalOffset = 0.05f; // Adjusted for more precise positioning

        // Perform a raycast downwards from the bat\"s position
        RaycastHit2D hit = Physics2D.Raycast(spawnedBat2.transform.position, Vector2.down, raycastDistance, groundLayer);

        // If the raycast hits the ground and the bat is below the intended vertical offset
        if (hit.collider != null && spawnedBat2.transform.position.y < hit.point.y + verticalOffset)
        {
            Vector3 newPosition = spawnedBat2.transform.position;
            newPosition.y = hit.point.y + verticalOffset; // Set the bat\"s Y position to be slightly above the ground hit point
            spawnedBat2.transform.position = newPosition;
            // If the bat is moving downwards, stop its vertical velocity to prevent bouncing or sinking
            if (bat2Rb.velocity.y < 0)
            {
                bat2Rb.velocity = new Vector2(bat2Rb.velocity.x, 0);
            }
        }
    }

    private bool ShouldPerformUpwardAttack()
    {
        if (playerCamera == null) return false;

        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;
        float relativeMouseY = mouseWorldPos.y - transform.position.y;

        if (relativeMouseY >= upwardZoneMinY && relativeMouseY <= upwardZoneMaxY)
        {
            return true;
        }
        else if (relativeMouseY <= normalZoneMaxY)
        {
            return false;
        }

        return false;
    }

    void StartAnticipationAttack(bool performUpwardAttack = false)
    {
        isAnticipating = true;
        isUpwardAttack = performUpwardAttack;
        nextAttackTime = Time.time + anticipationDuration + attackCooldown;

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(anticipationTriggerName);
        }

        if (enableCameraEffects && cameraEffects != null)
        {
            cameraEffects.StartHoldAndReleaseEffect();
        }

        if (ghostTargets.Count > 0)
        {
            ghostEffectCoroutine = StartCoroutine(GhostEffectRoutine());
        }

        StartCoroutine(AnticipationRoutine());
    }

    IEnumerator AnticipationRoutine()
    {
        Time.timeScale = 0.8f;
        yield return new WaitForSecondsRealtime(anticipationDuration);
        Time.timeScale = 1.0f;

        if (ghostEffectCoroutine != null)
        {
            StopCoroutine(ghostEffectCoroutine);
            ghostEffectCoroutine = null;
        }

        if (isUpwardAttack)
        {
            PerformUpwardAttack();
        }
        else
        {
            PerformBatAttack();
        }

        isAnticipating = false;
    }

    void StartAnticipationAndThrowSlash()
    {
        isAnticipating = true;
        nextAttackTime = Time.time + anticipationDuration + attackCooldown;
        hasBat = false;

        if (playerBatSpriteRenderer != null)
        {
            playerBatSpriteRenderer.enabled = false; // Disable the SpriteRenderer to hide the bat visual
            Debug.Log("Player bat SpriteRenderer disabled after throwing ThrowSlash.");
        }


        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(anticipationTriggerName);
        }

        if (enableCameraEffects && cameraEffects != null)
        {
            cameraEffects.StartHoldAndReleaseEffect();
        }

        if (ghostTargets.Count > 0)
        {
            ghostEffectCoroutine = StartCoroutine(GhostEffectRoutine());
        }

        StartCoroutine(AnticipationRoutineForThrowSlash());
    }

    IEnumerator AnticipationRoutineForThrowSlash()
    {
        Time.timeScale = 0.8f;
        yield return new WaitForSecondsRealtime(anticipationDuration);
        Time.timeScale = 1.0f;

        if (ghostEffectCoroutine != null)
        {
            StopCoroutine(ghostEffectCoroutine);
            ghostEffectCoroutine = null;
        }

        ThrowSlash();
        isAnticipating = false;
    }

    void ThrowSlash()
    {
        hitEnemies.Clear();

        if (throwSlashPrefab == null || playerCamera == null)
        {
            Debug.LogWarning("Cannot throw - missing prefab or camera");
            return;
        }

        Transform currentSpawnPoint = null;
        if (transform.localScale.x > 0)
        { // Player is facing right
            currentSpawnPoint = throwSlashSpawnPointRight;
        }
        else
        { // Player is facing left
            currentSpawnPoint = throwSlashSpawnPointLeft;
        }

        if (currentSpawnPoint == null)
        {
            Debug.LogWarning("ThrowSlash spawn point is not assigned for the current direction.");
            return;
        }

        Vector3 targetWorldPoint = playerCamera.ScreenToWorldPoint(new Vector3(lastMousePosition.x, lastMousePosition.y, playerCamera.nearClipPlane));
        targetWorldPoint.z = currentSpawnPoint.position.z;

        // Determine if the cursor is to the right or left of the player
        float playerScreenX = playerCamera.WorldToScreenPoint(transform.position).x;
        float cursorScreenX = lastMousePosition.x;

        if (cursorScreenX > playerScreenX) // Cursor is to the right of the player
        {
            targetWorldPoint.y += aimVerticalOffsetRightCursor;
        }
        else // Cursor is to the left of the player
        {
            targetWorldPoint.y += aimVerticalOffsetLeftCursor;
        }

        Vector2 throwDirection = (targetWorldPoint - currentSpawnPoint.position).normalized;

        if (throwDirection.magnitude < 0.1f)
        {
            throwDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        }

        if (playerView != null)
        {
            // ONLINE MODE: Call the RPC to create the slash effect for everyone.
            // We pass the NAME of the prefab you already have assigned in the Inspector.
            playerView.RPC("RPC_PerformVisualEffect", RpcTarget.All, throwSlashPrefab.name, currentSpawnPoint.position, Quaternion.identity);
        }
        else
        {
            // SINGLE-PLAYER MODE: Instantiate it directly, just like before.
            GameObject slashInstance = Instantiate(throwSlashPrefab, currentSpawnPoint.position, Quaternion.identity);
            // You would also need to put your velocity/rotation code here for single-player.
        

        Rigidbody2D slashRb = slashInstance.GetComponent<Rigidbody2D>();
        if (slashRb == null)
        {
            slashRb = slashInstance.AddComponent<Rigidbody2D>();
            slashRb.gravityScale = 0;
        }
        slashRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Collider2D slashCollider = slashInstance.GetComponent<Collider2D>();
        if (slashCollider == null)
        {
            slashCollider = slashInstance.AddComponent<BoxCollider2D>();
            slashCollider.isTrigger = true;
        }

        slashRb.velocity = throwDirection * throwSlashSpeed;

        float angle = Mathf.Atan2(throwDirection.y, throwDirection.x) * Mathf.Rad2Deg;
        slashInstance.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        
        // Removed ThrowSlashHandler initialization, as its logic is now integrated or simplified
        // If ThrowSlashHandler is still needed for other purposes, it should be re-evaluated.
        // For now, assuming it's solely for ThrowSlash spawning and can be removed.

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(throwBatTriggerName);
        }

        if (audioSource != null && throwBatSound != null)
        {
            audioSource.PlayOneShot(throwBatSound);
        }

        if (playerBatVisual != null)
        {
            playerBatVisual.SetActive(false);
        }

        hasBat = false;
        activeThrowSlash = slashInstance; // Track the flying projectile
        }
    }

    public void SetSpawnedBat2(GameObject bat2)
    {
        spawnedBat2 = bat2;
        if (bat2 != null && !activeBat2Objects.Contains(bat2))
        {
            activeBat2Objects.Add(bat2);
        }
    }

    void PickUpBat2()
    {
        if (spawnedBat2 != null)
        {
            activeBat2Objects.Remove(spawnedBat2);
            Destroy(spawnedBat2);
            spawnedBat2 = null;
        }
        hasBat = true;
        if (playerBatSpriteRenderer != null)
        {
            playerBatSpriteRenderer.enabled = true; // Enable the SpriteRenderer to show the bat visual
            Debug.Log("Player bat SpriteRenderer enabled after picking up Bat2.");
        }

        // When picking up the bat, reset the system state as if nothing happened
        ResetBatSystemState();
        Debug.Log("Picked up Bat2! Bat visual enabled and system state reset.");
    }

    IEnumerator GhostEffectRoutine()
    {
        float timer = 0f;

        while (timer < anticipationDuration)
        {
            foreach (SpriteRenderer targetRenderer in ghostTargets)
            {
                if (targetRenderer != null)
                {
                    CreateGhostCopy(targetRenderer);
                }
            }

            yield return new WaitForSecondsRealtime(ghostInterval);
            timer += ghostInterval;
        }
    }

    void CreateGhostCopy(SpriteRenderer originalRenderer)
    {
        if (originalRenderer == null) return;

        GameObject ghostObject = new GameObject("Ghost_" + originalRenderer.name);
        ghostObject.tag = "Ghost";
        activeGhostObjects.Add(ghostObject);

        SpriteRenderer ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();

        ghostObject.transform.position = originalRenderer.transform.position;
        ghostObject.transform.rotation = originalRenderer.transform.rotation;
        ghostObject.transform.localScale = originalRenderer.transform.lossyScale;

        ghostRenderer.sprite = originalRenderer.sprite;
        ghostRenderer.sortingLayerID = originalRenderer.sortingLayerID;
        ghostRenderer.sortingOrder = originalRenderer.sortingOrder - 1;
        ghostRenderer.flipX = originalRenderer.flipX;
        ghostRenderer.flipY = originalRenderer.flipY;

        if (ghostMaterial != null)
        {
            ghostRenderer.material = ghostMaterial;
        }
        else
        {
            Material tempMaterial = new Material(Shader.Find("Sprites/Default"));
            ghostRenderer.material = tempMaterial;
        }

        ghostRenderer.color = ghostColor;

        StartCoroutine(FadeOutGhost(ghostRenderer));
    }

    IEnumerator FadeOutGhost(SpriteRenderer ghostRenderer)
    {
        // Add null check at the start of the coroutine
        if (ghostRenderer == null) yield break;

        Color startColor = ghostRenderer.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        float timer = 0f;

        while (timer < ghostDuration)
        {
            // Add null check inside the loop as well
            if (ghostRenderer == null) yield break;

            timer += Time.unscaledDeltaTime;
            float progress = timer / ghostDuration;
            ghostRenderer.color = Color.Lerp(startColor, endColor, progress);
            yield return null;
        }

        if (ghostRenderer != null && ghostRenderer.gameObject != null)
        {
            activeGhostObjects.Remove(ghostRenderer.gameObject);
            Destroy(ghostRenderer.gameObject);
        }
    }

    void PerformBatAttack()
    {
        nextAttackTime = Time.time + attackCooldown;

       if (playerView != null)
    {
        // ONLINE MODE: Use an RPC to tell everyone to play the animation.
        playerView.RPC("RPC_PlayAnimationTrigger", RpcTarget.All, attackTriggerName);
    }
    else
    {
        // SINGLE-PLAYER MODE: Just play it locally like before.
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(attackTriggerName);
        }
    }
    // --- END OF MODIFICATION ---

    if (audioSource != null && attackSound != null)
    {
        audioSource.PlayOneShot(attackSound);
    }

        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        canDealDamage = true;

        if (enableCameraEffects && cameraEffects != null)
        {
            cameraEffects.StartShakeEffect();
        }
    }

    void PerformUpwardAttack()
    {
        nextAttackTime = Time.time + attackCooldown;

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(upwardAttackTriggerName);
        }

        if (audioSource != null && upwardAttackSound != null)
        {
            audioSource.PlayOneShot(upwardAttackSound);
        }

        canDealDamage = true;

        if (enableCameraEffects && cameraEffects != null)
        {
            cameraEffects.StartShakeEffect();
        }
    }
    [PunRPC]
    void RPC_PerformVisualEffect(string prefabName, Vector3 position, Quaternion rotation)
    {
        // This function is called on EVERYONE'S computer when an attack happens.
        // Its only job is to create the visual effect so everyone can see it.

        // Find the prefab in the Resources folder by its name.
        GameObject effectPrefab = Resources.Load<GameObject>(prefabName);
        if (effectPrefab != null)
        {
            // Create the visual effect locally for everyone.
            Instantiate(effectPrefab, position, rotation);
        }
        else
        {
            Debug.LogError("Could not find effect prefab in Resources folder: " + prefabName);
        }
    }
    [PunRPC]
    void RPC_PlayAnimationTrigger(string triggerName)
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(triggerName);
        }
    }

    public void ApplyDamage()
    {
        if (!canDealDamage || !hasBat) return;
        ApplyDamageAtPoint(attackPoint);
    }

    public void ApplyUpwardDamage()
    {
        if (!canDealDamage || !hasBat) return;
        Transform damagePoint = upwardAttackPoint != null ? upwardAttackPoint : attackPoint;
        ApplyDamageAtPoint(damagePoint);
    }

    private void ApplyDamageAtPoint(Transform damagePoint)
    {
        if (damagePoint == null)
        {
            canDealDamage = false;
            return;
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(damagePoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy.CompareTag("Player")) // Assure-toi que ton joueur a bien le tag "Player"
            {
                continue; // Ignore cet objet et passe au suivant
            }
            // Add null check for enemy GameObject before accessing its transform
            if (enemy == null || enemy.gameObject == null) continue;

            Vector2 knockbackDirection = ((Vector2)(enemy.transform.position - damagePoint.position)).normalized;

            // Use a common interface or base class for health components if possible
            // Add null checks before calling TryGetComponent
            if (enemy.TryGetComponent<FleaHealth>(out var fleaHealth) && fleaHealth != null)
            {
                fleaHealth.TakeDamage(damage, knockbackDirection, fleaKnockbackForce, null);
            }
            else if (enemy.TryGetComponent<InkHealth>(out var inkHealth) && inkHealth != null)
            {
                inkHealth.TakeDamage(damage, knockbackDirection, inkKnockbackForce);
            }
            else if (enemy.TryGetComponent<FlyHealth>(out var flyHealth) && flyHealth != null)
            {
                flyHealth.TakeDamage(damage, knockbackDirection, flyKnockbackForce);
            }
            else if (enemy.TryGetComponent<SprayerHealth>(out var sprayerHealth) && sprayerHealth != null)
            {
                sprayerHealth.TakeDamage(damage, knockbackDirection, sprayerKnockbackForce);
            }
            else if (enemy.TryGetComponent<RatKingHealth>(out var RatKingHealth) && RatKingHealth != null)
            {
                RatKingHealth.TakeDamage(damage);
            }
            else if (enemy.TryGetComponent<BarrelExplosion>(out var barrelExplosion) && barrelExplosion != null)
            {
                barrelExplosion.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning($"No recognized health script found on {enemy.name}. Damage applied without specific knockback.");
            }

            // Play bat hit enemy sound
            if (audioSource != null && batHitEnemySound != null)
            {
                audioSource.PlayOneShot(batHitEnemySound);
            }
        }

        canDealDamage = false;
    }

    // Public utility methods
    public bool IsAnticipating() => isAnticipating;
    public bool IsPerformingUpwardAttack() => isUpwardAttack;
    public bool HasBat() => hasBat;
    public bool CanAttack() => hasBat && !isAnticipating && Time.time >= nextAttackTime;

    // New: Bat Pointer Logic
    private void UpdateBatPointer()
    {
        if (spawnedBat2 == null || playerCamera == null || batPointerRectTransform == null)
        {
            if (batPointerRectTransform != null) batPointerRectTransform.gameObject.SetActive(false);
            return;
        }

        Vector3 screenPoint = playerCamera.WorldToViewportPoint(spawnedBat2.transform.position);
        bool onScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;

        if (onScreen)
        {
            batPointerRectTransform.gameObject.SetActive(false);
        }
        else
        {
            batPointerRectTransform.gameObject.SetActive(true);

            Vector3 directionToBat = (spawnedBat2.transform.position - transform.position).normalized;
            Vector3 pointerPosition = playerCamera.WorldToScreenPoint(spawnedBat2.transform.position);

            // Clamp pointer position to screen edges with offset
            pointerPosition.x = Mathf.Clamp(pointerPosition.x, edgeOffset, Screen.width - edgeOffset);
            pointerPosition.y = Mathf.Clamp(pointerPosition.y, edgeOffset, Screen.height - edgeOffset);

            batPointerRectTransform.position = pointerPosition;

            float angle = Mathf.Atan2(directionToBat.y, directionToBat.x) * Mathf.Rad2Deg;
            batPointerRectTransform.rotation = Quaternion.Euler(0, 0, angle - 90); // Adjust for pointer sprite orientation

            float distance = Vector3.Distance(transform.position, spawnedBat2.transform.position);
            float normalizedDistance = Mathf.Clamp01(distance / maxDistanceForScaling);
            float scale = Mathf.Lerp(minPointerSize, maxPointerSize, normalizedDistance);
            batPointerRectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    // New: Volume Control Logic
    void LoadVolumeSettings()
    {
        // Load saved volumes or set defaults
        float masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 0f); // Default to 0dB (full volume)
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0f);
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0f);

        if (masterMixer != null)
        {
            masterMixer.SetFloat("MasterVolume", masterVolume);
            masterMixer.SetFloat("MusicVolume", musicVolume);
            masterMixer.SetFloat("SFXVolume", sfxVolume);
        }

        // Update slider values if they exist
        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVolume;
    }

    void SetupVolumeSliders()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }
    }

    public void SetMasterVolume(float volume)
    {
        if (masterMixer != null) masterMixer.SetFloat("MasterVolume", volume);
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);
    }

    public void SetMusicVolume(float volume)
    {
        if (masterMixer != null) masterMixer.SetFloat("MusicVolume", volume);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (masterMixer != null) masterMixer.SetFloat("SFXVolume", volume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);
    }

    // OnDrawGizmosSelected for debugging (unchanged)
    void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
            Gizmos.DrawLine(transform.position, attackPoint.position);
        }

        if (upwardAttackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(upwardAttackPoint.position, attackRange);
            Gizmos.DrawLine(transform.position, upwardAttackPoint.position);
        }

        if (!hasBat)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, batPickupRange);
        }

        // Draw ghost target indicators
        Gizmos.color = Color.cyan;
        foreach (SpriteRenderer targetRenderer in ghostTargets)
        {
            if (targetRenderer != null)
            {
                Gizmos.DrawWireCube(targetRenderer.bounds.center, targetRenderer.bounds.size);
            }
        }

        // Draw mouse direction indicator (only in play mode)
        if (Application.isPlaying && playerCamera != null)
        {
            Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = transform.position.z;

            // Apply aim offset based on cursor position relative to player
            float playerScreenX = playerCamera.WorldToScreenPoint(transform.position).x;
            float cursorScreenX = Input.mousePosition.x;

            if (cursorScreenX > playerScreenX) // Cursor is to the right of the player
            {
                mouseWorldPos.y += aimVerticalOffsetRightCursor;
            }
            else // Cursor is to the left of the player
            {
                mouseWorldPos.y += aimVerticalOffsetLeftCursor;
            }

            Vector2 directionToMouse = (mouseWorldPos - transform.position).normalized;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)directionToMouse * 2f);
        }

        // Draw Upward Attack Zone
        Gizmos.color = Color.green;
        Vector3 playerPos = transform.position;
        Vector3 upwardZoneMin = new Vector3(playerPos.x - 1f, playerPos.y + upwardZoneMinY, playerPos.z);
        Vector3 upwardZoneMax = new Vector3(playerPos.x + 1f, playerPos.y + upwardZoneMaxY, playerPos.z);
        Gizmos.DrawCube(new Vector3(playerPos.x, (upwardZoneMin.y + upwardZoneMax.y) / 2, playerPos.z), new Vector3(2f, upwardZoneMax.y - upwardZoneMin.y, 0.1f));
    }
}




