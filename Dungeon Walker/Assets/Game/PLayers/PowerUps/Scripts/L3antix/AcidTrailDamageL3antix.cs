using UnityEngine;
using System.Collections;

public class AcidTrailDamageL3antix : MonoBehaviour
{
    [Header("Trail Settings")]
    public GameObject[] acidTrailObjects;
    public ParticleSystem[] acidVisuals; // 🟢 Controlled by movement + grounded
    public Transform damagePoint;
    public Vector2 damageSize = new Vector2(2f, 0.5f);
    public LayerMask enemyLayer;
    public int damage = 15;
    public float damageInterval = 1f;

    [HideInInspector] public L3antixMovement player;

    private bool canDamage = true;

    void Update()
    {
        if (player == null) return;

        PlayerDash dash = player.GetComponent<PlayerDash>();

        // --- Particle emission ---
        if (acidTrailObjects != null)
        {
            foreach (var obj in acidTrailObjects)
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

    void DealDamage()
    {
        if (damagePoint == null) return;

        Collider2D[] enemies = Physics2D.OverlapBoxAll(damagePoint.position, damageSize, 0f, enemyLayer);
        foreach (var enemy in enemies)
        {
            if (enemy.TryGetComponent(out FleaHealth flea)) flea.TakeDamage(damage, Vector2.zero);
            if (enemy.TryGetComponent(out FleaHealthV2 fleaV2)) fleaV2.TakeDamage(damage, Vector2.zero);
            if (enemy.TryGetComponent(out FlyHealth fly)) fly.TakeDamage(damage, Vector2.zero);
            if (enemy.TryGetComponent(out SprayerHealth sprayer)) sprayer.TakeDamage(damage, Vector2.zero);
            if (enemy.TryGetComponent(out InkHealth ink)) ink.TakeDamage(damage, Vector2.zero);
            if (enemy.TryGetComponent(out RatKingHealth rat)) rat.TakeDamage(damage);
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
