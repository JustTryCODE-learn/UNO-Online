using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonOfflineTestStarter : MonoBehaviourPunCallbacks
{
    public string testRoomName = "OFFLINE_TEST_ROOM";

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            return;
        }

        if (PhotonNetwork.IsConnectedAndReady)
        {
            CreateTestRoom();
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        CreateTestRoom();
    }

    private void CreateTestRoom()
    {
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 4,
            IsOpen = true,
            IsVisible = false
        };

        PhotonNetwork.CreateRoom(testRoomName, options);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined offline test room as Player " + PhotonNetwork.LocalPlayer.ActorNumber);
    }
}