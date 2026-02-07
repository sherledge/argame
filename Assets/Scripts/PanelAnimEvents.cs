using UnityEngine;

public class PanelAnimEvents : MonoBehaviour
{
    public CanvasGroup canvasGroup;

    // This MUST be public and parameterless
    public void OnHidden()
    {
        canvasGroup.alpha = 0;
    }
}
