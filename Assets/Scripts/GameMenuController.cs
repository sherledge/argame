using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[Serializable]
public class GameEntry
{
    public string id;
    public string displayName;
    public Sprite thumbnail;
    public string sceneName;
}

public class GameMenuController : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("Game Data")]
    public List<GameEntry> games = new List<GameEntry>();

    [Header("Visual Settings")]
    public float cardWidth = 450f;
    public float cardHeight = 700f;
    public float cardSpacing = 40f;      // visual gap between cards
    public float centerScale = 1.1f;
    public float sideScale = 0.85f;
    public float snapSmoothTime = 0.15f; // how “springy” the snap feels

    private ScrollView _scrollView;
    private VisualElement _cardsContainer;
    private readonly List<VisualElement> _cardElements = new List<VisualElement>();

    private bool _isDragging = false;
    private bool _layoutReady = false;

    private float _targetScrollX = 0f;
    private float _scrollVelocity = 0f;

    private int _currentCenteredIndex = 0;
    private float _contentPaddingLeft = 0f;

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("GameMenuController: UIDocument reference is missing.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        _scrollView = root.Q<ScrollView>("CarouselView");

        if (_scrollView == null)
        {
            Debug.LogError("GameMenuController: Could not find ScrollView 'CarouselView' in UXML.");
            return;
        }

        _cardsContainer = _scrollView.contentContainer;

        // Build cards
        BuildCards();

        // Hide scrollbars
        _scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        _scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;

        // Input events
        _scrollView.RegisterCallback<PointerDownEvent>(OnPointerDown);
        _scrollView.RegisterCallback<PointerUpEvent>(OnPointerUp);

        // Layout event – once we know viewport size we can center things
        root.RegisterCallback<GeometryChangedEvent>(OnLayoutReady);
    }

    private void OnDisable()
    {
        if (_scrollView != null)
        {
            _scrollView.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _scrollView.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }

        if (uiDocument != null)
        {
            uiDocument.rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnLayoutReady);
        }
    }

    private void BuildCards()
    {
        _cardsContainer.Clear();
        _cardElements.Clear();

        foreach (var game in games)
        {
            var card = CreateCardVisualElement(game);
            _cardsContainer.Add(card);
            _cardElements.Add(card);
        }
    }

    private VisualElement CreateCardVisualElement(GameEntry game)
    {
        var card = new VisualElement();
        card.AddToClassList("game-card");
        card.userData = game; // store data for click

        // Apply size + spacing from C#
        card.style.width = cardWidth;
        card.style.height = cardHeight;

        // We use margin-right as spacing, margin-left = 0
        card.style.marginLeft = 0f;
        card.style.marginRight = cardSpacing;

        // IMAGE
        var img = new UnityEngine.UIElements.Image();
        img.AddToClassList("game-card__image");

        if (game.thumbnail != null)
        {
            img.sprite = game.thumbnail;
        }

        // FOOTER
        var footer = new VisualElement();
        footer.AddToClassList("game-card__footer");

        var title = new Label(game.displayName);
        title.AddToClassList("game-card__title");
        footer.Add(title);

        card.Add(img);
        card.Add(footer);

        // On click: if centered → launch, else snap to it
        card.RegisterCallback<ClickEvent>(evt =>
        {
            int index = _cardElements.IndexOf(card);
            if (index < 0) return;

            if (index == _currentCenteredIndex)
            {
                LaunchGame(card);
            }
            else
            {
                SnapToIndex(index);
            }
        });

        return card;
    }

    private void LaunchGame(VisualElement card)
    {
        var data = card.userData as GameEntry;
        if (data == null)
            return;

        Debug.Log($"[GameMenu] Launching scene: {data.sceneName} ({data.displayName})");

        if (!string.IsNullOrEmpty(data.sceneName))
        {
            SceneManager.LoadScene(data.sceneName);
        }
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        _isDragging = true;
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        _isDragging = false;
        SnapToClosestCard();
    }

    private void OnLayoutReady(GeometryChangedEvent evt)
    {
        if (_scrollView == null || _cardElements.Count == 0)
            return;

        if (_scrollView.contentViewport.resolvedStyle.width <= 0f)
            return;

        float viewportWidth = _scrollView.contentViewport.resolvedStyle.width;

        // Center first and last cards by adding padding to content container
        float padding = Mathf.Max(0f, (viewportWidth - cardWidth) * 0.5f);
        _contentPaddingLeft = padding;

        _cardsContainer.style.paddingLeft = padding;
        _cardsContainer.style.paddingRight = padding;

        _layoutReady = true;

        // After layout is ready, snap to the first card so it’s nicely centered
        SnapToIndex(0, instant: true);
        UpdateCardVisuals();
    }

    private void Update()
    {
        if (_scrollView == null || !_layoutReady || _cardElements.Count == 0)
            return;

        // Smooth snapping when NOT dragging
        if (!_isDragging)
        {
            float currentX = _scrollView.scrollOffset.x;
            float newX = Mathf.SmoothDamp(currentX, _targetScrollX, ref _scrollVelocity, snapSmoothTime);

            if (!float.IsNaN(newX) && !float.IsInfinity(newX))
            {
                _scrollView.scrollOffset = new Vector2(newX, 0f);
            }
        }

        UpdateCardVisuals();
    }

    private void UpdateCardVisuals()
    {
        if (_cardElements.Count == 0 || !_layoutReady)
            return;

        float viewportWidth = _scrollView.contentViewport.layout.width;
        float viewportCenter = _scrollView.scrollOffset.x + viewportWidth * 0.5f;

        float itemFullWidth = cardWidth + cardSpacing;

        float bestDistance = float.MaxValue;
        int bestIndex = 0;

        for (int i = 0; i < _cardElements.Count; i++)
        {
            var card = _cardElements[i];

            // Compute card center purely by index math
            float cardCenter = _contentPaddingLeft + i * itemFullWidth + cardWidth * 0.5f;
            float distance = Mathf.Abs(viewportCenter - cardCenter);

            // Normalized distance used for scale/opacity
            float maxRange = itemFullWidth * 2f;
            float t = Mathf.Clamp01(distance / maxRange);

            float scaleValue = Mathf.Lerp(centerScale, sideScale, t);
            float alphaValue = Mathf.Lerp(1f, 0.5f, t);

            if (float.IsNaN(scaleValue) || float.IsInfinity(scaleValue))
                scaleValue = 1f;

            card.style.scale = new Scale(new Vector3(scaleValue, scaleValue, 1f));
            card.style.opacity = alphaValue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        // Update active card highlighting
        _currentCenteredIndex = bestIndex;
        for (int i = 0; i < _cardElements.Count; i++)
        {
            var card = _cardElements[i];
            if (i == _currentCenteredIndex)
                card.AddToClassList("game-card--active");
            else
                card.RemoveFromClassList("game-card--active");
        }
    }

    private void SnapToClosestCard()
    {
        if (_cardElements.Count == 0 || !_layoutReady)
            return;

        float viewportWidth = _scrollView.contentViewport.layout.width;
        float viewportCenter = _scrollView.scrollOffset.x + viewportWidth * 0.5f;

        float itemFullWidth = cardWidth + cardSpacing;

        float bestDistance = float.MaxValue;
        int bestIndex = 0;

        for (int i = 0; i < _cardElements.Count; i++)
        {
            float cardCenter = _contentPaddingLeft + i * itemFullWidth + cardWidth * 0.5f;
            float distance = Mathf.Abs(viewportCenter - cardCenter);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        SnapToIndex(bestIndex);
    }

    private void SnapToIndex(int index, bool instant = false)
    {
        if (!_layoutReady || index < 0 || index >= _cardElements.Count)
            return;

        float itemFullWidth = cardWidth + cardSpacing;

        // Thanks to how we set padding, index * itemFullWidth is enough
        _targetScrollX = index * itemFullWidth;

        if (instant)
        {
            _scrollView.scrollOffset = new Vector2(_targetScrollX, 0f);
        }
    }
}
