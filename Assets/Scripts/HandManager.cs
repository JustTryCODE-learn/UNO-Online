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
            // FIX: The second argument 'handParent' tells Unity WHERE to put the card
            GameObject newCard = Instantiate(cardPrefab, handParent);

            // Ensure the scale is reset to 1 (prevents the "too small" issue)
            newCard.transform.localScale = Vector3.one;

            newCard.GetComponent<CardDisplay>().SetCard(data);
        }
    }
}