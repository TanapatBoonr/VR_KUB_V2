using UnityEngine;
using UnityEngine.UI; 
using System.Collections;
using System.Collections.Generic;
using TMPro; 
using UnityEngine.SceneManagement; 

public class ScoringManager : MonoBehaviour
{
    public static ScoringManager Instance; 
    
    [Header("--- Scene & Teleport Settings ---")]
    [Tooltip("ชื่อ Scene ปัจจุบัน (สำหรับปุ่ม Try Again)")]
    public string currentSceneName;
    
    [Tooltip("ชื่อ Scene ถัดไปเมื่อจบด่าน (เช่น TriageRoom)")]
    public string nextSceneName = "TriageRoom";
    
    [Tooltip("ชื่อของ GameObject จุดหมายปลายทางใน Scene ถัดไป (เช่น SpawnPoint_B)")]
    public string destinationPointName = "SpawnPoint_B"; // ** จุดหมายปลายทางที่จะวาปไป **

    [Header("--- Game & Time Settings ---")]
    public float timeLimitSeconds = 60f; 

    [Header("--- System References ---")]
    public PlayerInventory playerInventory; // ต้องมีสคริปต์ PlayerInventory ใน Scene

    [Header("--- UI Elements (XR UI Canvas)")]
    public TextMeshProUGUI timerText; 
    public GameObject missionStartUI;
    public GameObject scoreSummaryUI; 
    public TextMeshProUGUI totalScoreText; 
    public TextMeshProUGUI timeTakenText; 
    public Transform scoreListContainer; 
    public TextMeshProUGUI scoreListItemPrefab; 

    // ************************************************************
    // *** Scoring Variables ***
    // ************************************************************
    private bool missionActive = false;
    private float currentTime;
    
    [Header("--- Scoring Values ---")]
    public int ppeScoreValue = 5; 
    public int correctItemScore = 1; 
    public int wrongItemPenalty = -2;
    
    private Dictionary<string, bool> itemResults = new Dictionary<string, bool>(); 
    private bool ppeWorn = false; 

    private readonly HashSet<string> requiredItems = new HashSet<string>
    {
        "Medical Elastic Bandage A", "Top gauze", "Tourniquet", 
        "Black_Tag-Triage", "Green_Tag-Triage", "Red_Tag-Triage", "Yello_Tag-Triage"
    };
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            currentTime = timeLimitSeconds;
        }
        else
        {
            Destroy(gameObject);
        }
        
        itemResults.Clear();
        ppeWorn = false;
    }
    
    void Start()
    {
        // การจัดการ UI ตอนเริ่มภารกิจ
        if (missionStartUI != null)
        {
            missionStartUI.SetActive(true);
            if (timerText != null && timerText.gameObject.activeInHierarchy)
            {
                timerText.gameObject.SetActive(false); 
            }
            if (scoreSummaryUI != null)
            {
                scoreSummaryUI.SetActive(false); 
            }
            missionActive = false;
        }
        else
        {
            Debug.LogWarning("Mission Start UI not assigned. Starting mission immediately.");
            StartMission(); 
        }
        
        currentTime = timeLimitSeconds;
        UpdateTimerUI(currentTime);
        
        // ซ่อน Prefab รายการคะแนนที่ใช้เป็น Template
        if (scoreListItemPrefab != null && scoreListItemPrefab.gameObject.activeSelf)
        {
            scoreListItemPrefab.gameObject.SetActive(false);
        }

        if (string.IsNullOrEmpty(currentSceneName))
        {
            currentSceneName = SceneManager.GetActiveScene().name;
        }
    }

    void Update()
    {
        if (missionActive)
        {
            if (currentTime > 0)
            {
                currentTime -= Time.deltaTime;
                UpdateTimerUI(currentTime);
            }
            else
            {
                currentTime = 0;
                EndMission(false);
            }
        }
    }

    // ฟังก์ชันสำหรับบันทึกสถานะการสวมใส่ PPE
    public void LogPPEWorn(bool wornStatus)
    {
        ppeWorn = wornStatus;
        if (ppeWorn)
        {
            if (!itemResults.ContainsKey("PPE Lv.C"))
            {
                itemResults.Add("PPE Lv.C", true); 
                Debug.Log("Scoring: Logged PPE Lv.C as worn and correct.");
            }
        }
        else
        {
            if (itemResults.ContainsKey("PPE Lv.C"))
            {
                 itemResults.Remove("PPE Lv.C");
            }
        }
    }

    public void StartMission()
    {
        if (missionStartUI != null) missionStartUI.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(true); 

        missionActive = true;
        Debug.Log("Pick Item Mission Started! Timer is running.");
    }

    public void EndMission(bool completedByPlayer)
    {
        if (!missionActive) return;

        missionActive = false;
        
        ProcessInventoryForScoring();

        float timeTaken = timeLimitSeconds - currentTime;
        int finalScore = CalculateFinalScore(); 
        
        DisplayScoreSummary(timeTaken, finalScore);
    }
    
    // ตรวจสอบไอเทมในกระเป๋าและบันทึกคะแนน
    private void ProcessInventoryForScoring()
    {
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventory reference is missing in ScoringManager! Cannot process items.");
            return;
        }
        
        bool ppeWasWorn = itemResults.ContainsKey("PPE Lv.C") && itemResults["PPE Lv.C"];
        itemResults.Clear(); 
        if (ppeWasWorn)
        {
            itemResults.Add("PPE Lv.C", true); // เพิ่มสถานะ PPE กลับเข้าไป
        }

        List<GameObject> itemsInBag = playerInventory.GetAllItemsInSockets();
        
        foreach (GameObject item in itemsInBag)
        {
            ScorableItem scorable = item.GetComponent<ScorableItem>();

            if (scorable != null)
            {
                string itemName = scorable.ItemDisplayName;
                
                if (itemResults.ContainsKey(itemName))
                {
                    Debug.LogWarning("Scoring: Duplicate item " + itemName + " found in bag. Skipping the duplicate.");
                    continue; 
                }

                bool isCorrectPick = requiredItems.Contains(itemName); 
                
                itemResults.Add(itemName, isCorrectPick);
                Debug.Log("Scoring: Logged Item: " + itemName + ", Correct: " + isCorrectPick);
            }
            else
            {
                 string itemName = item.name;
                 if (!itemResults.ContainsKey(itemName))
                 {
                    itemResults.Add(itemName, false);
                    Debug.LogWarning("Scoring: Item in bag without ScorableItem.cs (Penalty): " + itemName);
                 }
            }
        }
        
        Debug.Log("Scoring: Total " + itemResults.Count + " unique items processed (including PPE).");
    }

    private int CalculateFinalScore()
    {
        int score = 0;
        
        foreach (var item in itemResults)
        {
            if (item.Key == "PPE Lv.C")
            {
                score += item.Value ? ppeScoreValue : 0;
            }
            else if (item.Value) 
            {
                score += correctItemScore;
            }
            else 
            {
                score += wrongItemPenalty;
            }
        }

        return score;
    }
    
    private void DisplayScoreSummary(float timeTaken, int currentTotalScore)
    {
        if (timerText != null) timerText.gameObject.SetActive(false); 

        if (scoreListItemPrefab == null || scoreListContainer == null || scoreSummaryUI == null)
        {
            Debug.LogError("ERROR: One or more UI references are NOT assigned! Cannot display summary.");
            return;
        }

        // ล้างรายการคะแนนเก่า
        foreach (Transform child in scoreListContainer)
        {
            if (child.gameObject != scoreListItemPrefab.gameObject)
            {
                Destroy(child.gameObject);
            }
        }
        
        if (itemResults.Count == 0)
        {
            TextMeshProUGUI listItem = Instantiate(scoreListItemPrefab, scoreListContainer);
            listItem.gameObject.SetActive(true);
            listItem.text = "<color=#FFA500>• ไม่มีการหยิบไอเทมที่ถูกบันทึก</color>";
        }
        else
        {
            foreach (var item in itemResults)
            {
                string itemName = item.Key;
                bool isCorrect = item.Value;
                string resultText;
                string colorHex;

                if (itemName == "PPE Lv.C")
                {
                    // PPE ถูกนับใน itemResults อยู่แล้ว
                    resultText = "PPE Lv.C (สวมใส่ถูกต้อง! +" + ppeScoreValue + " คะแนน)";
                    colorHex = "#32CD32"; 
                }
                else if (isCorrect)
                {
                    resultText = itemName + " (ถูกต้อง! +" + correctItemScore + " คะแนน)";
                    colorHex = "#32CD32"; 
                }
                else 
                {
                    resultText = itemName + " (หยิบผิด! " + wrongItemPenalty + " คะแนน)";
                    colorHex = "#FF0000"; 
                }

                TextMeshProUGUI listItem = Instantiate(scoreListItemPrefab, scoreListContainer);
                listItem.gameObject.SetActive(true);
                listItem.text = "<color=" + colorHex + ">• " + resultText + "</color>";
            }
        }
        
        if (totalScoreText != null) totalScoreText.text = "คะแนนรวม: " + currentTotalScore.ToString() + " คะแนน";
        
        if (timeTakenText != null) timeTakenText.text = "เวลาที่ใช้ไป: " + FormatTime(timeTaken);
        
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)scoreListContainer);

        if (scoreSummaryUI != null) scoreSummaryUI.SetActive(true);
    }

    /// <summary>
    /// ฟังก์ชันที่ผูกกับปุ่ม 'ต่อไป' ใน UI สรุปผล เพื่อโหลด Scene ถัดไปและบันทึกจุดวาป
    /// </summary>
    public void GoToNextScene()
    {
        // 1. บันทึกชื่อจุดหมายปลายทางก่อนโหลด Scene ใหม่
        if (!string.IsNullOrEmpty(destinationPointName))
        {
            TeleportData.SetDestinationPointName(destinationPointName);
        }
        else
        {
            Debug.LogWarning("Destination Point Name is empty in ScoringManager. Player will spawn at default position.");
        }
        
        // 2. โหลด Scene ใหม่
        SceneManager.LoadScene(nextSceneName);
    }
    
    private string FormatTime(float timeInSeconds)
    {
        float minutes = Mathf.FloorToInt(timeInSeconds / 60);
        float seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    private void UpdateTimerUI(float timeToDisplay)
    {
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);  
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        if (timerText != null)
        {
            timerText.text = "เวลา: " + minutes.ToString("00") + ":" + seconds.ToString("00");
            timerText.color = (timeToDisplay <= 15f && timeToDisplay > 0) ? Color.red : Color.white;
        }
    }

    public void TryAgain()
    {
        if (!string.IsNullOrEmpty(currentSceneName))
        {
            SceneManager.LoadScene(currentSceneName);
        }
        else
        {
            Debug.LogError("Current Scene Name is not set. Cannot restart.");
        }
    }
}
