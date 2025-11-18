using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SoapTrailDamageL3antix : MonoBehaviour
{
    [Header("Trail Settings")]
    public GameObject[] soapTrailObjects;
    public ParticleSystem[] soapVisuals; // 🟢 Controlled by movement + grounded
    public Transform damagePoint;
    public Vector2 damageSize = new Vector2(2f, 0.5f);
    public LayerMask enemyLayer;
    public int damage = 15;
    public float damageInterval = 1f;
    [Header("Audio Settings")]
    [SerializeField] private AudioClip trailSound; // The looping soap sound.
    [Range(0f, 1f)]
    [SerializeField] private float trailVolume = 1f; // The volume slider.
    private AudioSource trailAudioSource;
    [Header("Stun Settings")]
    public int hitsBeforeStun = 3; // how many hits before stun
    public float stunDuration = 2f; // how long the stun lasts

    [HideInInspector] public L3antixMovement player;

    private Dictionary<GameObject, int> hitCounter = new Dictionary<GameObject, int>();

    private bool canDamage = true;
    void Awake()
    {
        // Create and configure the dedicated AudioSource for the trail sound
        trailAudioSource = gameObject.AddComponent<AudioSource>();
        trailAudioSource.clip = trailSound;
        trailAudioSource.volume = trailVolume;
        trailAudioSource.loop = true;
        trailAudioSource.playOnAwake = false;
    }
    void Update()
    {
        if (player == null)
        {
            // Safety check: If there's no player, ensure the sound is stopped.
            if (trailAudioSource != null && trailAudioSource.isPlaying)
            {
                trailAudioSource.Stop();
            }
            return;
        }
        bool isGamePaused = Time.timeScale == 0f;

        // Condition 2: Is the player grounded and moving?
        bool isGroundedAndMoving = player.IsGrounded() && Mathf.Abs(player.rb.velocity.x) > 0.1f;

        // Condition 3: Should the sound be playing? (Power-up active AND game not paused)
        bool shouldBePlaying = isGroundedAndMoving && !isGamePaused;

        if (shouldBePlaying && !trailAudioSource.isPlaying)
        {
            // If it should be playing but isn't, play it.
            trailAudioSource.Play();
        }
        else if (!shouldBePlaying && trailAudioSource.isPlaying)
        {
            // If it should NOT be playing but is, pause it.
            trailAudioSource.Pause();
        }
        PlayerDash dash = player.GetComponent<PlayerDash>();

        // --- Particle emission ---
        if (soapTrailObjects != null)
        {
            foreach (var obj in soapTrailObjects)
            {
                if (obj == null) continue;
                ParticleSystem ps = obj.GetComponent<ParticleSystem>();
                if (ps == null) continue;

                var em = ps.emission;

                // ✅ Emit if grounded OR dashing
                bool shouldEmit = player.IsGrounded() || (dash != null && dash.IsDashing);
                em.enabled = shouldEmit;

                // --- ADD THIS TO ENSURE PARTICLES ACTUALLY PLAY ---
                if (shouldEmit && !ps.isPlaying)
                {
                    ps.Play();
                }
            }
        }

        // --- Damage: only when grounded OR dashing AND moving OR dashing ---
        if ((player.IsGrounded() || (dash != null && dash.IsDashing)) &&
            (player.rb.velocity.x != 0 || (dash != null && dash.IsDashing)) &&
            canDamage)
        {
            DealDamage();
            StartCoroutine(DamageCooldown());
        }
    }
    public void DisablePowerUp()
    {
        // Disable the main logic.
        this.enabled = false;

        // Disable the trail particle systems.
        foreach (var trail in soapTrailObjects)
        {
            if (trail != null) trail.SetActive(false);
        }

        // --- THIS IS THE CRITICAL FIX ---
        // Disable the permanent visual sprites.
        foreach (var visual in soapVisuals)
        {
            if (visual != null) visual.gameObject.SetActive(false);
        }
        // --- END OF FIX ---
    }
    void DealDamage()
    {
        if (damagePoint == null) return;

        Collider2D[] enemies = Physics2D.OverlapBoxAll(damagePoint.position, damageSize, 0f, enemyLayer);
        foreach (var enemy in enemies)
        {
            // Deal normal damage
            if (enemy.TryGetComponent(out FleaHealth flea)) flea.TakeDamage(damage, Vector2.zero);
            if (enemy.TryGetComponent(out FleaHealthV2 fleaV2)) fleaV2.TakeDamage(damage, Vector2.zero);
            if (enemy.TryGetComponent(out FlyHealth fly)) fly.TakeDamage(damage, Vector2.zero);
            if (enemy.TryGetComponent(out SprayerHealth sprayer)) sprayer.TakeDamage(damage, Vector2.zero);
            if (enemy.TryGetComponent(out InkHealth ink)) ink.TakeDamage(damage, Vector2.zero);
            if (enemy.TryGetComponent(out RatKingHealth rat)) rat.TakeDamage(damage, Vector2.zero, 0f);

            // --- Stun counter ---
            if (!hitCounter.ContainsKey(enemy.gameObject))
                hitCounter[enemy.gameObject] = 0;

            hitCounter[enemy.gameObject]++;

            if (hitCounter[enemy.gameObject] >= hitsBeforeStun)
            {
                if (enemy.TryGetComponent(out EnemyStun stunComp))
                {
                    stunComp.Stun(stunDuration);
                }

                hitCounter[enemy.gameObject] = 0; // reset counter after stun
            }
        }
    }



    IEnumerator DamageCooldown()
    {
        canDamage = false;
        yield return new WaitForSeconds(damageInterval);
        canDamage = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (damagePoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(damagePoint.position, damageSize);
    }
}
