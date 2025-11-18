using UnityEngine;
using System.Collections;

public class BlueBallExplosion : MonoBehaviour
{
    [Header("Blue Ball Explosion Effects")]
    [Tooltip("Blue ball main explosion particle system prefab")]
    [SerializeField] private GameObject blueExplosionPrefab;
    [Tooltip("Blue ball additional explosion particle system 1 prefab")]
    [SerializeField] private GameObject blueExplosionPrefab2;
    [Tooltip("Blue ball additional explosion particle system 2 prefab")]
    [SerializeField] private GameObject blueExplosionPrefab3;

    [Tooltip("Scale of the main explosion")]
    public float explosionScale = 1f;
    [Tooltip("Scale of additional explosions")]
    public float additionalExplosionsScale = 0.8f;
    [Tooltip("Delay between additional explosions")]
    public float explosionDelay = 0.1f;
    [Tooltip("Random offset for additional explosions")]
    public float explosionRandomOffset = 0.5f;
    [Tooltip("Show explosion debug info")]
    public bool showExplosionDebug = false;

    public void PlayExplosion(Vector2 explosionPosition)
    {
        if (showExplosionDebug)
        {
            Debug.Log($"BlueBallExplosion: Playing explosion at {explosionPosition}");
        }

        // Create main explosion
        if (blueExplosionPrefab != null)
        {
            CreateSingleExplosion(blueExplosionPrefab, explosionPosition, explosionScale, 0f);
        }
        else if (showExplosionDebug)
        {
            Debug.LogWarning("BlueBallExplosion: Main explosion prefab is null.");
        }

        // Create additional explosions with delays
        if (blueExplosionPrefab2 != null)
        {
            StartCoroutine(CreateDelayedExplosion(blueExplosionPrefab2, explosionPosition, additionalExplosionsScale, explosionDelay));
        }
        else if (showExplosionDebug)
        {
            Debug.LogWarning("BlueBallExplosion: Additional explosion prefab 1 is null.");
        }

        if (blueExplosionPrefab3 != null)
        {
            StartCoroutine(CreateDelayedExplosion(blueExplosionPrefab3, explosionPosition, additionalExplosionsScale, explosionDelay * 2f));
        }
        else if (showExplosionDebug)
        {
            Debug.LogWarning("BlueBallExplosion: Additional explosion prefab 2 is null.");
        }
    }

    private void CreateSingleExplosion(GameObject explosionPrefab, Vector2 position, float scale, float delay)
    {
        if (explosionPrefab == null)
        {
            Debug.LogError("BlueBallExplosion: explosionPrefab is null in CreateSingleExplosion!");
            return;
        }

        Vector2 finalPosition = position;
        if (delay > 0f && explosionRandomOffset > 0f)
        {
            Vector2 randomOffset = Random.insideUnitCircle * explosionRandomOffset;
            finalPosition += randomOffset;
        }

        GameObject explosionInstance = Instantiate(explosionPrefab, finalPosition, Quaternion.identity);
        explosionInstance.name = explosionPrefab.name + "_PS_Instance";

        ParticleSystem particles = explosionInstance.GetComponent<ParticleSystem>();
        if (particles != null)
        {
            if (!particles.isPlaying)
            {
                particles.Play();
            }
            Destroy(explosionInstance, particles.main.duration + particles.main.startLifetime.constantMax + 0.1f);
        }
        else
        {
            Debug.LogWarning($"BlueBallExplosion: No ParticleSystem found on {explosionInstance.name}. Destroying after default duration.");
            Destroy(explosionInstance, 3f);
        }
    }

    private IEnumerator CreateDelayedExplosion(GameObject explosionPrefab, Vector2 position, float scale, float delay)
    {
        yield return new WaitForSeconds(delay);
        CreateSingleExplosion(explosionPrefab, position, scale, delay);
    }
}