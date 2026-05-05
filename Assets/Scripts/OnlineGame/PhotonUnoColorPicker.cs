using UnityEngine;

public class PhotonUnoColorPicker : MonoBehaviour
{
    private PhotonUnoGameManager gameManager;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(PhotonUnoGameManager manager)
    {
        gameManager = manager;
        gameObject.SetActive(true);
    }

    public void SelectRed()
    {
        SelectColor((int)UnoCard.CardColor.Red);
    }

    public void SelectBlue()
    {
        SelectColor((int)UnoCard.CardColor.Blue);
    }

    public void SelectGreen()
    {
        SelectColor((int)UnoCard.CardColor.Green);
    }

    public void SelectYellow()
    {
        SelectColor((int)UnoCard.CardColor.Yellow);
    }

    private void SelectColor(int color)
    {
        if (gameManager == null) return;

        gameManager.RequestPlaySelectedCardsWithColor(color);
        gameObject.SetActive(false);
    }
}
