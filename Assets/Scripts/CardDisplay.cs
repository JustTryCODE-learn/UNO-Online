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
        if (discardPile.CanPlayCard(cardData))
        {
            if (cardData.isWild)
            {
                FindObjectOfType<ColorPicker>().Show(cardData);
                Destroy(gameObject);
            }
            else
            {
                discardPile.UpdatePile(cardData);
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.Log("Invalid Move!");
        }
    }
}