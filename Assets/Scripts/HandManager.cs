using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    public List<UnoCard> hand = new List<UnoCard>();
    public GameObject cardPrefab; // The template for the card UI
    public Transform handParent;   // The UI Horizontal Layout Group

    // This is the method TurnManager was looking for!
    public void DrawCardFromDeck()
    {
        UnoCard data = FindObjectOfType<DeckManager>().DrawCard();
        if (data != null)
        {
            // 1. ADD DATA TO THE LIST
            hand.Add(data);
            // 2. SPAWN THE UI
            GameObject newCard = Instantiate(cardPrefab, handParent);
            newCard.transform.localScale = Vector3.one;
            // 3. INITIALIZE THE CARD
            CardDisplay display = newCard.GetComponent<CardDisplay>();
            display.SetCard(data);
        }
    }
}