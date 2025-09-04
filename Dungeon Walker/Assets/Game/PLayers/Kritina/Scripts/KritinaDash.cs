using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // We need this to access Photon features.

// This single script will now work for both online and offline modes.
public class PlayerDash : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private GhostEffect ghostEffect;
    [SerializeField] private KritinaMovement kritinaMovement;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Sound Settings")]
    public AudioClip dashSoundClip;
    [Range(0f, 1f)]
    public float dashVolume = 1f;

    // State variables
    private bool canDash = true;
    private bool isDashing = false;
    private float dashTimer;
    private float originalGravity;
    private Vector2 dashDirectionVector;

    public bool IsDashing => isDashing;

    // --- THIS IS THE KEY TO MAKING IT WORK FOR BOTH MODES ---
    private PhotonView view;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (ghostEffect == null) ghostEffect = GetComponent<GhostEffect>();
        if (kritinaMovement == null) kritinaMovement = GetComponent<KritinaMovement>();
        originalGravity = rb.gravityScale;

        // Try to get the PhotonView component. This will be null on the offline prefab.
        view = GetComponent<PhotonView>();
    }

    void Update()
    {
        // --- THE MAGIC CHECK ---
        // If 'view' is not null AND this is not our character, do nothing.
        // If 'view' IS null (offline mode), this check is skipped and the code runs.
        if (view != null && !view.IsMine)
        {
            return;
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
        if (view != null && !view.IsMine)
        {
            return;
        }

        if (canDash && !isDashing)
        {
            StartDash();
        }
    }

    // The FixedUpdate logic doesn't need the 'IsMine' check because 'isDashing'
    // is only ever set to true on the local client, so this code won't run on other clients' versions of this character.
    void FixedUpdate()
    {
        if (isDashing)
        {
            transform.position += (Vector3)dashDirectionVector * dashSpeed * Time.fixedDeltaTime;
            dashTimer += Time.fixedDeltaTime;

            if (dashTimer >= dashDuration)
            {
                EndDash();
            }
        }
    }

    // The rest of the functions are private and are only called by the protected Update/TriggerDash functions,
    // so they are already safe and do not need any changes.
    private void StartDash()
    {
        isDashing = true;
        canDash = false;
        dashTimer = 0f;

        if (kritinaMovement != null)
        {
            dashDirectionVector = kritinaMovement.isFacingRight ? Vector2.right : Vector2.left;
            kritinaMovement.enabled = false;
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

        if (kritinaMovement != null)
        {
            kritinaMovement.enabled = true;
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
}
