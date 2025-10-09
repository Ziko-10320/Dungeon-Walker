using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FirstGearGames.SmoothCameraShaker;
using UnityEngine.UI;
using TMPro;
public class SuperMoveController : MonoBehaviour
{
    [Header("Activation")]
    public KeyCode activationKey = KeyCode.F;
    private bool isSuperMoveActive = false;

    [Header("Player Visuals")]
    public Color playerSuperColor = Color.blue;
    public float playerFadeDuration = 0.5f;

    [Header("Screen Fade")]
    public UnityEngine.UI.Image screenFadeImage;
    public float screenFadeDuration = 1.0f;

    [Header("Enemy Detection")]
    public string[] enemyTags;
    public GameObject clawEffectPrefab;

    [Header("Timing")]
    public float enemyRevealDelay = 0.2f;
    public float superMoveEndDelay = 1.0f;

    public MonoBehaviour[] playerScriptsToDisable;
    public float playerReEnableDelay = 0.2f;

    [SerializeField] private GameObject startSuperMoveEffectPrefab;
    [SerializeField] private Transform particleSpawnPoint;

    [Header("Camera Shake")]
    public ShakeData clawImpactShake;

    private List<SpriteRenderer> playerRenderers;
    private List<Color> originalPlayerColors;
    private Camera mainCamera;
    private Rigidbody2D playerRb;
    private Animator playerAnimator;
    private Collider2D playerCollider;
    private float originalPlayerGravityScale;
    private bool originalIsTrigger;
    private RigidbodyType2D originalPlayerBodyType;
    [SerializeField] private PlayerHealth playerHealth;

    // ✅ track which enemies are already clawed
    private HashSet<EnemySuperTarget> processedEnemies = new HashSet<EnemySuperTarget>();
    [Header("Super System")]
    [SerializeField] public PlayerSuperMeter superMeter;  // drag the meter script here

    [Header("Super UI")]
    [SerializeField] private Slider superBarSlider;
    [SerializeField] private TextMeshProUGUI superBarText;
    [SerializeField] private GameObject superReadyIndicator;
    [SerializeField] private Button superButton;

    [Header("Shockwave Shader Settings")]
    [SerializeField] private Material shockwaveMaterial;
    [SerializeField] private GameObject ScreenShockwave;
    [SerializeField] private string shaderParam = "_WaveDistanceFromCenter";
    [SerializeField] private float startValue = -0.1f;
    [SerializeField] private float endValue = 1f;
    [SerializeField] private float shaderDuration = 1f;
    private int shaderID;
    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
    }
    void Start()
    {
        playerRb = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>();
      

        superMeter = FindObjectOfType<PlayerSuperMeter>();
        if (superButton != null)
        {
            superButton.onClick.AddListener(() =>
            {
                TryActivateSuper();
            });
        }
        if (superMeter == null)
        {
            Debug.LogError("❌ No PlayerSuperMeter found in scene!");
            return;
        }

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

        // hook events
        superMeter.onSuperReady.AddListener(() =>
        {
            if (superBarText != null) superBarText.text = "SUPER READY";
            if (superBarSlider != null) superBarSlider.value = 1f;
            if (superReadyIndicator != null) superReadyIndicator.SetActive(true);
        });

        superMeter.onSuperUsed.AddListener(() =>
        {
            if (superBarText != null) superBarText.text = "0%";
            if (superBarSlider != null) superBarSlider.value = 0f;
            if (superReadyIndicator != null) superReadyIndicator.SetActive(false);
        });

        if (playerRb != null) originalPlayerBodyType = playerRb.bodyType;
        if (playerRb != null) originalPlayerGravityScale = playerRb.gravityScale;
        if (playerCollider != null) originalIsTrigger = playerCollider.isTrigger;

        mainCamera = Camera.main;
        if (screenFadeImage != null)
        {
            screenFadeImage.color = new Color(0, 0, 0, 0);
            screenFadeImage.gameObject.SetActive(false);
        }

        playerRenderers = new List<SpriteRenderer>(GetComponentsInChildren<SpriteRenderer>());
        originalPlayerColors = new List<Color>();
        foreach (var renderer in playerRenderers) originalPlayerColors.Add(renderer.color);

        if (shockwaveMaterial != null)
        {
            // Get the integer ID for the shader property for performance.
            shaderID = Shader.PropertyToID(shaderParam);
        }
        if (ScreenShockwave != null)
        {
            // Ensure the shockwave object is off by default.
            ScreenShockwave.SetActive(false);
        }

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
        if (Input.GetKeyDown(activationKey))
        {
            TryActivateSuper();
        }
        if (!superMeter.HasSuperCharge())
        {
            if (superBarSlider != null)
                superBarSlider.value = superMeter.GetProgressNormalized();

            if (superBarText != null)
                superBarText.text = Mathf.RoundToInt(superMeter.GetProgressNormalized() * 100f) + "%";
        }
    }

    private void TryActivateSuper()
    {
        if (isSuperMoveActive) return;

        if (superMeter.HasSuperCharge())
        {
            superMeter.UseSuper();
            StartCoroutine(ExecuteSuperMove());
        }
        else
        {
            Debug.Log("❌ No super available!");
        }
    }

    private IEnumerator ExecuteSuperMove()
    {
        playerHealth.CancelDeathState();
        playerHealth.isInvincible = true;
        isSuperMoveActive = true;
        processedEnemies.Clear();
        if (playerHealth != null)
        {
            playerHealth.isSuperActive = true;
        }

        try
        {
            CameraShakerHandler.Shake(clawImpactShake);
            StartCoroutine(TriggerShockwave());
            if (startSuperMoveEffectPrefab != null)
            {
                Vector3 spawnPosition = (particleSpawnPoint != null) ? particleSpawnPoint.position : transform.position;
                GameObject effectInstance = Instantiate(startSuperMoveEffectPrefab, spawnPosition, Quaternion.identity);

                ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();
                if (ps != null) Destroy(effectInstance, ps.main.duration);
                else Destroy(effectInstance, 5f);
            }

            if (playerRb != null) playerRb.bodyType = RigidbodyType2D.Static;
            if (playerRb != null) playerRb.gravityScale = 0f;
            if (playerCollider != null) playerCollider.isTrigger = true;
            if (playerRb != null) playerRb.velocity = Vector2.zero;

            if (playerAnimator != null)
            {
                playerAnimator.SetBool("IsRunning", false);
                playerAnimator.SetBool("IsWalking", false);
            }

            foreach (var script in playerScriptsToDisable)
                if (script != null) script.enabled = false;

            // --- Phase 1: Fade Out ---
            StartCoroutine(FadePlayer(true));
            StartCoroutine(FadeScreen(true));
            yield return new WaitForSeconds(screenFadeDuration);

            // --- Phase 2: Detect and Reveal Enemies ---
            yield return new WaitForSeconds(enemyRevealDelay);

            List<EnemySuperTarget> visibleEnemies = FindVisibleEnemies();
            foreach (var enemy in visibleEnemies)
            {
                enemy.Stun(true);
            }
            yield return StartCoroutine(SpawnClawsSequentially(visibleEnemies));

            // ✅ start checking for new enemies mid-super
            StartCoroutine(MonitorNewEnemiesDuringSuper());

            if (visibleEnemies.Count > 0)
                CameraShakerHandler.Shake(clawImpactShake);

            // --- Phase 3: Return to Normal ---
            yield return new WaitForSeconds(superMoveEndDelay);

            StartCoroutine(FadePlayer(false));
            StartCoroutine(FadeScreen(false));

            foreach (var enemy in processedEnemies)
            {
                if (enemy != null)
                {
                    enemy.Stun(false);
                    enemy.EndFlash();
                }
            }
        }
        finally
        {
            if (playerRb != null) playerRb.gravityScale = originalPlayerGravityScale;
            if (playerCollider != null) playerCollider.isTrigger = originalIsTrigger;
            if (playerRb != null) playerRb.bodyType = originalPlayerBodyType;

            playerHealth.isInvincible = false;
        }

        yield return new WaitForSeconds(playerReEnableDelay);

        foreach (var script in playerScriptsToDisable)
            if (script != null) script.enabled = true;
        if (playerHealth != null)
        {
            playerHealth.isSuperActive = false;
        }
        isSuperMoveActive = false;
    }

    private IEnumerator SpawnClawsSequentially(List<EnemySuperTarget> enemies)
    {
        foreach (var enemy in enemies)
        {
            if (enemy != null && !processedEnemies.Contains(enemy))
            {
                processedEnemies.Add(enemy);
                enemy.StartFlash();
                if (clawEffectPrefab != null && enemy.clawSpawnPoint != null)
                    Instantiate(clawEffectPrefab, enemy.clawSpawnPoint.position, Quaternion.identity);
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    // ✅ keeps scanning for new enemies during the super
    private IEnumerator MonitorNewEnemiesDuringSuper()
    {
        while (isSuperMoveActive)
        {
            List<EnemySuperTarget> currentEnemies = FindVisibleEnemies();
            foreach (var enemy in currentEnemies)
            {
                if (enemy != null && !processedEnemies.Contains(enemy))
                {
                    enemy.Stun(true);
                    processedEnemies.Add(enemy);
                    enemy.StartFlash();
                    if (clawEffectPrefab != null && enemy.clawSpawnPoint != null)
                        Instantiate(clawEffectPrefab, enemy.clawSpawnPoint.position, Quaternion.identity);
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private List<EnemySuperTarget> FindVisibleEnemies()
    {
        List<EnemySuperTarget> visibleEnemies = new List<EnemySuperTarget>();
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

        foreach (string tag in enemyTags)
        {
            GameObject[] enemiesWithTag = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject enemyObj in enemiesWithTag)
            {
                Collider2D enemyCollider = enemyObj.GetComponent<Collider2D>();
                if (enemyCollider != null && GeometryUtility.TestPlanesAABB(planes, enemyCollider.bounds))
                {
                    EnemySuperTarget target = enemyObj.GetComponent<EnemySuperTarget>();
                    if (target != null)
                        visibleEnemies.Add(target);
                }
            }
        }
        return visibleEnemies;
    }

    private IEnumerator FadePlayer(bool fadeIn)
    {
        float timer = 0f;
        while (timer < playerFadeDuration)
        {
            float progress = timer / playerFadeDuration;
            for (int i = 0; i < playerRenderers.Count; i++)
            {
                Color targetColor = fadeIn ? playerSuperColor : originalPlayerColors[i];
                float targetAlpha = fadeIn ? 0f : originalPlayerColors[i].a;

                playerRenderers[i].color = Color.Lerp(playerRenderers[i].color, targetColor, progress);

                Color currentColor = playerRenderers[i].color;
                currentColor.a = Mathf.Lerp(currentColor.a, targetAlpha, progress);
                playerRenderers[i].color = currentColor;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < playerRenderers.Count; i++)
        {
            if (fadeIn)
                playerRenderers[i].color = new Color(playerSuperColor.r, playerSuperColor.g, playerSuperColor.b, 0);
            else
                playerRenderers[i].color = originalPlayerColors[i];
        }
    }

    private IEnumerator FadeScreen(bool fadeIn)
    {
        if (screenFadeImage == null) yield break;

        screenFadeImage.gameObject.SetActive(true);
        float targetAlpha = fadeIn ? 1f : 0f;
        float startAlpha = screenFadeImage.color.a;

        float timer = 0f;
        while (timer < screenFadeDuration)
        {
            float progress = timer / screenFadeDuration;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            screenFadeImage.color = new Color(0, 0, 0, newAlpha);
            timer += Time.deltaTime;
            yield return null;
        }

        screenFadeImage.color = new Color(0, 0, 0, targetAlpha);
        if (!fadeIn)
            screenFadeImage.gameObject.SetActive(false);
    }
    private IEnumerator TriggerShockwave()
    {
        // Failsafe checks
        if (shockwaveMaterial == null || ScreenShockwave == null)
        {
            yield break; // Exit if anything is missing
        }

        // 1. Activate the shockwave object and reset the shader value
        ScreenShockwave.SetActive(true);
        shockwaveMaterial.SetFloat(shaderID, startValue);

        // 2. Animate the value over the specified duration
        float timer = 0f;
        while (timer < shaderDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / shaderDuration;
            float currentValue = Mathf.Lerp(startValue, endValue, progress);
            shockwaveMaterial.SetFloat(shaderID, currentValue);
            yield return null; // Wait for the next frame
        }

        // 3. Ensure the final value is set and then deactivate the object
        shockwaveMaterial.SetFloat(shaderID, endValue);
        yield return new WaitForSeconds(0.1f); // Small delay to let the effect finish
        ScreenShockwave.SetActive(false);
    }
}
