using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReactionGameManager : MonoBehaviour
{
    [Header("Debug")]
    public GameObject debugCursorPrefab; 
    
    // Lists to hold the visual debug dots
    private List<GameObject> leftPlayerDots = new List<GameObject>();
    private List<GameObject> rightPlayerDots = new List<GameObject>();

    [Header("Dependencies")]
    public PoseDetectionProvider poseProvider;
    public Canvas mainCanvas;
    public Camera uiCamera;  
    public ResultsPanelManager resultsPanelManager; 

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
    public float hitRadius = 150f; 

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

    // --- FULL BODY PARTS DEFINITION ---
    // We include shoulders, elbows, wrists, hands, hips, knees, ankles, feet
    
    // Left side of the BODY (BlazePose IDs: 11,13,15,17,19,21,23,25,27,29,31)
    private readonly int[] LEFT_LIMBS_INDICES = { 
        15, 17, 19, 21, // Left Hand (Wrist, Pinky, Index, Thumb)
        27, 29, 31,     // Left Foot (Ankle, Heel, Toe)
        13, 23, 25      // Left Elbow, Hip, Knee (Optional, good for body collision)
    };

    // Right side of the BODY (BlazePose IDs: 12,14,16,18,20,22,24,26,28,30,32)
    private readonly int[] RIGHT_LIMBS_INDICES = { 
        16, 18, 20, 22, // Right Hand
        28, 30, 32,     // Right Foot
        14, 24, 26      // Right Elbow, Hip, Knee
    };

    private void Start()
    {
        if (debugCursorPrefab == null || mainCanvas == null) return;

        // Create a pool of debug dots for Left Player (P1)
        for (int i = 0; i < LEFT_LIMBS_INDICES.Length + RIGHT_LIMBS_INDICES.Length; i++)
        {
            GameObject dot = Instantiate(debugCursorPrefab, mainCanvas.transform);
            dot.name = $"P1_Dot_{i}";
            var img = dot.GetComponent<Image>();
            if (img) img.color = new Color(1, 0, 0, 0.5f); // Red for Left Player
            dot.SetActive(false);
            leftPlayerDots.Add(dot);
        }

        // Create a pool of debug dots for Right Player (P2)
        for (int i = 0; i < LEFT_LIMBS_INDICES.Length + RIGHT_LIMBS_INDICES.Length; i++)
        {
            GameObject dot = Instantiate(debugCursorPrefab, mainCanvas.transform);
            dot.name = $"P2_Dot_{i}";
            var img = dot.GetComponent<Image>();
            if (img) img.color = new Color(0, 0, 1, 0.5f); // Blue for Right Player
            dot.SetActive(false);
            rightPlayerDots.Add(dot);
        }
    }

    public void StartGame()
    {
        StopAllCoroutines();
        player1TotalScore = 0;
        player2TotalScore = 0;
        currentRoundIndex = 0;
        player1ScoreText.text = "0";
        player2ScoreText.text = "0";
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

        // Hide all dots at end
        foreach(var d in leftPlayerDots) d.SetActive(false);
        foreach(var d in rightPlayerDots) d.SetActive(false);

        if (resultsPanelManager != null)
            resultsPanelManager.ShowResults(player1TotalScore, player2TotalScore, null, null);
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
    
    // ----------- CHANGE HERE -----------
    // COMMENT OUT THIS LINE. 
    // This line was forcing the slot to move to a random spot.
    // By removing it, the item stays exactly where you placed the Slot in the Unity Editor.
    
    // rt.anchoredPosition = GetRandomPointInside(area); 
    // -----------------------------------

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

    private Vector2 GetRandomPointInside(RectTransform area)
    {
        Vector2 size = area.rect.size;
        return new Vector2(Random.Range(-size.x * 0.4f, size.x * 0.4f), Random.Range(-size.y * 0.4f, size.y * 0.4f));
    }

    private void UpdatePoseAndCheckHits()
    {
        var allPoses = poseProvider.GetAllDetectedPoseKeypoints();
        
        // Safety check
        if (allPoses == null || allPoses.Count == 0) return;

        // Filter and Sort
        var sorted = allPoses
            .Where(p => p != null && p.Length > 0) 
            .OrderBy(p => p[0].x)
            .ToList();

        if (sorted.Count == 0) return;

        Vector3[] poseLeft = null;
        Vector3[] poseRight = null;

        // Logic to determine P1 vs P2
        if (sorted.Count == 1)
        {
            // If only one person, check which side of screen they are on
            if (sorted[0][0].x < 0.5f) poseLeft = sorted[0];
            else poseRight = sorted[0];
        }
        else
        {
            poseLeft = sorted[0];
            poseRight = sorted[1];
        }

        // --- Process Left Player (P1) ---
        if (poseLeft != null && !playerLeft.finished)
        {
            // Combine Left Arm/Leg AND Right Arm/Leg indices because "Left Player" uses their whole body
            List<int> allIndices = new List<int>();
            allIndices.AddRange(LEFT_LIMBS_INDICES);
            allIndices.AddRange(RIGHT_LIMBS_INDICES);

            UpdateDebugDots(poseLeft, allIndices.ToArray(), leftPlayerDots);
            CheckHitsForBodyParts(playerLeft, poseLeft, allIndices.ToArray());
        }
        else
        {
            foreach(var d in leftPlayerDots) d.SetActive(false);
        }

        // --- Process Right Player (P2) ---
        if (poseRight != null && !playerRight.finished)
        {
            List<int> allIndices = new List<int>();
            allIndices.AddRange(LEFT_LIMBS_INDICES);
            allIndices.AddRange(RIGHT_LIMBS_INDICES);

            UpdateDebugDots(poseRight, allIndices.ToArray(), rightPlayerDots);
            CheckHitsForBodyParts(playerRight, poseRight, allIndices.ToArray());
        }
        else
        {
             foreach(var d in rightPlayerDots) d.SetActive(false);
        }
    }

    private void UpdateDebugDots(Vector3[] pose, int[] indices, List<GameObject> dotsPool)
    {
        // Hide all first
        foreach(var d in dotsPool) d.SetActive(false);

        // Show dots for tracked limbs
        for (int i = 0; i < indices.Length; i++)
        {
            int bodyPartIndex = indices[i];
            if (bodyPartIndex >= pose.Length) continue;

            if (i < dotsPool.Count)
            {
                GameObject dot = dotsPool[i];
                dot.SetActive(true);

                Vector2 normPos = new Vector2(pose[bodyPartIndex].x, pose[bodyPartIndex].y);
                Vector2 screenPos = NormalizedToScreenViaFeed(normPos);

                if (mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    dot.transform.position = screenPos;
                }
                else
                {
                    RectTransformUtility.ScreenPointToWorldPointInRectangle(
                        mainCanvas.transform as RectTransform, screenPos, uiCamera, out Vector3 worldPos);
                    dot.transform.position = worldPos;
                }
            }
        }
    }

    private void CheckHitsForBodyParts(PlayerState player, Vector3[] pose, int[] bodyPartIndices)
    {
        // Loop items
        for (int i = player.items.Count - 1; i >= 0; i--)
        {
            var item = player.items[i];
            if (item.hit) continue;

            Vector2 itemScreenPos;
            if (mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                itemScreenPos = item.rect.position;
            else
                itemScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, item.rect.position);

            // Check against ALL body parts
            foreach (int partIndex in bodyPartIndices)
            {
                if (partIndex >= pose.Length) continue;

                Vector2 bodyPartNorm = new Vector2(pose[partIndex].x, pose[partIndex].y);
                Vector2 bodyPartScreen = NormalizedToScreenViaFeed(bodyPartNorm);

                float dist = Vector2.Distance(bodyPartScreen, itemScreenPos);
                
                if (dist < hitRadius)
                {
                    OnItemTouched(player, item);
                    goto NextItem; 
                }
            }
            NextItem: continue;
        }
    }

    private Vector2 NormalizedToScreenViaFeed(Vector2 norm)
    {
        if (cameraFeedRect == null)
        {
            float x = norm.x * Screen.width;
            float y = (invertY ? (1f - norm.y) : norm.y) * Screen.height;
            return new Vector2(x, y);
        }

        float feedWidth = cameraFeedRect.rect.width;
        float feedHeight = cameraFeedRect.rect.height;

        float localX = (norm.x - 0.5f) * feedWidth;
        float localY;

        if (invertY) localY = ((1f - norm.y) - 0.5f) * feedHeight;
        else localY = (norm.y - 0.5f) * feedHeight;

        Vector3 localPosInFeed = new Vector3(localX, localY, 0f);
        Vector3 worldPos = cameraFeedRect.TransformPoint(localPosInFeed);

        Camera camForUI = (mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : uiCamera;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(camForUI, worldPos);
        return screenPos;
    }

    private void OnItemTouched(PlayerState player, SpawnedItem item)
    {
        item.hit = true;
        if (item.go != null) item.go.SetActive(false);

        if (item.isX)
        {
            if (player.roundCorrect > 0) player.roundCorrect--;
            player.finished = true;
            HideAllItems(player);
        }
        else if (item.value == player.expectedNext)
        {
            player.roundCorrect++;
            player.expectedNext++;
            if (player.expectedNext > player.maxNumber) player.finished = true;
        }
        else
        {
            player.finished = true;
            HideAllItems(player);
        }
    }

    private void HideAllItems(PlayerState player)
    {
        foreach (var it in player.items)
        {
            if (it.go != null) it.go.SetActive(false);
            it.hit = true; 
        }
    }
}