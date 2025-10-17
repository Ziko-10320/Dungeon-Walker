using System.Collections;
using UnityEngine;

public class BeePowerUpL3antix : MonoBehaviour
{
    [Header("Particles (Swarm)")]
    public ParticleSystem[] beeSwarms;
    [Header("Audio")]
    [SerializeField] private AudioClip beeSwarmSound; // The looping buzz sound.
    [Range(0f, 1f)]
    [SerializeField] private float beeSwarmVolume = 1f; // The volume slider.
    private AudioSource swarmAudioSource;
    [Header("Damage Zone")]
    public Transform damagePoint;   // Center point of the swarm (usually player position)
    public float damageRadius = 2f;
    public float damage = 5f;
    public float damageTickInterval = 0.5f;

    [Header("Layers")]
    public LayerMask enemyLayer;

    private bool isActive = false;
    private Coroutine damageCoroutine;

    private void Awake()
    {
        // --- ADD THIS NEW AUDIO SETUP LOGIC ---
        // Create and configure the dedicated AudioSource for the swarm sound
        swarmAudioSource = gameObject.AddComponent<AudioSource>();
        swarmAudioSource.clip = beeSwarmSound;
        swarmAudioSource.volume = beeSwarmVolume;
        swarmAudioSource.loop = true;
        swarmAudioSource.playOnAwake = false;
        // --- END OF NEW AUDIO LOGIC ---

        // Disabled by default
        SetBeeSwarm(false);
    }
    void Update()
    {
        // If the audio source doesn't exist, do nothing.
        if (swarmAudioSource == null) return;

        // Condition 1: Is the game paused?
        bool isGamePaused = Time.timeScale == 0f;

        // Condition 2: Should the sound be playing? (Power-up is active AND game is not paused)
        bool shouldBePlaying = isActive && !isGamePaused;

        // Sync the audio source state with our desired state
        if (shouldBePlaying && !swarmAudioSource.isPlaying)
        {
            // If it should be playing but isn't, play it (handles unpausing).
            swarmAudioSource.Play();
        }
        else if (!shouldBePlaying && swarmAudioSource.isPlaying)
        {
            // If it should NOT be playing but is, pause it (handles pausing or deactivation).
            swarmAudioSource.Pause();
        }
    }
    public void EnableBeePowerUp()
    {
        isActive = true;
        SetBeeSwarm(true);

        // The Update() function now handles this, so no audio code is needed here.

        if (damageCoroutine == null)
            damageCoroutine = StartCoroutine(DamageLoop());
    }

    public void DisableBeePowerUp()
    {
        isActive = false;
        SetBeeSwarm(false);

        // The Update() function now handles this, so no audio code is needed here.

        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
    }

    private void SetBeeSwarm(bool state)
    {
        if (beeSwarms == null) return;

        foreach (var ps in beeSwarms)
        {
            if (ps == null) continue;

            if (state)
            {
                ps.gameObject.SetActive(true);
                ps.Play();
            }
            else
            {
                ps.Stop();
                ps.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator DamageLoop()
    {
        while (isActive)
        {
            if (damagePoint != null)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(damagePoint.position, damageRadius, enemyLayer);
                foreach (var col in hits)
                {
                    if (col == null) continue;

                    if (col.TryGetComponent(out FleaHealth flea)) flea.TakeDamage((int)damage, Vector2.zero);
                    if (col.TryGetComponent(out FleaHealthV2 fleaV2)) fleaV2.TakeDamage((int)damage, Vector2.zero);
                    if (col.TryGetComponent(out FlyHealth fly)) fly.TakeDamage((int)damage, Vector2.zero);
                    if (col.TryGetComponent(out SprayerHealth sprayer)) sprayer.TakeDamage((int)damage, Vector2.zero);
                    if (col.TryGetComponent(out InkHealth ink)) ink.TakeDamage((int)damage, Vector2.zero);
                    if (col.TryGetComponent(out RatKingHealth rat)) rat.TakeDamage((int)damage);
                    if (col.TryGetComponent(out PlayerHealth player)) player.TakeDamage((int)damage, 0f, Vector2.zero);
                }
            }

            yield return new WaitForSeconds(damageTickInterval);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (damagePoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(damagePoint.position, damageRadius);
        }
    }
#endif
}
