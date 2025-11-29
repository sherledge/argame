using UnityEngine;
using System.Collections;
using TMPro; // For TMP_Text

public class LoadingBarsAnimator : MonoBehaviour
{
    public GameObject[] loadingBars; // Assign all 8 bars in order
    public float delayBetweenBars = 0.1f;
    public TMP_Text percentageText;  // Assign in Inspector
    public System.Action OnLoadingComplete; // Optional callback

public void StartLoading(System.Action callback)
{
    OnLoadingComplete = callback;
    gameObject.SetActive(true);

    // Reset all bars
    foreach (GameObject bar in loadingBars)
    {
        bar.SetActive(false);
    }

    // Reset percentage text
    if (percentageText != null)
        percentageText.text = "0%";

    StartCoroutine(AnimateBars());
}


    private IEnumerator AnimateBars()
    {
        int totalBars = loadingBars.Length;

        for (int i = 0; i < totalBars; i++)
        {
            loadingBars[i].SetActive(true);

            // Update percentage
            float percentage = ((i + 1) / (float)totalBars) * 100f;
            if (percentageText != null)
                percentageText.text = $"{Mathf.RoundToInt(percentage)}%";

            yield return new WaitForSeconds(delayBetweenBars);
        }

        // 100% fallback just in case
        if (percentageText != null)
            percentageText.text = "100%";

        OnLoadingComplete?.Invoke();
    }
}
