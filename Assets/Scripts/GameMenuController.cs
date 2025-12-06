using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[Serializable]
public class GameEntry
{
    public string id;
    public string sceneName;
    public string displayName;
    public Sprite thumbnail;
}

public class GameMenuController : MonoBehaviour
{
    [Header("UI Toolkit")]
    public UIDocument uiDocument;

    [Header("Configuration")]
    public List<GameEntry> games = new List<GameEntry>();
    
    // You can adjust these in Inspector now!
    public float cardWidth = 400f; 
    public float cardHeight = 600f;

    [Header("Scene Navigation")]
    public string settingsSceneName;
    public string creditsSceneName;

    private ScrollView _scrollView;
    private VisualElement _scrollContainer;
    
    // Track the currently selected card
    private VisualElement _selectedCardElement;
    private GameEntry _selectedGameEntry;

    private void OnEnable()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;

        _scrollView = root.Q<ScrollView>("CarouselView");
        if (_scrollView == null) return;

        var settingsBtn = root.Q<Button>("SettingsButton");
        var creditsBtn = root.Q<Button>("CreditsButton");

        if (settingsBtn != null) settingsBtn.clicked += () => LoadSceneSafe(settingsSceneName);
        if (creditsBtn != null) creditsBtn.clicked += () => LoadSceneSafe(creditsSceneName);

        // Hide scrollbars for cleaner look
        _scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        _scrollContainer = _scrollView.contentContainer;

        GenerateCards();
    }

    private void GenerateCards()
    {
        _scrollContainer.Clear();

        foreach (var entry in games)
        {
            var card = new VisualElement();
            card.AddToClassList("game-card");

            // --- Apply Size from Inspector ---
            card.style.width = cardWidth;
            card.style.height = cardHeight;
            // Prevent shrinking
            card.style.flexShrink = 0; 

            // Image
            var img = new Image();
            img.AddToClassList("game-card__image");
            if (entry.thumbnail != null) img.sprite = entry.thumbnail;

            // Footer
            var footer = new VisualElement();
            footer.AddToClassList("game-card__footer");
            var label = new Label(entry.displayName);
            label.AddToClassList("game-card__title");

            footer.Add(label);
            card.Add(img);
            card.Add(footer);

            // CLICK EVENT
            card.RegisterCallback<ClickEvent>(evt => OnCardClicked(card, entry));

            _scrollContainer.Add(card);
        }
    }

    private void OnCardClicked(VisualElement clickedCard, GameEntry entry)
    {
        // 1. If we clicked the ALREADY selected card, Play the game
        if (_selectedCardElement == clickedCard)
        {
            Debug.Log($"Starting Game: {entry.displayName}");
            LoadSceneSafe(entry.sceneName);
            return;
        }

        // 2. Otherwise, Select this card (Highlight it)
        SelectCard(clickedCard, entry);
    }

    private void SelectCard(VisualElement card, GameEntry entry)
    {
        // Deselect previous
        if (_selectedCardElement != null)
        {
            _selectedCardElement.RemoveFromClassList("game-card--selected");
        }

        // Select new
        _selectedCardElement = card;
        _selectedGameEntry = entry;
        
        if (_selectedCardElement != null)
        {
            _selectedCardElement.AddToClassList("game-card--selected");
            
            // Optional: Ensure it's fully visible
            _scrollView.ScrollTo(card);
        }
    }

    private void LoadSceneSafe(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogWarning("Scene name is empty!");
    }
}