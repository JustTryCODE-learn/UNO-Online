using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverLift = 45f;
    public float hoverScale = 1.15f;

    private RectTransform rectTransform;

    private Vector2 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private int originalSiblingIndex;

    private bool captured = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void CaptureOriginalTransform()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        originalPosition = rectTransform.anchoredPosition;
        originalRotation = rectTransform.localRotation;
        originalScale = rectTransform.localScale;
        originalSiblingIndex = transform.GetSiblingIndex();

        captured = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (rectTransform == null)
        {
            return;
        }

        if (!captured)
        {
            CaptureOriginalTransform();
        }

        // Bring hovered card to front temporarily
        transform.SetAsLastSibling();

        rectTransform.anchoredPosition = originalPosition + new Vector2(0f, hoverLift);
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localRotation = originalRotation;
        rectTransform.localScale = originalScale;

        // Return to original layer/order
        transform.SetSiblingIndex(originalSiblingIndex);
    }
}