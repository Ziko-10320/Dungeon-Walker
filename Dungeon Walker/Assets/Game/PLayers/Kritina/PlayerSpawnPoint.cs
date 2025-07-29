using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    [Header("Player Spawn Point Settings")]
    public Color gizmoColor = Color.blue;
    public float gizmoRadius = 0.5f;

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoRadius);
        Gizmos.DrawWireSphere(transform.position, gizmoRadius + 0.1f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, gizmoRadius + 0.2f);
    }
}

