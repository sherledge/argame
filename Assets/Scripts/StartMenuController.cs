using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

// 1. Add this namespace for Android Permissions
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class StartMenuController : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip ropeBreakClip;

    [Header("UI")]
    public Button startButton;

    [Header("Animators")]
    public Animator tugAnimator;
    public Animator playButtonAnimator;
    public Animator arrowAnimator;
    public Animator loadingBarAnimator;
    public Animator GameAnimator;

    [Header("Scene")]
    public string nextSceneName = "MenuScene";

    private bool clicked = false;

    private void Awake()
    {
        if (!PlayerPrefs.HasKey("SFX_ENABLED"))
        {
            PlayerPrefs.SetInt("SFX_ENABLED", 1);
            PlayerPrefs.Save();
        }
    }

    private void Start()
    {
        startButton.onClick.AddListener(OnStartClicked);
        Debug.Log("StartMenuController STARTED");

        // Make sure loading bar is hidden at start
        loadingBarAnimator.gameObject.SetActive(false);
    }

    private void OnStartClicked()
    {
        if (clicked) return;
        clicked = true;
        startButton.interactable = false;

        // 2. Start the Permission Check Coroutine instead of proceeding immediately
        StartCoroutine(AskPermissionAndStart());
    }

    // 3. New Coroutine to handle the permission flow
    private IEnumerator AskPermissionAndStart()
    {
#if UNITY_ANDROID
        // Check if we DO NOT have permission yet
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            // Request the permission
            Permission.RequestUserPermission(Permission.Camera);

            // Wait a split second for the dialog to potentially open
            yield return new WaitForSeconds(0.2f);

            // Wait until the user interacts with the dialog and the app regains focus
            yield return new WaitUntil(() => Application.isFocused);
        }
#endif

        // 4. Once permission is handled (or skipped if not Android), proceed
        ProceedWithGameSequence();
    }

    // 5. This contains your original game start logic (Sound + Animation)
    private void ProceedWithGameSequence()
    {
        // 🔊 PLAY ROPE BREAK SOUND HERE
        if (IsSfxEnabled() && sfxSource != null && ropeBreakClip != null)
        {
            sfxSource.PlayOneShot(ropeBreakClip);
        }

        StartCoroutine(PlayTransitionSequence());
    }

    bool IsSfxEnabled()
    {
        return PlayerPrefs.GetInt("SFX_ENABLED", 1) == 1;
    }

    private IEnumerator PlayTransitionSequence()
    {
        // 1️⃣ Tug breaks
        tugAnimator.SetTrigger("Break");

        // Wait for tug break animation (adjust time to your clip length)
        yield return new WaitForSeconds(1.2f);

        // 2️⃣ Button & arrow exit
        playButtonAnimator.SetTrigger("Exit");
        arrowAnimator.SetTrigger("Exit");
        GameAnimator.SetTrigger("Exit");

        yield return new WaitForSeconds(0.3f);

        // FORCE HIDE
        arrowAnimator.gameObject.SetActive(false);
        GameAnimator.gameObject.SetActive(false);

        // 3️⃣ Show loading bar
        loadingBarAnimator.gameObject.SetActive(true);
        loadingBarAnimator.SetTrigger("Show");

        // 4️⃣ Wait for loading animation to finish
        yield return new WaitForSeconds(1.5f); // length of loading animation

        // 5️⃣ Load next scene
        SceneManager.LoadScene(nextSceneName);
    }
}