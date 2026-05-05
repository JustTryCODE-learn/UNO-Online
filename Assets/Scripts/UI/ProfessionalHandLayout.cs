using System.Collections.Generic;
using UnityEngine;

public class ProfessionalHandLayout : MonoBehaviour
{
    [Header("First Card Fixed Position")]
    public Vector2 firstCardPosition = new Vector2(-350f, 0f);

    [Header("Card Layout")]
    public float cardSpacing = 55f;

    [Header("Curve")]
    public float curveHeight = 25f;
    public float maxRotation = 10f;
    public float handWidth = 700f;

    [Header("Scale")]
    public float normalScale = 1f;

    [Header("Locking")]
    public bool lockThisContainer = true;
    public bool lockParentChain = true;

    private RectTransform thisRect;

    private Vector2 lockedAnchoredPosition;
    private Vector2 lockedSizeDelta;
    private Vector2 lockedAnchorMin;
    private Vector2 lockedAnchorMax;
    private Vector2 lockedPivot;

    private readonly List<RectTransform> parentRects = new List<RectTransform>();
    private readonly List<Vector2> parentAnchoredPositions = new List<Vector2>();
    private readonly List<Vector2> parentSizeDeltas = new List<Vector2>();
    private readonly List<Vector2> parentAnchorMins = new List<Vector2>();
    private readonly List<Vector2> parentAnchorMaxs = new List<Vector2>();
    private readonly List<Vector2> parentPivots = new List<Vector2>();

    private bool saved = false;

    private void Awake()
    {
        SaveLockedTransforms();
    }

    private void LateUpdate()
    {
        RestoreLockedTransforms();
    }

    public void SaveLockedTransforms()
    {
        thisRect = GetComponent<RectTransform>();

        if (thisRect != null)
        {
            lockedAnchoredPosition = thisRect.anchoredPosition;
            lockedSizeDelta = thisRect.sizeDelta;
            lockedAnchorMin = thisRect.anchorMin;
            lockedAnchorMax = thisRect.anchorMax;
            lockedPivot = thisRect.pivot;
        }

        parentRects.Clear();
        parentAnchoredPositions.Clear();
        parentSizeDeltas.Clear();
        parentAnchorMins.Clear();
        parentAnchorMaxs.Clear();
        parentPivots.Clear();

        if (lockParentChain)
        {
            Transform current = transform.parent;

            while (current != null)
            {
                RectTransform rect = current.GetComponent<RectTransform>();

                if (rect != null)
                {
                    parentRects.Add(rect);
                    parentAnchoredPositions.Add(rect.anchoredPosition);
                    parentSizeDeltas.Add(rect.sizeDelta);
                    parentAnchorMins.Add(rect.anchorMin);
                    parentAnchorMaxs.Add(rect.anchorMax);
                    parentPivots.Add(rect.pivot);
                }

                Canvas canvas = current.GetComponent<Canvas>();

                if (canvas != null)
                {
                    break;
                }

                current = current.parent;
            }
        }

        saved = true;
    }

    private void RestoreLockedTransforms()
    {
        if (!saved)
        {
            return;
        }

        if (lockThisContainer && thisRect != null)
        {
            thisRect.anchoredPosition = lockedAnchoredPosition;
            thisRect.sizeDelta = lockedSizeDelta;
            thisRect.anchorMin = lockedAnchorMin;
            thisRect.anchorMax = lockedAnchorMax;
            thisRect.pivot = lockedPivot;
        }

        if (lockParentChain)
        {
            for (int i = 0; i < parentRects.Count; i++)
            {
                if (parentRects[i] == null)
                {
                    continue;
                }

                parentRects[i].anchoredPosition = parentAnchoredPositions[i];
                parentRects[i].sizeDelta = parentSizeDeltas[i];
                parentRects[i].anchorMin = parentAnchorMins[i];
                parentRects[i].anchorMax = parentAnchorMaxs[i];
                parentRects[i].pivot = parentPivots[i];
            }
        }
    }

    public void ArrangeCards()
    {
        RestoreLockedTransforms();

        int count = transform.childCount;

        if (count == 0)
        {
            return;
        }

        if (thisRect == null)
        {
            thisRect = GetComponent<RectTransform>();
        }

        float containerWidth = thisRect != null && thisRect.rect.width > 0 ? thisRect.rect.width : handWidth;
        float cardWidth = 0f;
        
        if (count > 0)
        {
            RectTransform firstCardRect = transform.GetChild(0).GetComponent<RectTransform>();
            if (firstCardRect != null)
            {
                cardWidth = firstCardRect.rect.width * normalScale;
            }
        }

        float availableWidth = containerWidth - cardWidth;
        if (availableWidth < 0) availableWidth = 0;

        float actualSpacing = cardSpacing;

        if (count > 1)
        {
            float maxSpacing = availableWidth / (count - 1);
            if (actualSpacing > maxSpacing)
            {
                actualSpacing = maxSpacing;
            }
        }

        float totalWidth = (count - 1) * actualSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            Transform child = transform.GetChild(i);
            RectTransform cardRect = child.GetComponent<RectTransform>();

            if (cardRect == null)
            {
                continue;
            }

            // Force every card to use normal anchored UI positioning.
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);

            float x = startX + i * actualSpacing;

            float normalizedX = Mathf.Clamp(x / (containerWidth * 0.5f), -1f, 1f);

            float y = firstCardPosition.y - Mathf.Abs(normalizedX) * curveHeight;
            float rotation = -normalizedX * maxRotation;

            cardRect.anchoredPosition = new Vector2(x, y);
            cardRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            cardRect.localScale = Vector3.one * normalScale;

            child.SetSiblingIndex(i);

            PhotonUnoCardButton cardButton = child.GetComponent<PhotonUnoCardButton>();
            if (cardButton != null)
            {
                cardButton.SetBasePosition(cardRect.anchoredPosition);
                cardButton.RefreshVisual();
            }

            CardHoverEffect hover = child.GetComponent<CardHoverEffect>();

            if (hover != null)
            {
                hover.CaptureOriginalTransform();
            }
        }

        RestoreLockedTransforms();
    }
}