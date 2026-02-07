using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

using System.Collections;
using UnityEngine.EventSystems;

public class TutorialPanelController : MonoBehaviour,
    IBeginDragHandler, IEndDragHandler
{
    [Header("Swipe Settings")]
public float swipeThreshold = 100f;

private Vector2 dragStartPos;

    [Header("Transition Settings")]
public float transitionDuration = 0.3f;
public float slideOffset = 800f; // pixels from right

    [Header("Slides")]
    public List<GameObject> slides;

    [Header("Dots")]
    public List<Image> dots;
    public Color activeDotColor = Color.white;
    public Color inactiveDotColor = Color.gray;

    [Header("Buttons")]
    public Button nextButton;
    public Button doneButton;
    public Button closeButton;

[Header("Tutorial Target")]
public MonoBehaviour tutorialListener; // Any game
private ITutorialListener listener;


    private int currentIndex = 0;

void Start()
{
    listener = tutorialListener as ITutorialListener;

    if (listener == null)
        Debug.LogError("Tutorial listener does not implement ITutorialListener");

    slides[0].SetActive(true);
    UpdateDots();
    UpdateButtons();

    nextButton.onClick.AddListener(NextSlide);
    doneButton.onClick.AddListener(FinishTutorial);
    closeButton.onClick.AddListener(FinishTutorial);
}

void Update()
{
    if (Input.GetMouseButtonDown(0))
        dragStartPos = Input.mousePosition;

    if (Input.GetMouseButtonUp(0))
    {
        float deltaX = Input.mousePosition.x - dragStartPos.x;

        if (Mathf.Abs(deltaX) > swipeThreshold)
        {
            if (deltaX < 0) NextSlide();
            else PreviousSlide();
        }
    }
}

  public void OnBeginDrag(PointerEventData eventData)
{
    Debug.Log("Swipe started");
    dragStartPos = eventData.position;
}

public void OnEndDrag(PointerEventData eventData)
{
    Debug.Log("Swipe ended");
    float deltaX = eventData.position.x - dragStartPos.x;

    Debug.Log("Swipe deltaX: " + deltaX);

    if (Mathf.Abs(deltaX) < swipeThreshold)
        return;

    if (deltaX < 0)
        NextSlide();
    else
        PreviousSlide();
}

void PreviousSlide()
{
    if (currentIndex > 0)
        ShowSlide(currentIndex - 1);
}

void ShowSlide(int newIndex)
{
    if (newIndex == currentIndex) return;

    StartCoroutine(AnimateSlideTransition(currentIndex, newIndex));
    currentIndex = newIndex;

    UpdateDots();
    UpdateButtons();
}
IEnumerator AnimateSlideTransition(int oldIndex, int newIndex)
{
    GameObject oldSlide = slides[oldIndex];
    GameObject newSlide = slides[newIndex];

    CanvasGroup oldGroup = oldSlide.GetComponent<CanvasGroup>();
    CanvasGroup newGroup = newSlide.GetComponent<CanvasGroup>();

    RectTransform oldRect = oldSlide.GetComponent<RectTransform>();
    RectTransform newRect = newSlide.GetComponent<RectTransform>();

    newSlide.SetActive(true);

    newGroup.alpha = 0f;
    newRect.anchoredPosition = new Vector2(slideOffset, 0);

    float t = 0f;

    while (t < transitionDuration)
    {
        t += Time.deltaTime;
        float p = t / transitionDuration;

        oldGroup.alpha = Mathf.Lerp(1, 0, p);
        newGroup.alpha = Mathf.Lerp(0, 1, p);

        oldRect.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(-slideOffset, 0), p);
        newRect.anchoredPosition = Vector2.Lerp(new Vector2(slideOffset, 0), Vector2.zero, p);

        yield return null;
    }

    oldSlide.SetActive(false);
    oldRect.anchoredPosition = Vector2.zero;
    oldGroup.alpha = 1f;
}
void UpdateButtons()
{
    bool isLast = currentIndex == slides.Count - 1;
    nextButton.gameObject.SetActive(!isLast);
    doneButton.gameObject.SetActive(isLast);
}
void UpdateDots()
{
    for (int i = 0; i < dots.Count; i++)
        dots[i].color = (i == currentIndex) ? activeDotColor : inactiveDotColor;
}

    void NextSlide()
    {
        if (currentIndex < slides.Count - 1)
            ShowSlide(currentIndex + 1);
    }

    void FinishTutorial()
{
    gameObject.SetActive(false);
    listener?.OnTutorialFinished();
}

}
