using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
    
public class DetectionManager : MonoBehaviour
{
    [Header("UI Elements")]
    public RawImage leftOverlay;
    public RawImage rightOverlay;
    public TextMeshProUGUI leftPromptText;
    public TextMeshProUGUI rightPromptText;
    public TextMeshProUGUI countdownText;


    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip leftDetectedClip;
    public AudioClip rightDetectedClip;

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

    public bool leftReady = false;
    public bool rightReady = false;
    public bool countdownStarted = false;

    private bool leftPulseStarted = false;
    private bool rightPulseStarted = false;
    private bool leftConfettiPlayed = false;
    private bool rightConfettiPlayed = false;
    private bool leftSoundPlayed = false;
    private bool rightSoundPlayed = false;

    private Coroutine leftPulseCoroutine;
    private Coroutine rightPulseCoroutine;

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

        //this.enabled = false;
        //Debug.Log("DetectionManager: Script disabled at Start.");
    }
void Update()
{
    AnimateLogo();

    if (countdownStarted) return;

    leftReady = false;
    rightReady = true;

    IEnumerable<Vector3[]> poses = poseProvider.GetAllDetectedPoseKeypoints();
    _currentPoses.Clear();

    if (poses != null)
    {
        _currentPoses.AddRange(poses);
    }

    if (_currentPoses.Count == 0)
    {
        UpdateUI();
        return;
    }
foreach (var pose in _currentPoses)
    {
        if (pose != null && pose.Length > 0)
        {
            // FIX START: 
            // x < 0.5f is the LEFT side of the screen (0.0 to 0.5)
            // x > 0.5f is the RIGHT side of the screen (0.5 to 1.0)
            
            if (pose[0].x < 0.5f) 
            {
                leftReady = true;
            }
            else 
            {
                rightReady = true;
            }
            // FIX END
        }
    }

    UpdateUI();

    if (leftReady && rightReady && !countdownStarted)
    {
        Debug.Log("Both players ready. Starting logo transition.");
        countdownStarted = true;

        if (logoTransition != null)
        {
            logoTransition.gameObject.SetActive(true);
            logoTargetPosition = Vector2.zero;
            slideLogoDown = true;
        }
    }
}

    void UpdateUI()
    {
        // LEFT
        if (leftReady)
        {
            if (!leftPulseStarted)
            {
                leftPulseCoroutine = StartCoroutine(PulseOverlay(leftOverlay));
                leftPulseStarted = true;

                leftPromptText.text = "🟢 Ready!";

                if (!leftSoundPlayed && audioSource != null && leftDetectedClip != null)
                {
                    audioSource.PlayOneShot(leftDetectedClip);
                    leftSoundPlayed = true;

#if UNITY_ANDROID || UNITY_IOS
                    Handheld.Vibrate(); // Replace if deprecated
#endif
                }
            }
        }
        else
        {
            if (leftPulseStarted)
            {
                if (leftPulseCoroutine != null)
                {
                    StopCoroutine(leftPulseCoroutine);
                    leftPulseCoroutine = null;
                }

                leftOverlay.color = new Color(0, 0, 0, 0.6f);
                leftPromptText.text = "🕵️ Searching...";
                leftPulseStarted = false;
                leftConfettiPlayed = false;
                leftSoundPlayed = false;
            }
        }

        // RIGHT
        if (rightReady)
        {
            if (!rightPulseStarted)
            {
                rightPulseCoroutine = StartCoroutine(PulseOverlay(rightOverlay));
                rightPulseStarted = true;

                rightPromptText.text = "🟢 Ready!";

                if (!rightSoundPlayed && audioSource != null && rightDetectedClip != null)
                {
                    audioSource.PlayOneShot(rightDetectedClip);
                    rightSoundPlayed = true;

#if UNITY_ANDROID || UNITY_IOS
                    Handheld.Vibrate();
#endif
                }
            }
        }
        else
        {
            if (rightPulseStarted)
            {
                if (rightPulseCoroutine != null)
                {
                    StopCoroutine(rightPulseCoroutine);
                    rightPulseCoroutine = null;
                }

                rightOverlay.color = new Color(0, 0, 0, 0.6f);
                rightPromptText.text = "🕵️ Searching...";
                rightPulseStarted = false;
                rightConfettiPlayed = false;
                rightSoundPlayed = false;
            }
        }
    }

    IEnumerator PulseOverlay(RawImage overlay)
    {
        while (true)
        {
            for (float a = 0.3f; a <= 0.6f; a += Time.deltaTime)
            {
                overlay.color = new Color(0, 1, 0, a);
                yield return null;
            }
            for (float a = 0.6f; a >= 0.3f; a -= Time.deltaTime)
            {
                overlay.color = new Color(0, 1, 0, a);
                yield return null;
            }
        }
    }

    void AnimateLogo()
    {
        if (slideLogoDown)
        {
            logoTransition.anchoredPosition = Vector2.MoveTowards(
                logoTransition.anchoredPosition,
                logoTargetPosition,
                logoSlideSpeed * Time.deltaTime
            );

            if (logoTransition.anchoredPosition == logoTargetPosition)
            {
                slideLogoDown = false;
                loadingBarsAnimator.StartLoading(OnBarsComplete);
            }
        }

        if (slideLogoUp)
        {
            logoTransition.anchoredPosition = Vector2.MoveTowards(
                logoTransition.anchoredPosition,
                logoOffscreenPosition,
                logoSlideSpeed * Time.deltaTime
            );

            if (logoTransition.anchoredPosition == logoOffscreenPosition)
            {
                slideLogoUp = false;
                StartTheGame();
            }
        }
    }

    void OnBarsComplete()
    {
        Debug.Log("Loading bars complete. Sliding logo up.");
        leftOverlay.gameObject.SetActive(false);
        rightOverlay.gameObject.SetActive(false);

        slideLogoUp = true;
    }

void StartTheGame()
{
    Debug.Log("Logo slide up complete. Starting game panel.");
    if (logoTransition != null) logoTransition.gameObject.SetActive(false);

    // --- THIS IS THE FINAL FIX ---
    // To prevent the camera from being turned off when we disable the detectionPanel,
    // we make the poseProvider a top-level object in the scene by setting its parent to null.
    // This ensures it stays active during the panel switch.
    if (poseProvider != null)
    {
        poseProvider.transform.SetParent(null, true);
    }
    // --- END OF FIX --

    detectionPanel.SetActive(false);
    gamePanel.SetActive(true);

    var gamePanelManager = gamePanel.GetComponent<CalorieGameManager>();
    if (gamePanelManager != null)
    {
        Debug.Log("🎮 GamePanelManager: Starting game via StartGame()");
        gamePanelManager.StartGame();
    }
    else
    {
        Debug.LogError("GamePanelManager not found on gamePanel.");
    }
}


}
