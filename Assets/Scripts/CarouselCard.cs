using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class CarouselCard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public string sceneName;

    Vector2 downPos;
    float clickThreshold = 10f; // pixels

    public void OnPointerDown(PointerEventData eventData)
    {
        downPos = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        float dist = Vector2.Distance(downPos, eventData.position);

        if (dist < clickThreshold)
        {
            Debug.Log("Card clicked → Load " + sceneName);
            SceneManager.LoadScene(sceneName);
        }
    }
}
