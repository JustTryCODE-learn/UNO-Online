using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhotonUnoGameManager : MonoBehaviourPunCallbacks, IOnEventCallback, IPunObservable
{
    private const byte REQ_PLAY_CARD = 1;
    private const byte REQ_DRAW_CARD = 2;
    private const byte REQ_END_TURN = 3;
    private const byte REQ_SLAP = 4;
    private const byte REQ_SLAP_EIGHT = 5;
    private const byte STATE_UPDATE = 20;

    [Header("Card Assets - same order on all clients")]
    public List<UnoCard> allCards = new List<UnoCard>();

    [Header("UI")]
    public GameObject cardPrefab;
    public Transform handParent;
    public Image discardImage;
    public TMP_Text statusText;
    public TMP_Text opponentText;
    public TMP_Text directionText;
    public TMP_Text penaltyText;
    public GameObject colorPickerPanel;
    public GameObject endTurnButton;
    public GameObject SlapButton;
    public GameObject slapSelectorPanel;
    public GameObject slap8Button;
    public TMP_Text slap8TimerText;

    private readonly List<int> actorOrder = new List<int>();
    private readonly Dictionary<int, List<NetCard>> hands = new Dictionary<int, List<NetCard>>();
    private readonly List<NetCard> drawPile = new List<NetCard>();
    private readonly List<NetCard> discardPile = new List<NetCard>();

    private int currentPlayerIndex = 0;
    private bool clockwise = true;
    private int activeColor = 0;

    private int pendingPenalty = 0;
    private int previousPenaltyValue = 0;
    private int waitingAfterDrawActor = -1;
    private int waitingForSlapActor = -1;
    private bool rule8Active = false;
    private float rule8Timer = 0f;
    private const float RULE_8_DURATION = 3f;
    private Dictionary<int, float> rule8Clicks = new Dictionary<int, float>();
    private int nextInstanceId = 1;

    private GameStatePacket lastState;

    public GameStatePacket GetLastState()
    {
        return lastState;
    }

    private void Awake()
    {
        if (colorPickerPanel != null) colorPickerPanel.SetActive(false);
        if (endTurnButton != null) endTurnButton.SetActive(false);
        if (slapSelectorPanel != null) slapSelectorPanel.SetActive(false);
        if (slap8Button != null) slap8Button.SetActive(false);
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartHostGame();
        }
        else
        {
            SetStatus("Waiting for host...");
        }
    }

    private void Update()
    {
        // Handle Rule of 8 timer on host
        if (PhotonNetwork.IsMasterClient && rule8Active)
        {
            rule8Timer -= Time.deltaTime;
            if (rule8Timer <= 0)
            {
                rule8Timer = 0;
                EndRule8();
            }
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void StartHostGame()
    {
        actorOrder.Clear();
        hands.Clear();
        drawPile.Clear();
        discardPile.Clear();

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            actorOrder.Add(p.ActorNumber);
            hands[p.ActorNumber] = new List<NetCard>();
        }

        BuildDeck();
        Shuffle(drawPile);

        for (int i = 0; i < 7; i++)
        {
            foreach (int actor in actorOrder)
            {
                hands[actor].Add(DrawFromPile());
            }
        }

        NetCard first = DrawFromPile();
        discardPile.Add(first);

        UnoCard firstData = GetData(first);
        activeColor = (int)firstData.color;

        currentPlayerIndex = 0;
        clockwise = true;
        pendingPenalty = 0;
        previousPenaltyValue = 0;
        waitingAfterDrawActor = -1;
        waitingForSlapActor = -1;
        rule8Active = false;
        rule8Timer = 0f;
        rule8Clicks.Clear();

        BroadcastState("Game started.");
    }

    private void BuildDeck()
    {
        drawPile.Clear();

        for (int i = 0; i < allCards.Count; i++)
        {
            UnoCard card = allCards[i];
            int copies = 2;

            if (card.isWild)
            {
                copies = 4;
            }
            else if (card.number == 0)
            {
                copies = 1;
            }

            for (int c = 0; c < copies; c++)
            {
                drawPile.Add(new NetCard
                {
                    instanceId = nextInstanceId++,
                    cardIndex = i
                });
            }
        }
    }

    private void Shuffle(List<NetCard> cards)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, cards.Count);
            NetCard temp = cards[i];
            cards[i] = cards[r];
            cards[r] = temp;
        }
    }

    private NetCard DrawFromPile()
    {
        if (drawPile.Count == 0)
        {
            RebuildDrawPile();
        }

        if (drawPile.Count == 0)
        {
            Debug.LogError("No cards left to draw.");
            return null;
        }

        NetCard card = drawPile[0];
        drawPile.RemoveAt(0);
        return card;
    }

    private void RebuildDrawPile()
    {
        if (discardPile.Count <= 1) return;

        NetCard top = discardPile[discardPile.Count - 1];
        discardPile.RemoveAt(discardPile.Count - 1);

        drawPile.AddRange(discardPile);
        discardPile.Clear();
        discardPile.Add(top);

        Shuffle(drawPile);
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == STATE_UPDATE)
        {
            string json = (string)photonEvent.CustomData;
            lastState = JsonUtility.FromJson<GameStatePacket>(json);
            RefreshClientUI();
            return;
        }

        if (!PhotonNetwork.IsMasterClient) return;

        if (photonEvent.Code == REQ_PLAY_CARD)
        {
            object[] data = (object[])photonEvent.CustomData;
            int actor = (int)data[0];
            int instanceId = (int)data[1];
            int chosenColor = (int)data[2];
            HostPlayCard(actor, instanceId, chosenColor);
        }
        else if (photonEvent.Code == REQ_DRAW_CARD)
        {
            int actor = (int)photonEvent.CustomData;
            HostDrawCard(actor);
        }
        else if (photonEvent.Code == REQ_END_TURN)
        {
            int actor = (int)photonEvent.CustomData;
            HostEndTurn(actor);
        }
        else if (photonEvent.Code == REQ_SLAP)
        {
            object[] data = (object[])photonEvent.CustomData;
            int actor = (int)data[0];
            int targetActor = (int)data[1];
            HostSlap(actor, targetActor);
        }
        else if (photonEvent.Code == REQ_SLAP_EIGHT)
        {
            int actor = (int)photonEvent.CustomData;
            HostRule8Slap(actor);
        }
    }
    public void RequestPlayCard(int instanceId, int chosenColor)
    {
        object[] data =
        {
            PhotonNetwork.LocalPlayer.ActorNumber,
            instanceId,
            chosenColor
        };

        PhotonNetwork.RaiseEvent(
            REQ_PLAY_CARD,
            data,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
    }

    public void RequestDrawCard()
    {
        PhotonNetwork.RaiseEvent(
            REQ_DRAW_CARD,
            PhotonNetwork.LocalPlayer.ActorNumber,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
    }

    public void RequestEndTurn()
    {
        PhotonNetwork.RaiseEvent(
            REQ_END_TURN,
            PhotonNetwork.LocalPlayer.ActorNumber,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
    }

    public void RequestSlap(int targetActor)
    {
        object[] data =
        {
            PhotonNetwork.LocalPlayer.ActorNumber,
            targetActor
        };

        PhotonNetwork.RaiseEvent(
            REQ_SLAP,
            data,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
    }

    public void RequestSlap8()
    {
        PhotonNetwork.RaiseEvent(
            REQ_SLAP_EIGHT,
            PhotonNetwork.LocalPlayer.ActorNumber,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
    }

    private void HostPlayCard(int actor, int instanceId, int chosenColor)
    {
        if (!IsCurrentActor(actor))
        {
            BroadcastState("Invalid move: not your turn.");
            return;
        }

        if (!hands.ContainsKey(actor)) return;

        NetCard card = FindCardInHand(actor, instanceId);
        if (card == null)
        {
            BroadcastState("Invalid move: card not found.");
            return;
        }

        UnoCard data = GetData(card);

        if (data.isWild && chosenColor < 0)
        {
            BroadcastState("Choose a color first.");
            return;
        }

        if (!CanPlay(actor, card))
        {
            BroadcastState("Invalid move.");
            return;
        }

        hands[actor].Remove(card);
        discardPile.Add(card);

        if (data.isWild)
        {
            activeColor = chosenColor;
        }
        else
        {
            activeColor = (int)data.color;
        }

        waitingAfterDrawActor = -1;

        if (hands[actor].Count == 0)
        {
            BroadcastGameOver(actor);
            return;
        }

        // Check if 7 was played (7-Slap mechanic)
        if (data.value == UnoCard.CardValue.Seven)
        {
            waitingForSlapActor = actor;
            BroadcastState("Player " + actor + " played a 7! Waiting to choose opponent to swap with.");
            return;
        }

        ApplyCardEffect(data);
        BroadcastState("Card played.");
    }

    private void ApplyCardEffect(UnoCard data)
    {
        if (data.value == UnoCard.CardValue.DrawTwo)
        {
            pendingPenalty += 2;
            previousPenaltyValue = 2;
            MoveTurn(1);
        }
        else if (data.value == UnoCard.CardValue.DrawFour)
        {
            pendingPenalty += 4;
            previousPenaltyValue = 4;
            MoveTurn(1);
        }
        else if (data.value == UnoCard.CardValue.Skip)
        {
            MoveTurn(2);
        }
        else if (data.value == UnoCard.CardValue.Reverse)
        {
            clockwise = !clockwise;

            if (actorOrder.Count == 2)
            {
                MoveTurn(2);
            }
            else
            {
                MoveTurn(1);
            }
        }
        else if (data.value == UnoCard.CardValue.Seven)
        {
            // 7-Slap: Player must choose opponent to swap hands with
            // Don't move turn yet; wait for slap selector
        }
        else if (data.number == 8)
        {
            // Rule of 8: Start slap race for everyone
            StartRule8();
        }
        else
        {
            MoveTurn(1);
        }
    }

    private void HostDrawCard(int actor)
    {
        if (!IsCurrentActor(actor))
        {
            BroadcastState("Invalid draw: not your turn.");
            return;
        }

        if (pendingPenalty > 0)
        {
            for (int i = 0; i < pendingPenalty; i++)
            {
                hands[actor].Add(DrawFromPile());
            }

            int amount = pendingPenalty;
            pendingPenalty = 0;
            previousPenaltyValue = 0;
            waitingAfterDrawActor = -1;

            MoveTurn(1);
            BroadcastState("Player drew penalty: " + amount);
            return;
        }

        NetCard drawn = DrawFromPile();
        hands[actor].Add(drawn);

        if (CanPlay(actor, drawn))
        {
            waitingAfterDrawActor = actor;
            BroadcastState("Drawn card can be played, or press End Turn.");
        }
        else
        {
            waitingAfterDrawActor = -1;
            MoveTurn(1);
            BroadcastState("Drew one card. Turn ended.");
        }
    }

    private void HostEndTurn(int actor)
    {
        if (waitingAfterDrawActor != actor) return;
        waitingAfterDrawActor = -1;
        MoveTurn(1);
        BroadcastState("Turn ended.");
    }

    private void HostSlap(int actor, int targetActor)
    {
        if (waitingForSlapActor != actor)
        {
            BroadcastState("Invalid slap: not waiting for slap.");
            return;
        }

        if (actor == targetActor)
        {
            BroadcastState("Cannot slap yourself.");
            return;
        }

        if (!hands.ContainsKey(targetActor))
        {
            BroadcastState("Invalid target player.");
            return;
        }

        // Swap hands
        List<NetCard> actorHand = hands[actor];
        List<NetCard> targetHand = hands[targetActor];

        hands[actor] = targetHand;
        hands[targetActor] = actorHand;

        waitingForSlapActor = -1;
        MoveTurn(1);
        BroadcastState("Player " + actor + " slaped and swapped hands with Player " + targetActor + "!");
    }

    private void StartRule8()
    {
        rule8Active = true;
        rule8Timer = RULE_8_DURATION;
        rule8Clicks.Clear();
        BroadcastState("An 8 was played! SLAP THE BUTTON! Last person to slap gets +2 cards!");
    }

    private void HostRule8Slap(int actor)
    {
        if (!rule8Active) return;

        // Record the click time
        if (!rule8Clicks.ContainsKey(actor))
        {
            rule8Clicks[actor] = rule8Timer;
        }
    }

    private void EndRule8()
    {
        rule8Active = false;

        List<int> penalizedActors = new List<int>();

        foreach (int actor in actorOrder)
        {
            if (!rule8Clicks.ContainsKey(actor))
            {
                penalizedActors.Add(actor);
            }
        }

        if (penalizedActors.Count == 0)
        {
            int slowestActor = -1;
            float lowestTimeRemaining = float.MaxValue;

            foreach (var kvp in rule8Clicks)
            {
                if (kvp.Value < lowestTimeRemaining)
                {
                    lowestTimeRemaining = kvp.Value;
                    slowestActor = kvp.Key;
                }
            }

            if (slowestActor >= 0)
            {
                penalizedActors.Add(slowestActor);
            }
        }

        if (penalizedActors.Count > 0)
        {
            string victims = "";
            foreach (int actor in penalizedActors)
            {
                // Add 2 penalty cards
                hands[actor].Add(DrawFromPile());
                hands[actor].Add(DrawFromPile());
                victims += "Player " + actor + " ";
            }

            BroadcastState("Too slow! Penalty (+2 cards) given to: " + victims);
        }

        rule8Clicks.Clear();
        MoveTurn(1);
    }

    private bool CanPlay(int actor, NetCard card)
    {
        UnoCard data = GetData(card);

        if (hands[actor].Count == 1 && IsNonWinningFinalCard(data))
        {
            return false;
        }

        if (pendingPenalty > 0)
        {
            int penaltyValue = GetPenaltyValue(data);
            return penaltyValue > 0 && penaltyValue >= previousPenaltyValue;
        }

        if (discardPile.Count == 0) return true;

        UnoCard top = GetData(discardPile[discardPile.Count - 1]);

        if (data.isWild) return true;
        if ((int)data.color == activeColor) return true;
        if (data.value == top.value) return true;

        return false;
    }

    private bool IsNonWinningFinalCard(UnoCard data)
    {
        return data.value == UnoCard.CardValue.Skip ||
               data.value == UnoCard.CardValue.Reverse ||
               data.value == UnoCard.CardValue.DrawTwo ||
               data.value == UnoCard.CardValue.Wild ||
               data.value == UnoCard.CardValue.DrawFour;
    }

    private int GetPenaltyValue(UnoCard data)
    {
        if (data.value == UnoCard.CardValue.DrawTwo) return 2;
        if (data.value == UnoCard.CardValue.DrawFour) return 4;
        return 0;
    }

    private bool IsCurrentActor(int actor)
    {
        if (actorOrder.Count == 0) return false;
        return actorOrder[currentPlayerIndex] == actor;
    }

    private void MoveTurn(int steps)
    {
        int dir = clockwise ? 1 : -1;
        currentPlayerIndex = Mod(currentPlayerIndex + steps * dir, actorOrder.Count);
    }

    private int Mod(int value, int count)
    {
        return (value % count + count) % count;
    }

    private NetCard FindCardInHand(int actor, int instanceId)
    {
        foreach (NetCard card in hands[actor])
        {
            if (card.instanceId == instanceId) return card;
        }

        return null;
    }

    private UnoCard GetData(NetCard card)
    {
        return allCards[card.cardIndex];
    }

    private void BroadcastState(string message)
    {
        GameStatePacket packet = new GameStatePacket();
        packet.actorNumbers = new List<int>(actorOrder);
        packet.hands = new List<PlayerHandPacket>();
        packet.currentActorNumber = actorOrder[currentPlayerIndex];
        packet.activeColor = activeColor;
        packet.clockwise = clockwise;
        packet.pendingPenalty = pendingPenalty;
        packet.previousPenaltyValue = previousPenaltyValue;
        packet.waitingAfterDrawActor = waitingAfterDrawActor;
        packet.waitingForSlapActor = waitingForSlapActor;
        packet.rule8Active = rule8Active;
        packet.rule8Timer = rule8Timer;
        packet.message = message;
        packet.gameOver = false;
        packet.winnerActorNumber = -1;

        if (discardPile.Count > 0)
        {
            packet.topCard = discardPile[discardPile.Count - 1];
        }

        foreach (int actor in actorOrder)
        {
            PlayerHandPacket handPacket = new PlayerHandPacket();
            handPacket.actorNumber = actor;
            handPacket.cards = new List<NetCard>(hands[actor]);
            packet.hands.Add(handPacket);
        }

        string json = JsonUtility.ToJson(packet);

        PhotonNetwork.RaiseEvent(
            STATE_UPDATE,
            json,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    private void BroadcastGameOver(int winnerActor)
    {
        GameStatePacket packet = new GameStatePacket();
        packet.actorNumbers = new List<int>(actorOrder);
        packet.hands = new List<PlayerHandPacket>();
        packet.currentActorNumber = winnerActor;
        packet.activeColor = activeColor;
        packet.clockwise = clockwise;
        packet.pendingPenalty = pendingPenalty;
        packet.previousPenaltyValue = previousPenaltyValue;
        packet.waitingAfterDrawActor = -1;
        packet.waitingForSlapActor = -1;
        packet.rule8Active = false;
        packet.rule8Timer = 0f;
        packet.message = "Player " + winnerActor + " wins!";
        packet.gameOver = true;
        packet.winnerActorNumber = winnerActor;

        if (discardPile.Count > 0)
        {
            packet.topCard = discardPile[discardPile.Count - 1];
        }

        foreach (int actor in actorOrder)
        {
            PlayerHandPacket handPacket = new PlayerHandPacket();
            handPacket.actorNumber = actor;
            handPacket.cards = new List<NetCard>(hands[actor]);
            packet.hands.Add(handPacket);
        }

        string json = JsonUtility.ToJson(packet);

        PhotonNetwork.RaiseEvent(
            STATE_UPDATE,
            json,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    private void RefreshClientUI()
    {
        if (lastState == null) return;

        SetStatus(lastState.message + "\nTurn: Player " + lastState.currentActorNumber);

        if (directionText != null)
        {
            directionText.text = lastState.clockwise ? "Direction: Clockwise" : "Direction: Counter-clockwise";
        }

        if (penaltyText != null)
        {
            penaltyText.text = lastState.pendingPenalty > 0
                ? "Pending Penalty: +" + lastState.pendingPenalty
                : "Pending Penalty: None";
        }

        if (discardImage != null && lastState.topCard != null)
        {
            discardImage.sprite = allCards[lastState.topCard.cardIndex].cardSprite;
        }

        RefreshHandUI();
        RefreshOpponentUI();

        bool canEndTurn = lastState.waitingAfterDrawActor == PhotonNetwork.LocalPlayer.ActorNumber;
        if (endTurnButton != null) endTurnButton.SetActive(canEndTurn);

        // Check if we're waiting for slap
        bool needsSlap = lastState.waitingForSlapActor == PhotonNetwork.LocalPlayer.ActorNumber;
        if (slapSelectorPanel != null) slapSelectorPanel.SetActive(needsSlap);

        // Check if Rule of 8 is active
        if (slap8Button != null) slap8Button.SetActive(rule8Active);
        if (slap8TimerText != null && rule8Active)
        {
            slap8TimerText.text = Mathf.Max(0, rule8Timer).ToString("F1");
        }
    }

    private void RefreshHandUI()
    {
        if (handParent == null || cardPrefab == null) return;

        for (int i = handParent.childCount - 1; i >= 0; i--)
        {
            Destroy(handParent.GetChild(i).gameObject);
        }

        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
        PlayerHandPacket myHand = null;

        foreach (PlayerHandPacket hand in lastState.hands)
        {
            if (hand.actorNumber == localActor)
            {
                myHand = hand;
                break;
            }
        }

        if (myHand == null) return;

        foreach (NetCard card in myHand.cards)
        {
            GameObject obj = Instantiate(cardPrefab, handParent);
            obj.transform.localScale = Vector3.one;

            PhotonUnoCardButton btn = obj.GetComponent<PhotonUnoCardButton>();
            if (btn != null)
            {
                btn.Setup(this, card, allCards[card.cardIndex]);
            }
        }
    }

    private void RefreshOpponentUI()
    {
        if (opponentText == null || lastState == null) return;

        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
        string text = "";

        foreach (PlayerHandPacket hand in lastState.hands)
        {
            if (hand.actorNumber == localActor) continue;
            text += "Player " + hand.actorNumber + ": " + hand.cards.Count + " cards\n";
        }

        opponentText.text = text;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log(msg);
    }

    public bool IsMyTurn()
    {
        return lastState != null &&
               lastState.currentActorNumber == PhotonNetwork.LocalPlayer.ActorNumber &&
               !lastState.gameOver;
    }

    public bool IsCardWild(UnoCard card)
    {
        return card != null && card.isWild;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {

    }
}

[Serializable]
public class NetCard
{
    public int instanceId;
    public int cardIndex;
}

[Serializable]
public class PlayerHandPacket
{
    public int actorNumber;
    public List<NetCard> cards = new List<NetCard>();
}

[Serializable]
public class GameStatePacket
{
    public List<int> actorNumbers = new List<int>();
    public List<PlayerHandPacket> hands = new List<PlayerHandPacket>();
    public NetCard topCard;
    public int currentActorNumber;
    public int activeColor;
    public bool clockwise;
    public int pendingPenalty;
    public int previousPenaltyValue;
    public int waitingAfterDrawActor;
    public int waitingForSlapActor;
    public bool rule8Active;
    public float rule8Timer;
    public bool gameOver;
    public int winnerActorNumber;
    public string message;
}
