using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PhotonUnoCardButton : MonoBehaviour, IPointerClickHandler
{
    private PhotonUnoGameManager gameManager;
    private NetCard netCard;
    private UnoCard cardData;
    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    public void Setup(PhotonUnoGameManager manager, NetCard card, UnoCard data)
    {
        gameManager = manager;
        netCard = card;
        cardData = data;

        if (image == null) image = GetComponent<Image>();
        if (image != null) image.sprite = data.cardSprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameManager == null || netCard == null || cardData == null) return;

        if (!gameManager.IsMyTurn())
        {
            Debug.Log("Not your turn.");
            return;
        }

        if (gameManager.IsCardWild(cardData))
        {
            PhotonUnoColorPicker picker = FindObjectOfType<PhotonUnoColorPicker>();
            if (picker != null)
            {
                picker.Show(gameManager, netCard.instanceId);
            }
            else
            {
                Debug.LogError("No PhotonUnoColorPicker found in scene.");
            }

            return;
        }

        gameManager.RequestPlayCard(netCard.instanceId, -1);
    }
}
