using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CarouselController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform container;
    public List<RectTransform> cards;

    [Header("Layout")]
[Header("Layout")]
public float cardWidth = 633f;
public float gap = 80f;

public float centerScale = 1.1f;
public float sideScale = 0.85f;
public float snapSpeed = 12f;

float CardSpacing => cardWidth + gap;

    private float targetX;
    private bool dragging;
    private int currentIndex = 0;

    void Start()
    {
        targetX = container.anchoredPosition.x;
        UpdateVisuals();
    }

    void Update()
    {
        if (!dragging)
        {
            container.anchoredPosition = Vector2.Lerp(
                container.anchoredPosition,
                new Vector2(targetX, 0),
                Time.deltaTime * snapSpeed
            );
        }

        UpdateVisuals();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("Dragging " + eventData.delta);
        container.anchoredPosition += new Vector2(eventData.delta.x, 0);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        SnapToClosest();
    }

    void SnapToClosest()
    {
        float minDist = float.MaxValue;
        int bestIndex = 0;

        for (int i = 0; i < cards.Count; i++)
        {
            float cardX = -i * CardSpacing;
            float dist = Mathf.Abs(container.anchoredPosition.x - cardX);

            if (dist < minDist)
            {
                minDist = dist;
                bestIndex = i;
            }
        }

        currentIndex = bestIndex;
        targetX = -currentIndex * CardSpacing;
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            float cardWorldX = container.anchoredPosition.x + i * CardSpacing;
            float t = Mathf.Clamp01(Mathf.Abs(cardWorldX) / CardSpacing);

            float scale = Mathf.Lerp(centerScale, sideScale, t);
            cards[i].localScale = Vector3.one * scale;
        }
    }
}
