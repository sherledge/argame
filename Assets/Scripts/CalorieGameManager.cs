using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CalorieGameManager : MonoBehaviour, IGameStarter
{
    // --- Final Results Storage ---
    private Texture2D player1FinalImage;
    private Texture2D player2FinalImage;

    [Header("Animation References")]
    public Animator player1Panda; // Drag Player 1's Panda GameObject (with Animator) here
    public Animator player2Panda; // Drag Player 2's Panda GameObject (with Animator) here

    [Header("UI Elements")]
    public TMP_Text timerText;
    public RawImage liveCameraFeedRawImage;
    public RawImage player1ImageDisplay;
    public RawImage player2ImageDisplay;
    public TMP_Text player1ScoreText; // Displays Calories for P1
    public TMP_Text player2ScoreText; // Displays Calories for P2

    [Header("Dependencies")]
    public PoseDetectionProvider poseProvider;
    public ResultsPanelManager resultsPanelManager;

    [Header("Game Settings")]
    public float gameDuration = 60f; // How long the workout lasts in seconds
    public float caloriesPerJump = 0.5f; // How many calories credited per jump
    
    [Header("Jump Detection Settings")]
    [Tooltip("How much Y movement is required to trigger a jump")]
    public float jumpThreshold = 0.05f; 
    [Tooltip("Time in seconds before another jump can be registered (prevents double counting)")]
    public float jumpCooldown = 0.5f;

    [Header("Panel References")]
    public GameObject gamePanel;

    // --- State Variables ---
    private float currentTime;
    private float player1Calories = 0;
    private float player2Calories = 0;
    private bool poseFeedReady = false;
    private bool isGameActive = false;
    private Texture2D finalThumbnail;

    // Jump Tracking Variables
    private float p1LastHipY;
    private float p2LastHipY;
    private float p1JumpTimer;
    private float p2JumpTimer;

    // Data Structures matching your Pose Provider
    [System.Serializable]
    public class KeypointData { public float x, y, z, visibility; }

    public void StartGame()
    {
        ResetGame();
        player1ImageDisplay.gameObject.SetActive(false);
        player2ImageDisplay.gameObject.SetActive(false);
        timerText.gameObject.SetActive(false);
        
        StartCoroutine(WaitForPoseFeed());
    }

    public void ResetGame()
    {
        currentTime = gameDuration;
        player1Calories = 0;
        player2Calories = 0;
        poseFeedReady = false;
        isGameActive = false;
        
        // Reset Jump Tracking
        p1JumpTimer = 0;
        p2JumpTimer = 0;
        p1LastHipY = 0;
        p2LastHipY = 0;

        // Cleanup Textures
        if (finalThumbnail != null) { Destroy(finalThumbnail); finalThumbnail = null; }
        if (player1FinalImage != null) { Destroy(player1FinalImage); player1FinalImage = null; }
        if (player2FinalImage != null) { Destroy(player2FinalImage); player2FinalImage = null; }

        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        player1ScoreText.text = $"{player1Calories:F1} Kcal";
        player2ScoreText.text = $"{player2Calories:F1} Kcal";
    }

    void TransitionToResultsPanel()
    {
        isGameActive = false;
        gamePanel.SetActive(false);

        // Capture the final moment for the results screen
        finalThumbnail = CaptureFromRawImage(liveCameraFeedRawImage);
        SplitAndAssignFinalImages(finalThumbnail);

        resultsPanelManager.ShowResults(
            Mathf.FloorToInt(player1Calories), 
            Mathf.FloorToInt(player2Calories), 
            player1FinalImage, 
            player2FinalImage
        );
    }

    IEnumerator WaitForPoseFeed()
    {
        Debug.Log("Waiting for camera feed...");
        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            bool textureReady = liveCameraFeedRawImage != null && liveCameraFeedRawImage.texture != null;
            if (textureReady)
            {
                Debug.Log("Camera feed ready!");
                poseFeedReady = true;
                break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (poseFeedReady)
        {
            yield return new WaitForSeconds(0.5f); // Let pose detection warm up
            StartGameplay();
        }
        else
        {
            Debug.LogError("FATAL: Camera feed not ready.");
            // Handle error state or return to menu
        }
    }

    public void StartGameplay()
    {
        player1ImageDisplay.gameObject.SetActive(true);
        player2ImageDisplay.gameObject.SetActive(true);
        timerText.gameObject.SetActive(true);
        
        // Setup initial texture display just to ensure they are visible
        // In this mode, we might just show the raw feed in the back, 
        // but if you want separate cropped feeds, we can do that in Update.
        
        isGameActive = true;
    }

    void Update()
    {
        if (!isGameActive || !poseFeedReady) return;

        // 1. Update Timer
        currentTime -= Time.deltaTime;
        timerText.text = Mathf.CeilToInt(currentTime).ToString();

        if (currentTime <= 0)
        {
            currentTime = 0;
            TransitionToResultsPanel();
            return;
        }

        // 2. Process Pose Data
        List<Vector3[]> currentDetectedPoses = poseProvider.GetAllDetectedPoseKeypoints()?.ToList();
        
        if (currentDetectedPoses != null && currentDetectedPoses.Count > 0)
        {
            // Sort poses: P1 is usually Right on screen (Left physically), P2 is Left on screen
            // Sorting by X coordinate
            var sortedPoses = currentDetectedPoses.OrderBy(p => p[0].x).ToList();

            Vector3[] p1Pose = null;
            Vector3[] p2Pose = null;

            if (sortedPoses.Count > 1)
            {
                p2Pose = sortedPoses[0]; // Left side of screen (Player 2)
                p1Pose = sortedPoses[1]; // Right side of screen (Player 1)
            }
            else if (sortedPoses.Count == 1)
            {
                // Guess based on position
                if (sortedPoses[0][0].x > 0.5f) p1Pose = sortedPoses[0];
                else p2Pose = sortedPoses[0];
            }

            // 3. Detect Jumps
            if (p1Pose != null) DetectJump(p1Pose, 1);
            if (p2Pose != null) DetectJump(p2Pose, 2);
        }

        // 4. Decrease Jump Cooldowns
        if (p1JumpTimer > 0) p1JumpTimer -= Time.deltaTime;
        if (p2JumpTimer > 0) p2JumpTimer -= Time.deltaTime;
    }

    // Logic to detect a vertical jump based on Hip movement
    void DetectJump(Vector3[] poseLandmarks, int playerID)
    {
        // MediaPipe Pose landmarks 23 and 24 are hips. 
        // Index 0 is nose. 
        // Let's use the average of hips (indices 23 and 24) if available, or just the nose (0) if simplified.
        // Assuming your pose provider returns standard MediaPipe 33 landmarks.
        // If your array is smaller, adjust indices.
        
        float currentY = 0f;

        // Safety check for array length (Standard MP has 33 points)
        if (poseLandmarks.Length > 24)
        {
            // Average of Left Hip (23) and Right Hip (24)
            currentY = (poseLandmarks[23].y + poseLandmarks[24].y) / 2f;
        }
        else
        {
            // Fallback to Nose
            currentY = poseLandmarks[0].y;
        }

        float lastY = (playerID == 1) ? p1LastHipY : p2LastHipY;
        float cooldown = (playerID == 1) ? p1JumpTimer : p2JumpTimer;

        // Note: In many Normalized coordinates, Y=0 is Top, Y=1 is Bottom.
        // So a Jump means Y value DECREASES. 
        // We check the absolute delta to be safe, or specifically check "Up".
        
        // Calculate difference
        float deltaY = lastY - currentY; // Positive if moving UP (towards 0)

        // If cooldown is 0 AND we moved up significantly
        if (cooldown <= 0 && deltaY > jumpThreshold)
        {
            // JUMP DETECTED!
            RegisterJump(playerID);
        }

        // Update tracking
        if (playerID == 1) p1LastHipY = currentY;
        else p2LastHipY = currentY;
    }

    void RegisterJump(int playerID)
    {
        if (playerID == 1)
        {
            player1Calories += caloriesPerJump;
            p1JumpTimer = jumpCooldown;
            if (player1Panda != null) player1Panda.SetTrigger("Jump");
        }
        else
        {
            player2Calories += caloriesPerJump;
            p2JumpTimer = jumpCooldown;
            if (player2Panda != null) player2Panda.SetTrigger("Jump");
        }
        
        UpdateScoreUI();
    }

    #region Helper Methods

    // Split the final image for the results screen
    void SplitAndAssignFinalImages(Texture2D source)
    {
        if (source == null) return;

        int width = source.width;
        int height = source.height;
        int halfWidth = width / 2;

        // Player 1 (Right side in mirrored feed)
        Texture2D p1Tex = new Texture2D(halfWidth, height, source.format, false);
        p1Tex.SetPixels(source.GetPixels(halfWidth, 0, halfWidth, height));
        p1Tex.Apply();

        // Player 2 (Left side in mirrored feed)
        Texture2D p2Tex = new Texture2D(halfWidth, height, source.format, false);
        p2Tex.SetPixels(source.GetPixels(0, 0, halfWidth, height));
        p2Tex.Apply();

        player1FinalImage = p1Tex;
        player2FinalImage = p2Tex;
    }

    Texture2D CaptureFromRawImage(RawImage rawImage)
    {
        if (rawImage == null || rawImage.texture == null) return null;
        Texture sourceTexture = rawImage.texture;
        RenderTexture renderTex = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.sRGB);
        Graphics.Blit(sourceTexture, renderTex);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTex;
        Texture2D capturedTex = new Texture2D(renderTex.width, renderTex.height, TextureFormat.RGBA32, false);
        capturedTex.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        capturedTex.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);
        return capturedTex;
    }

    #endregion
}