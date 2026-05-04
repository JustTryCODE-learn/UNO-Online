using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class HostRoomManager : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public TMP_InputField roomCodeInput;
    public TMP_Text statusText;

    private void Start()
    {
        statusText.text = "Connecting to Photon...";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        statusText.text = "Connected. Ready to host or join.";
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        statusText.text = "Ready. Click Host Room or enter code to Join Room.";
    }

    public void HostRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            statusText.text = "Photon is not ready yet.";
            return;
        }

        string randomCode = GenerateRoomCode();

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 4,
            IsOpen = true,
            IsVisible = false
        };

        roomCodeInput.text = randomCode;
        statusText.text = "Creating room with code: " + randomCode;

        PhotonNetwork.CreateRoom(randomCode, options);
    }

    public void JoinRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            statusText.text = "Photon is not ready yet.";
            return;
        }

        string code = roomCodeInput.text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            statusText.text = "Please type the room code.";
            return;
        }

        statusText.text = "Joining room: " + code;
        PhotonNetwork.JoinRoom(code);
    }

    private string GenerateRoomCode()
    {
        int number = Random.Range(1000, 9999);
        return "UNO" + number;
    }

    public override void OnJoinedRoom()
    {
        SceneManager.LoadScene("Room");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        statusText.text = "Join failed. Check the room code.";
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        statusText.text = "Create room failed. Try again.";
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        statusText.text = "Disconnected: " + cause;
    }
}