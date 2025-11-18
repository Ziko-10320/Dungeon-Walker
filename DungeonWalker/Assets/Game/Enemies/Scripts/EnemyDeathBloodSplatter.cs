using UnityEngine;

public class EnemyDeathBloodSplatter : MonoBehaviour
{
    [Header("Death Splatter Settings")]
    public LayerMask wallLayer; // Layer for walls/background where blood should splatter
    public float splatterOffsetZ = 0.1f; // Offset to ensure splatter is visible on top of the wall

    // Call this method when the enemy dies
    public void OnEnemyDeath()
    {
        if (BloodSplatterManager.Instance == null)
        {
            Debug.LogWarning("BloodSplatterManager.Instance is null. Make sure it's initialized.");
            return;
        }

        Vector3 splatterPosition = transform.position;
        splatterPosition.z += splatterOffsetZ;

        string enemyTag = gameObject.tag;
        if (string.IsNullOrEmpty(enemyTag) || enemyTag == "Untagged")
        {
            Debug.LogWarning("EnemyDeathBloodSplatter: GameObject has no tag or is 'Untagged'. Using 'Default'. Ensure enemy GameObjects have appropriate tags.");
            enemyTag = "Default";
        }

        Quaternion splatterRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));

        BloodSplatterManager.Instance.CreateSplatter(splatterPosition, splatterRotation, SplatterType.Death, enemyTag);
    }

    // Example of how you might call OnEnemyDeath from another script (e.g., enemy health script)
    /*
    private void OnDisable()
    {
        // This is just an example. You should call OnEnemyDeath explicitly when the enemy's health reaches zero.
        OnEnemyDeath();
    }
    */
}

