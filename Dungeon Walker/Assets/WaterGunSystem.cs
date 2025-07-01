using UnityEngine;
using UnityEngine.InputSystem;

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
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Pass the calculated 'direction' to the bullet
            bulletInst = Instantiate(bullet, bulletSpawnPoint.position, Quaternion.identity); // Use Quaternion.identity for initial rotation
            BulletBehavior bulletBehavior = bulletInst.GetComponent<BulletBehavior>();
            if (bulletBehavior != null)
            {
                bulletBehavior.SetDirection(direction); // Set the direction of the bullet
            }
            WaterFlash.Play();
            WaterFlash2.Play();
        }
    }
}
