using UnityEngine;

public class CameraFollowMouseHorizontal : MonoBehaviour
{
    public Transform player;               // Drag Player GameObject here
    public float maxHorizontalDistance = 3f; // How far camera moves left/right from player
    public float followSpeed = 5f;         // Camera move speed

    void LateUpdate()
    {
        if (player == null) return;

        // Get mouse position in world
        Vector3 cursorWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cursorWorldPos.z = 0f;

        // Calculate horizontal target position
        float targetX = (player.position.x + cursorWorldPos.x) / 2f;
        float offsetX = targetX - player.position.x;

        if (Mathf.Abs(offsetX) > maxHorizontalDistance)
            targetX = player.position.x + Mathf.Sign(offsetX) * maxHorizontalDistance;

        // Final camera position
        Vector3 desiredPos = new Vector3(targetX, player.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);
    }
}