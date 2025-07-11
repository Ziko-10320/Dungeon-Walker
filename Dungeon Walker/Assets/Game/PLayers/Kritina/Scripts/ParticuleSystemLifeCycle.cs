using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ParticleSystemManager : MonoBehaviour
{
    public static ParticleSystemManager Instance { get; private set; }

    [Header("Green Ball Particle Systems")]
    public ParticleSystem greenExplosionPS;
    public ParticleSystem greenExplosionPS2;
    public ParticleSystem greenExplosionPS3;

    [Header("Orange Ball Particle Systems")]
    public ParticleSystem orangeExplosionPS;
    public ParticleSystem orangeExplosionPS2;
    public ParticleSystem orangeExplosionPS3;

    [Header("Blue Ball Particle Systems")]
    public ParticleSystem blueExplosionPS;
    public ParticleSystem blueExplosionPS2;
    public ParticleSystem blueExplosionPS3;

    [Header("General Particle Settings")]
    public float explosionScale = 1f; // Not directly used for PS scale, but for reference
    public float additionalExplosionsScale = 0.8f; // Not directly used for PS scale, but for reference
    public float explosionDelay = 0.1f;
    public float explosionRandomOffset = 0.5f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void PlayExplosionEffects(Vector2 explosionPosition, string ballType)
    {
        ParticleSystem mainPS = null;
        ParticleSystem additionalPS1 = null;
        ParticleSystem additionalPS2 = null;

        switch (ballType)
        {
            case "GreenBall":
                mainPS = greenExplosionPS;
                additionalPS1 = greenExplosionPS2;
                additionalPS2 = greenExplosionPS3;
                break;
            case "OrangeBall":
                mainPS = orangeExplosionPS;
                additionalPS1 = orangeExplosionPS2;
                additionalPS2 = orangeExplosionPS3;
                break;
            case "BlueBall":
                mainPS = blueExplosionPS;
                additionalPS1 = blueExplosionPS2;
                additionalPS2 = blueExplosionPS3;
                break;
            default:
                Debug.LogWarning($"Unknown ball type: {ballType}. No specific particle systems found.");
                break;
        }

        // Play main explosion particle system
        if (mainPS != null)
        {
            PlayParticleSystem(mainPS, explosionPosition, explosionScale, 0f);
        }

        // Play additional particle systems with delays
        if (additionalPS1 != null)
        {
            StartCoroutine(PlayDelayedParticleSystem(additionalPS1, explosionPosition, additionalExplosionsScale, explosionDelay));
        }

        if (additionalPS2 != null)
        {
            StartCoroutine(PlayDelayedParticleSystem(additionalPS2, explosionPosition, additionalExplosionsScale, explosionDelay * 2f));
        }
    }

    private void PlayParticleSystem(ParticleSystem ps, Vector2 position, float scale, float delay)
    {
        if (ps == null)
        {
            Debug.LogError("ParticleSystem is null in PlayParticleSystem!");
            return;
        }

        // Set position of the particle system to the explosion point
        ps.transform.position = position;

        // Add random offset for additional explosions
        if (explosionRandomOffset > 0f)
        {
            Vector2 randomOffset = Random.insideUnitCircle * explosionRandomOffset;
            ps.transform.position += (Vector3)randomOffset;
        }

        // Ensure the particle system is stopped and cleared before playing to reset it
        if (ps.isPlaying)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        ps.Play();
        Debug.Log($"Playing particle system: {ps.name} at {ps.transform.position}");
    }

    private IEnumerator PlayDelayedParticleSystem(ParticleSystem ps, Vector2 position, float scale, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayParticleSystem(ps, position, scale, delay);
    }
}
