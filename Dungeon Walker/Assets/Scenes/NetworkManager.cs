using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Lobby UI")]
    public TMP_InputField roomNameInput;
    public Button createRoomButton;
    public GameObject LobbyPanel; // <-- Add this
    public GameObject RoomPanel;  // <-- Add this

    [Header("Room UI")] // <-- Add this for organization
    public Button leaveRoomButton; // <-- Add this

    void Start()
    {
        Debug.Log("Connecting to Master Server...");
        PhotonNetwork.ConnectUsingSettings();

        // Start with both panels off. We'll turn them on when ready.
        LobbyPanel.SetActive(false);
        RoomPanel.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Successfully Connected to Master Server!");
        PhotonNetwork.JoinLobby(); // <-- IMPORTANT: We must join a lobby to get room lists later.
    }

    // This is a new callback, called when we join the lobby.
    public override void OnJoinedLobby()
    {
        Debug.Log("Successfully Joined Lobby!");
        LobbyPanel.SetActive(true); // Show the lobby panel
        RoomPanel.SetActive(false); // Hide the room panel
    }

    public void OnCreateRoomButtonClicked()
    {
        // When we click create, disable the UI so we can't click it again.
        LobbyPanel.SetActive(false);

        string roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName))
        {
            roomName = "Room " + Random.Range(1000, 10000);
        }
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    // --- ADD THIS NEW FUNCTION ---
    public void OnLeaveRoomButtonClicked()
    {
        PhotonNetwork.LeaveRoom(); // This tells Photon we want to leave our current room.
    }

    // This is a new callback, called when we successfully leave a room.
    public override void OnLeftRoom()
    {
        Debug.Log("Left the room.");
        // When we leave, we go back to the lobby view.
        LobbyPanel.SetActive(true);
        RoomPanel.SetActive(false);
    }
    // -----------------------------

    public override void OnJoinedRoom()
    {
        Debug.Log("Successfully joined room: " + PhotonNetwork.CurrentRoom.Name);
        RoomPanel.SetActive(true); // Show the room panel
        LobbyPanel.SetActive(false); // Hide the lobby panel

        // We will move the scene loading logic later, maybe to a "Start Game" button.
        // For now, let's comment it out so we can test the panels.
        // PhotonNetwork.LoadLevel("SampleScene"); 
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Create Room Failed: " + message);
        // If we fail, re-enable the lobby panel so the user can try again.
        LobbyPanel.SetActive(true);
    }
}
