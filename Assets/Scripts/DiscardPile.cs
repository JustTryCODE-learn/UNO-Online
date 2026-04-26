using UnityEngine;
using UnityEngine.UI;

public class DiscardPile : MonoBehaviour
{
    public UnoCard currentCard;
    public TurnManager turnManager;
    private Image pileImage;

    void Awake()
    {
        pileImage = GetComponent<Image>();
    }

    public void UpdatePile(UnoCard newCard, UnoCard.CardColor chosenColor = UnoCard.CardColor.Wild)
    {
        if (newCard.isWild)
        {
            currentCard = newCard.GetVersion(chosenColor);
        }
        else
        {
            currentCard = newCard;
        }

        pileImage.sprite = currentCard.cardSprite;

        if (newCard.value == UnoCard.CardValue.DrawTwo)
        {
            turnManager.HandleDrawPower(2);
        }
        else if (newCard.value == UnoCard.CardValue.DrawFour)
        {
            turnManager.HandleDrawPower(4);
        }
        else if (newCard.value == UnoCard.CardValue.Reverse)
        {
            turnManager.HandleReverse();
        }
        else if (newCard.value == UnoCard.CardValue.Skip)
        {
            turnManager.HandleSkip();
        }
        else
        {
            // If it's a normal number card, just go to the next turn
            turnManager.NextTurn();
        }
    }

    void Start()
    {
        UnoCard firstCard = FindObjectOfType<DeckManager>().DrawCard();
        UpdatePile(firstCard);
    }

    public bool CanPlayCard(UnoCard cardToPlay)
    {
        if (cardToPlay.isWild) return true;

        if (cardToPlay.color == currentCard.color || cardToPlay.value == currentCard.value)
        {
            return true;
        }

        return false;
    }
}