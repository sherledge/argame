using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReactionGameManager : MonoBehaviour, IGameStarter, ITutorialListener
{
    [Header("Audio")]
public AudioSource sfxSource;
public AudioClip correctTouchSfx;
public AudioClip wrongTouchSfx;

    [Header("Tutorial")]
    public GameObject tutorialPanel;

    [Header("Dependencies")]
    public PoseDetectionProvider poseProvider;
    public Canvas mainCanvas;
    public Camera uiCamera;  
    public ResultsPanelManager resultsPanelManager; 

    [Header("Camera Feed")]
    public RawImage cameraFeedRawImage; // <--- NEW: ASSIGN THIS IN INSPECTOR

    [Header("Play Areas")]
    public RectTransform leftPlayArea;    
    public RectTransform rightPlayArea;   

    [Header("Item Slots")]
    public List<Image> leftItemSlots;     
    public List<Image> rightItemSlots;    

    [Header("Sprites")]
    public List<Sprite> numberSprites;    
    public Sprite xSprite;                

    [Header("UI")]
    public TMP_Text roundLabel;
    public TMP_Text player1ScoreText;
    public TMP_Text player2ScoreText;

    [Header("Camera Feed Mapping")]
    public RectTransform cameraFeedRect;  
    public bool invertY = true;           

    [Header("Game Settings")]
    public float roundDuration = 10f;
    [Tooltip("Detection distance in pixels.")]
    public float hitRadius = 200f; 

    // --- Image Capture Variables ---
    private Texture2D player1FinalImage;
    private Texture2D player2FinalImage;

    // --- Internal Structures ---
    private class SpawnedItem
    {
        public int value;
        public bool isX;
        public RectTransform rect;
        public GameObject go;
        public bool hit;
    }

    private class PlayerState
    {
        public int expectedNext;
        public int maxNumber;
        public int roundCorrect;
        public bool finished;
        public List<SpawnedItem> items = new List<SpawnedItem>();
    }

    private PlayerState playerLeft = new PlayerState();
    private PlayerState playerRight = new PlayerState();

    private int currentRoundIndex = 0;  
    private int totalRounds = 3;
    private int player1TotalScore = 0;
    private int player2TotalScore = 0;

    private int[][] roundNumbers = new int[3][]
    {
        new int[] {1,2,3},
        new int[] {1,2,3,4},
        new int[] {1,2,3,4,5}
    };

    private bool[] roundHasX = new bool[3] { false, true, true };

    private readonly int[] BODY_INDICES = { 
        15, 17, 19, 21, // Left Hand
        16, 18, 20, 22, // Right Hand
        27, 29, 31,     // Left Foot
        28, 30, 32,     // Right Foot
        13, 14,         // Elbows
        25, 26          // Knees
    };

    private void Start() { }

    public void StartGame()
    {
        StopAllCoroutines();

        player1TotalScore = 0;
        player2TotalScore = 0;
        currentRoundIndex = 0;
        player1ScoreText.text = "0";
        player2ScoreText.text = "0";

        // Clean up old images
        if (player1FinalImage != null) { Destroy(player1FinalImage); player1FinalImage = null; }
        if (player2FinalImage != null) { Destroy(player2FinalImage); player2FinalImage = null; }

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);
        else
            OnTutorialFinished();
    }
bool IsSfxEnabled()
{
    return PlayerPrefs.GetInt("SFX_ENABLED", 1) == 1;
}

    public void OnTutorialFinished()
    {
        Debug.Log("[ReactionGameManager] Tutorial finished, starting game");
        StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (currentRoundIndex < totalRounds)
        {
            SetupRound(currentRoundIndex);

            float elapsed = 0f;
            while (elapsed < roundDuration && (!playerLeft.finished || !playerRight.finished))
            {
                UpdatePoseAndCheckHits();
                elapsed += Time.deltaTime;
                yield return null;
            }

            player1TotalScore += playerLeft.roundCorrect;
            player2TotalScore += playerRight.roundCorrect;
            player1ScoreText.text = player1TotalScore.ToString();
            player2ScoreText.text = player2TotalScore.ToString();

            ClearPlayerItems(playerLeft);
            ClearPlayerItems(playerRight);

            currentRoundIndex++;
            yield return new WaitForSeconds(1f);
        }

        // --- CHANGED: Capture and Pass Images ---
        if (resultsPanelManager != null)
        {
            // 1. Capture Logic
            Texture2D fullSnap = CaptureFromRawImage(cameraFeedRawImage);
            SplitAndAssignFinalImages(fullSnap);

            // 2. Pass Logic
            resultsPanelManager.ShowResults(player2TotalScore, player1TotalScore, player1FinalImage, player2FinalImage);
        }
    }


    private void SetupRound(int roundIndex)
    {
        roundLabel.text = $"Round {roundIndex + 1}";
        int[] nums = roundNumbers[roundIndex];
        bool hasX = roundHasX[roundIndex];

        SetupPlayerState(playerLeft, nums, hasX, leftPlayArea, leftItemSlots);
        SetupPlayerState(playerRight, nums, hasX, rightPlayArea, rightItemSlots);
    }

    private void SetupPlayerState(PlayerState player, int[] numbers, bool hasX, RectTransform area, List<Image> slots)
    {
        player.items.Clear();
        player.expectedNext = 1;
        player.maxNumber = numbers.Last();
        player.roundCorrect = 0;
        player.finished = false;

        foreach (var slot in slots) if (slot != null) slot.gameObject.SetActive(false);

        int slotIndex = 0;
        foreach (int n in numbers)
        {
            if (slotIndex >= slots.Count) break;
            SpawnSingleItem(player, slots[slotIndex], area, n, false);
            slotIndex++;
        }
        if (hasX && slotIndex < slots.Count)
        {
            SpawnSingleItem(player, slots[slotIndex], area, -1, true);
        }
    }

    private void SpawnSingleItem(PlayerState player, Image img, RectTransform area, int val, bool isX)
    {
        if (img == null) return;

        RectTransform rt = img.rectTransform;

        if (isX && xSprite != null)
        {
            img.sprite = xSprite;
        }
        else if (!isX && numberSprites != null && val >= 1 && val <= numberSprites.Count)
        {
            img.sprite = numberSprites[val - 1];
        }

        img.gameObject.SetActive(true);

        player.items.Add(new SpawnedItem
        {
            value = val,
            isX = isX,
            rect = rt,
            go = img.gameObject,
            hit = false
        });
    }

    private void ClearPlayerItems(PlayerState player)
    {
        foreach (var item in player.items) if (item.go != null) item.go.SetActive(false);
        player.items.Clear();
    }


    // --- HIT DETECTION LOGIC ---
    private void UpdatePoseAndCheckHits()
    {
        var allPoses = poseProvider.GetAllDetectedPoseKeypoints();
        if (allPoses == null || allPoses.Count == 0) return;

        var validPoses = allPoses.Where(p => p != null && p.Length > 22).ToList();
        if (validPoses.Count == 0) return;

        var sorted = validPoses.OrderBy(p => p[0].x).ToList();

        Vector3[] poseLeft = null;
        Vector3[] poseRight = null;

        if (sorted.Count == 1)
        {
            if (sorted[0][0].x < 0.5f) poseLeft = sorted[0]; 
            else poseRight = sorted[0];
        }
        else
        {
            poseLeft = sorted[0];
            poseRight = sorted[1];
        }

        if (poseLeft != null && !playerLeft.finished) ProcessPlayerHitLogic(playerLeft, poseLeft);
        if (poseRight != null && !playerRight.finished) ProcessPlayerHitLogic(playerRight, poseRight);
    }

    private void ProcessPlayerHitLogic(PlayerState player, Vector3[] pose)
    {
        foreach (int bodyIdx in BODY_INDICES)
        {
            if (bodyIdx >= pose.Length) continue;

            Vector2 normPos = new Vector2(pose[bodyIdx].x, pose[bodyIdx].y);
            Vector2 screenPos = NormalizedToScreenViaFeed(normPos);

            for (int i = player.items.Count - 1; i >= 0; i--)
            {
                var item = player.items[i];
                if (item.hit || item.go == null || !item.go.activeSelf) continue;

                Vector2 itemScreenPos = GetScreenPos(item.rect.position);

                if (Vector2.Distance(screenPos, itemScreenPos) <= hitRadius)
                {
                    OnItemTouched(player, item);
                    break; 
                }
            }
        }
    }

    // --- COORDINATE MATH ---
    private Vector2 NormalizedToScreenViaFeed(Vector2 norm)
    {
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
        Camera cam = (mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : uiCamera;
        return RectTransformUtility.WorldToScreenPoint(cam, worldPos);
    }

    // --- GAME RULES ---
private void OnItemTouched(PlayerState player, SpawnedItem item)
{
    item.hit = true;
    if (item.go != null) item.go.SetActive(false);

    // ❌ WRONG: X touched
    if (item.isX)
    {
        PlayWrongSfx();

        if (player.roundCorrect > 0)
            player.roundCorrect--;

        player.finished = true;
        HideAllItems(player);
        return;
    }

    // ✅ CORRECT number in correct order
    if (item.value == player.expectedNext)
    {
        PlayCorrectSfx();

        player.roundCorrect++;
        player.expectedNext++;

        if (player.expectedNext > player.maxNumber)
            player.finished = true;
    }
    // ❌ WRONG number (out of order)
    else
    {
        PlayWrongSfx();

        player.finished = true;
        HideAllItems(player);
    }
}
void PlayCorrectSfx()
{
    if (!IsSfxEnabled()) return;
    if (sfxSource != null && correctTouchSfx != null)
        sfxSource.PlayOneShot(correctTouchSfx);
}

void PlayWrongSfx()
{
    if (!IsSfxEnabled()) return;
    if (sfxSource != null && wrongTouchSfx != null)
        sfxSource.PlayOneShot(wrongTouchSfx);
}

    private void HideAllItems(PlayerState player)
    {
        foreach (var it in player.items)
        {
            if (it.go != null) it.go.SetActive(false);
            it.hit = true; 
        }
    }

    // ----------------------------------------------------------------------
    //  IMAGE CAPTURE LOGIC (COPIED)
    // ----------------------------------------------------------------------

    Texture2D CaptureFromRawImage(RawImage rawImage)
    {
        if (rawImage == null || rawImage.texture == null)
            return null;

        Texture src = rawImage.texture;

        RenderTexture rt = RenderTexture.GetTemporary(
            src.width,
            src.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear
        );

        Graphics.Blit(src, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return tex;
    }

    void SplitAndAssignFinalImages(Texture2D source)
    {
        if (source == null) return;

        int width = source.width;
        int height = source.height;
        int halfWidth = width / 2;

        Texture2D p1Tex = new Texture2D(halfWidth, height, source.format, false);
        p1Tex.SetPixels(source.GetPixels(halfWidth, 0, halfWidth, height));
        p1Tex.Apply();

        Texture2D p2Tex = new Texture2D(halfWidth, height, source.format, false);
        p2Tex.SetPixels(source.GetPixels(0, 0, halfWidth, height));
        p2Tex.Apply();

        player1FinalImage = p1Tex;
        player2FinalImage = p2Tex;
    }
}