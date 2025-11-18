using UnityEngine;
using System.Collections;

public class ReviveSystem : MonoBehaviour
{
    [Header("Revive PowerUp")]
    public bool hasRevivePowerUp = false; // set by PowerUpManager when equipped
    public bool hasUsedRevive = false;    // one-time use per run

    [Header("Visuals")]
    public GameObject darkFlare;          // initially disabled
    public GameObject heartWithWings;     // initially disabled
    public Animator heartAnimator;        // Animator on heart (triggers: "In", "Out")
    public ParticleSystem[] healingParticles; // healing particle array

    [Header("Settings")]
    [Tooltip("Total pause duration in seconds (1.0 - 1.5 recommended).")]
    public float revivePauseDuration = 1.5f;
    [Tooltip("Seconds of temporary invincibility after revive.")]
    public float postReviveInvincibility = 0.8f;
    [Header("Audio")]
    [SerializeField] private AudioClip reviveSound; // The sound to play once on revive.
    [Range(0f, 1f)]
    [SerializeField] private float reviveVolume = 1f;
    // internal refs
    private PlayerHealth playerHealth;
    private Rigidbody2D rb;
    private KritinaMovement movement;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<KritinaMovement>();

        if (darkFlare != null) darkFlare.SetActive(false);
        if (heartWithWings != null) heartWithWings.SetActive(false);
    }

    // Called by PlayerHealth.Die() (or PowerUpManager) when player dies
    public void TryRevive()
    {
        if (hasRevivePowerUp && !hasUsedRevive)
        {
            hasUsedRevive = true;   // consume now so it can't be triggered twice
            hasRevivePowerUp = false;
            Debug.Log("Revive consumed. Starting revive sequence...");
            StartCoroutine(ReviveSequence());
        }
        else
        {
            Debug.Log("TryRevive called but no revive available.");
        }
    }

    private IEnumerator ReviveSequence()
    {
        if (playerHealth != null) playerHealth.ResetDeathEffects();

        if (reviveSound != null)
        {
            AudioSource.PlayClipAtPoint(reviveSound, transform.position, reviveVolume);
        }
        // 1) Ensure heart animator and any flare animator run in UnscaledTime
        if (heartAnimator != null) heartAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        if (heartWithWings != null)
        {
            var anim = heartWithWings.GetComponent<Animator>();
            if (anim != null) anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        // Make healing particles use unscaled time so they can be played while paused if needed
        if (healingParticles != null)
        {
            foreach (var ps in healingParticles)
            {
                if (ps == null) continue;
                var main = ps.main;
                main.useUnscaledTime = true;
            }
        }

        // 2) Prevent player movement/physics during revive sequence
        if (movement != null) movement.enabled = false;
        if (rb != null) rb.velocity = Vector2.zero;

        // set player temporarily invincible for the sequence
        if (playerHealth != null) playerHealth.isInvincible = true;

        // 3) Pause the game
        Time.timeScale = 0f;

        // 4) Enable visuals
        if (darkFlare != null) darkFlare.SetActive(true);
        if (heartWithWings != null) heartWithWings.SetActive(true);

        // 5) Play "In" animation
        if (heartAnimator != null) heartAnimator.SetTrigger("In");

        // Wait half the pause (unscaled)
        yield return new WaitForSecondsRealtime(revivePauseDuration / 2f);

        // 6) Then trigger "Out"
        if (heartAnimator != null) heartAnimator.SetTrigger("Out");

        // Wait the remaining half
        yield return new WaitForSecondsRealtime(revivePauseDuration / 2f);

        // 7) Disable visuals
        if (darkFlare != null) darkFlare.SetActive(false);
        if (heartWithWings != null) heartWithWings.SetActive(false);

        // 8) Restore player health
        if (playerHealth != null)
        {
            playerHealth.FullHeal(); // uses your existing function
            Debug.Log("Player health restored by revive.");
        }

        // 9) Resume the game
        Time.timeScale = 1f;

        // 10) Play healing particles (post-resume)
        if (healingParticles != null)
        {
            foreach (var ps in healingParticles)
            {
                if (ps == null) continue;
                // ensure particle is playing; main.useUnscaledTime was set but now timescale restored
                ps.Play();
            }
        }

        // 11) Small invincibility window so player doesn't die instantly after revive
        if (playerHealth != null)
            StartCoroutine(TemporaryInvulnerability(postReviveInvincibility));

        // 12) Re-enable movement (allow input again)
        if (movement != null) movement.enabled = true;
        if (playerHealth != null) playerHealth.OnReviveComplete();
    }

    private IEnumerator TemporaryInvulnerability(float duration)
    {
        if (playerHealth == null) yield break;
        playerHealth.isInvincible = true;
        yield return new WaitForSeconds(duration); // scaled time is fine here (game running)
        playerHealth.isInvincible = false;
    }
    public void DoAdRevive()
    {
        Debug.Log("Ad Revive triggered! Starting the normal revive sequence.");
        // THIS IS THE FIX. We call the working ReviveSequence, not the broken one on PlayerHealth.
        StartCoroutine(ReviveSequence());
    }
    public IEnumerator PlayReviveAnimation()
    {
        // This coroutine's ONLY job is to play the visual effects.
        // It does NOT change health, movement, or game state.

        if (reviveSound != null) AudioSource.PlayClipAtPoint(reviveSound, transform.position, reviveVolume);
        if (heartAnimator != null) heartAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

        Time.timeScale = 0f; // Pause for the animation

        if (darkFlare != null) darkFlare.SetActive(true);
        if (heartWithWings != null) heartWithWings.SetActive(true);
        if (heartAnimator != null) heartAnimator.SetTrigger("In");

        yield return new WaitForSecondsRealtime(revivePauseDuration / 2f);

        if (heartAnimator != null) heartAnimator.SetTrigger("Out");

        yield return new WaitForSecondsRealtime(revivePauseDuration / 2f);

        if (darkFlare != null) darkFlare.SetActive(false);
        if (heartWithWings != null) heartWithWings.SetActive(false);

        Time.timeScale = 1f; // Resume the game

        Debug.Log("[ReviveSystem] Animation finished.");
    }
}
