using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunAudioStateBehavior : StateMachineBehaviour
{
    private AudioSource audioSource; // Will reference the AudioSource on the player

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Get the AudioSource component from the same GameObject as the Animator
        if (audioSource == null) // Only get it once
        {
            audioSource = animator.GetComponent<AudioSource>();
        }

        // If we found an AudioSource and it has a clip assigned for "Run", play it
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Stop the audio when the animation state exits
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}