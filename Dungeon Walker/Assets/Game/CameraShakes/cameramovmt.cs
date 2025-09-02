using UnityEngine;

public class CameraFollowMouseHorizontal : MonoBehaviour
{
    // I've renamed 'player' to 'target' for clarity, but it works the same.
    // This variable will now be set by our GameUIManager script.
    public Transform target;

    public float maxHorizontalDistance = 3f; // How far camera moves left/right from player
    public float followSpeed = 5f;         // Camera move speed

    // ---- NEW PUBLIC FUNCTION ----
    // This function allows other scripts to tell the camera what to follow.
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        Debug.Log("Camera target has been set to: " + newTarget.name);
    }
    // ---------------------------

    // Your existing LateUpdate function does not need to change at all.
    // It will simply use whatever 'target' is currently assigned.
    void LateUpdate()
    {
        // If there's no target, the camera shouldn't move.
        if (target == null) return;

        // Get mouse position in world
        Vector3 cursorWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cursorWorldPos.z = 0f;

        // Calculate horizontal target position based on the current 'target'
        float targetX = (target.position.x + cursorWorldPos.x) / 2f;
        float offsetX = targetX - target.position.x;

        if (Mathf.Abs(offsetX) > maxHorizontalDistance)
            targetX = target.position.x + Mathf.Sign(offsetX) * maxHorizontalDistance;

        // Final camera position
        // I've changed player.position.y to target.position.y to match the variable rename.
        Vector3 desiredPos = new Vector3(targetX, target.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);
    }
}
