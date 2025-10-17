using System.Collections;
using UnityEngine;

public class ReviveUpgradedSystem : MonoBehaviour
{
    [Header("Revive Settings")]
    public bool hasReviveUpgradedPowerUp = false;
    private bool hasUsedRevive = false;
    public bool HasUsedRevive => hasUsedRevive;

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject reviveHeartUI;   // heart upgraded UI
    [SerializeField] private Animator heartAnimator;     // controls "In" & "Out" animations
    [SerializeField] private Animator shockwaveAnimator; // explosion shockwave animator
    [SerializeField] private Material shockwaveMaterial; // material with WaveDistanceFromCenter
    public GameObject darkFlare;
    public GameObject CircleShockWave;
    [Header("Particles")]
    [SerializeField] private ParticleSystem[] healingParticles;   // play when heart appears
    [SerializeField] private ParticleSystem[] explosionParticles; // play when explosion happens
    [Header("Audio")]
    [SerializeField] private AudioClip reviveSound; // The sound to play once on revive.
    [Range(0f, 1f)]
    [SerializeField] private float reviveVolume = 1f;
    [Header("Explosion Settings")]
    [SerializeField] private Transform damagePoint;
    [SerializeField] private float damageRadius = 3f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private int damage = 50;
    [SerializeField] private float damageDelay = 0.5f; // seconds after explosion before applying damage

    [Header("Shockwave Shader Settings")]
    [SerializeField] private string shaderParam = "_WaveDistanceFromCenter";
    [SerializeField] private float startValue = -0.1f;
    [SerializeField] private float endValue = 1f;
    [SerializeField] private float shaderDuration = 1f;
    public GameObject ScreenShockwave;
    private int shaderID;

    [Header("Explosion Prefab Effects")]
    [SerializeField] private GameObject[] explosionPrefabs; // assign in inspector
    [SerializeField] private Transform prefabSpawnPoint;    // assign in inspector
    [SerializeField] private float prefabLifetime = 2f;     // how long before auto-destroy

    public void EquipReviveUpgraded()
    {
        hasReviveUpgradedPowerUp = true;
        hasUsedRevive = false;
        shaderID = Shader.PropertyToID(shaderParam);
    }

    public void TryRevive()
    {
        if (!hasReviveUpgradedPowerUp || hasUsedRevive) return;

        hasUsedRevive = true;
        Debug.Log("❤️ Revive Upgraded Triggered!");
        if (reviveSound != null)
        {
            AudioSource.PlayClipAtPoint(reviveSound, transform.position, reviveVolume);
        }
        // Cancel death + heal
        playerHealth.CancelDeathState();
        playerHealth.FullHeal();

        // Show heart + play "In" anim
        if (reviveHeartUI != null) reviveHeartUI.SetActive(true);
        if (heartAnimator != null) heartAnimator.SetTrigger("In");

        if (darkFlare != null) darkFlare.SetActive(true);
        // Healing particles
        foreach (var ps in healingParticles)
        {
            if (ps != null) ps.Play();
        }

        // Start sequence
        playerHealth.StartCoroutine(ReviveSequence());
    }

    private IEnumerator ReviveSequence()
    {
        // Pause game
        Time.timeScale = 0f;

        // Wait for heart "In" anim (set duration of anim in Animator, e.g. 1.5s)
        yield return new WaitForSecondsRealtime(0.85f);

        // Play "Out" animation
        if (heartAnimator != null) heartAnimator.SetTrigger("Out");

        // Wait for "Out" anim to finish (again depends on anim length, e.g. 0.5s)
        yield return new WaitForSecondsRealtime(0.85f);

        // Resume game
        Time.timeScale = 1f;

        if (CircleShockWave != null)
            CircleShockWave.SetActive(true);
        // Hide heart UI
        if (reviveHeartUI != null) reviveHeartUI.SetActive(false);

        if (darkFlare != null) darkFlare.SetActive(false);
        // Explosion particles
        foreach (var ps in explosionParticles)
        {
            if (ps != null) ps.Play();
        }

        if (explosionPrefabs != null && prefabSpawnPoint != null)
        {
            foreach (var prefab in explosionPrefabs)
            {
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab, prefabSpawnPoint.position, prefabSpawnPoint.rotation);
                    Destroy(instance, prefabLifetime); // auto-destroy after X seconds
                }
            }
        }

        // Shockwave animator
        if (shockwaveAnimator != null)
            shockwaveAnimator.SetTrigger("Play");
        StartCoroutine(DisableCircleShockWaveAfterTime(1f));
        // Reset shader
        if (shockwaveMaterial != null)
            shockwaveMaterial.SetFloat(shaderID, startValue);

        // Delay damage + apply
        playerHealth.StartCoroutine(DelayedDamage());
        
        // Animate shader wave
        playerHealth.StartCoroutine(AnimateShockwaveShader());
    }
    private IEnumerator DisableCircleShockWaveAfterTime(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (CircleShockWave != null)
            CircleShockWave.SetActive(false);
    }

    private IEnumerator DelayedDamage()
    {
        yield return new WaitForSecondsRealtime(damageDelay);

        if (damagePoint == null) yield break;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(damagePoint.position, damageRadius, enemyLayer);

        foreach (var enemy in enemies)
        {
            if (enemy.TryGetComponent(out FleaHealth flea)) flea.TakeDamage(damage, Vector2.zero);
            if (enemy.TryGetComponent(out FleaHealthV2 fleaV2)) fleaV2.TakeDamage(damage, Vector2.zero);
            if (enemy.TryGetComponent(out FlyHealth fly)) fly.TakeDamage(damage, Vector2.zero);
            if (enemy.TryGetComponent(out SprayerHealth sprayer)) sprayer.TakeDamage(damage, Vector2.zero);
            if (enemy.TryGetComponent(out InkHealth ink)) ink.TakeDamage(damage, Vector2.zero);
            if (enemy.TryGetComponent(out RatKingHealth rat)) rat.TakeDamage(damage);
        }

        Debug.Log($"💥 Explosion dealt {damage} damage to {enemies.Length} enemies!");
    }

    private IEnumerator AnimateShockwaveShader()
    {
        if (ScreenShockwave != null) ScreenShockwave.SetActive(true);
        if (shockwaveMaterial == null) yield break;

        float elapsed = 0f;
        while (elapsed < shaderDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / shaderDuration);
            float value = Mathf.Lerp(startValue, endValue, t);
            shockwaveMaterial.SetFloat(shaderID, value);
            yield return null;
        }

        
    }

    private void OnDrawGizmosSelected()
    {
        if (damagePoint == null) return;

        Gizmos.color = Color.red; // Color of the circle
        Gizmos.DrawWireSphere(damagePoint.position, damageRadius);
    }

}
