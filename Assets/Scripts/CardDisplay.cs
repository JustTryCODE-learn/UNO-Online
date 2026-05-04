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
        TurnManager tm = FindObjectOfType<TurnManager>();
        if (tm.currentPlayerIndex != 0) return;

        if (discardPile.CanPlayCard(cardData))
        {
            discardPile.UpdatePile(cardData);
            
            FindObjectOfType<HandManager>().hand.Remove(cardData);
            Destroy(gameObject);
            
            tm.NextTurn();
        }
        else
        {
            Debug.Log("Invalid Move!");
        }
    }
}