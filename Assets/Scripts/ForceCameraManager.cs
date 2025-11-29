using UnityEngine;
using UnityEngine.UI;
using System.Collections;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class ForceCameraManager : MonoBehaviour
{
    public static ForceCameraManager Instance { get; private set; }

    [Header("Assign in Inspector")]
    [Tooltip("The RawImage on your UI that will show the camera feed.")]
    public RawImage cameraFeedDisplay;

    private WebCamTexture _activeWebCamTexture;
    private bool _isCameraReady = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        StartCoroutine(InitializeCamera());
    }

    void OnDisable()
    {
        if (_activeWebCamTexture != null && _activeWebCamTexture.isPlaying)
        {
            _activeWebCamTexture.Stop();
        }
        _isCameraReady = false;
    }

    private IEnumerator InitializeCamera()
    {
        Debug.Log("<b>[ForceCameraManager]</b> Starting camera initialization...");
        _isCameraReady = false;

        if (cameraFeedDisplay == null)
        {
            Debug.LogError("<b>[ForceCameraManager]</b> FATAL: Camera Feed Display (RawImage) is not assigned in the Inspector!");
            yield break;
        }

        // --- THIS IS THE FINAL FIX ---
        // We must request both Camera and Storage permissions for the app to function.
        yield return RequestAllPermissions();
        // --- END OF FIX ---

        float timeout = 5.0f;
        float elapsedTime = 0f;
        while (WebCamTexture.devices.Length == 0 && elapsedTime < timeout)
        {
            Debug.Log("<b>[ForceCameraManager]</b> Waiting for camera devices to be listed by OS...");
            yield return new WaitForSeconds(0.1f);
            elapsedTime += 0.1f;
        }

        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogError("<b>[ForceCameraManager]</b> FATAL: No camera devices found on this device.");
            yield break;
        }

        WebCamDevice? selfieCamera = null;
        foreach (var device in WebCamTexture.devices)
        {
            if (device.name.Equals("Camera 1", System.StringComparison.OrdinalIgnoreCase))
            {
                selfieCamera = device;
                break;
            }
        }

        if (selfieCamera == null)
        {
            Debug.LogWarning("<b>[ForceCameraManager]</b> Could not find 'Camera 1'. Defaulting to first front-facing camera.");
            foreach (var device in WebCamTexture.devices) { if (device.isFrontFacing) { selfieCamera = device; break; } }
            if (selfieCamera == null) { selfieCamera = WebCamTexture.devices[0]; }
        }

        Debug.Log($"<b>[ForceCameraManager]</b> Initializing device: {selfieCamera?.name}");
        _activeWebCamTexture = new WebCamTexture(selfieCamera?.name, 1280, 720, 30);
        
        cameraFeedDisplay.texture = _activeWebCamTexture;
        cameraFeedDisplay.color = Color.white;
        
        _activeWebCamTexture.Play();
        Debug.Log("<b>[ForceCameraManager]</b> Play() has been called.");

        elapsedTime = 0;
        while (!_activeWebCamTexture.didUpdateThisFrame && elapsedTime < timeout)
        {
            yield return null;
            elapsedTime += Time.deltaTime;
        }

        if (_activeWebCamTexture.isPlaying && _activeWebCamTexture.didUpdateThisFrame)
        {
            _isCameraReady = true;
            Debug.Log($"<b>[ForceCameraManager]</b> SUCCESS! Camera is live and ready. Resolution: {_activeWebCamTexture.width}x{_activeWebCamTexture.height}");
        }
        else
        {
            Debug.LogError("<b>[ForceCameraManager]</b> FAILED to start camera within the timeout period.");
        }
    }
    
    // New method to handle all required permissions
    private IEnumerator RequestAllPermissions()
    {
        // Camera Permission
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.Log("<b>[ForceCameraManager]</b> Requesting camera permission...");
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }
        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.Log("<b>[ForceCameraManager]</b> Camera permission granted.");
        }
        else
        {
            Debug.LogError("<b>[ForceCameraManager]</b> FATAL: Camera permission was denied.");
            yield break; // Stop if camera is denied
        }

        // Storage Permissions (for Android)
        #if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Debug.Log("<b>[ForceCameraManager]</b> Requesting storage write permission...");
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            yield return new WaitForSeconds(0.5f); // Give time for the user to see the dialog
        }
        if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Debug.Log("<b>[ForceCameraManager]</b> Storage write permission granted.");
        }
        else
        {
            Debug.LogWarning("<b>[ForceCameraManager]</b> Storage write permission was denied. Video saving will fail.");
        }
        #endif
    }

    void Update()
    {
        if (!_isCameraReady) return;

        float videoAspectRatio = (float)_activeWebCamTexture.width / (float)_activeWebCamTexture.height;
        var fitter = cameraFeedDisplay.GetComponent<AspectRatioFitter>();
        if (fitter != null)
        {
            fitter.aspectRatio = videoAspectRatio;
        }
        cameraFeedDisplay.rectTransform.localEulerAngles = new Vector3(0, 0, -_activeWebCamTexture.videoRotationAngle);
        cameraFeedDisplay.rectTransform.localScale = new Vector3(-1, 1, 1);
    }

    public Texture GetLiveCameraTexture()
    {
        return _isCameraReady ? _activeWebCamTexture : null;
    }
}
