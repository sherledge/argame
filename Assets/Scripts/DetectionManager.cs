using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DetectionManager : MonoBehaviour
{
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

    void Start()
    {
        countdownText.gameObject.SetActive(false);
        gamePanel.SetActive(false);

        if (logoTransition != null)
        {
            logoOffscreenPosition = new Vector2(0, Screen.height);
            logoTransition.anchoredPosition = logoOffscreenPosition;
            logoTransition.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        AnimateLogo();

        if (countdownStarted) return;

        // 1. Get Poses
        IEnumerable<Vector3[]> poses = poseProvider.GetAllDetectedPoseKeypoints();
        _currentPoses.Clear();
        if (poses != null) _currentPoses.AddRange(poses);

        // 2. Separate Poses Left vs Right
        Vector3[] leftPose = null;
        Vector3[] rightPose = null;

        foreach (var pose in _currentPoses)
        {
            if (pose != null && pose.Length > 0)
            {
                // Check Nose (index 0) or Hip (index 23/24) for center position
                if (pose[0].x < 0.5f) leftPose = pose;
                else rightPose = pose;
            }
        }

        // 3. Update Visuals & Check Readiness
        // If pose is null, the UI script handles the "Searching" state
        leftPlayerUI.UpdateSkeleton(leftPose);
        rightPlayerUI.UpdateSkeleton(rightPose);

        // 4. Check if we can start game
        if (leftPlayerUI.IsFullyReady && rightPlayerUI.IsFullyReady && !countdownStarted)
        {
            StartCoroutine(StartSequence());
        }
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
        if (logoTransition != null) logoTransition.gameObject.SetActive(false);

        // Prevent camera shutoff
        if (poseProvider != null)
        {
            poseProvider.transform.SetParent(null, true);
        }

        detectionPanel.SetActive(false);
        gamePanel.SetActive(true);

        var gameStarter = gamePanel.GetComponent<IGameStarter>();
        if (gameStarter != null)
        {
            gameStarter.StartGame();
        }
        else
        {
            Debug.LogError("No IGameStarter found on gamePanel!");
        }
    }
    // Add this inside DetectionManager class
public void ResetDetection()
{
    countdownStarted = false;
    slideLogoUp = false;
    slideLogoDown = false;

    // Reset UI visibility
    if (leftPlayerUI != null) 
    {
        leftPlayerUI.gameObject.SetActive(true);
        leftPlayerUI.SetSearchingState();
    }
    
    if (rightPlayerUI != null) 
    {
        rightPlayerUI.gameObject.SetActive(true);
        rightPlayerUI.SetSearchingState();
    }

    if (logoTransition != null)
    {
        logoTransition.gameObject.SetActive(false);
    }
    
    Debug.Log("Detection state reset.");
}
}