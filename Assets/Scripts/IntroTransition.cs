using UnityEngine;
using UnityEngine.UI;

public class IntroTransition : MonoBehaviour
{
    public GameObject poseDetectionProvider;
public RectTransform introContent; // Assign this to the parent container (IntroContent)
    public GameObject introPanel;
    public GameObject detectionPanel;
    public float slideSpeed = 1000f;

    // NEW: Reference to the DetectionManager script
    public DetectionManager detectionManager;

    public bool slideUp = false;
    private Vector2 offscreenTarget;

    void Start()
    {
        poseDetectionProvider.SetActive(false);
        offscreenTarget = new Vector2(0, Screen.height); // slide upward off screen

        detectionPanel.SetActive(false);
        // Ensure introPanel is active at start if this script is on it
        introPanel.SetActive(true);

        // IMPORTANT: Ensure DetectionManager is assigned in Inspector
        if (detectionManager == null)
        {
            Debug.LogError("IntroTransition: DetectionManager reference is not set! Please assign it in the Inspector.");
        }
    }

    void Update()
    {
if (slideUp)
{
    introContent.anchoredPosition = Vector2.MoveTowards(
        introContent.anchoredPosition,
        offscreenTarget,
        slideSpeed * Time.deltaTime
    );

    if (introContent.anchoredPosition == offscreenTarget)
    {
        slideUp = false;
        introPanel.SetActive(false);
        detectionPanel.SetActive(true);

        if (detectionManager != null)
        {
            detectionManager.enabled = true;
        }
    }
}

    }

    public void OnStartGamePressed()

    {      
                // --- THIS IS THE FIX ---
        // Activate the GameObject that contains the camera and MediaPipe logic.
        // This will allow its scripts (like PoseLandmarkerRunner) to run their Start() methods and initialize the camera.
        poseDetectionProvider.SetActive(true);
        // --- END OF FIX ---
          detectionManager.enabled = true;

        slideUp = true;
    }
    public void ResetIntro()
{
    // Reset video background position
    introContent.anchoredPosition = Vector2.zero;

    // Make sure intro panel is active
    introPanel.SetActive(true);
    detectionPanel.SetActive(false);
        detectionManager.enabled = false;

    // Stop any ongoing slide animation
    slideUp = false;

    // If you also want to stop pose detection
    poseDetectionProvider.SetActive(false);

    Debug.Log("IntroTransition: Reset to initial state.");
}

}
