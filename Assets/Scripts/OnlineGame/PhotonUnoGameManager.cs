using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhotonUnoGameManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private const byte REQ_PLAY_SELECTED_CARDS = 1;
    private const byte REQ_DRAW_CARD = 2;
    private const byte REQ_PASS = 3;
    private const byte STATE_UPDATE = 20;
    private const byte REQ_CHOOSE_DIRECTION = 4;
    private const byte REQ_CHOOSE_SWAP_TARGET = 5;
    private const byte REQ_REACTION_RESPONSE = 6;
    private const byte REQ_RULE_0_ROTATE = 7;
    private const byte REQ_RULE_7_SWAP = 8;
    private const byte START_REACTION_EVENT = 9;
    private const byte REQ_REACTION_CLICK = 10;

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
    public TMP_Text selectionWarningText;

    public GameObject colorPickerPanel;
    public GameObject playButton;
    public GameObject drawButton;

    public GameObject playerPickerPanel;
    public GameObject playerButtonPrefab;
    public Transform playerButtonContainer;

    private readonly List<int> actorOrder = new List<int>();
    private readonly Dictionary<int, List<NetCard>> hands = new Dictionary<int, List<NetCard>>();
    private readonly List<NetCard> drawPile = new List<NetCard>();
    private readonly List<NetCard> discardPile = new List<NetCard>();

    private readonly List<int> selectedCardIds = new List<int>();

    private int currentPlayerIndex = 0;
    private bool clockwise = true;
    private int activeColor = 0;

    private int pendingPenalty = 0;
    private int previousPenaltyValue = 0;
    private int nextInstanceId = 1;
    private bool hostHasDrawn = false;
    private bool isAnimatingCards = false;

    private bool reactionWindowActive = false;
    private int reactionTargetActor = -1;
    private float reactionTimer = 0f;
    private const float REACTION_DURATION = 2.5f;

    private GameStatePacket lastState;

    private void Awake()
    {
        if (colorPickerPanel != null)
        {
            colorPickerPanel.SetActive(false);
        }

        if (selectionWarningText != null)
        {
            selectionWarningText.text = "";
        }
    }

    private System.Collections.IEnumerator Start()
    {
        SetStatus("Waiting for Photon room...");

        while (!PhotonNetwork.InRoom)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        if (PhotonNetwork.IsMasterClient)
        {
            StartHostGame();
        }
        else
        {
            SetStatus("Waiting for host...");
        }
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void StartHostGame()
    {
        if (!PhotonNetwork.InRoom)
        {
            SetStatus("Cannot start game. Not inside a Photon room.");
            return;
        }

        if (PhotonNetwork.PlayerList.Length == 0)
        {
            SetStatus("Cannot start game. No players found.");
            return;
        }

        if (allCards == null || allCards.Count == 0)
        {
            SetStatus("Cannot start game. All Cards list is empty.");
            return;
        }

        actorOrder.Clear();
        hands.Clear();
        drawPile.Clear();
        discardPile.Clear();
        selectedCardIds.Clear();

        currentPlayerIndex = 0;
        clockwise = true;
        activeColor = 0;
        pendingPenalty = 0;
        previousPenaltyValue = 0;
        nextInstanceId = 1;

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
                NetCard drawn = DrawFromPile();

                if (drawn != null)
                {
                    hands[actor].Add(drawn);
                }
            }
        }

        NetCard first = DrawFromPile();

        if (first == null)
        {
            SetStatus("Cannot start game. Draw pile is empty.");
            return;
        }

        discardPile.Add(first);

        UnoCard firstData = GetData(first);

        activeColor = firstData.isWild
            ? (int)UnoCard.CardColor.Red
            : (int)firstData.color;

        BroadcastState("Game started.");
    }

    private void BuildDeck()
    {
        drawPile.Clear();

        for (int i = 0; i < allCards.Count; i++)
        {
            UnoCard card = allCards[i];

            if (card == null)
            {
                Debug.LogWarning("All Cards has empty slot at index: " + i);
                continue;
            }

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

        Debug.Log("Draw pile created. Cards: " + drawPile.Count);
    }

    private void Shuffle(List<NetCard> cards)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, cards.Count);

            NetCard temp = cards[i];
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
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
        if (discardPile.Count <= 1)
        {
            return;
        }

        NetCard topCard = discardPile[discardPile.Count - 1];
        discardPile.RemoveAt(discardPile.Count - 1);

        drawPile.AddRange(discardPile);
        discardPile.Clear();
        discardPile.Add(topCard);

        Shuffle(drawPile);
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == STATE_UPDATE)
        {
            string json = (string)photonEvent.CustomData;
            lastState = JsonUtility.FromJson<GameStatePacket>(json);

            ClearSelection();
            RefreshClientUI();

            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (photonEvent.Code == REQ_PLAY_SELECTED_CARDS)
        {
            object[] data = (object[])photonEvent.CustomData;

            int actor = (int)data[0];
            int[] instanceIds = (int[])data[1];
            int chosenColor = (int)data[2];

            HostPlaySelectedCards(actor, instanceIds, chosenColor);
        }
        else if (photonEvent.Code == REQ_DRAW_CARD)
        {
            int actor = (int)photonEvent.CustomData;
            HostDrawCard(actor);
        }
        else if (photonEvent.Code == REQ_PASS)
        {
            int actor = (int)photonEvent.CustomData;
            HostPass(actor);
        }

        if (photonEvent.Code == START_REACTION_EVENT)
        {
            int targetActor = (int)photonEvent.CustomData;
            if (targetActor == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                // Show your "UNO!" UI button here
                // unoButton.SetActive(true);
            }
        }
        else if (photonEvent.Code == REQ_REACTION_CLICK && PhotonNetwork.IsMasterClient)
        {
            reactionWindowActive = false;
            BroadcastState("Player " + (int)photonEvent.CustomData + " said UNO!");
        }

        switch (photonEvent.Code)
        {
            case REQ_PLAY_SELECTED_CARDS:
                object[] playData = (object[])photonEvent.CustomData;
                HostPlaySelectedCards((int)playData[0], (int[])playData[1], (int)playData[2]);
                break;

            case REQ_RULE_0_ROTATE: // Handler for 0
                int actingPlayer = (int)photonEvent.CustomData;
                HostRotateHands(actingPlayer);
                break;

            case REQ_CHOOSE_SWAP_TARGET: // Handler for 7
                object[] swapData = (object[])photonEvent.CustomData;
                HostSwapHands((int)swapData[0], (int)swapData[1]);
                break;

            case REQ_DRAW_CARD:
                HostDrawCard((int)photonEvent.CustomData);
                break;

            case REQ_PASS:
                HostPass((int)photonEvent.CustomData);
                break;
        }
    }

    public void ToggleSelectedCard(int instanceId)
    {
        if (!IsMyTurn())
        {
            SetStatus("Not your turn.");
            return;
        }

        bool isAlreadySelected = selectedCardIds.Contains(instanceId);

        // Always allow deselecting
        if (isAlreadySelected)
        {
            selectedCardIds.Remove(instanceId);
            UpdatePlayButtonText();
            UpdateSelectionText();
            RefreshAllCardVisuals();
            return;
        }

        // For new selection, check if it's valid (highlighted)
        if (!IsCardValidToPlay(instanceId))
        {
            SetStatus("Invalid move: this card cannot be played now.");
            return;
        }

        selectedCardIds.Add(instanceId);

        UpdatePlayButtonText();
        UpdateSelectionText();
        RefreshAllCardVisuals();
    }

    public bool IsCardValidToPlay(int instanceId)
    {
        if (!IsMyTurn()) return false;

        NetCard card = FindLocalCardById(instanceId);
        if (card == null) return false;

        // If nothing selected, any card that can be played on the pile is valid
        if (selectedCardIds.Count == 0)
        {
            return CanPlay(PhotonNetwork.LocalPlayer.ActorNumber, card);
        }

        // If cards are selected, only cards that can be added to the current set (same value) are valid
        return CanAddCardToSelection(instanceId);
    }

    public void RefreshAllCardVisuals()
    {
        if (handParent == null) return;

        foreach (Transform child in handParent)
        {
            PhotonUnoCardButton button = child.GetComponent<PhotonUnoCardButton>();
            if (button != null)
            {
                button.RefreshVisual();
            }
        }
    }

    private bool CanAddCardToSelection(int newInstanceId)
    {
        if (selectedCardIds.Count == 0)
        {
            return true;
        }

        NetCard newCard = FindLocalCardById(newInstanceId);

        if (newCard == null)
        {
            return false;
        }

        NetCard firstSelectedCard = FindLocalCardById(selectedCardIds[0]);

        if (firstSelectedCard == null)
        {
            return false;
        }

        UnoCard.CardValue firstValue = allCards[firstSelectedCard.cardIndex].value;
        UnoCard.CardValue newValue = allCards[newCard.cardIndex].value;

        return newValue == firstValue;
    }

    public bool IsCardSelected(int instanceId)
    {
        return selectedCardIds.Contains(instanceId);
    }

    public void RequestPlaySelectedCards()
    {
        if (!IsMyTurn())
        {
            SetStatus("Not your turn.");
            return;
        }

        if (selectedCardIds.Count == 0)
        {
            if (lastState != null && lastState.hasDrawn)
            {
                RequestPass();
                return;
            }

            SetStatus("Select one or more cards first.");
            UpdateSelectionText();
            return;
        }

        if (!IsCurrentSelectionValid())
        {
            SetStatus("Invalid set: selected cards must have the same value.");
            UpdateSelectionText();
            return;
        }

        NetCard firstCard = FindLocalCardById(selectedCardIds[0]);
        if (firstCard == null) return;
        UnoCard data = allCards[firstCard.cardIndex];

        if (SelectionContainsWild())
        {
            PhotonUnoColorPicker picker = colorPickerPanel.GetComponent<PhotonUnoColorPicker>();

            if (picker != null)
            {
                picker.Show(this);
                return;
            }

            SetStatus("Wild selected. Add a color picker to choose color.");
            return;
        }

        if (data.value == UnoCard.CardValue.Seven)
        {
            SetStatus("7 Played! Choose a player to swap hands with.");
            OpenPlayerPicker();
            return;
        }

        if (data.value == UnoCard.CardValue.Zero)
        {
            SetStatus("0 Played! All hands will rotate in " + (clockwise ? "clockwise" : "counter-clockwise") + " direction.");
            RequestRotateHands();
            return;
        }

        SendPlaySelectedRequest(-1);
    }

    private void RequestRotateHands()
    {
        PhotonNetwork.RaiseEvent(
            REQ_RULE_0_ROTATE,
            PhotonNetwork.LocalPlayer.ActorNumber,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
    }

    public void RequestPlaySelectedCardsWithColor(int chosenColor)
    {
        if (!IsCurrentSelectionValid())
        {
            SetStatus("Invalid set: selected cards must have the same value.");
            UpdateSelectionText();
            return;
        }

        SendPlaySelectedRequest(chosenColor);
    }

    private void SendPlaySelectedRequest(int chosenColor)
    {
        int[] ids = selectedCardIds.ToArray();

        // Start local animation before sending the request
        StartCoroutine(AnimateCardsToDiscard(new List<int>(selectedCardIds)));

        object[] data =
        {
            PhotonNetwork.LocalPlayer.ActorNumber,
            ids,
            chosenColor
        };

        PhotonNetwork.RaiseEvent(
            REQ_PLAY_SELECTED_CARDS,
            data,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
    }

    private System.Collections.IEnumerator AnimateCardsToDiscard(List<int> instanceIds)
    {
        if (instanceIds.Count == 0 || discardImage == null) yield break;

        isAnimatingCards = true;

        List<RectTransform> cardRects = new List<RectTransform>();

        // 1. Immediately find and detach ALL cards so they aren't destroyed by RefreshHandUI
        Canvas canvas = handParent.GetComponentInParent<Canvas>();
        Transform animLayer = (canvas != null) ? canvas.transform : handParent.parent;

        foreach (int id in instanceIds)
        {
            GameObject cardObj = FindCardGameObject(id);
            if (cardObj != null)
            {
                // Move to root canvas and to the front of the draw order
                // worldPositionStays = true ensures it doesn't jump when parent changes
                cardObj.transform.SetParent(animLayer, true);
                cardObj.transform.SetAsLastSibling();

                PhotonUnoCardButton btn = cardObj.GetComponent<PhotonUnoCardButton>();
                if (btn != null) btn.enabled = false;

                cardRects.Add(cardObj.GetComponent<RectTransform>());
            }
        }

        if (cardRects.Count == 0)
        {
            isAnimatingCards = false;
            yield break;
        }

        // 2. Animate them one by one from the safe layer
        float totalAvailableTime = 2.0f;
        float durationPerCard = totalAvailableTime / cardRects.Count;

        foreach (RectTransform rect in cardRects)
        {
            if (rect != null)
            {
                yield return StartCoroutine(FlyToDiscard(rect, durationPerCard));
            }
        }

        isAnimatingCards = false;
        UpdateDiscardUI();
    }

    private System.Collections.IEnumerator FlyToDiscard(RectTransform rect, float duration)
    {
        if (rect == null || discardImage == null) yield break;

        Vector3 startPos = rect.position;
        Vector3 targetPos = discardImage.rectTransform.position;

        Quaternion startRot = rect.rotation;
        Quaternion targetRot = Quaternion.identity;

        Vector3 startScale = rect.localScale;
        Vector3 targetScale = discardImage.rectTransform.localScale;

        float elapsed = 0;
        while (elapsed < duration)
        {
            if (rect == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easedT = t * t * (3f - 2f * t); // Smoothstep

            rect.position = Vector3.Lerp(startPos, targetPos, easedT);
            rect.rotation = Quaternion.Slerp(startRot, targetRot, easedT);
            rect.localScale = Vector3.Lerp(startScale, targetScale, easedT);

            yield return null;
        }

        // Card has landed! Update the discard pile image immediately
        PhotonUnoCardButton cardBtn = rect.GetComponent<PhotonUnoCardButton>();
        if (cardBtn != null && discardImage != null)
        {
            discardImage.sprite = cardBtn.GetCardData().cardSprite;
        }

        Destroy(rect.gameObject);
    }

    private GameObject FindCardGameObject(int instanceId)
    {
        if (handParent == null) return null;

        foreach (Transform child in handParent)
        {
            PhotonUnoCardButton btn = child.GetComponent<PhotonUnoCardButton>();
            if (btn != null && btn.GetNetCard() != null && btn.GetNetCard().instanceId == instanceId)
            {
                return child.gameObject;
            }
        }
        return null;
    }

    private void HostRotateHands(int actor)
    {
        NetCard zeroCard = hands[actor].Find(c => allCards[c.cardIndex].value == UnoCard.CardValue.Zero);
        if (zeroCard != null)
        {
            hands[actor].Remove(zeroCard);
            discardPile.Add(zeroCard);
        }
        List<int> actors = new List<int>(actorOrder);
        Dictionary<int, List<NetCard>> newHands = new Dictionary<int, List<NetCard>>();
        for (int i = 0; i < actors.Count; i++)
        {
            int nextIndex = Mod(i + (clockwise ? 1 : -1), actors.Count);
            newHands[actors[nextIndex]] = hands[actors[i]];
        }
        foreach (var entry in newHands)
        {
            hands[entry.Key] = entry.Value;
        }
        MoveTurn(1);
        BroadcastState("0 Played! Hands rotated " + (clockwise ? "Clockwise" : "Counter-Clockwise"));
    }

    private void HostSwapHands(int actor, int targetActorId)
    {
        NetCard sevenCard = hands[actor].Find(c => allCards[c.cardIndex].value == UnoCard.CardValue.Seven);
        if (sevenCard != null)
        {
            hands[actor].Remove(sevenCard);
            discardPile.Add(sevenCard);
        }
        List<NetCard> handA = new List<NetCard>(hands[actor]);
        List<NetCard> handB = new List<NetCard>(hands[targetActorId]);
        hands[actor] = handB;
        hands[targetActorId] = handA;
        MoveTurn(1);
        BroadcastState("Player " + actor + " swapped hands with Player " + targetActorId + "!");
    }

    public void RequestDrawCard()
    {
        ClearSelection();

        PhotonNetwork.RaiseEvent(
            REQ_DRAW_CARD,
            PhotonNetwork.LocalPlayer.ActorNumber,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
    }

    public void RequestPass()
    {
        ClearSelection();

        PhotonNetwork.RaiseEvent(
            REQ_PASS,
            PhotonNetwork.LocalPlayer.ActorNumber,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
    }

    private void HostPlaySelectedCards(int actor, int[] instanceIds, int chosenColor)
    {
        if (!IsCurrentActor(actor))
        {
            BroadcastState("Invalid move: not your turn.");
            return;
        }

        if (instanceIds == null || instanceIds.Length == 0)
        {
            BroadcastState("Invalid move: no cards selected.");
            return;
        }

        if (!hands.ContainsKey(actor))
        {
            BroadcastState("Invalid move: player hand not found.");
            return;
        }

        List<NetCard> selectedCards = new List<NetCard>();

        foreach (int id in instanceIds)
        {
            NetCard card = FindCardInHand(actor, id);

            if (card == null)
            {
                BroadcastState("Invalid move: selected card not found.");
                return;
            }

            if (selectedCards.Contains(card))
            {
                BroadcastState("Invalid move: duplicate selected card.");
                return;
            }

            selectedCards.Add(card);
        }

        if (!AreSelectedCardsValid(actor, selectedCards, chosenColor))
        {
            BroadcastState("Invalid selected card group.");
            return;
        }

        foreach (NetCard card in selectedCards)
        {
            hands[actor].Remove(card);
            discardPile.Add(card);
        }

        hostHasDrawn = false;

        UnoCard lastPlayedData = GetData(selectedCards[selectedCards.Count - 1]);

        if (SelectionHasWild(selectedCards))
        {
            activeColor = chosenColor;
        }
        else
        {
            activeColor = (int)lastPlayedData.color;
        }

        if (hands[actor].Count == 0)
        {
            BroadcastGameOver(actor);
            return;
        }

        if (hands[actor].Count == 1)
        {
            StartReactionWindow(actor);
        }

        ApplySelectedCardEffects(selectedCards);

        BroadcastState("Played " + selectedCards.Count + " card(s).");
    }

    private void StartReactionWindow(int actor)
    {
        reactionWindowActive = true;
        reactionTargetActor = actor;
        reactionTimer = REACTION_DURATION;

        PhotonNetwork.RaiseEvent(START_REACTION_EVENT, actor,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true });
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient || !reactionWindowActive) return;

        reactionTimer -= Time.deltaTime;
        if (reactionTimer <= 0)
        {
            reactionWindowActive = false;
            ApplyUnoPenalty(reactionTargetActor);
        }
    }

    private void ApplyUnoPenalty(int actor)
    {
        for (int i = 0; i < 2; i++)
        {
            NetCard drawn = DrawFromPile();
            if (drawn != null) hands[actor].Add(drawn);
        }
        BroadcastState("Player " + actor + " forgot to say UNO! +2 cards.");
    }

    private bool AreSelectedCardsValid(int actor, List<NetCard> selectedCards, int chosenColor)
    {
        if (selectedCards == null || selectedCards.Count == 0)
        {
            return false;
        }

        UnoCard firstData = GetData(selectedCards[0]);

        foreach (NetCard card in selectedCards)
        {
            UnoCard data = GetData(card);

            if (data.value != firstData.value)
            {
                return false;
            }
        }

        if (SelectionHasWild(selectedCards) && chosenColor < 0)
        {
            return false;
        }

        bool wouldEmptyHand = hands[actor].Count == selectedCards.Count;

        if (wouldEmptyHand)
        {
            foreach (NetCard card in selectedCards)
            {
                if (IsNonWinningFinalCard(GetData(card)))
                {
                    return false;
                }
            }
        }

        if (!CanPlay(actor, selectedCards[0]))
        {
            return false;
        }

        return true;
    }

    private void ApplySelectedCardEffects(List<NetCard> selectedCards)
    {
        UnoCard data = GetData(selectedCards[0]);
        int count = selectedCards.Count;

        if (data.value == UnoCard.CardValue.DrawTwo)
        {
            pendingPenalty += 2 * count;
            previousPenaltyValue = 2;
            MoveTurn(1);
        }
        else if (data.value == UnoCard.CardValue.DrawFour)
        {
            pendingPenalty += 4 * count;
            previousPenaltyValue = 4;
            MoveTurn(1);
        }
        else if (data.value == UnoCard.CardValue.Skip)
        {
            MoveTurn(1 + count);
        }
        else if (data.value == UnoCard.CardValue.Reverse)
        {
            for (int i = 0; i < count; i++)
            {
                clockwise = !clockwise;
            }

            if (actorOrder.Count == 2)
            {
                MoveTurn(1 + count);
            }
            else
            {
                MoveTurn(1);
            }
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
                NetCard penaltyCard = DrawFromPile();

                if (penaltyCard != null)
                {
                    hands[actor].Add(penaltyCard);
                }
            }

            int amount = pendingPenalty;

            pendingPenalty = 0;
            previousPenaltyValue = 0;

            MoveTurn(1);

            BroadcastState("Player drew penalty: " + amount);
            return;
        }

        NetCard drawn = DrawFromPile();

        if (drawn != null)
        {
            hands[actor].Add(drawn);

            // If the drawn card is playable, don't end turn yet. Let them choose to play or pass.
            if (CanPlay(actor, drawn))
            {
                hostHasDrawn = true;
                BroadcastState("Drew a playable card. You can play it or Pass.");
                return;
            }
        }

        hostHasDrawn = false;
        MoveTurn(1);

        BroadcastState("Drew one card. Turn ended.");
    }

    private void HostPass(int actor)
    {
        if (!IsCurrentActor(actor)) return;

        hostHasDrawn = false;
        MoveTurn(1);
        BroadcastState("Player passed.");
    }

    private bool CanPlay(int actor, NetCard card)
    {
        UnoCard data = GetData(card);

        // On clients, we must use the last state received from the host.
        // On the host, we use the local authority variables.
        int currentPenalty = PhotonNetwork.IsMasterClient ? pendingPenalty : (lastState != null ? lastState.pendingPenalty : 0);
        int currentPrevPenalty = PhotonNetwork.IsMasterClient ? previousPenaltyValue : (lastState != null ? lastState.previousPenaltyValue : 0);
        int currentActiveColor = PhotonNetwork.IsMasterClient ? activeColor : (lastState != null ? lastState.activeColor : 0);

        NetCard topNetCard = null;
        if (PhotonNetwork.IsMasterClient)
        {
            if (discardPile.Count > 0) topNetCard = discardPile[discardPile.Count - 1];
        }
        else
        {
            if (lastState != null) topNetCard = lastState.topCard;
        }

        if (currentPenalty > 0)
        {
            int penaltyValue = GetPenaltyValue(data);
            return penaltyValue > 0 && penaltyValue >= currentPrevPenalty;
        }

        if (topNetCard == null)
        {
            return true;
        }

        UnoCard topData = GetData(topNetCard);

        if (data.isWild)
        {
            return true;
        }

        if ((int)data.color == currentActiveColor)
        {
            return true;
        }

        if (data.value == topData.value)
        {
            return true;
        }

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
        if (data.value == UnoCard.CardValue.DrawTwo)
        {
            return 2;
        }

        if (data.value == UnoCard.CardValue.DrawFour)
        {
            return 4;
        }

        return 0;
    }

    private bool IsCurrentActor(int actor)
    {
        if (actorOrder.Count == 0)
        {
            return false;
        }

        return actorOrder[currentPlayerIndex] == actor;
    }

    private void MoveTurn(int steps)
    {
        if (actorOrder.Count == 0)
        {
            return;
        }

        int direction = clockwise ? 1 : -1;

        currentPlayerIndex = Mod(
            currentPlayerIndex + steps * direction,
            actorOrder.Count
        );
    }

    private int Mod(int value, int count)
    {
        return (value % count + count) % count;
    }

    private NetCard FindCardInHand(int actor, int instanceId)
    {
        if (!hands.ContainsKey(actor))
        {
            return null;
        }

        foreach (NetCard card in hands[actor])
        {
            if (card.instanceId == instanceId)
            {
                return card;
            }
        }

        return null;
    }

    private UnoCard GetData(NetCard card)
    {
        return allCards[card.cardIndex];
    }

    private void BroadcastState(string message)
    {
        if (actorOrder.Count == 0)
        {
            return;
        }

        GameStatePacket packet = BuildStatePacket(message, false, -1);

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
        GameStatePacket packet = BuildStatePacket(
            "Player " + winnerActor + " wins!",
            true,
            winnerActor
        );

        string json = JsonUtility.ToJson(packet);

        PhotonNetwork.RaiseEvent(
            STATE_UPDATE,
            json,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    private GameStatePacket BuildStatePacket(string message, bool gameOver, int winnerActor)
    {
        GameStatePacket packet = new GameStatePacket();

        packet.actorNumbers = new List<int>(actorOrder);
        packet.hands = new List<PlayerHandPacket>();

        packet.currentActorNumber = actorOrder[currentPlayerIndex];
        packet.activeColor = activeColor;
        packet.clockwise = clockwise;

        packet.pendingPenalty = pendingPenalty;
        packet.previousPenaltyValue = previousPenaltyValue;
        packet.hasDrawn = hostHasDrawn;

        packet.message = message;
        packet.gameOver = gameOver;
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

        return packet;
    }

    private void RefreshClientUI()
    {
        if (lastState == null)
        {
            return;
        }

        SetStatus(lastState.message + "\nTurn: Player " + lastState.currentActorNumber);

        if (directionText != null)
        {
            directionText.text = lastState.clockwise
                ? "Direction: Clockwise"
                : "Direction: Counter-clockwise";
        }

        if (penaltyText != null)
        {
            penaltyText.text = lastState.pendingPenalty > 0
                ? "Pending Penalty: +" + lastState.pendingPenalty
                : "Pending Penalty: None";
        }

        UpdateDiscardUI();

        RefreshHandUI();
        RefreshOpponentUI();
        UpdatePlayButtonText();
        UpdateSelectionText();
    }

    private void RefreshHandUI()
    {
        if (handParent == null || cardPrefab == null)
        {
            return;
        }

        RectTransform handRect = handParent.GetComponent<RectTransform>();

        Vector2 savedPosition = Vector2.zero;
        Vector2 savedSize = Vector2.zero;
        Vector2 savedAnchorMin = Vector2.zero;
        Vector2 savedAnchorMax = Vector2.zero;
        Vector2 savedPivot = Vector2.zero;

        if (handRect != null)
        {
            savedPosition = handRect.anchoredPosition;
            savedSize = handRect.sizeDelta;
            savedAnchorMin = handRect.anchorMin;
            savedAnchorMax = handRect.anchorMax;
            savedPivot = handRect.pivot;
        }

        ProfessionalHandLayout layout = handParent.GetComponent<ProfessionalHandLayout>();

        if (layout != null)
        {
            layout.ArrangeCards();
        }

        for (int i = handParent.childCount - 1; i >= 0; i--)
        {
            Transform child = handParent.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
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

        if (myHand == null)
        {
            return;
        }

        foreach (NetCard card in myHand.cards)
        {
            GameObject cardObject = Instantiate(cardPrefab, handParent);
            cardObject.transform.localScale = Vector3.one;

            PhotonUnoCardButton button = cardObject.GetComponent<PhotonUnoCardButton>();

            if (button != null)
            {
                button.Setup(this, card, allCards[card.cardIndex]);
            }
        }

        if (layout != null)
        {
            layout.ArrangeCards();
        }

        if (handRect != null)
        {
            handRect.anchoredPosition = savedPosition;
            handRect.sizeDelta = savedSize;
            handRect.anchorMin = savedAnchorMin;
            handRect.anchorMax = savedAnchorMax;
            handRect.pivot = savedPivot;
        }
    }

    private void UpdateDiscardUI()
    {
        if (isAnimatingCards) return;

        if (discardImage != null && lastState != null && lastState.topCard != null)
        {
            discardImage.sprite = allCards[lastState.topCard.cardIndex].cardSprite;
        }
    }

    private void RefreshOpponentUI()
    {
        if (opponentText == null || lastState == null)
        {
            return;
        }

        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
        string text = "";

        foreach (PlayerHandPacket hand in lastState.hands)
        {
            if (hand.actorNumber == localActor)
            {
                continue;
            }

            text += "Player " + hand.actorNumber + ": " + hand.cards.Count + " cards\n";
        }

        opponentText.text = text;
    }

    private void ClearSelection()
    {
        selectedCardIds.Clear();

        if (selectionWarningText != null)
        {
            selectionWarningText.text = "";
        }

        UpdatePlayButtonText();
        RefreshAllCardVisuals();
    }

    private bool IsCurrentSelectionValid()
    {
        if (selectedCardIds.Count <= 1)
        {
            return true;
        }

        NetCard firstCard = FindLocalCardById(selectedCardIds[0]);

        if (firstCard == null)
        {
            return false;
        }

        UnoCard.CardValue firstValue = allCards[firstCard.cardIndex].value;

        foreach (int id in selectedCardIds)
        {
            NetCard card = FindLocalCardById(id);

            if (card == null)
            {
                return false;
            }

            UnoCard data = allCards[card.cardIndex];

            if (data.value != firstValue)
            {
                return false;
            }
        }

        return true;
    }

    private void UpdateSelectionText()
    {
        if (selectionWarningText == null)
        {
            return;
        }

        if (selectedCardIds.Count == 0)
        {
            selectionWarningText.text = "";
            return;
        }

        selectionWarningText.text = "Selected cards: " + selectedCardIds.Count;
    }

    private bool SelectionContainsWild()
    {
        if (lastState == null)
        {
            return false;
        }

        foreach (int id in selectedCardIds)
        {
            NetCard card = FindLocalCardById(id);

            if (card != null && allCards[card.cardIndex].isWild)
            {
                return true;
            }
        }

        return false;
    }

    private bool SelectionHasWild(List<NetCard> cards)
    {
        foreach (NetCard card in cards)
        {
            if (GetData(card).isWild)
            {
                return true;
            }
        }

        return false;
    }

    private NetCard FindLocalCardById(int id)
    {
        if (lastState == null)
        {
            return null;
        }

        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;

        foreach (PlayerHandPacket hand in lastState.hands)
        {
            if (hand.actorNumber != localActor)
            {
                continue;
            }

            foreach (NetCard card in hand.cards)
            {
                if (card.instanceId == id)
                {
                    return card;
                }
            }
        }

        return null;
    }

    private void UpdatePlayButtonText()
    {
        UpdateDrawButton();

        if (playButton == null)
        {
            return;
        }

        TMP_Text buttonText = playButton.GetComponentInChildren<TMP_Text>();
        Button button = playButton.GetComponent<Button>();

        if (button != null)
        {
            if (lastState != null && lastState.hasDrawn && selectedCardIds.Count == 0)
            {
                if (buttonText != null) buttonText.text = "Pass";
                button.interactable = IsMyTurn();
            }
            else
            {
                if (buttonText != null)
                {
                    buttonText.text = selectedCardIds.Count > 0
                        ? "Play (" + selectedCardIds.Count + ")"
                        : "Play";
                }

                button.interactable =
                    IsMyTurn() &&
                    selectedCardIds.Count > 0 &&
                    IsCurrentSelectionValid();
            }
        }
    }

    private void UpdateDrawButton()
    {
        if (drawButton == null) return;

        Button button = drawButton.GetComponent<Button>();
        TMP_Text buttonText = drawButton.GetComponentInChildren<TMP_Text>();

        if (button != null)
        {
            if (buttonText != null) buttonText.text = "Draw";

            // Only interactable if it's my turn, I haven't drawn yet, and I have no legal moves
            button.interactable = IsMyTurn() && (lastState == null || !lastState.hasDrawn) && !HasAnyLegalMove();
        }
    }

    private bool HasAnyLegalMove()
    {
        if (lastState == null) return false;

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

        if (myHand == null) return false;

        foreach (NetCard card in myHand.cards)
        {
            if (CanPlay(localActor, card)) return true;
        }

        return false;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        Debug.Log(message);
    }

    public bool IsMyTurn()
    {
        return lastState != null &&
               lastState.currentActorNumber == PhotonNetwork.LocalPlayer.ActorNumber &&
               !lastState.gameOver;
    }

    // --- HOUSE RULE 7 LOGIC ---
    public void OpenPlayerPicker()
    {
        if (playerPickerPanel != null)
        {
            playerPickerPanel.SetActive(true);
        }
        foreach (Transform child in playerButtonContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (var player in Photon.Pun.PhotonNetwork.PlayerList)
        {
            if (player.IsLocal) continue;
            GameObject btnObj = Instantiate(playerButtonPrefab, playerButtonContainer);
            TMPro.TMP_Text btnText = btnObj.GetComponentInChildren<TMPro.TMP_Text>();
            if (btnText != null)
            {
                btnText.text = "Swap with Player " + player.ActorNumber;
            }
            int targetId = player.ActorNumber;
            btnObj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
            {
                SendSwapRequest(targetId);
                playerPickerPanel.SetActive(false);
            });
        }
    }
    private void SendSwapRequest(int targetId)
    {
        object[] data = new object[] { PhotonNetwork.LocalPlayer.ActorNumber, targetId };
        RaiseEventOptions reo = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        SendOptions so = new SendOptions { Reliability = true };

        PhotonNetwork.RaiseEvent(REQ_CHOOSE_SWAP_TARGET, data, reo, so);
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
    public bool hasDrawn;

    public bool gameOver;
    public int winnerActorNumber;

    public string message;
}