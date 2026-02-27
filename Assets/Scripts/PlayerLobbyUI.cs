using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class PlayerLobbyUI : MonoBehaviour
{
    // --- NEW: FACE LOGIN SECTION ---
    [Header("Face Login Info")]
    public TextMeshProUGUI playerNameText; // Drag your Name Text here
    public TextMeshProUGUI scoreText;      // Drag your Score Text here

    // This checks if the Skeleton is good (Visible) AND if we have a name (Logged In)
    // You can customize this. For now, we trust the visual check.
    public bool IsFullyReady { get; private set; } = false;

    // --- EXISTING: SKELETON VISUALS ---
    [Header("Monkey & Bubble")]
    public TextMeshProUGUI speechBubbleText;
    public GameObject monkeyImage; 

    [Header("Skeleton Visuals (Images)")]
    public Image headImage;
    public Image torsoImage;
    public Image leftArmImage;
    public Image rightArmImage;
    public Image leftLegImage;
    public Image rightLegImage;

    [Header("Settings")]
    public Color missingColor = Color.white;
    public Color detectedColor = Color.green;


    // --- 1. NEW METHODS FOR DETECTION MANAGER ---
    // These are the functions your DetectionManager was looking for!

    public void SetPlayerName(string name)
    {
        if (playerNameText != null) 
            playerNameText.text = name;
    }

    public void SetScore(int score)
    {
        if (scoreText != null) 
            scoreText.text = "Score: " + score.ToString();
    }


    // --- 2. EXISTING LOGIC (Unchanged) ---

    public void SetSearchingState()
    {
        SetColor(headImage, missingColor);
        SetColor(torsoImage, missingColor);
        SetColor(leftArmImage, missingColor);
        SetColor(rightArmImage, missingColor);
        SetColor(leftLegImage, missingColor);
        SetColor(rightLegImage, missingColor);

        if(speechBubbleText != null) 
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

        // BlazePose Landmark mapping (simplified):
        // 0=Nose, 11=L_Shoulder, 12=R_Shoulder, 15=L_Wrist, 16=R_Wrist, 
        // 23=L_Hip, 24=R_Hip, 27=L_Ankle, 28=R_Ankle

        bool headVis = IsVisible(landmarks, 0);
        bool lArmVis = IsVisible(landmarks, 11) && IsVisible(landmarks, 15);
        bool rArmVis = IsVisible(landmarks, 12) && IsVisible(landmarks, 16);
        bool torsoVis = IsVisible(landmarks, 11) && IsVisible(landmarks, 24);
        bool lLegVis = IsVisible(landmarks, 23) && IsVisible(landmarks, 27);
        bool rLegVis = IsVisible(landmarks, 24) && IsVisible(landmarks, 28);

        // Update Colors
        SetColor(headImage, headVis ? detectedColor : missingColor);
        SetColor(torsoImage, torsoVis ? detectedColor : missingColor);
        SetColor(leftArmImage, lArmVis ? detectedColor : missingColor);
        SetColor(rightArmImage, rArmVis ? detectedColor : missingColor);
        SetColor(leftLegImage, lLegVis ? detectedColor : missingColor);
        SetColor(rightLegImage, rLegVis ? detectedColor : missingColor);

        // Determine Monkey Feedback
        if (!headVis)
        {
            if(speechBubbleText != null) speechBubbleText.text = "I can't see your face!";
            IsFullyReady = false;
        }
        else if (!lArmVis || !rArmVis)
        {
            if(speechBubbleText != null) speechBubbleText.text = "Wave your hands!";
            IsFullyReady = false;
        }
        else if (!lLegVis || !rLegVis)
        {
            if(speechBubbleText != null) speechBubbleText.text = "Step back a bit!";
            IsFullyReady = false;
        }
        else
        {
            if(speechBubbleText != null) speechBubbleText.text = "Perfect! Ready!";
            IsFullyReady = true;
        }
    }

    private bool IsVisible(Vector3[] pose, int index)
    {
        if (index >= pose.Length) return false;
        Vector3 point = pose[index];
        return point.x > 0.01f && point.x < 0.99f && point.y > 0.01f && point.y < 0.99f;
    }

    private void SetColor(Image img, Color c)
    {
        if (img != null) img.color = c;
    }
}