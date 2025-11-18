using UnityEngine;

public class DeclineReviveHandler : MonoBehaviour
{
    // We will drag both of our player health scripts here in the Inspector.
    [SerializeField] private PlayerHealth kritinaHealth;
    [SerializeField] private L3antixHealth l3antixHealth;

    // This is the public method that the "Give Up" button will call.
    public void HandleDecline()
    {
        Debug.Log("[DeclineReviveHandler] 'Give Up' button clicked. Finding active player...");

        // Check which player is currently active in the game.
        if (kritinaHealth != null && kritinaHealth.gameObject.activeInHierarchy)
        {
            // If Kritina is the active player, ONLY tell her to decline.
            Debug.Log("Kritina is active. Calling her DeclineRevive method.");
            kritinaHealth.DeclineRevive();
        }
        else if (l3antixHealth != null && l3antixHealth.gameObject.activeInHierarchy)
        {
            // If L3antix is the active player, ONLY tell him to decline.
            Debug.Log("L3antix is active. Calling his DeclineRevive method.");
            l3antixHealth.DeclineRevive();
        }
        else
        {
            Debug.LogWarning("[DeclineReviveHandler] Could not find any active player to decline revive for.");
        }
    }
}
