using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Header("Spawn Point Settings")]
    public float spawnRadius = 5f;
    public Color gizmoColor = Color.red;

    [Header("Visual Settings")]
    public bool showGizmo = true;
    public bool showWireframe = true;

    // Méthode pour obtenir une position aléatoire dans le rayon de spawn
    public Vector3 GetRandomSpawnPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        return spawnPosition;
    }

    // Méthode pour vérifier si une position est dans le rayon de spawn
    public bool IsPositionInSpawnRadius(Vector3 position)
    {
        float distance = Vector3.Distance(transform.position, position);
        return distance <= spawnRadius;
    }

    // Affichage visuel dans l'éditeur Unity
    void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color = gizmoColor;

        if (showWireframe)
        {
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
        else
        {
            Gizmos.DrawSphere(transform.position, spawnRadius);
        }

        // Dessiner un petit cube au centre pour marquer le point exact
        Gizmos.color = Color.white;
        Gizmos.DrawCube(transform.position, Vector3.one * 0.5f);
    }

    void OnDrawGizmosSelected()
    {
        // Affichage plus détaillé quand le GameObject est sélectionné
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        // Afficher quelques points d'exemple de spawn
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 point = transform.position + new Vector3(
                Mathf.Cos(angle) * spawnRadius * 0.8f,
                0,
                Mathf.Sin(angle) * spawnRadius * 0.8f
            );
            Gizmos.DrawCube(point, Vector3.one * 0.2f);
        }
    }
}