using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SimpleAI : MonoBehaviour
{
    private HandManager handManager;
    private DiscardPile discardPile;
    private TurnManager turnManager;

    void Awake()
    {
        handManager = GetComponent<HandManager>();
        discardPile = FindObjectOfType<DiscardPile>();
        turnManager = FindObjectOfType<TurnManager>();
    }

    // This is called by the TurnManager when it's this AI's turn
    public void TakeTurn()
    {
        StartCoroutine(AIThinkRoutine());
    }

    IEnumerator AIThinkRoutine()
    {
        // 1. Wait a bit so it looks like the AI is "thinking"
        yield return new WaitForSeconds(1.5f);

        UnoCard bestCard = null;

        // 2. Scan the hand for a playable card
        foreach (UnoCard card in handManager.hand)
        {
            if (discardPile.CanPlayCard(card))
            {
                bestCard = card;
                break; // Found a match!
            }
        }

        // 3. Play the card or Draw
        if (bestCard != null)
        {
            PlayCard(bestCard);
        }
        else
        {
            Debug.Log(gameObject.name + " has no match, drawing card...");
            handManager.DrawCardFromDeck();
            // Standard UNO: After drawing, if it's playable, AI plays it. 
            // For now, let's just end the turn.
            turnManager.NextTurn();
        }
    }

    void PlayCard(UnoCard card)
    {
        handManager.hand.Remove(card);

        foreach (Transform child in handManager.handParent)
        {
            if (child.GetComponent<CardDisplay>().cardData == card)
            {
                Destroy(child.gameObject);
                break;
            }
        }

        discardPile.UpdatePile(card);
        turnManager.NextTurn();
    }
}