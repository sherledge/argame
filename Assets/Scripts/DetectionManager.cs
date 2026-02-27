using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DetectionManager : MonoBehaviour
{
    [Header("Pre-Detection Loading Overlay")]
    public GameObject loadingOverlay;        
    public float minimumLoadingTime = 1.5f;  

    private bool devGameStarted = false;

    [Header("Dev / Debug")]
    public bool skipDetection = false;

    [Header("Player UI")]
    public PlayerLobbyUI leftPlayerUI;
    public PlayerLobbyUI rightPlayerUI;
    public TextMeshProUGUI countdownText;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip playersReadyClip; 
    
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

    private enum AppState { Loading, PoseDetection, GameRunning }
    private AppState currentState = AppState.Loading;

    private bool countdownStarted = false;
    private bool slideLogoUp = false;
    private bool slideLogoDown = false;
    private Vector2 logoTargetPosition;
    private Vector2 logoOffscreenPosition;

    private List<Vector3[]> _currentPoses = new List<Vector3[]>();
    private bool detectionEnabled = false;
    public void OnDetectionLoadingFinished() { }
    void Start()
    {
        countdownText.gameObject.SetActive(false);
        gamePanel.SetActive(false);
        detectionPanel.SetActive(false); 

        if (loadingOverlay != null) loadingOverlay.SetActive(true); 

        if (logoTransition != null)
        {
            logoOffscreenPosition = new Vector2(0, Screen.height);
            logoTransition.anchoredPosition = logoOffscreenPosition;
            logoTransition.gameObject.SetActive(false);
        }

        // Set default names for Lobby Skeletons
        if(leftPlayerUI != null) leftPlayerUI.SetPlayerName("Player 1");
        if(rightPlayerUI != null) rightPlayerUI.SetPlayerName("Player 2");

        StartCoroutine(LoadingSequence());
    }

    IEnumerator LoadingSequence()
    {
        if (skipDetection)
        {
            if (loadingOverlay != null) loadingOverlay.SetActive(false);
            DevStartGameImmediately();
            yield break;
        }

        if (loadingOverlay != null) loadingOverlay.SetActive(true);
        yield return new WaitForSeconds(minimumLoadingTime);
        
        while (poseProvider == null) yield return null;

        if (loadingOverlay != null) loadingOverlay.SetActive(false);

        // Go straight to pose detection
        EnablePoseDetection(); 
    }

    void EnablePoseDetection()
    {
        currentState = AppState.PoseDetection;
        detectionPanel.SetActive(true);
        detectionEnabled = true; 
    }

    void Update()
    {
        if (skipDetection && currentState != AppState.GameRunning)
        {
            StopAllCoroutines(); 
            if(loadingOverlay != null) loadingOverlay.SetActive(false);
            DevStartGameImmediately();
            return;
        }

        if (currentState != AppState.PoseDetection) return;
        if (countdownStarted) return;

        IEnumerable<Vector3[]> poses = poseProvider.GetAllDetectedPoseKeypoints();
        _currentPoses.Clear();
        if (poses != null) _currentPoses.AddRange(poses);

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

        leftPlayerUI.UpdateSkeleton(leftPose);
        rightPlayerUI.UpdateSkeleton(rightPose);

        if (leftPlayerUI.IsFullyReady && rightPlayerUI.IsFullyReady)
        {
            StartCoroutine(StartSequence());
        }
        
        if(slideLogoDown || slideLogoUp) AnimateLogo();
    }

    IEnumerator StartSequence()
    {
        countdownStarted = true;
        if (audioSource != null && playersReadyClip != null) audioSource.PlayOneShot(playersReadyClip);

        yield return new WaitForSeconds(1.0f);

        if (logoTransition != null)
        {
            logoTransition.gameObject.SetActive(true);
            logoTargetPosition = Vector2.zero;
            slideLogoDown = true;
        }
        else StartTheGame();
    }

    void DevStartGameImmediately()
    {
        if (devGameStarted) return;
        devGameStarted = true;
        currentState = AppState.GameRunning;
        detectionPanel.SetActive(false);
        StartTheGame();
    }

    void AnimateLogo()
    {
        if (slideLogoDown)
        {
            logoTransition.anchoredPosition = Vector2.MoveTowards(logoTransition.anchoredPosition, logoTargetPosition, logoSlideSpeed * Time.deltaTime);
            if (logoTransition.anchoredPosition == logoTargetPosition)
            {
                slideLogoDown = false;
                if(loadingBarsAnimator != null) loadingBarsAnimator.StartLoading(OnBarsComplete);
                else OnBarsComplete();
            }
        }

        if (slideLogoUp)
        {
            logoTransition.anchoredPosition = Vector2.MoveTowards(logoTransition.anchoredPosition, logoOffscreenPosition, logoSlideSpeed * Time.deltaTime);
            if (logoTransition.anchoredPosition == logoOffscreenPosition)
            {
                slideLogoUp = false;
                StartTheGame();
            }
        }
    }

    void OnBarsComplete()
    {
        leftPlayerUI.gameObject.SetActive(false);
        rightPlayerUI.gameObject.SetActive(false);
        slideLogoUp = true;
    }

    void StartTheGame()
    {
        detectionPanel.SetActive(false);
        gamePanel.SetActive(true);
        var gameStarter = gamePanel.GetComponent<MonoBehaviour>(); 
        if(gameStarter != null) gameStarter.SendMessage("StartGame", SendMessageOptions.DontRequireReceiver);
    }

    public void ResetDetection()
    {
        devGameStarted = false;  
        countdownStarted = false;
        slideLogoUp = false;
        slideLogoDown = false;
        currentState = AppState.Loading; 
        EnablePoseDetection();
        gamePanel.SetActive(false);
    }
}