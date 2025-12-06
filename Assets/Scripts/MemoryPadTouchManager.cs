using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MemoryPadTouchManager : MonoBehaviour
{
    [Header("References")]
    public PoseDetectionProvider poseProvider;
    public MemoryGamePanelManager memoryGamePanelManager;

    [Header("UI")]
    public Canvas uiCanvas;                  // your MemoryGameCanvas
    public RectTransform cameraFeedRect;     // RectTransform of the RawImage that shows the camera

    [Header("Player 1 Pads (Right side)")]
    public RectTransform p1RedPad;
    public RectTransform p1BluePad;
    public RectTransform p1GreenPad;
    public RectTransform p1YellowPad;

    [Header("Player 2 Pads (Left side)")]
    public RectTransform p2RedPad;
    public RectTransform p2BluePad;
    public RectTransform p2GreenPad;
    public RectTransform p2YellowPad;

    [Header("Camera")]
    public Camera uiCamera;                  // if Canvas = Screen Space - Camera, assign that camera

    [Header("Tuning")]
    [Tooltip("How far (in pixels) the hand can be from pad center to count as a hit.")]
    public float hitRadius = 200f;

    [Tooltip("If your Y is flipped, toggle this.")]
    public bool invertY = true;

    [Header("Debug")]
    [Tooltip("Small UI Image prefab to show the hand points.")]
    public RectTransform debugHandPointPrefab;
    private readonly List<RectTransform> _debugPoints = new List<RectTransform>();

    // Pose landmark indices (for MediaPipe Pose):
    // 15=L.Wrist, 17=L.Pinky, 19=L.Index, 21=L.Thumb
    // 16=R.Wrist, 18=R.Pinky, 20=R.Index, 22=R.Thumb
    private readonly int[] LEFT_HAND_INDICES  = { 15, 17, 19, 21 };
    private readonly int[] RIGHT_HAND_INDICES = { 16, 18, 20, 22 };

    private void Update()
    {
        if (poseProvider == null || memoryGamePanelManager == null || uiCanvas == null)
            return;

        HideDebugPoints();

        List<Vector3[]> poses = null;
        try
        {
            var tmp = poseProvider.GetAllDetectedPoseKeypoints();
            if (tmp != null)
                poses = tmp.ToList();
        }
        catch
        {
            return;
        }

        if (poses == null || poses.Count == 0)
            return;

        // Sort players: left on screen = P2, right = P1 (like your mimic game)
        var sortedPoses = poses
            .Where(p => p != null && p.Length > 22)
            .OrderBy(p => p[0].x)
            .ToList();

        Vector3[] p2Pose = null;
        Vector3[] p1Pose = null;

        if (sortedPoses.Count > 1)
        {
            p2Pose = sortedPoses[0];
            p1Pose = sortedPoses[1];
        }
        else if (sortedPoses.Count == 1)
        {
            if (sortedPoses[0][0].x > 0.5f) p1Pose = sortedPoses[0];
            else p2Pose = sortedPoses[0];
        }

        if (p1Pose != null) CheckHandCloud(1, p1Pose);
        if (p2Pose != null) CheckHandCloud(2, p2Pose);
    }

    private void CheckHandCloud(int playerIndex, Vector3[] pose)
    {
        CheckIndicesAgainstPads(playerIndex, pose, LEFT_HAND_INDICES);
        CheckIndicesAgainstPads(playerIndex, pose, RIGHT_HAND_INDICES);
    }

    private void CheckIndicesAgainstPads(int playerIndex, Vector3[] pose, int[] indicesToCheck)
    {
        foreach (int index in indicesToCheck)
        {
            if (index < 0 || index >= pose.Length)
                continue;

            Vector2 normPos = new Vector2(pose[index].x, pose[index].y);

            // Convert normalized → screen **via camera feed rect**
            Vector2 screenPos = NormalizedToScreenViaFeed(normPos);

            ShowDebugPoint(screenPos);

            CheckPointOnPads(playerIndex, screenPos);
        }
    }

    /// <summary>
    /// Converts normalized coordinates (0..1 relative to the camera image)
    /// into screen-space, using the camera feed RectTransform for proper alignment.
    /// </summary>
    private Vector2 NormalizedToScreenViaFeed(Vector2 norm)
    {
        if (cameraFeedRect == null)
        {
            // fallback: whole screen
            float x = norm.x * Screen.width;
            float y = (invertY ? (1f - norm.y) : norm.y) * Screen.height;
            return new Vector2(x, y);
        }

        // 1. Get local position inside the camera feed rect (pivot=center)
        float feedWidth = cameraFeedRect.rect.width;
        float feedHeight = cameraFeedRect.rect.height;

        // norm.x, norm.y are [0,1] within the original image
        float localX = (norm.x - 0.5f) * feedWidth;
        float localY;

        if (invertY)
        {
            // if your pose y=0 is top, y=1 bottom, we invert when mapping into UI
            localY = ((1f - norm.y) - 0.5f) * feedHeight;
        }
        else
        {
            localY = (norm.y - 0.5f) * feedHeight;
        }

        Vector3 localPosInFeed = new Vector3(localX, localY, 0f);

        // 2. Convert local point in feed → world
        Vector3 worldPos = cameraFeedRect.TransformPoint(localPosInFeed);

        // 3. World → screen
        Camera camForUI = (uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : uiCamera;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(camForUI, worldPos);
        return screenPos;
    }

      private void CheckPointOnPads(int playerIndex, Vector2 screenPos)
    {
        if (uiCanvas == null) return;

        if (playerIndex == 1)
        {
            if (IsPointNearPad(p1RedPad, screenPos))
                memoryGamePanelManager.OnPlayerColorTouched(1, MemoryGamePanelManager.MemoryColor.Red);

            if (IsPointNearPad(p1BluePad, screenPos))
                memoryGamePanelManager.OnPlayerColorTouched(1, MemoryGamePanelManager.MemoryColor.Blue);

            if (IsPointNearPad(p1GreenPad, screenPos))
                memoryGamePanelManager.OnPlayerColorTouched(1, MemoryGamePanelManager.MemoryColor.Green);

            if (IsPointNearPad(p1YellowPad, screenPos))
                memoryGamePanelManager.OnPlayerColorTouched(1, MemoryGamePanelManager.MemoryColor.Yellow);
        }
        else if (playerIndex == 2)
        {
            if (IsPointNearPad(p2RedPad, screenPos))
                memoryGamePanelManager.OnPlayerColorTouched(2, MemoryGamePanelManager.MemoryColor.Red);

            if (IsPointNearPad(p2BluePad, screenPos))
                memoryGamePanelManager.OnPlayerColorTouched(2, MemoryGamePanelManager.MemoryColor.Blue);

            if (IsPointNearPad(p2GreenPad, screenPos))
                memoryGamePanelManager.OnPlayerColorTouched(2, MemoryGamePanelManager.MemoryColor.Green);

            if (IsPointNearPad(p2YellowPad, screenPos))
                memoryGamePanelManager.OnPlayerColorTouched(2, MemoryGamePanelManager.MemoryColor.Yellow);
        }
    }

    private bool IsPointNearPad(RectTransform pad, Vector2 screenPos)
    {
        if (pad == null) return false;

        Camera camForUI = (uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : uiCamera;
        Vector2 padScreenPos = RectTransformUtility.WorldToScreenPoint(camForUI, pad.position);

        float dist = Vector2.Distance(screenPos, padScreenPos);
        return dist <= hitRadius;
    }
    // ---------- DEBUG DOTS ----------

    private void ShowDebugPoint(Vector2 screenPos)
    {
        if (debugHandPointPrefab == null || uiCanvas == null)
            return;

        RectTransform canvasRect = uiCanvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        Vector2 localPos;
        Camera camForUI = (uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : uiCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, camForUI, out localPos);

        RectTransform point = GetNextDebugPoint();
        point.SetParent(canvasRect, false);
        point.anchoredPosition = localPos;
        point.gameObject.SetActive(true);
    }

    private RectTransform GetNextDebugPoint()
    {
        foreach (var p in _debugPoints)
        {
            if (p != null && !p.gameObject.activeSelf)
                return p;
        }

        var newPoint = Instantiate(debugHandPointPrefab);
        _debugPoints.Add(newPoint);
        return newPoint;
    }

    private void HideDebugPoints()
    {
        foreach (var p in _debugPoints)
        {
            if (p != null)
                p.gameObject.SetActive(false);
        }
    }
}
