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
    public TMP_Text playerListText;
    public Button startGameButton;

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
        PhotonNetwork.NickName = "Player" + Random.Range(100, 1000);

        // --- THIS IS THE FIX ---
        // This line tells Photon that all clients in a room should automatically
        // load the scene that the Master Client (host) loads.
        PhotonNetwork.AutomaticallySyncScene = true;
        // -----------------------

        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Successfully Joined Lobby!");
        LobbyPanel.SetActive(true);
        RoomPanel.SetActive(false);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (Transform item in roomListContent)
        {
            Destroy(item.gameObject);
        }

        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList || !room.IsVisible || room.PlayerCount == 0)
            {
                continue;
            }

            GameObject newRoomItem = Instantiate(roomListItemPrefab, roomListContent);
            TMP_Text roomNameText = newRoomItem.GetComponentInChildren<TMP_Text>();
            Button joinRoomButton = newRoomItem.GetComponent<Button>();
            roomNameText.text = room.Name;
            joinRoomButton.onClick.AddListener(() => { PhotonNetwork.JoinRoom(room.Name); });
        }
    }

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

        UpdatePlayerList();
        CheckIfHostAndShowStartButton();
    }

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

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log(newPlayer.NickName + " joined the room.");
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log(otherPlayer.NickName + " left the room.");
        UpdatePlayerList();
        CheckIfHostAndShowStartButton();
    }

    void UpdatePlayerList()
    {
        playerListText.text = "Players in Room:\n";
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            playerListText.text += player.NickName + "\n";
        }
    }

    void CheckIfHostAndShowStartButton()
    {
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }

    public void OnStartGameButtonClicked()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.LoadLevel("SampleScene");
    }
}
