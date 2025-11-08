using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class LaserTrap : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float toggleInterval = 1.5f; // seconds between on/off
    [Tooltip("How long to disable player movement after knockback (seconds).")]
    [SerializeField] private float stunDuration = 0.25f;
    [Tooltip("If true, will try to disable the player's movement script (KritinaMovement) briefly so knockback isn't cancelled.")]
    [SerializeField] private bool disableMovementDuringKnockback = true;
    [Tooltip("Small random delay added to initial toggle so many lasers don't blink in perfect sync. 0 = no random.")]
    [SerializeField] private float randomStartOffsetMax = 0f;

    // No manual sprite reference required — we try to find a SpriteRenderer on this object automatically.
    private BoxCollider2D boxCollider;
    private SpriteRenderer laserVisual;
    private bool isActive = true;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }

        // Try to find a sprite renderer on the same GameObject (optional)
        laserVisual = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        // This method is called every time the GameObject is set to active.
        // We will let the WaveManager be responsible for starting the coroutine
        // by calling ResetTrap(). For now, we can just make sure the laser
        // starts in a predictable state.

        // Default to being ON when first enabled.
        isActive = true;
        if (boxCollider != null) boxCollider.enabled = true;
        if (laserVisual != null) laserVisual.enabled = true;
    }

    private IEnumerator ToggleLaserRoutine(float initialDelay)
    {
        // optional initial offset to desync many lasers
        if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            yield return new WaitForSeconds(toggleInterval);
            ToggleLaser();
        }
    }

    private void ToggleLaser()
    {
        isActive = !isActive;

        if (boxCollider != null)
            boxCollider.enabled = isActive;

        if (laserVisual != null)
            laserVisual.enabled = isActive;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        if (!other.CompareTag("Player")) return;

        // --- THIS IS THE START OF THE CHANGE ---

        // Try to get the health component for Kritina
        PlayerHealth kritinaHealth = other.GetComponent<PlayerHealth>();
        if (kritinaHealth != null)
        {
            // Tell PlayerHealth the damage happened but with zero knockback parameters.
            kritinaHealth.TakeDamage(damage, 0f, Vector2.zero);
        }

        // ALSO, try to get the health component for L3antix
        L3antixHealth l3antixHealth = other.GetComponent<L3antixHealth>();
        if (l3antixHealth != null)
        {
            // Tell L3antixHealth it took damage.
            l3antixHealth.TakeDamage(damage, 0f, Vector2.zero);
        }

        // --- END OF THE CHANGE ---

        // The rest of the knockback logic is perfect and remains the same.
        Rigidbody2D playerRb = other.attachedRigidbody ?? other.GetComponent<Rigidbody2D>();
        float dirX = (other.transform.position.x < transform.position.x) ? -1f : 1f;
        Vector2 knockDir = new Vector2(dirX, 0f);

        if (playerRb != null)
        {
            StartCoroutine(ApplyKnockbackRoutine(playerRb, knockDir, knockbackForce, stunDuration, disableMovementDuringKnockback));
        }
    }

    private IEnumerator ApplyKnockbackRoutine(Rigidbody2D rb, Vector2 direction, float force, float stunTime, bool disableMove)
    {
        if (rb == null) yield break;

        // Wait for physics step(s) so PlayerHealth's coroutine (which may zero out velocity) is done.
        // Two fixed updates is a safe bet across different configurations.
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // Try to disable player's movement script (if present) so it doesn't immediately cancel our velocity.
        MonoBehaviour movementScript = null;
        if (disableMove)
        {
            // Try common movement script names — adjust if your movement class has a different name.
            movementScript = rb.GetComponent("KritinaMovement") as MonoBehaviour
                            ?? rb.GetComponent("L3antixMovement") as MonoBehaviour
                             ?? rb.GetComponent("PlayerMovement") as MonoBehaviour
                             ?? rb.GetComponent("CharacterController2D") as MonoBehaviour;
            if (movementScript != null)
            {
                movementScript.enabled = false;
            }
        }

        // Apply deterministic horizontal velocity (set directly)
        float vx = direction.x * force;
        rb.velocity = new Vector2(vx, rb.velocity.y);

        // Also add a small impulse so physics reacts strongly (optional)
        rb.AddForce(new Vector2(direction.x * force * 0.25f, 0f), ForceMode2D.Impulse);

        // Keep the player stunned (movement disabled) for stunTime seconds
        if (movementScript != null)
        {
            yield return new WaitForSeconds(stunTime);

            // Re-enable movement (if player hasn't been destroyed)
            if (movementScript != null)
                movementScript.enabled = true;
        }
        else
        {
            // If we didn't disable movement, still wait a short moment so knockback feels consistent
            yield return new WaitForSeconds(0.05f);
        }
    }
    public void ResetTrap()
    {
        StopAllCoroutines();

        // Reset the laser to its initial "on" state.
        isActive = true;
        if (boxCollider != null) boxCollider.enabled = true;
        if (laserVisual != null) laserVisual.enabled = true;

        // --- THIS IS THE KEY CHANGE ---
        // The logic from Start() now lives here.
        if (randomStartOffsetMax > 0f)
        {
            float r = Random.Range(0f, randomStartOffsetMax);
            StartCoroutine(ToggleLaserRoutine(r));
        }
        else
        {
            StartCoroutine(ToggleLaserRoutine(0f));
        }
        // --- END OF CHANGE ---

        Debug.Log($"{gameObject.name} has been reset and is now toggling.");
    }
    // --- ALSO ADD THIS NEW METHOD ---
    public void StopTrap()
    {
        // This method ensures the coroutine is stopped when the laser is disabled.
        StopAllCoroutines();
    }
}
