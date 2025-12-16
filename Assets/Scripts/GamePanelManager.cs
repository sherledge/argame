using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Removed using statements for NatSuite and System.IO

public class GamePanelManager : MonoBehaviour, IGameStarter
{
    private Texture2D player1FinalImage;
    private Texture2D player2FinalImage;

    [Header("Reference Assets")]
    public List<Sprite> referenceSprites;
    public List<TextAsset> referenceJsons;

    [Header("UI Elements")]
    public Image referenceImageDisplay;
    public TMP_Text timerText;
    public RawImage liveCameraFeedRawImage;
    public Image referenceFrameImage;
    public RawImage player1ImageDisplay;
    public RawImage player2ImageDisplay;
    public TMP_Text player1ScoreText;
    public TMP_Text player2ScoreText;

    [Header("Dependencies")]
    public PoseDetectionProvider poseProvider;
    public ResultsPanelManager resultsPanelManager;

    [Header("Game Settings")]
    public int roundsToPlay = 5;
    [Range(0.01f, 5f)]
    public float poseComparisonSensitivity = 0.5f;

    [Header("Panel References")]
    public GameObject gamePanel;
    
    // --- All NatCorder Variables have been removed ---

    // --- State Variables ---
// This should be a direct reference to the GameRecorder script.
    private int currentRound = 0;
    private int player1Score = 0;
    private int player2Score = 0;
    private bool poseFeedReady = false;
    private Texture2D finalThumbnail;

    [System.Serializable]
    public class KeypointData { public float x, y, z, visibility; }
    [System.Serializable]
    public class PoseJsonData { public KeypointData[] normalizedLandmarks; }

    public void StartGame()
    {
        ResetGame();
        referenceImageDisplay.gameObject.SetActive(false);
        player1ImageDisplay.gameObject.SetActive(false);
        player2ImageDisplay.gameObject.SetActive(false);
        timerText.gameObject.SetActive(false);
        
        // The call to StartRecording() has been removed.
        StartCoroutine(WaitForPoseFeed());
    }

    // The entire StartRecording() method has been deleted.

    // This method is no longer async and does not handle video.
// In GamePanelManager.cs

// In GamePanelManager.cs
// --- AFTER ---
void TransitionToResultsPanel()
{
    // 1. Simply tell the recorder to stop.
    // It will automatically handle saving and saving to the gallery via its events.

    
    // 2. The rest of your code remains the same.
    gamePanel.SetActive(false);

    resultsPanelManager.ShowResults(
        player1Score, 
        player2Score, 
        player1FinalImage, 
        player2FinalImage
    );
}

    public void ResetGame()
    {
        currentRound = 0;
        player1Score = 0;
        player2Score = 0;
        poseFeedReady = false;
        if (finalThumbnail != null) { Destroy(finalThumbnail); finalThumbnail = null; }
        if (player1FinalImage != null) { Destroy(player1FinalImage); player1FinalImage = null; }
        if (player2FinalImage != null) { Destroy(player2FinalImage); player2FinalImage = null; }
    }



    IEnumerator WaitForPoseFeed()
    {
        Debug.Log("Waiting for camera feed to become ready...");
        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            bool textureReady = liveCameraFeedRawImage != null && liveCameraFeedRawImage.texture != null;
            if (textureReady)
            {
                Debug.Log("Camera feed texture is ready!");
                poseFeedReady = true;
                break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (poseFeedReady)
        {
            Debug.Log("Giving pose detection a moment to initialize...");
            yield return new WaitForSeconds(0.5f);

        }
        else
        {
            Debug.LogError("FATAL: Camera feed texture was not ready within the timeout period!");
            TransitionToResultsPanel();
        }
        Debug.Log("[GamePanelManager] Pose feed ready, starting round...");
StartRound();

    }
public void StartRound()
{
    StartCoroutine(PlayRound());
}


    IEnumerator PlayRound()
    {
        while (currentRound < roundsToPlay)
        {
            if (currentRound < referenceSprites.Count)
            {
                referenceImageDisplay.sprite = referenceSprites[currentRound];
                referenceImageDisplay.gameObject.SetActive(true);
                referenceFrameImage.gameObject.SetActive(true);
                AnimateReferenceImage();
            }
            yield return new WaitForSeconds(3f);
            referenceImageDisplay.gameObject.SetActive(false);
            referenceFrameImage.gameObject.SetActive(false);
            timerText.gameObject.SetActive(true);
            for (int timer = 3; timer > 0; timer--)
            {
                timerText.text = timer.ToString();
                yield return new WaitForSeconds(1f);
            }
            timerText.text = "Pose!";
            yield return new WaitForSeconds(1f);
            timerText.gameObject.SetActive(false);
            List<Vector3[]> currentDetectedPoses = poseProvider.GetAllDetectedPoseKeypoints()?.ToList();
            Texture2D capturedFullImage = CaptureFromRawImage(liveCameraFeedRawImage);
            if (capturedFullImage != null)
            {
                int width = capturedFullImage.width;
                int height = capturedFullImage.height;
                int halfWidth = width / 2;

// --- FIX START: Swapping the pixel read logic ---
                
                // Player 1 is physically on the LEFT, but in a mirrored webcam, 
                // they appear on the RIGHT side of the texture.
                // So Player 1 needs to grab from halfWidth to width.
                Texture2D player1CroppedTex = new Texture2D(halfWidth, height, capturedFullImage.format, false);
                player1CroppedTex.SetPixels(capturedFullImage.GetPixels(halfWidth, 0, halfWidth, height));
                player1CroppedTex.Apply();

                // Player 2 is physically on the RIGHT, but in a mirrored webcam,
                // they appear on the LEFT side of the texture.
                // So Player 2 needs to grab from 0 to halfWidth.
                Texture2D player2CroppedTex = new Texture2D(halfWidth, height, capturedFullImage.format, false);
                player2CroppedTex.SetPixels(capturedFullImage.GetPixels(0, 0, halfWidth, height));
                player2CroppedTex.Apply();

                // --- FIX END ---

                if (player1FinalImage != null) Destroy(player1FinalImage);
                if (player2FinalImage != null) Destroy(player2FinalImage);

                player1FinalImage = new Texture2D(player1CroppedTex.width, player1CroppedTex.height, player1CroppedTex.format, false);
                player1FinalImage.SetPixels(player1CroppedTex.GetPixels());
                player1FinalImage.Apply();

                player2FinalImage = new Texture2D(player2CroppedTex.width, player2CroppedTex.height, player2CroppedTex.format, false);
                player2FinalImage.SetPixels(player2CroppedTex.GetPixels());
                player2FinalImage.Apply();

                player1ImageDisplay.texture = player1CroppedTex;
                player2ImageDisplay.texture = player2CroppedTex;
                player1ImageDisplay.gameObject.SetActive(true);
                player2ImageDisplay.gameObject.SetActive(true);
                player1ImageDisplay.rectTransform.localScale = new Vector3(-1, 1, 1);
                player2ImageDisplay.rectTransform.localScale = new Vector3(-1, 1, 1);
                Destroy(capturedFullImage);
            }
            Vector3[] targetPose = (currentRound < referenceJsons.Count) ? LoadTargetPose(referenceJsons[currentRound]) : null;
            float p1Match = 0f, p2Match = 0f;
            if (targetPose != null && targetPose.Length > 0 && currentDetectedPoses != null)
            {
                List<Vector3[]> sortedPoses = currentDetectedPoses.Where(p => p != null && p.Length > 0).OrderBy(p => p[0].x).ToList();
                Vector3[] p1Pose = null, p2Pose = null;
                if (sortedPoses.Count > 1)
                {
                    p1Pose = sortedPoses[1];
                    p2Pose = sortedPoses[0];
                }
                else if (sortedPoses.Count == 1)
                {
                    if (sortedPoses[0][0].x > 0.5f) { p1Pose = sortedPoses[0]; }
                    else { p2Pose = sortedPoses[0]; }
                }
                p1Match = ComparePose(p1Pose, targetPose);
                p2Match = ComparePose(p2Pose, targetPose);
            }
            int p1RoundScore = Mathf.RoundToInt(p1Match);
            int p2RoundScore = Mathf.RoundToInt(p2Match);
            player1Score += p1RoundScore;
            player2Score += p2RoundScore;
            player1ScoreText.text = $"{p1RoundScore}%";
            player2ScoreText.text = $"{p2RoundScore}%";
            yield return new WaitForSeconds(3f);
            player1ImageDisplay.gameObject.SetActive(false);
            player2ImageDisplay.gameObject.SetActive(false);
            if (player1ImageDisplay.texture != null) { Destroy(player1ImageDisplay.texture); player1ImageDisplay.texture = null; }
            if (player2ImageDisplay.texture != null) { Destroy(player2ImageDisplay.texture); player2ImageDisplay.texture = null; }
            player1ScoreText.text = "";
            player2ScoreText.text = "";
            
            currentRound++;
        }

        yield return new WaitForEndOfFrame();
        finalThumbnail = CaptureFromRawImage(liveCameraFeedRawImage);
        
        TransitionToResultsPanel();
    }
    
    #region Helper Methods
    void AnimateReferenceImage()
    {
        referenceImageDisplay.transform.localScale = Vector3.zero;
        referenceFrameImage.transform.localScale = Vector3.zero;
        referenceImageDisplay.transform.LeanScale(Vector3.one, 0.5f).setEaseOutBack();
        referenceFrameImage.transform.LeanScale(Vector3.one, 0.5f).setEaseOutBack();
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
       Vector3[] LoadTargetPose(TextAsset jsonAsset)
    {
        if (jsonAsset == null || string.IsNullOrEmpty(jsonAsset.text)) return null;
        try {
            PoseJsonData poseData = JsonUtility.FromJson<PoseJsonData>(jsonAsset.text);
            if (poseData == null || poseData.normalizedLandmarks == null) return null;
            return poseData.normalizedLandmarks.Select(k => new Vector3(k.x, k.y, k.z)).ToArray();
        } catch { return null; }
    }
    
    
    float ComparePose(Vector3[] poseA, Vector3[] poseB)
    {
        if (poseA == null || poseB == null || poseA.Length != poseB.Length || poseA.Length == 0) return 0f;
        float totalDiff = 0f;
        for (int i = 0; i < poseA.Length; i++) {
            totalDiff += Vector3.Distance(poseA[i], poseB[i]);
        }
        float avgDiff = totalDiff / poseA.Length;
        float similarity = Mathf.Max(0f, 1f - avgDiff * poseComparisonSensitivity);
        return similarity * 100f;
    }
    #endregion
}