using UnityEngine;

public class PhotonUnoColorPicker : MonoBehaviour
{
    private PhotonUnoGameManager gameManager;
    private int pendingCardInstanceId = -1;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(PhotonUnoGameManager manager, int cardInstanceId)
    {
        gameManager = manager;
        pendingCardInstanceId = cardInstanceId;
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
        if (gameManager == null || pendingCardInstanceId < 0) return;

        gameManager.RequestPlayCard(pendingCardInstanceId, color);

        pendingCardInstanceId = -1;
        gameObject.SetActive(false);
    }
}
