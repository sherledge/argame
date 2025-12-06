using System.Collections.Generic;
using UnityEngine;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using Mediapipe.Tasks.Vision.PoseLandmarker;

public class PoseDetectionProvider : MonoBehaviour
{
    public PoseLandmarkerRunner runner;

    // Lock object for thread safety
    private readonly object _lock = new object();
    
    // Store the latest result safely
    private PoseLandmarkerResult _latestResult;
    private bool _hasData = false;

    private void OnEnable()
    {
        if (runner != null) runner.OnResult += OnPoseResult;
    }

    private void OnDisable()
    {
        if (runner != null) runner.OnResult -= OnPoseResult;
    }

    // Runs on Background Thread (MediaPipe)
// In PoseDetectionProvider.cs inside OnPoseResult
private void OnPoseResult(PoseLandmarkerResult result)
{
    lock (_lock)
    {
        // Don't just assign reference; ensure we aren't holding a list that C++ might clear
        _latestResult = result; 
        _hasData = true;
    }
}

    // Runs on Main Thread (Game)
    public List<Vector3[]> GetAllDetectedPoseKeypoints()
    {
        List<Vector3[]> allPoses = new List<Vector3[]>();

        lock (_lock)
        {
            // Safety Checks
            if (!_hasData || 
                _latestResult.poseLandmarks == null || 
                _latestResult.poseLandmarks.Count == 0) 
            {
                return allPoses;
            }

            foreach (var pose in _latestResult.poseLandmarks)
            {
                if (pose.landmarks == null || pose.landmarks.Count == 0) continue;

                // Create array of exactly 33 points (Standard BlazePose)
                // We use 33 because BlazePose always has 33 landmarks.
                int count = pose.landmarks.Count;
                Vector3[] landmarks = new Vector3[count];

                for (int i = 0; i < count; i++)
                {
                    var lm = pose.landmarks[i];
                    landmarks[i] = new Vector3((float)lm.x, (float)lm.y, (float)lm.z);
                }

                allPoses.Add(landmarks);
            }
        }

        return allPoses;
    }
}