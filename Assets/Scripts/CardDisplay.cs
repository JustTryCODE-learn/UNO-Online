using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardDisplay : MonoBehaviour, IPointerClickHandler
{
    public UnoCard cardData;
    private Image uiImage;
    private DiscardPile discardPile;

    void Awake()
    {
        uiImage = GetComponent<Image>();
        discardPile = GameObject.FindObjectOfType<DiscardPile>();
    }
    public void SetCard(UnoCard data)
    {
        GetComponent<Image>().sprite = data.cardSprite;
    }
    public void LoadCard(UnoCard newData)
    {
        cardData = newData;
        uiImage.sprite = cardData.cardSprite;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        DiscardPile pile = FindObjectOfType<DiscardPile>();
        TurnManager tm = FindObjectOfType<TurnManager>();

        // Only play if it's your turn, dealing is done, and the card is valid
        if (tm.currentPlayerIndex == 0 && !tm.isDealing)
        {
            if (pile.CanPlayCard(cardData))
            {
                // 1. Update the Discard Pile visuals and trigger special powers
                pile.UpdatePile(cardData);

                // 2. Remove the card from your data list
                FindObjectOfType<HandManager>().hand.Remove(cardData);

                // 3. Remove the card from the screen
                Destroy(gameObject);
            }
        }
    }
}