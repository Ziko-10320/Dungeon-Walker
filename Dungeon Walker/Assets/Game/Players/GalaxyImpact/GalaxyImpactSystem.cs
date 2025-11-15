using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FirstGearGames.SmoothCameraShaker;
public class GalaxyImapctSystem : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private L3antixMovement playerMovement;
    [SerializeField] private Rigidbody2D playerRb;
    [SerializeField] private L3antixHealth playerHealth;
    [SerializeField] private L3antixDash playerDash;
    [Header("Super Move Settings")]
    [Tooltip("Le nom du trigger dans l'Animator pour lancer l'animation du super coup.")]
    [SerializeField] private string superMoveTriggerName = "GalaxyImpact";
    [Tooltip("La touche pour activer le super coup.")]
    [SerializeField] private KeyCode superMoveKey = KeyCode.F;
    [Tooltip("Le cooldown en secondes avant de pouvoir réutiliser le super coup.")]
    [SerializeField] private float superMoveCooldown = 10f;
    [Header("Audio")]
    [Tooltip("Sound that plays once when the super move is activated.")]
    public AudioClip superActivationSound;
    [Range(0f, 1f)] public float superActivationVolume = 1.0f;
    [Header("Scripts to Disable During Super")]
    [Tooltip("Faites glisser ici tous les scripts qui doivent être désactivés pendant le super coup (ex: scripts d'armes, dash, etc.).")]
    public List<MonoBehaviour> scriptsToDisable;

    [Header("Damage Settings")]
    [Tooltip("Le point d'origine de la zone de dégâts.")]
    [SerializeField] private Transform damageAreaOrigin;
    [Tooltip("Le rayon de la zone de dégâts.")]
    [SerializeField] private float damageRadius = 2.0f;
    [Tooltip("Les dégâts infligés par chaque 'tick' de dégâts.")]
    [SerializeField] private int damagePerTick = 5;
    [Tooltip("La couche (Layer) des ennemis pour les détecter.")]
    [SerializeField] private LayerMask enemyLayer;

    // --- NOUVELLES VARIABLES POUR LES DÉGÂTS SUR LA DURÉE ---
    [Header("Damage Over Time Settings")]
    [Tooltip("La durée totale pendant laquelle les dégâts sont appliqués (en secondes).")]
    [SerializeField] private float damageDuration = 1.5f;
    [Tooltip("L'intervalle entre chaque 'tick' de dégâts (en secondes).")]
    [SerializeField] private float damageTickInterval = 0.3f;

    public ShakeData CameraShakeDeath;

    // State variables
    private bool isPerformingSuperMove = false;
    private float originalGravityScale;
    private float lastSuperMoveTime = -100f;
    private Coroutine damageCoroutine; // Pour garder une référence à notre coroutine

    [Header("Super System")]
    [SerializeField] private L3antixSuperMeter superMeter; // assign in Inspector

    [Header("Super UI")]
    [SerializeField] private UnityEngine.UI.Slider superBarSlider;
    [SerializeField] private TMPro.TextMeshProUGUI superBarText;
    [SerializeField] private GameObject superReadyIndicator;
    [SerializeField] private UnityEngine.UI.Button superButton;
    void Awake()
    {
        if (playerAnimator == null) playerAnimator = GetComponent<Animator>();
        if (playerMovement == null) playerMovement = GetComponent<L3antixMovement>();
        if (playerHealth == null) playerHealth = GetComponent<L3antixHealth>();
        if (playerDash == null) playerDash = GetComponent<L3antixDash>();
        if (playerRb == null) playerRb = GetComponent<Rigidbody2D>();

        if (superMeter == null)
            superMeter = FindObjectOfType<L3antixSuperMeter>();

        if (superButton != null)
            superButton.onClick.AddListener(() => TryActivateSuper());

        if (superMeter == null)
            Debug.LogError("❌ No L3antixSuperMeter found!");

        if (superBarSlider != null)
        {
            superBarSlider.minValue = 0f;
            superBarSlider.maxValue = 1f;
            superBarSlider.value = superMeter.GetProgressNormalized();
        }

        if (superBarText != null)
            superBarText.text = Mathf.RoundToInt(superMeter.GetProgressNormalized() * 100f) + "%";

        if (superReadyIndicator != null)
            superReadyIndicator.SetActive(superMeter.HasSuperCharge());

        superMeter.onSuperReady.AddListener(() =>
        {
            if (superBarText != null) superBarText.text = "CLICK ME";
            if (superBarSlider != null) superBarSlider.value = 1f;
            if (superReadyIndicator != null) superReadyIndicator.SetActive(true);
        });

        superMeter.onSuperUsed.AddListener(() =>
        {
            
            if (superBarSlider != null) superBarSlider.value = 0f;
            if (superReadyIndicator != null) superReadyIndicator.SetActive(false);
        });
    }
    public void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || Camera.main == null) return;

        // Create a clean, independent object for the sound
        GameObject soundPlayerObject = new GameObject("GalaxyImpact_FORCE_PLAY_SOUND");

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
    private void OnEnable()
    {
        // Check if the slider exists to prevent errors.
        if (superBarSlider != null)
        {
            // Enable the entire GameObject that the slider is on.
            superBarSlider.gameObject.SetActive(true);
        }
    }

    private void OnDisable()
    {
        // Check if the slider exists to prevent errors.
        if (superBarSlider != null)
        {
            // Disable the entire GameObject that the slider is on.
            superBarSlider.gameObject.SetActive(false);
        }
    }
    void Update()
    {
        bool canPerformSuper = !isPerformingSuperMove &&
                                Time.time >= lastSuperMoveTime + superMoveCooldown &&
                                (playerDash == null || !playerDash.IsDashing); // On vérifie si le script de dash n'existe pas OU s'il n'est pas en cours

        if (Input.GetKeyDown(superMoveKey)&& canPerformSuper)
        {
            TryActivateSuper();
        }
        if (superMeter != null && !superMeter.HasSuperCharge())
        {
            if (superBarSlider != null)
                superBarSlider.value = superMeter.GetProgressNormalized();

            if (superBarText != null)
                superBarText.text = Mathf.RoundToInt(superMeter.GetProgressNormalized() * 100f) + "%";
        }
    }


    private void StartSuperMove()
    {
        if (playerHealth != null) playerHealth.isInvincible = true;
        isPerformingSuperMove = true;
        lastSuperMoveTime = Time.time;

        if (playerRb != null)
        {
            playerRb.bodyType = RigidbodyType2D.Static;
        }
        playerMovement.enabled = false;
        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null)
            {
                script.enabled = false;
            }
        }
        originalGravityScale = playerRb.gravityScale;
        playerRb.gravityScale = 0f;
        playerRb.velocity = Vector2.zero;

        playerAnimator.SetTrigger(superMoveTriggerName);

        // --- MODIFICATION : On lance la coroutine de dégâts ---
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
        }
        damageCoroutine = StartCoroutine(DamageOverTimeRoutine());
        // --- FIN DE LA MODIFICATION ---

        Debug.Log("Super Move Started!");
    }

    // --- NOUVELLE COROUTINE POUR LES DÉGÂTS SUR LA DURÉE ---
    private IEnumerator DamageOverTimeRoutine()
    {
        float timer = 0f;
        while (timer < damageDuration)
        {
            // Appliquer les dégâts
            ApplyDamageTick();

            // Attendre le prochain intervalle
            yield return new WaitForSeconds(damageTickInterval);

            timer += damageTickInterval;
        }
    }

    // La fonction qui applique réellement les dégâts (appelée par la coroutine)
    private void ApplyDamageTick()
    {
        Debug.Log("Applying Super Move damage tick!");
       
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(damageAreaOrigin.position, damageRadius, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            FleaHealth enemyHealth = enemy.GetComponent<FleaHealth>();
            if (enemyHealth != null)
            {
                Vector2 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                enemyHealth.TakeDamage(damagePerTick, directionToEnemy, 0f);
            }
            FleaHealthV2 enemyHealthV2 = enemy.GetComponent<FleaHealthV2>();
            if (enemyHealthV2 != null)
            {
                Vector2 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                enemyHealthV2.TakeDamage(damagePerTick, directionToEnemy, 0f);
            }
            FlyHealth flyHealth = enemy.GetComponent<FlyHealth>();
            if (flyHealth != null)
            {
                Vector2 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                flyHealth.TakeDamage(damagePerTick, directionToEnemy, 0f);
            }
            SprayerHealth SprayerHealth = enemy.GetComponent<SprayerHealth>();
            if (SprayerHealth != null)
            {
                Vector2 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                SprayerHealth.TakeDamage(damagePerTick, directionToEnemy, 0f);
            }
           InkHealth InkHealth = enemy.GetComponent<InkHealth>();
            if (InkHealth != null)
            {
                Vector2 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                InkHealth.TakeDamage(damagePerTick, directionToEnemy, 0f);
            }
            RatKingHealth ratKingHealth = enemy.GetComponent<RatKingHealth>();
            if (ratKingHealth != null)
            {
                Vector2 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                ratKingHealth.TakeDamage(damagePerTick, Vector2.zero, 0f);
            }
        }
    }

    private void TryActivateSuper()
    {
        if (superMeter != null && superMeter.HasSuperCharge())
        {
            superMeter.UseSuper();
            PlaySound(superActivationSound, superActivationVolume);
            StartSuperMove();
        }
        else
        {
            Debug.Log("❌ 3antix Super not ready!");
        }
    }

    public void TriggerCameraShake()
    {
        CameraShakerHandler.Shake(CameraShakeDeath);
    }
    public void FinishSuperMove()
    {
        // On s'assure d'arrêter la coroutine de dégâts au cas où l'animation serait plus courte que la durée des dégâts
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
        if (playerHealth != null) playerHealth.isInvincible = false;
        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null)
            {
                script.enabled = true;
            }
        }
        if (playerRb != null)
        {
            playerRb.bodyType = RigidbodyType2D.Dynamic;
        }
        playerRb.gravityScale = originalGravityScale;
        playerMovement.enabled = true;
        isPerformingSuperMove = false;
        Debug.Log("Super Move Finished!");
    }

    // --- AJOUT : VISUALISATION DES GIZMOS ---
    // Cette fonction dessine la zone de dégâts dans l'éditeur pour un réglage facile.
    void OnDrawGizmosSelected()
    {
        if (damageAreaOrigin == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(damageAreaOrigin.position, damageRadius);
    }
}
