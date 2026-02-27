using UnityEngine;
using System.Collections;
[RequireComponent(typeof(CanvasGroup))]
public class FadeTextLoop : MonoBehaviour
{
    [Tooltip("Duration of each fade phase (fade in or fade out)")]
    public float fadeDuration = 1f;

    [Tooltip("Optional delay between fade out and fade in cycles")]
    public float delayBetweenFades = 0.5f; 

    private CanvasGroup canvasGroup;

    void Awake() 
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup component not found on this GameObject. Please add one.", this);
        }
    }

    void OnEnable() 
    {
     
        canvasGroup.alpha = 1f; 
        StartCoroutine(FadeLoop());
    }

    void OnDisable() 
    {
        StopAllCoroutines(); 
    }

    private IEnumerator FadeLoop()
    {
        while (true) // Infinite loop for continuous fading
        {
            // --- Fade Out ---
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup.alpha, 0f, fadeDuration));

            // Optional delay after fading out
            if (delayBetweenFades > 0)
            {
                yield return new WaitForSeconds(delayBetweenFades);
            }

            // --- Fade In ---
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup.alpha, 1f, fadeDuration));

            // Optional delay after fading in
            if (delayBetweenFades > 0)
            {
                yield return new WaitForSeconds(delayBetweenFades);
            }
        }
    }

    // A reusable helper method for fading a CanvasGroup
    private IEnumerator FadeCanvasGroup(float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            yield return null; // Wait for the next frame
        }
        canvasGroup.alpha = endAlpha; // Ensure final alpha is set precisely
    }
}