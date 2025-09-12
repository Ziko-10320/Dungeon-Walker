using UnityEngine;
using Photon.Pun;

// We implement IPunObservable to get access to the OnPhotonSerializeView method.
public class PlayerSyncManager : MonoBehaviour, IPunObservable
{
    [Header("Required Components")]
    [Tooltip("The main PhotonView of this player prefab.")]
    [SerializeField] private PhotonView photonView;

    [Tooltip("The Animator component for this player.")]
    [SerializeField] private Animator animator;

    [Tooltip("The Rigidbody2D component for this player.")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Synchronization Settings")]
    [Tooltip("How smoothly the remote player's position is interpolated. Higher is smoother but has more delay.")]
    [SerializeField] private float smoothingFactor = 10f;

    // These variables will store the "networked" state of the remote player.
    private Vector2 networkPosition;
    private float networkRotation;
    private Vector2 networkVelocity;
    private bool isTeleporting = true; // Used to snap position on first update.
    private bool isTeleportingOnFirstUpdate = true;
    [SerializeField] private WeaponSwitchManager weaponSwitchManager;
    private float distance;
    private float angle;
    private Vector2 networkPositionOnReceive;
    private float lastPacketReceiveTime;
    void Awake()
    {
        // Safety checks to ensure all components are assigned.
        if (photonView == null) photonView = GetComponent<PhotonView>();
        if (animator == null) animator = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (weaponSwitchManager == null) weaponSwitchManager = GetComponent<WeaponSwitchManager>();
        if (photonView == null || animator == null || rb == null)
        {
            Debug.LogError("PlayerSyncManager is missing one or more required components!", this);
            enabled = false;
        }
    }

  void FixedUpdate()
{
    if (!photonView.IsMine)
    {
        // Calculate the time since the last network update was received.
        float timeSinceLastPacket = Time.time - lastPacketReceiveTime;

        // Calculate the estimated current network position based on the last received data
        // and the time that has passed. This is our target for interpolation.
        Vector2 estimatedNetworkPosition = networkPosition + networkVelocity * timeSinceLastPacket;
        float estimatedNetworkRotation = networkRotation; // Rotation is usually not extrapolated with velocity

        // Determine how far we are from the target. The further we are, the faster we should move.
        float distanceToTarget = Vector2.Distance(rb.position, estimatedNetworkPosition);

        // Calculate the interpolation speed. This will make the player move faster when further away.
        // We use a factor that ensures it catches up quickly but still smoothly.
        // You can adjust the 'smoothingFactor' (e.g., 10, 20, 30) in the Inspector.
        float interpolationSpeed = smoothingFactor * distanceToTarget; // The faster we are, the faster we move

        // Apply the interpolation for position and rotation.
        // We use a small value (e.g., 0.1f) to ensure it's always moving, even if distance is small.
        rb.position = Vector2.MoveTowards(rb.position, estimatedNetworkPosition, interpolationSpeed * Time.fixedDeltaTime);
        rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, estimatedNetworkRotation, interpolationSpeed * Time.fixedDeltaTime);

        // Optional: If the player is still too far off, snap them. This prevents extreme teleportation
        // if there's a sudden large jump in network position (e.g., due to packet loss).
        if (distanceToTarget > 5f) // Adjust this threshold (e.g., 5 units) as needed
        {
            rb.position = estimatedNetworkPosition;
            rb.rotation = estimatedNetworkRotation;
            Debug.LogWarning("Player snapped due to significant desync.");
        }
    }
}

    /// <summary>
    /// This is the most important function for synchronization.
    /// It's called by Photon multiple times per second.
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // This is OUR player. We are sending our data.
            stream.SendNext(rb.position);
            stream.SendNext(rb.rotation);
            stream.SendNext(rb.velocity);
            stream.SendNext(animator.GetBool("isRunning"));

            // --- NEW: Send the name of our current weapon ---
            stream.SendNext(weaponSwitchManager.GetCurrentWeaponName());
        }
        else
        {
            // This is a REMOTE player. We are receiving their data.
            this.networkPosition = (Vector2)stream.ReceiveNext();
            this.networkRotation = (float)stream.ReceiveNext();
            this.networkVelocity = (Vector2)stream.ReceiveNext();

            lastPacketReceiveTime = Time.time;
            networkPositionOnReceive = rb.position;

            if (isTeleportingOnFirstUpdate)
            {
                rb.position = networkPosition;
                rb.rotation = networkRotation;
                isTeleportingOnFirstUpdate = false;
            }

            this.animator.SetBool("isRunning", (bool)stream.ReceiveNext());

            // --- NEW: Receive the weapon name and force the switch ---
           
        }
    }

    /// <summary>
    /// A public function to trigger a particle effect over the network.
    /// </summary>
    /// <param name="particleSystem">The particle system to play.</param>
    public void PlayParticleEffect(ParticleSystem particleSystem)
    {
        if (particleSystem == null) return;

        // Call the RPC to play the effect on all clients.
        // We send the path to the particle system so other clients can find it.
        photonView.RPC("RPC_PlayParticleEffect", RpcTarget.All, GetGameObjectPath(particleSystem.gameObject));
    }

    [PunRPC]
    private void RPC_PlayParticleEffect(string path)
    {
        // This code runs on every player's machine.
        GameObject targetObject = FindObjectFromPath(path);
        if (targetObject != null)
        {
            ParticleSystem ps = targetObject.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }
        }
    }
    [PunRPC]
    private void RPC_InstantiateEffect(string prefabName, Vector3 position, Quaternion rotation)
    {
        GameObject effectPrefab = Resources.Load<GameObject>(prefabName);
        if (effectPrefab != null)
        {
            // 1. Instantiate the prefab as a GameObject.
            GameObject instance = Instantiate(effectPrefab, position, rotation);

            // --- THIS IS THE CRITICAL FIX ---
            // 2. Get the ParticleSystem component from the new instance.
            ParticleSystem ps = instance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // 3. Explicitly tell it to play.
                ps.Play();
            }
            // --- END OF FIX ---

            // The self-destruct logic is still good.
            Destroy(instance, 3f);
        }
        else
        {
            Debug.LogError("Could not find effect prefab in Resources folder: " + prefabName);
        }
    }

    // --- ADD THIS PUBLIC HELPER FUNCTION ---
    public void InstantiateEffect(string prefabName, Vector3 position, Quaternion rotation)
    {
        // This is the function our launcher will call.
        // It sends the RPC to all clients.
        photonView.RPC("RPC_InstantiateEffect", RpcTarget.All, prefabName, position, rotation);
    }
    // --- Helper functions to find objects by path ---
    private string GetGameObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path;
    }

    private GameObject FindObjectFromPath(string path)
    {
        return GameObject.Find(path);
    }
}
