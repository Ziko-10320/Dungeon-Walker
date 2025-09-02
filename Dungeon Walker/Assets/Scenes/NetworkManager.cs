using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic; // --- NEW --- Required for using Lists

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Lobby UI")]
    public TMP_InputField roomNameInput;
    public Button createRoomButton;
    public GameObject LobbyPanel;
    public GameObject RoomPanel;

    // --- NEW: Variables for the Room List ---
    [Header("Room List UI")]
    public GameObject roomListItemPrefab; // We will create this prefab in Unity
    public Transform roomListContent;     // The parent object for the list items
    // -----------------------------------------

    [Header("Room UI")]
    public Button leaveRoomButton;

    void Start()
    {
        Debug.Log("Connecting to Master Server...");
        PhotonNetwork.ConnectUsingSettings();

        LobbyPanel.SetActive(false);
        RoomPanel.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Successfully Connected to Master Server!");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Successfully Joined Lobby!");
        LobbyPanel.SetActive(true);
        RoomPanel.SetActive(false);
    }

    // --- NEW: Callback for when the list of rooms from Photon is updated ---
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("Room list has been updated.");

        // 1. Clear the old list of rooms
        foreach (Transform item in roomListContent)
        {
            Destroy(item.gameObject);
        }

        // 2. Create a new UI item for each room in the new list
        foreach (RoomInfo room in roomList)
        {
            // Don't show rooms that are full or have been removed
            if (room.RemovedFromList || !room.IsVisible || room.PlayerCount == 0)
            {
                continue;
            }

            GameObject newRoomItem = Instantiate(roomListItemPrefab, roomListContent);

            // Get the Text and Button components from the prefab
            TMP_Text roomNameText = newRoomItem.GetComponentInChildren<TMP_Text>();
            Button joinRoomButton = newRoomItem.GetComponent<Button>();

            // Set the room name on the text component
            roomNameText.text = room.Name;

            // Add a listener to the button so it joins the correct room when clicked
            joinRoomButton.onClick.AddListener(() =>
            {
                PhotonNetwork.JoinRoom(room.Name);
            });
        }
    }
    // ---------------------------------------------------------------------

    public void OnCreateRoomButtonClicked()
    {
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

    public void OnLeaveRoomButtonClicked()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left the room.");
        LobbyPanel.SetActive(true);
        RoomPanel.SetActive(false);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Successfully joined room: " + PhotonNetwork.CurrentRoom.Name);
        RoomPanel.SetActive(true);
        LobbyPanel.SetActive(false);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Create Room Failed: " + message);
        LobbyPanel.SetActive(true);
    }

    // --- NEW: Callback for when joining a room fails ---
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Join Room Failed: " + message);
        // If joining fails, re-enable the lobby so the user can try again
        LobbyPanel.SetActive(true);
    }
    // ----------------------------------------------------
}