using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    // This is the list you are looking for!
    public List<UnoCard> allCards = new List<UnoCard>();

    private List<UnoCard> drawPile = new List<UnoCard>();

    void Awake()
    {
        GenerateFullDeck();
        ShuffleDeck();
    }

    void GenerateFullDeck()
    {
        drawPile.Clear();

        foreach (UnoCard card in allCards)
        {
            int copiesToAdd = 0;

            if (card.isWild)
            {
                // Wild and Wild Draw Four: 4 copies each
                copiesToAdd = 4;
            }
            else if (card.number == 0)
            {
                // Number 0: 1 copy per color
                copiesToAdd = 1;
            }
            else
            {
                // Numbers 1-9 and Action Cards (Skip, Reverse, Draw 2): 2 copies per color
                copiesToAdd = 2;
            }

            for (int i = 0; i < copiesToAdd; i++)
            {
                drawPile.Add(card);
            }
        }

        Debug.Log("Deck Generated! Total cards: " + drawPile.Count);
    }

    public void ShuffleDeck()
    {
        // Basic shuffle logic
        for (int i = 0; i < drawPile.Count; i++)
        {
            UnoCard temp = drawPile[i];
            int randomIndex = Random.Range(i, drawPile.Count);
            drawPile[i] = drawPile[randomIndex];
            drawPile[randomIndex] = temp;
        }
    }

    public UnoCard DrawCard()
    {
        if (drawPile.Count == 0)
        {
            Debug.LogWarning("Deck is empty! Reshuffling...");
            // Here you would normally move cards from the discard pile back to the deck
            return null;
        }

        UnoCard drawnCard = drawPile[0];
        drawPile.RemoveAt(0);
        return drawnCard;
    }
}