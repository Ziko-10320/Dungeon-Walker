using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using Photon.Pun;
using TMPro;
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

    [Header("Attack Button UI (Mobile)")]
    [SerializeField] private RectTransform attackButtonRect;          // The visible attack button area (assign the button RectTransform)
    [SerializeField] private RectTransform attackButtonHandle;        // Handle (child) that will move when aiming (assign)
    [SerializeField] private RectTransform attackButtonBackground;    // Background (joystick circle) that will appear on hold (assign)
    [SerializeField] private RectTransform uiCanvasRectTransform;     // Root Canvas RectTransform (assign)
    [SerializeField] private LineRenderer aimLineRenderer;             // **NEW**: Assign your LineRenderer component here
    [SerializeField] private float holdToAimTime = 0.4f;              // Hold time to switch to aim mode
    [SerializeField] private float maxHandleDistance = 100f;       // Max distance in pixels the handle can move from background center

    [SerializeField] private TextMeshProUGUI debugText;
    // runtime state
    private bool isAttackButtonPressed = false;
    private float attackButtonPressTimer = 0f;
    private Vector2 attackButtonStartScreenPos;
    private bool isAimingWithButton = false;
    private Vector2 buttonAimDirection = Vector2.right;

    [SerializeField] private GameObject attackButtonUI; // assign your Attack Button object in Inspector
    [SerializeField] private GameObject joystickUI;
    [SerializeField] private Image attackButtonImage;
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

    private PlayerSuperMeter superMeter;
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

        if (attackJoystick != null)
            attackJoystick.gameObject.SetActive(false);

        if (attackButtonBackground != null)
            attackButtonBackground.gameObject.SetActive(false);

        if (attackButtonHandle != null)
            attackButtonHandle.gameObject.SetActive(false);

        if (attackButtonUI != null) attackButtonUI.SetActive(false);

        if (attackJoystick != null)
        {
            attackJoystick.gameObject.SetActive(true); // This hands control back!
        }
        if (batPointerRectTransform != null)
        {
            batPointerRectTransform.gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        Debug.Log("BatAttackSystem OnEnable called - resetting to fresh state");
        EnsureBatExists();
        ResetBatSystemState();
        if (attackButtonUI != null) attackButtonUI.SetActive(true);
        if (attackJoystick != null)
        {
            attackJoystick.gameObject.SetActive(false);
        }
    }

    void Awake()
    {
        // Crée la batte dès le début pour s'assurer qu'elle existe.
        EnsureBatExists();
        playerView = GetComponentInParent<PhotonView>();
        superMeter = GetComponent<PlayerSuperMeter>();
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
        if (MobileInput && attackButtonRect != null && attackButtonRect.gameObject.activeInHierarchy)
        {
            // TOUCH PATH (mobile)
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                Vector2 touchPos = touch.position;

                if (touch.phase == TouchPhase.Began)
                {
                    // Did we press the attack button area?
                    if (IsScreenPointOverRectTransform(attackButtonRect, touchPos))
                    {
                        isAttackButtonPressed = true;
                        attackButtonPressTimer = 0f;
                        attackButtonStartScreenPos = touchPos;
                    }
                }
                else if (isAttackButtonPressed && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
                {
                    attackButtonPressTimer += Time.deltaTime;

                    // Enter aim mode after hold time
                    if (!isAimingWithButton && attackButtonPressTimer >= holdToAimTime)
                    {
                        isAimingWithButton = true;
                        ShowAimJoystickAtScreenPos(attackButtonRect.position);
                    }

                    // If we're aiming, update handle position & direction
                    if (isAimingWithButton)
                    {
                        UpdateHandleWithScreenPoint(touchPos);
                    }
                }
                else if (isAttackButtonPressed && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
                {
                    // Release
                    isAttackButtonPressed = false;

                    if (isAimingWithButton)
                    {
                        // perform throw in direction of handle
                        isAimingWithButton = false;
                        HideAimJoystick();

                        // build a simulated screen target from the aim direction
                        if (buttonAimDirection.sqrMagnitude < 0.01f) buttonAimDirection = Vector2.right;
                        Vector3 screenCenter = playerCamera != null ? playerCamera.WorldToScreenPoint(transform.position) : new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
                        lastMousePosition = (Vector3)(screenCenter + (Vector3)buttonAimDirection * 300f); // 300 px distance -> you can tweak
                        ThrowSlash();
                    }
                    else
                    {
                        // quick tap -> melee attack
                        bool isAimingUp = runningJoystick != null && runningJoystick.Direction.y > 0.7f;
                        StartAnticipationAttack(isAimingUp);
                    }

                    attackButtonPressTimer = 0f;
                }
            }
            // MOUSE PATH (editor / PC mobile testing)
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 mousePos = Input.mousePosition;
                    if (IsScreenPointOverRectTransform(attackButtonRect, mousePos))
                    {
                        isAttackButtonPressed = true;
                        attackButtonPressTimer = 0f;
                        attackButtonStartScreenPos = mousePos;
                    }
                }
                else if (isAttackButtonPressed && Input.GetMouseButton(0))
                {
                    attackButtonPressTimer += Time.deltaTime;
                    if (!isAimingWithButton && attackButtonPressTimer >= holdToAimTime)
                    {
                        isAimingWithButton = true;
                        // FIX: Use the stored start position, not Vector2.zero
                        ShowAimJoystickAtScreenPos(attackButtonStartScreenPos);
                    }
                    if (isAimingWithButton)
                    {
                        UpdateHandleWithScreenPoint(Input.mousePosition);
                    }
                }
                else if (isAttackButtonPressed && Input.GetMouseButtonUp(0))
                {
                    isAttackButtonPressed = false;

                    if (isAimingWithButton)
                    {
                        isAimingWithButton = false;
                        HideAimJoystick();
                        if (buttonAimDirection.sqrMagnitude < 0.01f) buttonAimDirection = Vector2.right;
                        Vector3 screenCenter = playerCamera != null ? playerCamera.WorldToScreenPoint(transform.position) : new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
                        lastMousePosition = (Vector3)(screenCenter + (Vector3)buttonAimDirection * 300f);
                        ThrowSlash();
                    }
                    else
                    {
                        bool isAimingUp = runningJoystick != null && runningJoystick.Direction.y > 0.7f;
                        StartAnticipationAttack(isAimingUp);
                    }

                    attackButtonPressTimer = 0f;
                }
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
            ThrowSlash();
        }
    }

    private bool IsScreenPointOverRectTransform(RectTransform rect, Vector2 screenPoint)
    {
        if (rect == null) return false;
        Canvas canvas = uiCanvasRectTransform != null ? uiCanvasRectTransform.GetComponent<Canvas>() : null;
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? (canvas.worldCamera ?? playerCamera) : null;

        Vector2 local;
        bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, cam, out local);
        if (!ok) return false;
        return rect.rect.Contains(local);
    }

    // --- START OF THE FINAL CHANGE: The Correct ShowAimJoystickAtScreenPos() Method ---
    private void ShowAimJoystickAtScreenPos(Vector2 screenPos)
    {
        if (attackButtonBackground == null || attackButtonHandle == null) return;

        // Step 1: Enable the UI. They will appear at their fixed editor positions.
        attackButtonBackground.gameObject.SetActive(true);
        attackButtonHandle.gameObject.SetActive(true);

        // Step 2: Force the handle to start at the center of the background.
        // Since the handle is a child of the background with centered anchors,
        // (0,0) is its exact center.
        attackButtonHandle.anchoredPosition = Vector2.zero;

        // Step 3: Hide the main attack button.
        if (attackButtonImage != null)
        {
            attackButtonImage.enabled = false;
        }

        // Step 4: Activate the aim line and set a default direction.
        if (aimLineRenderer != null)
        {
            aimLineRenderer.enabled = true;
            // Default aim is where the player is facing until you drag.
            Vector2 initialDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            buttonAimDirection = initialDirection;
            UpdateAimLineDirection(initialDirection);
        }
    }
    private void UpdateHandleWithScreenPoint(Vector2 screenPoint)
    {
        if (attackButtonBackground == null || attackButtonHandle == null) return;

        // This is the most reliable way to handle joystick logic when the background is fixed.

        // Step 1: Convert the screen touch point to a local point INSIDE the background's rectangle.
        // This is the key: the frame of reference is the background itself.
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            attackButtonBackground, // The frame of reference is the background.
            screenPoint,
            playerCamera, // Use the main camera for coordinate conversion.
            out localPoint
        );

        // Step 2: Clamp the magnitude of this local point.
        // This ensures the handle's local position never exceeds the joystick's radius.
        Vector2 clampedLocalPoint = Vector2.ClampMagnitude(localPoint, maxHandleDistance);

        // Step 3: Apply this clamped local position to the handle.
        // Because the handle is a child of the background, this works perfectly.
        attackButtonHandle.anchoredPosition = clampedLocalPoint;

        // Step 4: Update the aim direction based on the handle's movement.
        // If the handle is near the center, keep the last direction to prevent aim from resetting.
        if (clampedLocalPoint.sqrMagnitude > 0.01f)
        {
            buttonAimDirection = clampedLocalPoint.normalized;
        }

        // Step 5: Update the aim line visualization.
        UpdateAimLineDirection(buttonAimDirection);
    }
    private void UpdateAimLineDirection(Vector2 direction)
    {
        if (aimLineRenderer == null) return;

        Transform spawnPoint = (transform.localScale.x > 0) ? throwSlashSpawnPointRight : throwSlashSpawnPointLeft;
        if (spawnPoint != null)
        {
            aimLineRenderer.SetPosition(0, spawnPoint.position);
            aimLineRenderer.SetPosition(1, spawnPoint.position + (Vector3)direction.normalized * 15f);
        }
    }
    private void HideAimJoystick()
    {
        if (attackButtonBackground != null) attackButtonBackground.gameObject.SetActive(false);
        if (attackButtonHandle != null) attackButtonHandle.gameObject.SetActive(false);

        if (attackButtonHandle != null) attackButtonHandle.anchoredPosition = Vector2.zero;

        if (aimLineRenderer != null)
        {
            aimLineRenderer.enabled = false;
        }

        // --- NEW: Show the main attack button again ---
        if (attackButtonImage != null)
        {
            attackButtonImage.enabled = true;
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

        if (attackButtonBackground != null)
            attackButtonBackground.gameObject.SetActive(false);
        if (attackButtonHandle != null)
            attackButtonHandle.gameObject.SetActive(false);

        // If you want to completely hide the old attack joystick (so it's never visible),
        // keep it disabled by default in inspector or force-disable here:
        if (attackJoystick != null)
            attackJoystick.gameObject.SetActive(false);

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
                    if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                        L3antixSuperMeter.Instance.AddDamage(throwSlashDamage);

                    if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                        PlayerSuperMeter.Instance.AddDamage(throwSlashDamage);
                     
                }
                else if (enemyCollider.TryGetComponent<InkHealth>(out var inkHealth) && inkHealth != null)
                {
                    inkHealth.TakeDamage(throwSlashDamage, knockbackDirection, inkKnockbackForce);
                    if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                        L3antixSuperMeter.Instance.AddDamage(throwSlashDamage);

                    if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                        PlayerSuperMeter.Instance.AddDamage(throwSlashDamage);
                }
                else if (enemyCollider.TryGetComponent<FlyHealth>(out var flyHealth) && flyHealth != null)
                {
                    flyHealth.TakeDamage(throwSlashDamage, knockbackDirection, flyKnockbackForce);
                    if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                        L3antixSuperMeter.Instance.AddDamage(throwSlashDamage);

                    if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                        PlayerSuperMeter.Instance.AddDamage(throwSlashDamage);
                }
                else if (enemyCollider.TryGetComponent<SprayerHealth>(out var sprayerHealth) && sprayerHealth != null)
                {
                    sprayerHealth.TakeDamage(throwSlashDamage, knockbackDirection, sprayerKnockbackForce);
                    if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                        L3antixSuperMeter.Instance.AddDamage(throwSlashDamage);

                    if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                        PlayerSuperMeter.Instance.AddDamage(throwSlashDamage);
                }
                else if (enemyCollider.TryGetComponent<RatKingHealth>(out var RatKingHealth) && RatKingHealth != null)
                {
                    RatKingHealth.TakeDamage(throwSlashDamage);
                    if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                        L3antixSuperMeter.Instance.AddDamage(throwSlashDamage);

                    if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                        PlayerSuperMeter.Instance.AddDamage(throwSlashDamage);
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
            Vector3 spawnPosition = hitPosition;
            spawnPosition.y += 0.1f; // Adjust to prevent sinking

            // --- THE FIX: Directly assign the new instance to spawnedBat2 ---
            // This is the most important step. We create it and immediately track it.
            spawnedBat2 = Instantiate(bat2Prefab, spawnPosition, Quaternion.identity);

            // Add to the static tracking list for cleanup, which is good practice.
            if (!activeBat2Objects.Contains(spawnedBat2))
            {
                activeBat2Objects.Add(spawnedBat2);
            }
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


    void ThrowSlash()
    {
        hitEnemies.Clear();

        if (throwSlashPrefab == null)
        {
            Debug.LogWarning("Cannot throw - throwSlashPrefab is not assigned!");
            return;
        }

        Transform currentSpawnPoint = (transform.localScale.x > 0) ? throwSlashSpawnPointRight : throwSlashSpawnPointLeft;
        if (currentSpawnPoint == null)
        {
            Debug.LogWarning("ThrowSlash spawn point is not assigned for the current direction.");
            return;
        }

        Vector2 throwDirection = buttonAimDirection;
        if (throwDirection.sqrMagnitude < 0.01f)
        {
            throwDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        }

        // Step 1: Instantiate the projectile at the exact spawn point.
        GameObject slashInstance = Instantiate(throwSlashPrefab, currentSpawnPoint.position, Quaternion.identity);
        activeThrowSlash = slashInstance;

        // Step 2: Rotate it to face the correct direction.
        float angle = Mathf.Atan2(throwDirection.y, throwDirection.x) * Mathf.Rad2Deg;
        slashInstance.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Step 3: Get our new mover script and tell it where to go.
        // This is the key to perfect accuracy.
        ProjectileMover mover = slashInstance.GetComponent<ProjectileMover>();
        if (mover != null)
        {
            mover.Initialize(throwDirection, throwSlashSpeed);
        }
        else
        {
            Debug.LogError("ThrowSlash prefab is missing the ProjectileMover script!");
        }

        // Step 4: Trigger animations and sound.
        if (playerAnimator != null) playerAnimator.SetTrigger(throwBatTriggerName);
        if (audioSource != null && throwBatSound != null) audioSource.PlayOneShot(throwBatSound);

        // Step 5: Hide the player's bat.
        if (playerBatVisual != null) playerBatVisual.SetActive(false);
        hasBat = false;
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

        // --- THE FIX: Explicitly hide the pointer when the bat is picked up ---
        if (batPointerRectTransform != null)
        {
            batPointerRectTransform.gameObject.SetActive(false);
        }

        if (playerBatSpriteRenderer != null)
        {
            playerBatSpriteRenderer.enabled = true;
            Debug.Log("Player bat SpriteRenderer enabled after picking up Bat2.");
        }

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
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(damage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(damage);

            }
            else if (enemy.TryGetComponent<InkHealth>(out var inkHealth) && inkHealth != null)
            {
                inkHealth.TakeDamage(damage, knockbackDirection, inkKnockbackForce);
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(damage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(damage);

            }
            else if (enemy.TryGetComponent<FlyHealth>(out var flyHealth) && flyHealth != null)
            {
                flyHealth.TakeDamage(damage, knockbackDirection, flyKnockbackForce);
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(damage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(damage);

            }
            else if (enemy.TryGetComponent<SprayerHealth>(out var sprayerHealth) && sprayerHealth != null)
            {
                sprayerHealth.TakeDamage(damage, knockbackDirection, sprayerKnockbackForce);
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(damage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(damage);

            }
            else if (enemy.TryGetComponent<RatKingHealth>(out var RatKingHealth) && RatKingHealth != null)
            {
                RatKingHealth.TakeDamage(damage);
                if (L3antixSuperMeter.Instance != null && L3antixSuperMeter.Instance.isActiveAndEnabled)
                    L3antixSuperMeter.Instance.AddDamage(damage);

                if (PlayerSuperMeter.Instance != null && PlayerSuperMeter.Instance.isActiveAndEnabled)
                    PlayerSuperMeter.Instance.AddDamage(damage);

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
        if (batPointerRectTransform == null || spawnedBat2 == null || playerCamera == null)
        {
            if (batPointerRectTransform != null) batPointerRectTransform.gameObject.SetActive(false);
            return;
        }

        Vector3 targetScreenPos = playerCamera.WorldToScreenPoint(spawnedBat2.transform.position);
        bool isTargetOnScreen = targetScreenPos.z > 0 && targetScreenPos.x > 0 && targetScreenPos.x < Screen.width && targetScreenPos.y > 0 && targetScreenPos.y < Screen.height;

        if (isTargetOnScreen)
        {
            batPointerRectTransform.gameObject.SetActive(false);
        }
        else
        {
            batPointerRectTransform.gameObject.SetActive(true);

            if (targetScreenPos.z < 0)
            {
                targetScreenPos *= -1;
            }

            Vector3 screenCenter = new Vector3(Screen.width, Screen.height, 0) / 2;
            Vector3 direction = (targetScreenPos - screenCenter).normalized;
            float pointerX = screenCenter.x + direction.x * (screenCenter.x - edgeOffset);
            float pointerY = screenCenter.y + direction.y * (screenCenter.y - edgeOffset);

            // This is the clamped screen position in pixels. This part of the logic is correct.
            Vector3 pointerScreenPos = new Vector3(Mathf.Clamp(pointerX, edgeOffset, Screen.width - edgeOffset), Mathf.Clamp(pointerY, edgeOffset, Screen.height - edgeOffset), 0);

            // --- THE CRUCIAL FIX FOR "SCREEN SPACE - CAMERA" ---
            // Instead of setting .position directly, we must convert the screen point
            // into a local position within the canvas.

            Vector2 localPointerPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)batPointerRectTransform.parent, // The parent of the pointer (usually the whole canvas)
                pointerScreenPos, // The pixel position we calculated
                playerCamera,     // The camera associated with the canvas
                out localPointerPosition // The resulting local position
            );

            // Apply the correctly converted local position. This will work in Screen Space - Camera.
            batPointerRectTransform.anchoredPosition = localPointerPosition;

            // --- Rotation Logic (This remains the same and is correct) ---
            Vector3 directionToBat = (spawnedBat2.transform.position - transform.position).normalized;
            float angle = Mathf.Atan2(directionToBat.y, directionToBat.x) * Mathf.Rad2Deg;
            batPointerRectTransform.rotation = Quaternion.Euler(0, 0, angle);
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




