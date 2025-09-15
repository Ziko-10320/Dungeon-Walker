using UnityEngine;
using System.Collections;

public class SoapTrailDamage : MonoBehaviour
{
    [Header("Trail Settings")]
    public GameObject[] soapTrailObjects;
    public Transform damagePoint;
    public Vector2 damageSize = new Vector2(2f, 0.5f);
    public LayerMask enemyLayer;
    public int damage = 15;
    public float damageInterval = 1f;

    [HideInInspector] public KritinaMovement player;

    private bool canDamage = true;

    private bool trailsWereActive = false;
    void Update()
    {
        if (!enabled) return;
        if (player == null) return;

        bool grounded = player.IsGrounded();

        // --- Control SoapTrail Particles based on grounded state ---
        foreach (var trail in soapTrailObjects)
        {
            if (trail == null) continue;

            var ps = trail.GetComponent<ParticleSystem>();
            if (ps == null) continue;

            if (grounded && !trailsWereActive)
            {
                ps.Play();
            }
            else if (!grounded && trailsWereActive)
            {
                ps.Stop();
            }
        }
        trailsWereActive = grounded;

        // --- Damage logic works only while grounded ---
        if (grounded && canDamage)
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
            FleaHealth flea = enemy.GetComponent<FleaHealth>();
            if (flea != null)
            {
                flea.TakeDamage(damage, Vector2.zero);
            }

            FlyHealth fly = enemy.GetComponent<FlyHealth>();
            if (fly != null)
            {
                fly.TakeDamage(damage, Vector2.zero);
            }

            SprayerHealth sprayer = enemy.GetComponent<SprayerHealth>();
            if (sprayer != null)
            {
                sprayer.TakeDamage(damage, Vector2.zero);
            }

            InkHealth ink = enemy.GetComponent<InkHealth>();
            if (ink != null)
            {
                ink.TakeDamage(damage, Vector2.zero);
            }

            RatKingHealth rat = enemy.GetComponent<RatKingHealth>();
            if (rat != null)
            {
                rat.TakeDamage(damage);
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
