using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PhotonUnoCardButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Selection Visual")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0.65f, 1f, 0.65f, 1f);

    [Header("Valid Visual")]
    public GameObject validVisual;

    [Header("References")]
    public Image cardImage;
    public float selectedLift = 30f;

    private PhotonUnoGameManager gameManager;
    private NetCard netCard;
    private UnoCard cardData;

    private Vector2 basePosition;
    private bool basePositionCaptured = false;

    public NetCard GetNetCard() => netCard;
    public UnoCard GetCardData() => cardData;

    private void Awake()
    {
        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }
    }

    public void Setup(PhotonUnoGameManager manager, NetCard card, UnoCard data)
    {
        gameManager = manager;
        netCard = card;
        cardData = data;

        if (cardImage == null) cardImage = GetComponent<Image>();

        if (cardImage != null)
        {
            cardImage.sprite = data.cardSprite;
        }

        RefreshVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameManager == null || netCard == null || cardData == null) return;

        gameManager.ToggleSelectedCard(netCard.instanceId);
    }

    public void SetBasePosition(Vector2 pos)
    {
        basePosition = pos;
        basePositionCaptured = true;
    }

    public void RefreshVisual()
    {
        if (gameManager == null || netCard == null) return;

        bool isSelected = gameManager.IsCardSelected(netCard.instanceId);
        bool isValid = gameManager.IsCardValidToPlay(netCard.instanceId);

        if (cardImage != null)
        {
            cardImage.color = isSelected ? selectedColor : normalColor;
        }

        if (validVisual != null)
        {
            // Show aura if valid and not already selected
            validVisual.SetActive(isValid && !isSelected);
        }

        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null && basePositionCaptured)
        {
            Vector2 pos = basePosition;
            if (isSelected)
            {
                pos.y += selectedLift;
            }
            rect.anchoredPosition = pos;
        }
    }
}
