using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is the clean, offline version of your dash script.
public class PlayerDash : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private GhostEffect ghostEffect;
    // This should now reference your OFFLINE movement script.
    // You might need to rename the class if you followed the previous advice.
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

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (ghostEffect == null) ghostEffect = GetComponent<GhostEffect>();
        if (kritinaMovement == null) kritinaMovement = GetComponent<KritinaMovement>();
        originalGravity = rb.gravityScale;
    }

    void Update()
    {
        // The Photon 'IsMine' check is removed. This now runs normally.
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDashing)
        {
            StartDash();
        }
    }

    public void TriggerDash()
    {
        // The Photon 'IsMine' check is removed here as well.
        if (canDash && !isDashing)
        {
            StartDash();
        }
    }

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
