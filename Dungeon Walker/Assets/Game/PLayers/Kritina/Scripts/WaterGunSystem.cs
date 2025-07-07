using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // Required for Coroutines

public class WaterGunSystem : MonoBehaviour
{
    [SerializeField] private GameObject Gun;
    [SerializeField] private GameObject Arm;
    [SerializeField] private GameObject bullet;
    [SerializeField] private Transform bulletSpawnPoint;
    public ParticleSystem WaterFlash;
    public ParticleSystem WaterFlash2;
    private Vector2 direction;
    private Vector2 worldPosition;

    private GameObject bulletInst;

    [Tooltip("Maximum angle (in degrees) the gun/arm can rotate forward")]
    public float maxAimAngle = 80f;

    [Tooltip("Reference to the player's transform for flip detection")]
    public Transform playerTransform; // Assign your player here

    [Tooltip("Minimum distance required to rotate gun/arm")]
    public float minDistanceToAim = 0.5f;

    // --- NEW: Firing Rate and Ammo System Variables ---
    [Header("Firing & Ammo System")]
    [SerializeField] private float fireRate = 0.5f; // Time between shots (e.g., 0.5 seconds)
    [SerializeField] private int maxAmmo = 12; // Maximum bullets before reload
    [SerializeField] private float reloadTime = 2.0f; // Time it takes to reload

    private float nextFireTime = 0f; // Time when the next shot is allowed
    private int currentAmmo; // Current ammo count
    private bool isReloading = false; // Is the gun currently reloading?

    void Awake()
    {
        currentAmmo = maxAmmo; // Initialize current ammo
    }

    void Update()
    {
        HandleGunAndArmRotation();
        HandleGunShoot();
    }

    private void HandleGunAndArmRotation()
    {
        // Get mouse position in world space
        worldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 gunPosition = Gun.transform.position;

        // Calculate direction from gun to mouse
        direction = (worldPosition - gunPosition).normalized;

        // Only rotate if mouse is far enough away
        float distance = (worldPosition - gunPosition).magnitude;

        if (distance > minDistanceToAim)
        {
            // Determine current forward direction based on player scale
            Vector2 forwardDirection = playerTransform.localScale.x > 0 ? Vector2.right : Vector2.left;

            // Calculate angle between current forward and mouse direction
            float angle = Vector2.SignedAngle(forwardDirection, direction);

            // Clamp angle to prevent aiming behind
            float clampedAngle = Mathf.Clamp(angle, -maxAimAngle, maxAimAngle);

            // Apply clamped rotation
            Quaternion targetRotation = Quaternion.Euler(0, 0, clampedAngle);
            Gun.transform.rotation = targetRotation;
            Arm.transform.rotation = targetRotation;
        }
        else
        {
            // Optional: freeze rotation or snap back to default
            Gun.transform.localRotation = Quaternion.identity;
            Arm.transform.localRotation = Quaternion.identity;
        }
    }

    private void HandleGunShoot()
    {
        // Allow shooting only if:
        // 1. Left mouse button is pressed (based on your provided code)
        // 2. Gun is not reloading
        // 3. Enough time has passed since the last shot
        if (Mouse.current.leftButton.wasPressedThisFrame && !isReloading && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                // Shoot the bullet
                bulletInst = Instantiate(bullet, bulletSpawnPoint.position, Quaternion.identity);
                BulletBehavior bulletBehavior = bulletInst.GetComponent<BulletBehavior>();
                if (bulletBehavior != null)
                {
                    bulletBehavior.SetDirection(direction); // Set the direction of the bullet
                }

                // Play shooting effects
                if (WaterFlash != null) WaterFlash.Play();
                if (WaterFlash2 != null) WaterFlash2.Play();

                currentAmmo--; // Decrease ammo
                nextFireTime = Time.time + fireRate; // Set time for next shot

                // If ammo runs out, start reloading
                if (currentAmmo <= 0)
                {
                    StartCoroutine(Reload());
                }
            }
            else
            {
                // If player tries to shoot with 0 ammo, start reload if not already reloading
                if (!isReloading)
                {
                    StartCoroutine(Reload());
                }
            }
        }
    }

    // Coroutine for reloading
    private IEnumerator Reload()
    {
        isReloading = true; // Set reloading state to true
        Debug.Log("Reloading started...");

        // Wait for reload duration
        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo; // Refill ammo
        isReloading = false; // End reloading state
        Debug.Log("Reloading complete. Current ammo: " + currentAmmo);
    }

    // You can add public methods to get current ammo or reloading status for UI display
    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }

    public bool IsReloading()
    {
        return isReloading;
    }
}

