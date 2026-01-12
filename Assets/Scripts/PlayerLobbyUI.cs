using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class PlayerLobbyUI : MonoBehaviour
{
    [Header("Monkey & Bubble")]
    public TextMeshProUGUI speechBubbleText;
    public GameObject monkeyImage; // Optional: To animate/bounce the monkey later

    [Header("Skeleton Visuals (Images)")]
    // Drag white stickman part images here. They will turn green when detected.
    public Image headImage;
    public Image torsoImage;
    public Image leftArmImage;
    public Image rightArmImage;
    public Image leftLegImage;
    public Image rightLegImage;

    [Header("Settings")]
    public Color missingColor = Color.white;
    public Color detectedColor = Color.green;

    // Internal state
    public bool IsFullyReady { get; private set; } = false;

    // Helper to reset UI when no player is found
    public void SetSearchingState()
    {
        SetColor(headImage, missingColor);
        SetColor(torsoImage, missingColor);
        SetColor(leftArmImage, missingColor);
        SetColor(rightArmImage, missingColor);
        SetColor(leftLegImage, missingColor);
        SetColor(rightLegImage, missingColor);

        speechBubbleText.text = "Where are you? Come closer!";
        IsFullyReady = false;
    }

    public void UpdateSkeleton(Vector3[] landmarks)
    {
        if (landmarks == null || landmarks.Length == 0)
        {
            SetSearchingState();
            return;
        }

        // --- 1. Check Visibility of Key Parts ---
        // BlazePose Landmark mapping (simplified):
        // 0=Nose, 11=L_Shoulder, 12=R_Shoulder, 15=L_Wrist, 16=R_Wrist, 
        // 23=L_Hip, 24=R_Hip, 27=L_Ankle, 28=R_Ankle

        bool headVis = IsVisible(landmarks, 0);
        bool lArmVis = IsVisible(landmarks, 11) && IsVisible(landmarks, 15);
        bool rArmVis = IsVisible(landmarks, 12) && IsVisible(landmarks, 16);
        bool torsoVis = IsVisible(landmarks, 11) && IsVisible(landmarks, 24);
        bool lLegVis = IsVisible(landmarks, 23) && IsVisible(landmarks, 27);
        bool rLegVis = IsVisible(landmarks, 24) && IsVisible(landmarks, 28);

        // --- 2. Update Colors ---
        SetColor(headImage, headVis ? detectedColor : missingColor);
        SetColor(torsoImage, torsoVis ? detectedColor : missingColor);
        SetColor(leftArmImage, lArmVis ? detectedColor : missingColor);
        SetColor(rightArmImage, rArmVis ? detectedColor : missingColor);
        SetColor(leftLegImage, lLegVis ? detectedColor : missingColor);
        SetColor(rightLegImage, rLegVis ? detectedColor : missingColor);

        // --- 3. Determine Monkey Feedback ---
        if (!headVis)
        {
            speechBubbleText.text = "I can't see your face! Look at the camera.";
            IsFullyReady = false;
        }
        else if (!lArmVis || !rArmVis)
        {
            speechBubbleText.text = "Wave your hands! Let me see them.";
            IsFullyReady = false;
        }
        else if (!lLegVis || !rLegVis)
        {
            speechBubbleText.text = "Step back! I need to see your feet.";
            IsFullyReady = false;
        }
        else
        {
            speechBubbleText.text = "Perfect! You look ready!";
            IsFullyReady = true;
        }
    }

    // Check if point is roughly within screen bounds and valid
    private bool IsVisible(Vector3[] pose, int index)
    {
        if (index >= pose.Length) return false;
        Vector3 point = pose[index];
        // Assuming normalized coordinates (0 to 1). 
        // If your pose provider returns screen pixels, compare against Screen.width/height
        return point.x > 0.01f && point.x < 0.99f && point.y > 0.01f && point.y < 0.99f;
    }

    private void SetColor(Image img, Color c)
    {
        if (img != null) img.color = c;
    }
}