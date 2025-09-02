using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // <<< 1. ADD THIS LINE

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

    private PhotonView view; // <<< 2. ADD THIS VARIABLE

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (ghostEffect == null) ghostEffect = GetComponent<GhostEffect>();
        if (kritinaMovement == null) kritinaMovement = GetComponent<KritinaMovement>();
        originalGravity = rb.gravityScale;

        view = GetComponent<PhotonView>(); // <<< 3. GET THE COMPONENT
    }

    void Update()
    {
        // <<< 4. WRAP THE INPUT CHECK
        // Only check for the dash key if this is my character.
        if (view.IsMine)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDashing)
            {
                StartDash();
            }
        }
    }

    public void TriggerDash()
    {
        // <<< 5. ALSO PROTECT THE PUBLIC TRIGGER
        // This is for your mobile button.
        if (view.IsMine)
        {
            if (canDash && !isDashing)
            {
                StartDash();
            }
        }
    }

    void FixedUpdate()
    {
        // We only need to run the dash logic if we are actually dashing.
        // This part doesn't need the 'IsMine' check because 'isDashing' is only ever
        // set to true on the local client that owns the character.
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

    private void StartDash()
    {
        // This function is now only ever called on the client that owns the character,
        // so all the logic inside it is safe.
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
