using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FirstGearGames.SmoothCameraShaker; // Make sure you have this using statement

public class SuperMoveController : MonoBehaviour
{
    [Header("Activation")]
    public KeyCode activationKey = KeyCode.F;
    private bool isSuperMoveActive = false;

    [Header("Player Visuals")]
    [Tooltip("The color the player will turn during the super move.")]
    public Color playerSuperColor = Color.blue;
    [Tooltip("How long it takes for the player to fade in/out.")]
    public float playerFadeDuration = 0.5f;

    [Header("Screen Fade")]
    [Tooltip("A UI Image with a solid black color covering the screen.")]
    public UnityEngine.UI.Image screenFadeImage;
    [Tooltip("How long it takes for the screen to fade to black.")]
    public float screenFadeDuration = 1.0f;

    [Header("Enemy Detection")]
    [Tooltip("An array of tags that identify enemies.")]
    public string[] enemyTags;
    [Tooltip("The prefab for the claw effect to be spawned on enemies.")]
    public GameObject clawEffectPrefab;

    [Header("Timing")]
    [Tooltip("The delay after the screen is black before enemies are revealed.")]
    public float enemyRevealDelay = 0.2f;
    [Tooltip("The delay after enemies are revealed before the move ends.")]
    public float superMoveEndDelay = 1.0f;

    public MonoBehaviour[] playerScriptsToDisable;
    [Tooltip("Delay before re-enabling player scripts after the super move ends.")]
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

    void Start()
    {
        playerRb = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>(); // <-- Add this
        if (playerRb != null)
        {
            originalPlayerBodyType = playerRb.bodyType; // <-- ADD THIS
        }
        if (playerRb != null)
        {
            originalPlayerGravityScale = playerRb.gravityScale; // <-- Add this
        }
        if (playerCollider != null)
        {
            originalIsTrigger = playerCollider.isTrigger; // <-- Add this
        }
        mainCamera = Camera.main;
        if (screenFadeImage != null)
        {
            screenFadeImage.color = new Color(0, 0, 0, 0);
            screenFadeImage.gameObject.SetActive(false);
        }

        // Find all sprite renderers in the player and its children
        playerRenderers = new List<SpriteRenderer>(GetComponentsInChildren<SpriteRenderer>());
        originalPlayerColors = new List<Color>();
        foreach (var renderer in playerRenderers)
        {
            originalPlayerColors.Add(renderer.color);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(activationKey) && !isSuperMoveActive)
        {
            StartCoroutine(ExecuteSuperMove());
        }
    }

    private IEnumerator ExecuteSuperMove()
    {
        isSuperMoveActive = true;
        CameraShakerHandler.Shake(clawImpactShake);
        if (startSuperMoveEffectPrefab != null)
        {
            // On détermine la position de spawn. Si le point de spawn n'est pas défini, on utilise la position du joueur.
            Vector3 spawnPosition = (particleSpawnPoint != null) ? particleSpawnPoint.position : transform.position;

            // On instancie (crée) une nouvelle copie de l'effet à la position voulue.
            GameObject effectInstance = Instantiate(startSuperMoveEffectPrefab, spawnPosition, Quaternion.identity);

            // Optionnel mais recommandé : détruire l'objet de l'effet après sa durée.
            // Cela suppose que l'effet ne boucle pas.
            ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(effectInstance, ps.main.duration);
            }
            else
            {
                // Si ce n'est pas un système de particules, on le détruit après quelques secondes pour éviter de polluer la scène.
                Destroy(effectInstance, 5f);
            }
        }
        if (playerRb != null)
        {
            playerRb.bodyType = RigidbodyType2D.Static;
        }
        if (playerRb != null) playerRb.gravityScale = 0f;
        if (playerCollider != null) playerCollider.isTrigger = true;
        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
        }
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsRunning", false); // Adjust based on your animator
            playerAnimator.SetBool("IsWalking", false); // Adjust based on your animator
        }
        foreach (var script in playerScriptsToDisable)
        {
            if (script != null) script.enabled = false;
        }
        // --- Phase 1: Fade Out ---
        StartCoroutine(FadePlayer(true));
        StartCoroutine(FadeScreen(true));
        yield return new WaitForSeconds(screenFadeDuration);

        // --- Phase 2: Detect and Reveal Enemies ---
        yield return new WaitForSeconds(enemyRevealDelay);

        List<EnemySuperTarget> visibleEnemies = FindVisibleEnemies();
        foreach (var enemy in visibleEnemies)
        {
            enemy.StartFlash();
            if (clawEffectPrefab != null && enemy.clawSpawnPoint != null)
            {
                Instantiate(clawEffectPrefab, enemy.clawSpawnPoint.position, Quaternion.identity);
            }
        }

        // Shake camera on claw impact
        if (visibleEnemies.Count > 0)
        {
            CameraShakerHandler.Shake(clawImpactShake);
        }

        // --- Phase 3: Return to Normal ---
        yield return new WaitForSeconds(superMoveEndDelay);

        StartCoroutine(FadePlayer(false));
        StartCoroutine(FadeScreen(false));

        foreach (var enemy in visibleEnemies)
        {
            enemy.EndFlash();
        }
        if (playerRb != null) playerRb.gravityScale = originalPlayerGravityScale;
        if (playerCollider != null) playerCollider.isTrigger = originalIsTrigger;
        if (playerRb != null)
        {
            playerRb.bodyType = originalPlayerBodyType;
        }
        yield return new WaitForSeconds(playerReEnableDelay);
        // Re-enable player scripts
        foreach (var script in playerScriptsToDisable)
        {
            if (script != null) script.enabled = true;
        }

        isSuperMoveActive = false;
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
                    {
                        visibleEnemies.Add(target);
                    }
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

        // Ensure final state is set
        for (int i = 0; i < playerRenderers.Count; i++)
        {
            if (fadeIn)
            {
                playerRenderers[i].color = new Color(playerSuperColor.r, playerSuperColor.g, playerSuperColor.b, 0);
            }
            else
            {
                playerRenderers[i].color = originalPlayerColors[i];
            }
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
        {
            screenFadeImage.gameObject.SetActive(false);
        }
    }
}
