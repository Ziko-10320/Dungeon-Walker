using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Lobby UI")]
    public TMP_InputField roomNameInput;
    public Button createRoomButton;
    public GameObject LobbyPanel;
    public GameObject RoomPanel;

    [Header("Room List UI")]
    public GameObject roomListItemPrefab;
    public Transform roomListContent;

    [Header("Room UI")]
    public Button leaveRoomButton;
    public TMP_Text playerListText; // This is the one that's not working
    public Button startGameButton;

    void Start()
    {
        PhotonNetwork.KeepAliveInBackground = 120f;
        LobbyPanel.SetActive(false);
        RoomPanel.SetActive(false);

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinLobby();
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        LobbyPanel.SetActive(true);
        RoomPanel.SetActive(false);
        // We need to set a default nickname here if it's not set
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "Player" + Random.Range(100, 1000);
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (Transform item in roomListContent)
        {
            Destroy(item.gameObject);
        }
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList || !room.IsVisible || room.PlayerCount == 0) continue;
            GameObject newRoomItem = Instantiate(roomListItemPrefab, roomListContent);
            newRoomItem.GetComponentInChildren<TMP_Text>().text = room.Name;
            newRoomItem.GetComponent<Button>().onClick.AddListener(() => { PhotonNetwork.JoinRoom(room.Name); });
        }
    }

    public void OnCreateRoomButtonClicked()
    {
        LobbyPanel.SetActive(false);
        string roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName)) roomName = "Room " + Random.Range(1000, 10000);
        RoomOptions roomOptions = new RoomOptions() { MaxPlayers = 2 };
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public void OnLeaveRoomButtonClicked()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        LobbyPanel.SetActive(true);
        RoomPanel.SetActive(false);
    }

    // --- THIS IS A CRITICAL AREA ---
    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom() called for local player.");
        RoomPanel.SetActive(true);
        LobbyPanel.SetActive(false);

        // Immediately update the list to show who is here now.
        UpdatePlayerList();
        CheckIfHostAndShowStartButton();
    }

    // This is called when ANOTHER player enters the room you are already in.
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log(newPlayer.NickName + " entered the room.");
        // Update the list to include the new player.
        UpdatePlayerList();
    }

    // This is called when ANOTHER player leaves the room.
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log(otherPlayer.NickName + " left the room.");
        // Update the list to remove the player who left.
        UpdatePlayerList();
        CheckIfHostAndShowStartButton(); // Re-check host status
    }
    // -----------------------------

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Create Room Failed: " + message);
        LobbyPanel.SetActive(true);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Join Room Failed: " + message);
        LobbyPanel.SetActive(true);
    }

    // --- THIS IS THE FUNCTION THAT BUILDS THE LIST ---
    void UpdatePlayerList()
    {
        // 1. Check if the text object is assigned.
        if (playerListText == null)
        {
            Debug.LogError("PlayerListText is not assigned in the NetworkManager Inspector!");
            return;
        }

        Debug.Log("Updating player list...");
        // 2. Clear the previous list.
        playerListText.text = "";

        // 3. Loop through Photon's official player list for the current room.
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            // 4. Add each player's name to the text field on a new line.
            playerListText.text += player.NickName + "\n";
        }
        Debug.Log("Player list updated. Content: " + playerListText.text);
    }

    void CheckIfHostAndShowStartButton()
    {
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        }
    }

    public void OnStartGameButtonClicked()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.LoadLevel("Co-op");
    }
}
