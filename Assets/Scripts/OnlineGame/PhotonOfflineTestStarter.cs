using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonOfflineTestStarter : MonoBehaviourPunCallbacks
{
    public string testRoomName = "OFFLINE_TEST_ROOM";

    [Header("Test Hand Setup")]
    public bool overrideStartingHand = false;
    public int startingHandSize = 7;
    public System.Collections.Generic.List<UnoCard> specificStartingCards;

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        PhotonUnoGameManager gameManager = FindObjectOfType<PhotonUnoGameManager>();
        if (gameManager != null)
        {
            gameManager.overrideStartingHand = overrideStartingHand;
            gameManager.overrideHandSize = startingHandSize;
            gameManager.specificStartingCards = specificStartingCards;
        }

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