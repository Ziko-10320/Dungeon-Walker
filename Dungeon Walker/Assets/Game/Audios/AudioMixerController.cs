using UnityEngine;
using UnityEngine.Audio;

public class AudioMixerController : MonoBehaviour
{
    // This field will let us link our Audio Mixer in the Unity Editor
    public AudioMixer mainMixer;

    // This function will handle the transition to the MUFFLED state
    public void TransitionToMuffled(float transitionTime)
    {
        mainMixer.FindSnapshot("Muffled").TransitionTo(transitionTime);
    }

    // This function will handle the transition back to the NORMAL state
    public void TransitionToNormal(float transitionTime)
    {
        mainMixer.FindSnapshot("Normal").TransitionTo(transitionTime);
    }
}
