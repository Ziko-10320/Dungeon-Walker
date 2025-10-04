using UnityEngine;

// This script's ONLY job is to ensure the particle plays when enabled.
// The Particle System's "Stop Action: Disable" will handle turning it off.
public class PoolableParticle : MonoBehaviour
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // OnEnable is called every time the object is activated from the pool.
    void OnEnable()
    {
        if (ps != null)
        {
            // Just play the effect. The system itself will disable it.
            ps.Play();
        }
    }
}
