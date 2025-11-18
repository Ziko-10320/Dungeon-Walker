using UnityEngine;
using Photon.Pun;
using System.Collections; 

public class CameraTargetSetter : MonoBehaviour
{
    // A reference to the camera follow script that is on the same object.
    private CameraFollowMouseHorizontal cameraFollowScript;

    void Awake()
    {
        // Get the follow script that is attached to this same camera object.
        cameraFollowScript = GetComponent<CameraFollowMouseHorizontal>();
        if (cameraFollowScript == null)
        {
            Debug.LogError("CameraTargetSetter: Could not find the 'CameraFollowMouseHorizontal' script on this camera!", this);
            enabled = false; // Disable this script if the follow script is missing.
            return;
        }
    }

    void Start()
    {
        // We use a small delay to make sure the player has been spawned by Photon.
        // This is more reliable than trying to find it immediately in Start().
        StartCoroutine(FindAndSetTarget());
    }

    private IEnumerator FindAndSetTarget()
    {
        // Wait for a very short moment to ensure Photon has instantiated the player.
        yield return new WaitForSeconds(0.1f);

        GameObject localPlayer = null;

        // --- THE TARGETING LOGIC ---
        // Find all potential player objects in the scene.
        GameObject[] players = GameObject.FindGameObjectsWithTag("OnlinePlayer");

        // Loop through them to find the one that is OURS.
        foreach (GameObject player in players)
        {
            PhotonView view = player.GetComponent<PhotonView>();
            if (view != null && view.IsMine)
            {
                // This is our local player!
                localPlayer = player;
                break; // We found it, no need to keep searching.
            }
        }

        // If we are in single-player, the "OnlinePlayer" search will fail.
        // So, we fall back to finding the "Player" tag.
        if (localPlayer == null)
        {
            localPlayer = GameObject.FindGameObjectWithTag("Player");
        }
        // --- END OF LOGIC ---


        // --- SET THE TARGET ---
        if (localPlayer != null)
        {
            // We found our player! Now, find the specific "CM" target within it.
            Transform targetCM = FindChildWithTag(localPlayer.transform, "CM");

            if (targetCM != null)
            {
                // We found the "CM" object. Tell the camera to follow it.
                cameraFollowScript.SetTarget(targetCM);
            }
            else
            {
                // If there's no "CM" object, we'll just follow the main player object as a fallback.
                Debug.LogWarning("Could not find a child object with the tag 'CM'. The camera will follow the main player object instead.", localPlayer);
                cameraFollowScript.SetTarget(localPlayer.transform);
            }
        }
        else
        {
            Debug.LogError("CameraTargetSetter: Could not find any local player with tag 'OnlinePlayer' or 'Player' after waiting.", this);
        }
    }

    // A helper function to find a child object by its tag.
    private Transform FindChildWithTag(Transform parent, string tag)
    {
        foreach (Transform child in parent)
        {
            if (child.CompareTag(tag))
            {
                return child;
            }
        }
        return null; // Return null if no child with the tag is found.
    }
}
