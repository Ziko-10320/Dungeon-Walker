using UnityEngine;

public class BloodParticleCollisionHandler : MonoBehaviour
{
    public SplatterType splatterType;
    private int collisionCount = 0;
    public int splatterSpawnRate = 4; // Spawn one splatter for every X collisions

    void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            ParticleSystem ps = GetComponent<ParticleSystem>();
            if (ps == null) return;

            ParticleCollisionEvent[] collisionEvents = new ParticleCollisionEvent[ps.main.maxParticles];
            int numCollisionEvents = ps.GetCollisionEvents(other, collisionEvents);

            string enemyTag = transform.root.tag;
            if (string.IsNullOrEmpty(enemyTag) || enemyTag == "Untagged")
            {
                enemyTag = transform.parent?.tag;
            }
            if (string.IsNullOrEmpty(enemyTag) || enemyTag == "Untagged")
            {
                enemyTag = "Default";
                Debug.LogWarning("Could not determine enemy tag for blood splatter. Using 'Default'. Ensure enemy GameObjects have appropriate tags.");
            }

            for (int i = 0; i < numCollisionEvents; i++)
            {
                collisionCount++;
                if (collisionCount % splatterSpawnRate == 0) // Only create splatter every 'splatterSpawnRate' collisions
                {
                    Vector2 collisionPoint = collisionEvents[i].intersection;
                    Quaternion splatterRotation = Quaternion.identity;

                    if (BloodSplatterManager.Instance != null)
                    {
                        BloodSplatterManager.Instance.CreateSplatter(collisionPoint, splatterRotation, splatterType, enemyTag);
                    }
                    else
                    {
                        Debug.LogWarning("BloodSplatterManager.Instance is null. Make sure it's initialized.");
                    }
                }
            }
        }
    }
}

