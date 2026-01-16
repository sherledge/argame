using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class MemoryPadTouchManager : MonoBehaviour
{
    [Header("References")]
    public PoseDetectionProvider poseProvider;
    public MemoryGamePanelManager memoryGamePanelManager;

    [Header("UI")]
    public Canvas uiCanvas;
    public RectTransform cameraFeedRect;
    public Camera uiCamera;

    [Header("Player 1 Pads (Visual Right)")]
    public RectTransform p1RedPad;
    public RectTransform p1BluePad;
    public RectTransform p1GreenPad;
    public RectTransform p1YellowPad;

    [Header("Player 2 Pads (Visual Left)")]
    public RectTransform p2RedPad;
    public RectTransform p2BluePad;
    public RectTransform p2GreenPad;
    public RectTransform p2YellowPad;

    [Header("Tuning")]
    public float hitRadius = 150f;
    public bool invertY = true;

    [Header("Debug")]
    public GameObject debugCursorPrefab;
    private List<GameObject> p1Dots = new List<GameObject>();
    private List<GameObject> p2Dots = new List<GameObject>();

    // Using the full limb set from Reaction Game for better hit detection
    private readonly int[] BODY_INDICES = { 
        15, 17, 19, 21, // Left Hand
        16, 18, 20, 22, // Right Hand
        13, 14,         // Elbows
        23, 24          // Hips (Optional, helps if they lean into a bottom pad)
    };

    private void Start()
    {
        // Initialize Debug Dots
        for (int i = 0; i < BODY_INDICES.Length; i++)
        {
            if (debugCursorPrefab == null) break;
            GameObject d1 = Instantiate(debugCursorPrefab, uiCanvas.transform);
            d1.GetComponent<Image>().color = new Color(1, 0, 0, 0.5f); // Red for P1
            d1.SetActive(false);
            p1Dots.Add(d1);

            GameObject d2 = Instantiate(debugCursorPrefab, uiCanvas.transform);
            d2.GetComponent<Image>().color = new Color(0, 0, 1, 0.5f); // Blue for P2
            d2.SetActive(false);
            p2Dots.Add(d2);
        }
    }

    private void Update()
    {
        var allPoses = poseProvider.GetAllDetectedPoseKeypoints();
        if (allPoses == null || allPoses.Count == 0) return;

        var validPoses = allPoses.Where(p => p != null && p.Length > 22).ToList();
        if (validPoses.Count == 0) return;

        // Sort: lowest X is Screen Left (P2), highest X is Screen Right (P1)
        var sorted = validPoses.OrderBy(p => p[0].x).ToList();

        Vector3[] p2Pose = null; // Right
        Vector3[] p1Pose = null; // Left

        if (sorted.Count == 1)
        {
            if (sorted[0][0].x > 0.5f) p2Pose = sorted[0];
            else p1Pose = sorted[0];
        }
        else
        {
            p1Pose = sorted[0];
            p2Pose = sorted[1];
        }

        // Process Hits
        if (p1Pose != null) ProcessPlayer(1, p1Pose, p1Dots);
        else HideDots(p1Dots);

        if (p2Pose != null) ProcessPlayer(2, p2Pose, p2Dots);
        else HideDots(p2Dots);
    }

    private void ProcessPlayer(int playerIndex, Vector3[] pose, List<GameObject> dots)
    {
        HideDots(dots);

        foreach (int bodyIdx in BODY_INDICES)
        {
            if (bodyIdx >= pose.Length) continue;

            Vector2 normPos = new Vector2(pose[bodyIdx].x, pose[bodyIdx].y);
            Vector2 screenPos = NormalizedToScreenViaFeed(normPos);

            // Update Debug Dot
            int listIdx = System.Array.IndexOf(BODY_INDICES, bodyIdx);
            if (listIdx < dots.Count)
            {
                dots[listIdx].SetActive(true);
                dots[listIdx].transform.position = GetWorldPos(screenPos);
            }

            // Check Pads
            CheckPadCollisions(playerIndex, screenPos);
        }
    }

    private void CheckPadCollisions(int playerIndex, Vector2 handScreenPos)
    {
        // Get the pads belonging to THIS player
        RectTransform red = (playerIndex == 1) ? p1RedPad : p2RedPad;
        RectTransform blue = (playerIndex == 1) ? p1BluePad : p2BluePad;
        RectTransform green = (playerIndex == 1) ? p1GreenPad : p2GreenPad;
        RectTransform yellow = (playerIndex == 1) ? p1YellowPad : p2YellowPad;

        if (IsHit(red, handScreenPos)) memoryGamePanelManager.OnPlayerColorTouched(playerIndex, MemoryGamePanelManager.MemoryColor.Red);
        if (IsHit(blue, handScreenPos)) memoryGamePanelManager.OnPlayerColorTouched(playerIndex, MemoryGamePanelManager.MemoryColor.Blue);
        if (IsHit(green, handScreenPos)) memoryGamePanelManager.OnPlayerColorTouched(playerIndex, MemoryGamePanelManager.MemoryColor.Green);
        if (IsHit(yellow, handScreenPos)) memoryGamePanelManager.OnPlayerColorTouched(playerIndex, MemoryGamePanelManager.MemoryColor.Yellow);
    }

    private bool IsHit(RectTransform pad, Vector2 handScreenPos)
    {
        if (pad == null || !pad.gameObject.activeInHierarchy) return false;
        
        Vector2 padScreenPos = GetScreenPos(pad.position);
        return Vector2.Distance(handScreenPos, padScreenPos) <= hitRadius;
    }

    private Vector2 NormalizedToScreenViaFeed(Vector2 norm)
    {
        // Simple direct mapping (matching fixed ReactionGame logic)
        float screenX = norm.x; 
        float correctedY = invertY ? (1f - norm.y) : norm.y;

        if (cameraFeedRect == null)
            return new Vector2(screenX * Screen.width, correctedY * Screen.height);

        float localX = (screenX - 0.5f) * cameraFeedRect.rect.width;
        float localY = (correctedY - 0.5f) * cameraFeedRect.rect.height;

        Vector3 worldPos = cameraFeedRect.TransformPoint(new Vector3(localX, localY, 0));
        return GetScreenPos(worldPos);
    }

    private Vector2 GetScreenPos(Vector3 worldPos)
    {
        Camera cam = (uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : uiCamera;
        return RectTransformUtility.WorldToScreenPoint(cam, worldPos);
    }

    private Vector3 GetWorldPos(Vector2 screenPos)
    {
        Camera cam = (uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : uiCamera;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(uiCanvas.transform as RectTransform, screenPos, cam, out Vector3 worldPos);
        return worldPos;
    }

    private void HideDots(List<GameObject> dots)
    {
        foreach (var d in dots) d.SetActive(false);
    }
}