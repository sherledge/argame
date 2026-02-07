using UnityEngine;
using UnityEngine.UI; // 1. Added this namespace for Button

public class PanelController : MonoBehaviour
{
    [Header("UI Components")]
    public CanvasGroup canvasGroup;
    public Animator animator;
    public Button closeButton; // 2. Assign your Close Button here in the Inspector

    private void Start()
    {
        // 3. Automatically tell the button to call Hide() when clicked
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
    }

    public void Show()
    {
        Debug.Log("SHOW CALLED");

        if (animator == null)
        {
            Debug.LogError("Animator reference is NULL");
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("Animator has NO controller assigned");
            return;
        }

        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        animator.ResetTrigger("Hide");
        animator.SetTrigger("Show");
    }

    public void Hide()
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        animator.SetTrigger("Hide");
    }

    public void OnHidden()
    {
        canvasGroup.alpha = 0;
    }
    
    // Good practice to remove listeners if the object is destroyed
    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
        }
    }
}