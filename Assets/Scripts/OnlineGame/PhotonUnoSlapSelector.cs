using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;

public class PhotonUnoSlapSelector : MonoBehaviour
{
    private PhotonUnoGameManager gameManager;
    public Transform buttonParent;
    public GameObject opponentButtonPrefab;

    private void Awake()
    {
        if (buttonParent == null)
        {
            buttonParent = transform;
        }
    }

    private void OnEnable()
    {
        gameManager = FindObjectOfType<PhotonUnoGameManager>();
        RefreshOpponentList();
    }

    private void RefreshOpponentList()
    {
        // Clear existing buttons
        for (int i = buttonParent.childCount - 1; i >= 0; i--)
        {
            Destroy(buttonParent.GetChild(i).gameObject);
        }

        if (gameManager == null) return;

        GameStatePacket state = gameManager.GetLastState();
        if (state == null) return;

        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;

        // Create button for each opponent
        foreach (var hand in state.hands)
        {
            if (hand.actorNumber == localActor) continue; // Skip self

            GameObject btnObj = Instantiate(opponentButtonPrefab != null ? opponentButtonPrefab : new GameObject(), buttonParent);
            btnObj.name = "OpponentButton_" + hand.actorNumber;

            Button btn = btnObj.GetComponent<Button>();
            if (btn == null) btn = btnObj.AddComponent<Button>();

            TMP_Text text = btnObj.GetComponentInChildren<TMP_Text>();
            if (text == null)
            {
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(btnObj.transform);
                text = textObj.AddComponent<TextMeshProUGUI>();
                text.alignment = TextAlignmentOptions.Center;
            }

            text.text = "Player " + hand.actorNumber + " (" + hand.cards.Count + " cards)";

            int targetActor = hand.actorNumber;
            btn.onClick.AddListener(() => OnOpponentSelected(targetActor));
        }
    }

    private void OnOpponentSelected(int targetActor)
    {
        if (gameManager != null)
        {
            gameManager.RequestSlap(targetActor);
            gameObject.SetActive(false);
        }
    }
}
