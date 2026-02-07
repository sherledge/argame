using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DetectionManager : MonoBehaviour
{
    [Header("Pre-Detection Loading Overlay")]
public GameObject loadingOverlay;          // full-screen animation panel
public float minimumLoadingTime = 1.5f;    // safety delay (seconds)

    private bool devGameStarted = false;

    [Header("Dev / Debug")]
public bool skipDetection = true;

    [Header("New Player UI")]
    public PlayerLobbyUI leftPlayerUI;
    public PlayerLobbyUI rightPlayerUI;
    public TextMeshProUGUI countdownText;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip playersReadyClip; // Changed from individual clips to one "Both Ready" clip
    
    [Header("Loading Bar Animation")]
    public LoadingBarsAnimator loadingBarsAnimator;

    [Header("Panel References")]
    public GameObject detectionPanel;
    public GameObject gamePanel;

    [Header("Logo Transition Elements")]
    public RectTransform logoTransition;
    public float logoSlideSpeed = 2000f;

    [Header("Dependencies")]
    public PoseDetectionProvider poseProvider;

    // Logic State
    private bool countdownStarted = false;
    private bool slideLogoUp = false;
    private bool slideLogoDown = false;
    private Vector2 logoTargetPosition;
    private Vector2 logoOffscreenPosition;

    private List<Vector3[]> _currentPoses = new List<Vector3[]>();
private bool detectionEnabled = false;

void Start()
{
    countdownText.gameObject.SetActive(false);
    gamePanel.SetActive(false);

    detectionEnabled = false;        // BLOCK detection
    detectionPanel.SetActive(false); // Hide detection

    if (loadingOverlay != null)
        loadingOverlay.SetActive(true); // SHOW loading animation

    if (logoTransition != null)
    {
        logoOffscreenPosition = new Vector2(0, Screen.height);
        logoTransition.anchoredPosition = logoOffscreenPosition;
        logoTransition.gameObject.SetActive(false);
    }
}

IEnumerator LoadingSequence()
{
    // Show loading overlay
    if (loadingOverlay != null)
        loadingOverlay.SetActive(true);

    // Wait minimum time (prevents instant flash)
    yield return new WaitForSeconds(minimumLoadingTime);

    // OPTIONAL: Wait until camera feed exists
    while (poseProvider == null ||
           poseProvider.GetAllDetectedPoseKeypoints() == null)
    {
        yield return null;
    }

    // Hide loading overlay
    if (loadingOverlay != null)
        loadingOverlay.SetActive(false);

    // Enable detection
    detectionEnabled = true;

    // Show detection UI
    if (detectionPanel != null)
        detectionPanel.SetActive(true);

    Debug.Log("Loading complete → Detection enabled");
}


void Update()
{
    if (!detectionEnabled)
        return;

    if (skipDetection)
    {
        DevStartGameImmediately();
        return;
    }

    if (countdownStarted) return;



    // 1. Get poses
    IEnumerable<Vector3[]> poses = poseProvider.GetAllDetectedPoseKeypoints();
    _currentPoses.Clear();
    if (poses != null) _currentPoses.AddRange(poses);

    // 2. Split left / right
    Vector3[] leftPose = null;
    Vector3[] rightPose = null;

    foreach (var pose in _currentPoses)
    {
        if (pose != null && pose.Length > 0)
        {
            if (pose[0].x < 0.5f) leftPose = pose;
            else rightPose = pose;
        }
    }

    // 3. Update UI
    leftPlayerUI.UpdateSkeleton(leftPose);
    rightPlayerUI.UpdateSkeleton(rightPose);

    // 4. Start game immediately when ready
    if (leftPlayerUI.IsFullyReady && rightPlayerUI.IsFullyReady)
    {
        StartTheGame();
        countdownStarted = true; // hard stop
    }
}
public void OnDetectionLoadingFinished()
{
    Debug.LogError("🔥🔥🔥 ANIMATION CALLBACK FIRED 🔥🔥🔥");

    if (loadingOverlay != null)
        loadingOverlay.SetActive(false);

    detectionEnabled = true;
    detectionPanel.SetActive(true);
}



    IEnumerator StartSequence()
    {
        countdownStarted = true;
        Debug.Log("Both players visible and fully detected!");

        if (audioSource != null && playersReadyClip != null)
        {
            audioSource.PlayOneShot(playersReadyClip);
        }

        // Short delay to let them read "Perfect!"
        yield return new WaitForSeconds(1.0f);

        if (logoTransition != null)
        {
            logoTransition.gameObject.SetActive(true);
            logoTargetPosition = Vector2.zero;
            slideLogoDown = true;
        }
    }
void DevStartGameImmediately()
{
    if (devGameStarted) return;   // ✅ HARD STOP

    devGameStarted = true;

    Debug.Log("DEV MODE: Skipping detection and starting game");

    detectionPanel.SetActive(false);
    gamePanel.SetActive(true);



    var gameStarter = gamePanel.GetComponent<IGameStarter>();
    if (gameStarter != null)
        gameStarter.StartGame();
}


    void AnimateLogo()
    {
        if (slideLogoDown)
        {
            logoTransition.anchoredPosition = Vector2.MoveTowards(
                logoTransition.anchoredPosition, logoTargetPosition, logoSlideSpeed * Time.deltaTime);

            if (logoTransition.anchoredPosition == logoTargetPosition)
            {
                slideLogoDown = false;
                loadingBarsAnimator.StartLoading(OnBarsComplete);
            }
        }

        if (slideLogoUp)
        {
            logoTransition.anchoredPosition = Vector2.MoveTowards(
                logoTransition.anchoredPosition, logoOffscreenPosition, logoSlideSpeed * Time.deltaTime);

            if (logoTransition.anchoredPosition == logoOffscreenPosition)
            {
                slideLogoUp = false;
                StartTheGame();
            }
        }
    }

    void OnBarsComplete()
    {
        // Hide the lobby UI before sliding up
        leftPlayerUI.gameObject.SetActive(false);
        rightPlayerUI.gameObject.SetActive(false);
        slideLogoUp = true;
    }

void StartTheGame()
{
    detectionPanel.SetActive(false);
    gamePanel.SetActive(true);

    var gameStarter = gamePanel.GetComponent<IGameStarter>();
    if (gameStarter != null)
        gameStarter.StartGame();
    else
        Debug.LogError("No IGameStarter found on gamePanel!");
}

    // Add this inside DetectionManager class
public void ResetDetection()
{
    devGameStarted = false;   // ✅ IMPORTANT

    countdownStarted = false;
    slideLogoUp = false;
    slideLogoDown = false;

    detectionPanel.SetActive(true);
    gamePanel.SetActive(false);

    Debug.Log("Detection state reset.");
}

}