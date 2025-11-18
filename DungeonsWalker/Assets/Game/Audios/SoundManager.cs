using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Tooltip("How many sound players to create at the start.")]
    [SerializeField] private int poolSize = 20;

    private List<AudioSource> soundPlayerPool;
    private int poolIndex = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create the pool of AudioSources
        soundPlayerPool = new List<AudioSource>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject soundPlayerObject = new GameObject("SoundPlayer_" + i);
            soundPlayerObject.transform.SetParent(this.transform);
            AudioSource audioSource = soundPlayerObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // Make it a 3D sound by default
            soundPlayerPool.Add(audioSource);
        }
    }

    // This is the new, fast way to play a sound.
    public void PlaySound(AudioClip clip, Vector3 position, float volume = 1.0f)
    {
        if (clip == null) return;

        // Get the next available AudioSource from the pool.
        poolIndex = (poolIndex + 1) % poolSize;
        AudioSource sourceToUse = soundPlayerPool[poolIndex];

        // Position it and play the sound.
        sourceToUse.transform.position = position;
        sourceToUse.clip = clip;
        sourceToUse.volume = volume;
        sourceToUse.Play();
    }
}
