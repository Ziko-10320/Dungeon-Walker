using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro; // Use TextMeshPro for the status text

public class CharacterSelectManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    public Button character1Button;
    public Button character2Button;
    public GameObject chooseButton;
    public TMP_Text connectionStatusText; // The text to show connection status

    [Header("Character Prefabs")]
    public GameObject character1Prefab;
    public GameObject character2Prefab;

    [Header("Scene to Load")]
    public string lobbySceneName = "OnlineLobbyScene";

    private string selectedPrefabName;

    void Start()
    {
        // This line prevents the game from pausing when the window loses focus.
        PhotonNetwork.KeepAliveInBackground = 120f;

        // --- Keep everything disabled at the start ---
        character1Button.interactable = false;
        character2Button.interactable = false;
        chooseButton.SetActive(false);
        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Connecting to Server...";
        }

        // --- Connect to Photon ---
        Debug.Log("Connecting to Photon...");
        PhotonNetwork.ConnectUsingSettings();
    }

    // --- This function is called by Photon ONLY when the connection is successful ---
    public override void OnConnectedToMaster()
    {
        Debug.Log("SUCCESS! Connected to Master Server.");
        // Now that we are connected, enable the character buttons.
        character1Button.interactable = true;
        character2Button.interactable = true;
        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Select Your Character!";
        }
    }

    // This function is called if the connection fails.
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError("Failed to connect. Reason: " + cause);
        if (connectionStatusText != null)
        {
            connectionStatusText.text = "Connection Failed. Please restart.";
        }
    }

    // --- The rest of the script handles the button clicks ---

    public void OnSelectCharacter1()
    {
        if (character1Prefab == null) return;
        selectedPrefabName = character1Prefab.name;
        chooseButton.SetActive(true);
    }

    public void OnSelectCharacter2()
    {
        if (character2Prefab == null) return;
        selectedPrefabName = character2Prefab.name;
        chooseButton.SetActive(true);
    }

    public void OnConfirmSelection()
    {
        if (string.IsNullOrEmpty(selectedPrefabName)) return;

        // This is now safe because we know we are connected.
        Hashtable playerProperties = new Hashtable();
        playerProperties.Add("character", selectedPrefabName);
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);

        SceneManager.LoadScene(lobbySceneName);
    }
}
