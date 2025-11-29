using System.Collections.Generic;
using UnityEngine;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Tasks.Components.Containers;

public class PoseDetectionProvider : MonoBehaviour
{
    public PoseLandmarkerRunner runner;

    private PoseLandmarkerResult _latest;

    private void OnEnable()
    {
        if (runner == null)
        {
            Debug.LogError("PoseLandmarkerRunner missing!");
            return;
        }
        // This will now work because we added the event to the Runner
        runner.OnResult += OnPoseResult;
    }

    private void OnDisable()
    {
        if (runner != null)
            runner.OnResult -= OnPoseResult;
    }

    private void OnPoseResult(PoseLandmarkerResult result)
    {
        _latest = result;
    }

    public bool HasPose()
    {
        // FIX: Removed "_latest != null" check because it is a struct.
        // We only check if the internal list 'poseLandmarks' is valid.
        return _latest.poseLandmarks != null &&
               _latest.poseLandmarks.Count > 0 &&
               _latest.poseLandmarks[0].landmarks != null;
    }

    public IList<NormalizedLandmark> GetFirstPose()
    {
        if (!HasPose())
            return null;

        return _latest.poseLandmarks[0].landmarks;
    }

    public List<Vector3[]> GetAllDetectedPoseKeypoints()
    {
        List<Vector3[]> all = new List<Vector3[]>();

        if (!HasPose())
            return all;

        foreach (var pose in _latest.poseLandmarks)
        {
            // FIX: Removed "pose == null" check because it is a struct.
            if (pose.landmarks == null)
                continue;

            Vector3[] arr = new Vector3[pose.landmarks.Count];
            for (int i = 0; i < pose.landmarks.Count; i++)
            {
                var lm = pose.landmarks[i];
                arr[i] = new Vector3((float)lm.x, (float)lm.y, (float)lm.z);
            }

            all.Add(arr);
        }

        return all;
    }
}