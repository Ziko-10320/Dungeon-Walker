using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio; // Required for checking mixer groups

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    // The sound clip to play. Assign this in the Inspector.
    public AudioClip clickSound;

    private Button button;

    // A static reference to the AudioSource for UI sounds.
    // This is shared by all buttons to be efficient.
    private static AudioSource uiAudioSource;

    void Start()
    {
        // Find the correct AudioSource only once if it hasn't been found yet.
        if (uiAudioSource == null)
        {
            // Find all AudioSource components in the scene.
            AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
            foreach (AudioSource source in allAudioSources)
            {
                // The correct AudioSource is the one NOT connected to a mixer group.
                if (source.outputAudioMixerGroup == null)
                {
                    uiAudioSource = source;
                    break; // Stop searching once we've found it.
                }
            }
        }

        button = GetComponent<Button>();
        button.onClick.AddListener(PlaySound);
    }

    void PlaySound()
    {
        // Check if we have a sound clip and a valid AudioSource.
        if (clickSound != null && uiAudioSource != null)
        {
            // Play the sound through our dedicated UI AudioSource.
            uiAudioSource.PlayOneShot(clickSound);
        }
        else
        {
            if (clickSound == null)
            {
                Debug.LogWarning("Button click sound not assigned on: " + gameObject.name);
            }
            if (uiAudioSource == null)
            {
                // This error means it couldn't find an AudioSource with an empty 'Output' field.
                Debug.LogError("No AudioSource found for UI sounds. Please add an AudioSource component and ensure its 'Output' field is set to 'None'.");
            }
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlaySound);
        }
    }
}
