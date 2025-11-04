// PointerTargetSettings.cs
using UnityEngine;

public class PointerTargetSettings : MonoBehaviour
{
    [Header("Arrow Pointer Settings")]
    [Tooltip("How far from the center the arrow will circle.")]
    public float arrowCircleRadius = 1.5f;

    [Tooltip("An offset to adjust the center of the circle (e.g., move it up to the character's chest).")]
    public Vector2 arrowCenterOffset = Vector2.zero;

    // --- THIS IS THE FIX ---
    // Renamed from OnDrawGizmosSelected to OnDrawGizmos.
    // Now it will draw the circle even when the player isn't selected, just like your checkpoints.
    void OnDrawGizmos()
    {
        // Calculate the center of the circle using this object's position
        Vector3 circleCenter = transform.position + (Vector3)arrowCenterOffset;

        // Draw the wireframe circle
        Gizmos.color = Color.cyan; // A nice bright color for the arrow's path
        Gizmos.DrawWireSphere(circleCenter, arrowCircleRadius);

        // Draw a small cross at the center point to make it easy to see
        Gizmos.DrawLine(circleCenter - Vector3.up * 0.2f, circleCenter + Vector3.up * 0.2f);
        Gizmos.DrawLine(circleCenter - Vector3.left * 0.2f, circleCenter + Vector3.left * 0.2f);
    }
}
