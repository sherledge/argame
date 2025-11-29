using UnityEngine;
using System.Collections; // Make sure this is included for IEnumerator

[RequireComponent(typeof(CanvasGroup))] // Ensures CanvasGroup is present
public class FadeTextLoop : MonoBehaviour
{
    [Tooltip("Duration of each fade phase (fade in or fade out)")]
    public float fadeDuration = 1f;

    [Tooltip("Optional delay between fade out and fade in cycles")]
    public float delayBetweenFades = 0.5f; 

    private CanvasGroup canvasGroup;

    void Awake() // Use Awake to ensure CanvasGroup is ready before Start
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup component not found on this GameObject. Please add one.", this);
        }
    }

    void OnEnable() // Start the coroutine when the GameObject becomes active
    {
        // Ensure the text starts fully visible (or invisible if you want to fade in first)
        // For a fade out, start visible. For a fade in, start invisible.
        // Let's assume we want to start by fading OUT, so text should be visible.
        canvasGroup.alpha = 1f; 
        StartCoroutine(FadeLoop());
    }

    void OnDisable() // Stop the coroutine when the GameObject is disabled
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