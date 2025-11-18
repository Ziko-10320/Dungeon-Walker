using UnityEngine;
using System.Collections;

public class PooledProjectileController : MonoBehaviour
{
    private FlyAttack owner;
    private bool isCharged;
    private float lifetime;
    private Rigidbody2D rb;

    public void Initialize(FlyAttack ownerScript, bool charged, Vector2 targetPos, float speed, float life)
    {
        this.owner = ownerScript;
        this.isCharged = charged;
        this.lifetime = life;
        this.rb = GetComponent<Rigidbody2D>();

        // Enable physics and fire
        if (rb != null)
        {
            rb.isKinematic = false;
            Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
            rb.velocity = direction * speed;
        }

        // Start the self-destruct timer
        StartCoroutine(LifetimeRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        // If still active after lifetime, trigger an explosion and return to pool
        owner.HandleProjectileCollision(this.gameObject, null, isCharged);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if we hit the player or the ground
        if (((1 << other.gameObject.layer) & owner.playerLayer) != 0 ||
            ((1 << other.gameObject.layer) & owner.groundLayer) != 0)
        {
            // Tell the main FlyAttack script to handle the collision logic
            owner.HandleProjectileCollision(this.gameObject, other.gameObject, isCharged);
        }
    }
}