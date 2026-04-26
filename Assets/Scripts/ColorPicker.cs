using UnityEngine;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{
    public DiscardPile discardPile;
    private UnoCard pendingWildCard;

    public void Show(UnoCard wildCard)
    {
        pendingWildCard = wildCard;
        gameObject.SetActive(true);
    }

    public void SelectRed() => FinalizeSelection(UnoCard.CardColor.Red);
    public void SelectBlue() => FinalizeSelection(UnoCard.CardColor.Blue);
    public void SelectGreen() => FinalizeSelection(UnoCard.CardColor.Green);
    public void SelectYellow() => FinalizeSelection(UnoCard.CardColor.Yellow);

    private void FinalizeSelection(UnoCard.CardColor color)
    {
        discardPile.UpdatePile(pendingWildCard, color);
        gameObject.SetActive(false);
    }
}