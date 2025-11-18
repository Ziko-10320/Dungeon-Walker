using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // <<< 1. ADD THIS LINE to use Photon's library.

public class L3antixDash : MonoBehaviour
{
    // --- All your existing variables are perfect and don't need changes ---
    #region Variable Declarations
    [Header("Component References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private GhostEffect ghostEffect;
    [SerializeField] private L3antixMovement l3antixMovement;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Sound Settings")]
    public AudioClip dashSoundClip;
    [Range(0f, 1f)]
    public float dashVolume = 1f;

    private bool canDash = true;
    private bool isDashing = false;
    private float dashTimer;
    private float originalGravity;
    private Vector2 dashDirectionVector;
    #endregion

    public bool IsDashing => isDashing;

    // --- 2. ADD THIS VARIABLE to hold the PhotonView component ---
    private PhotonView view;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (ghostEffect == null) ghostEffect = GetComponent<GhostEffect>();
        if (l3antixMovement == null) l3antixMovement = GetComponent<L3antixMovement>();
        originalGravity = rb.gravityScale;

        // --- 3. GET THE PHOTONVIEW component when the script wakes up ---
        // In single-player, this will be null.
        view = GetComponentInParent<PhotonView>();
    }

    void Update()
    {
        // --- 4. ADD THE "NETWORK-AWARE" CHECK at the very top ---
        // This single check makes the script work for both modes.
        if (view != null && !view.IsMine)
        {
            return; // If this is an online character that we don't own, stop here.
        }

        // This input check will now only run for the local player or in single-player.
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDashing)
        {
            StartDash();
        }
    }

    // We must also protect the public function for the mobile button.
    public void TriggerDash()
    {
        // Use the same check here.
        if (view != null && !view.IsMine)
        {
            return;
        }

        if (canDash && !isDashing)
        {
            StartDash();
        }
    }

    // FixedUpdate does not need the check because 'isDashing' is only ever set
    // to true on the local client, so this code won't run on other clients' versions of this character.
    void FixedUpdate()
    {
        if (isDashing)
        {
            transform.position += (Vector3)dashDirectionVector * dashSpeed * Time.fixedDeltaTime;
            dashTimer += Time.deltaTime; // Use Time.deltaTime for consistency if called from Update

            if (dashTimer >= dashDuration)
            {
                EndDash();
            }
        }
    }

    // The rest of your functions are private and are only called by the protected functions,
    // so they are already safe and do not need any changes.
    #region Helper Functions
    private void StartDash()
    {
        isDashing = true;
        canDash = false;
        dashTimer = 0f;

        if (l3antixMovement != null)
        {
            dashDirectionVector = l3antixMovement.isFacingRight ? Vector2.right : Vector2.left;
            l3antixMovement.enabled = false;
        }
        else
        {
            dashDirectionVector = Vector2.right;
        }

        if (dashSoundClip != null)
        {
            AudioSource.PlayClipAtPoint(dashSoundClip, transform.position, dashVolume);
        }

        rb.velocity = Vector2.zero;
        rb.gravityScale = 0f;

        if (ghostEffect != null)
        {
            ghostEffect.StartGhostEffect();
        }
    }

    private void EndDash()
    {
        isDashing = false;

        if (l3antixMovement != null)
        {
            l3antixMovement.enabled = true;
        }

        rb.gravityScale = originalGravity;
        rb.velocity = Vector2.zero;

        if (ghostEffect != null)
        {
            ghostEffect.StopGhostEffect();
        }

        StartCoroutine(DashCooldown());
    }

    private IEnumerator DashCooldown()
    {
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
    #endregion
}
