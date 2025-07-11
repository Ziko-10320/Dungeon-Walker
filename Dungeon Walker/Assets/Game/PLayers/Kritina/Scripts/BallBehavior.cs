using UnityEngine;
using System.Collections;

public class ParticleSystemLifecycles : MonoBehaviour
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps == null)
        {
            Debug.LogWarning($"ParticleSystemLifecycle: No ParticleSystem found on {gameObject.name}. Destroying this component.");
            Destroy(this); // Destroy this component if no ParticleSystem is found
            return;
        }

        // Ensure Play On Awake is false for the prefab, as we control playing here
        // If it's true, it will play automatically, but we want to explicitly play it.
        // If it's already playing from Play On Awake, this won't hurt.
        if (!ps.isPlaying)
        {
            ps.Play();
        }

        // Destroy the GameObject after the particle system has finished playing
        // We add a small buffer to ensure all particles have faded out.
        float totalDuration = ps.main.duration + ps.main.startLifetime.constantMax + 0.1f;
        Destroy(gameObject, totalDuration);
    }
}

