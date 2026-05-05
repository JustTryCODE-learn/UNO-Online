using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public TMP_Text roomCodeText;
    public TMP_Text playerCountText;
    public TMP_Text playerListText;

    public GameObject playButton;
    public GameObject readyButton;

    private bool localReady = false;

    private const string READY_KEY = "Ready";

    private void Awake()
    {
        // This ensures that when the MasterClient calls PhotonNetwork.LoadLevel(),
        // all other players in the room load the same level automatically.
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        // Host does not need to ready.
        // Non-host starts as not ready.
        if (!PhotonNetwork.IsMasterClient)
        {
            SetLocalReady(false);
        }

        UpdateRoomUI();
        UpdateButtons();
    }

    private void SetLocalReady(bool ready)
    {
        localReady = ready;

        Hashtable props = new Hashtable
        {
            { READY_KEY, ready }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void ReadyButtonClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            return;
        }

        localReady = !localReady;
        SetLocalReady(localReady);

        UpdateRoomUI();
        UpdateButtons();
    }

    public void PlayButtonClicked()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            playerCountText.text = "Need at least 2 players to start.";
            return;
        }

        if (!AreAllNonHostPlayersReady())
        {
            playerCountText.text = "Waiting for all players to be ready.";
            return;
        }

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        PhotonNetwork.LoadLevel("Game");
    }

    private bool AreAllNonHostPlayersReady()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.IsMasterClient)
            {
                continue;
            }

            if (!player.CustomProperties.ContainsKey(READY_KEY))
            {
                return false;
            }

            bool isReady = (bool)player.CustomProperties[READY_KEY];

            if (!isReady)
            {
                return false;
            }
        }

        return true;
    }

    private void UpdateButtons()
    {
        bool isHost = PhotonNetwork.IsMasterClient;

        if (playButton != null)
        {
            playButton.SetActive(isHost);
        }

        if (readyButton != null)
        {
            readyButton.SetActive(!isHost);
        }

        TMP_Text readyButtonText = readyButton.GetComponentInChildren<TMP_Text>();

        if (readyButtonText != null)
        {
            readyButtonText.text = localReady ? "Unready" : "Ready";
        }

        TMP_Text playButtonText = playButton.GetComponentInChildren<TMP_Text>();

        if (playButtonText != null)
        {
            playButtonText.text = "Play";
        }
    }

    private void UpdateRoomUI()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            roomCodeText.text = "No room";
            playerCountText.text = "Players: 0/4";
            playerListText.text = "";
            return;
        }

        roomCodeText.text = "Room Code: " + PhotonNetwork.CurrentRoom.Name;

        playerCountText.text =
            "Players: " +
            PhotonNetwork.CurrentRoom.PlayerCount +
            "/" +
            PhotonNetwork.CurrentRoom.MaxPlayers;

        string list = "";

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            string playerName = string.IsNullOrEmpty(player.NickName)
                ? "Player " + player.ActorNumber
                : player.NickName;

            if (player.IsMasterClient)
            {
                list += playerName + "  |  Host\n";
            }
            else
            {
                bool isReady = false;

                if (player.CustomProperties.ContainsKey(READY_KEY))
                {
                    isReady = (bool)player.CustomProperties[READY_KEY];
                }

                string readyText = isReady ? "Ready" : "Not Ready";

                list += playerName + "  |  " + readyText + "\n";
            }
        }

        playerListText.text = list;
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateRoomUI();
        UpdateButtons();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateRoomUI();
        UpdateButtons();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        UpdateRoomUI();
        UpdateButtons();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            SetLocalReady(false);
        }

        UpdateRoomUI();
        UpdateButtons();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Menu");
    }
}