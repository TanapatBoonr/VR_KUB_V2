using UnityEngine;
using UnityEngine.UI; 
using System.Collections;
using System.Collections.Generic;
using TMPro; 
using UnityEngine.SceneManagement; // ใช้สำหรับการโหลด Scene

public class ScoringManager : MonoBehaviour
{
    public static ScoringManager Instance; // Singleton
    
    [Header("--- Scene & Time Settings ---")]
    public string currentSceneName;
    public float timeLimitSeconds = 60f; 
    public string nextSceneName = "TriageRoom";

    [Header("--- System References ---")]
    [Tooltip("ลาก PlayerInventory ที่แนบกับกระเป๋ามาใส่")]
    public PlayerInventory playerInventory;

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
    
    // Key: Item Name, Value: True=Correct Pick (บันทึกเฉพาะรายการที่หยิบหรือกดสวมใส่)
    private Dictionary<string, bool> itemResults = new Dictionary<string, bool>(); 
    private bool ppeWorn = false; 

    // รายการไอเทมที่ต้องหยิบ (Hardcoded ตามโจทย์)
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
        
        // ล้างผลลัพธ์เก่า
        itemResults.Clear();
        ppeWorn = false;
    }
    
    void Start()
    {
        // ... (โค้ด Start เดิม) ...
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
        // ... (โค้ด Update เดิม) ...
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
    
    // ************************************************************
    // *** PUBLIC FUNCTIONS สำหรับ Unity Button & Game Logic ***
    // ************************************************************

    // ฟังก์ชันสำหรับบันทึกผลการสวมใส่ PPE (เรียกจากปุ่ม 'สวมใส่' UI)
    public void LogPPEWorn(bool wornStatus)
    {
        ppeWorn = wornStatus;
        if (ppeWorn)
        {
            // บันทึก PPE ลงใน itemResults (ใช้ Key พิเศษเพื่อแยก)
            if (!itemResults.ContainsKey("PPE Lv.C"))
            {
                itemResults.Add("PPE Lv.C", true); 
                Debug.Log($"Scoring: Logged PPE Lv.C as worn and correct.");
            }
        }
        else
        {
            // ถ้ามีการกดสวมใส่ซ้ำและสถานะเป็น False (เผื่อกรณีมีปุ่มถอด)
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

    // ฟังก์ชันที่ถูกเรียกโดยปุ่ม 'เสร็จสิ้น' (End Mission)
    public void EndMission(bool completedByPlayer)
    {
        if (!missionActive) return;

        missionActive = false;
        
        // 1. ดึงข้อมูลจากกระเป๋าและประมวลผลก่อนจบภารกิจ
        ProcessInventoryForScoring();

        float timeTaken = timeLimitSeconds - currentTime;
        int finalScore = CalculateFinalScore(); 
        
        DisplayScoreSummary(timeTaken, finalScore);
    }
    
    // ************************************************************
    // *** CORE LOGIC: ดึงข้อมูลจาก PlayerInventory ***
    // ************************************************************
    private void ProcessInventoryForScoring()
    {
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventory reference is missing in ScoringManager! Cannot process items.");
            return;
        }
        
        // 1. เก็บสถานะ PPE ไว้ก่อน แล้วล้าง itemResults
        bool ppeWasWorn = itemResults.ContainsKey("PPE Lv.C") && itemResults["PPE Lv.C"];
        itemResults.Clear(); 
        if (ppeWasWorn)
        {
            itemResults.Add("PPE Lv.C", true);
        }

        // 2. ดึงรายการ GameObject ที่อยู่ในกระเป๋าทั้งหมด
        List<GameObject> itemsInBag = playerInventory.GetAllItemsInSockets();
        
        // 3. วนลูปดูไอเทมที่ผู้เล่นหยิบ
        foreach (GameObject item in itemsInBag)
        {
            ScorableItem scorable = item.GetComponent<ScorableItem>();

            if (scorable != null)
            {
                string itemName = scorable.ItemDisplayName;
                
                // ป้องกันการนับไอเทมซ้ำ ถ้าผู้เล่นหยิบไอเทมชนิดเดียวกันมา 2 ชิ้น
                if (itemResults.ContainsKey(itemName))
                {
                    Debug.LogWarning($"Scoring: Duplicate item {itemName} found in bag. Skipping the duplicate.");
                    continue; 
                }

                bool isCorrectPick = requiredItems.Contains(itemName); 
                
                itemResults.Add(itemName, isCorrectPick);
                Debug.Log($"Scoring: Logged Item: {itemName}, Correct: {isCorrectPick}");
            }
            else
            {
                 // ไอเทมที่อยู่ในกระเป๋าแต่ไม่มี ScorableItem.cs ถือว่าหยิบผิด
                 // ใช้ชื่อ GameObject เป็น Key
                 string itemName = item.name;
                 if (!itemResults.ContainsKey(itemName))
                 {
                    itemResults.Add(itemName, false);
                    Debug.LogWarning($"Scoring: Item in bag without ScorableItem.cs (Penalty): {itemName}");
                 }
            }
        }
        
        Debug.Log($"Scoring: Total {itemResults.Count} unique items processed (including PPE).");
    }


    // ************************************************************
    // *** PRIVATE HELPER FUNCTIONS (ส่วน UI และคำนวณ) ***
    // ************************************************************

    private int CalculateFinalScore()
    {
        int score = 0;
        
        // 1. คำนวณคะแนนจาก Item และ PPE
        foreach (var item in itemResults)
        {
            if (item.Key == "PPE Lv.C")
            {
                // คะแนน PPE (จะถูกนับเมื่อ item.Value เป็น true เท่านั้น ซึ่งถูกกำหนดไว้ใน LogPPEWorn)
                score += item.Value ? ppeScoreValue : 0;
            }
            else if (item.Value) // Item ที่หยิบถูก
            {
                score += correctItemScore;
            }
            else // Item ที่หยิบผิด (Penalty)
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

        // 1. เคลียร์รายการเดิมทั้งหมด
        foreach (Transform child in scoreListContainer)
        {
            if (child.gameObject != scoreListItemPrefab.gameObject)
            {
                Destroy(child.gameObject);
            }
        }
        
        // 2. วนลูปแสดงผลลัพธ์ของ Item และ PPE ทั้งหมด
        if (itemResults.Count == 0)
        {
            // แสดงข้อความถ้าไม่มีการหยิบไอเทมที่เกี่ยวข้องเลย
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
                    // รายการ PPE (ถือว่าถูกต้องเสมอถ้าอยู่ใน List เพราะถูกใส่เข้ามาเมื่อกดสวมใส่)
                    resultText = $"PPE Lv.C (สวมใส่ถูกต้อง! +{ppeScoreValue} คะแนน)";
                    colorHex = "#32CD32"; 
                }
                else if (isCorrect)
                {
                    // รายการ Item ที่หยิบถูก
                    resultText = $"{itemName} (ถูกต้อง! +{correctItemScore} คะแนน)";
                    colorHex = "#32CD32"; // Green
                }
                else 
                {
                    // รายการ Item ที่หยิบผิด
                    resultText = $"{itemName} (หยิบผิด! {wrongItemPenalty} คะแนน)";
                    colorHex = "#FF0000"; // Red
                }

                TextMeshProUGUI listItem = Instantiate(scoreListItemPrefab, scoreListContainer);
                listItem.gameObject.SetActive(true);
                listItem.text = $"<color={colorHex}>• {resultText}</color>";
            }
        }
        
        // 3. แสดงคะแนนรวม
        if (totalScoreText != null) totalScoreText.text = "คะแนนรวม: " + currentTotalScore.ToString() + " คะแนน";
        
        // 4. แสดงเวลาที่ใช้ไป
        if (timeTakenText != null) timeTakenText.text = "เวลาที่ใช้ไป: " + FormatTime(timeTaken);
        
        // 5. บังคับให้ Layout Group อัปเดต
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)scoreListContainer);

        // 6. แสดง UI สรุปผล
        if (scoreSummaryUI != null) scoreSummaryUI.SetActive(true);
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
            timerText.text = $"เวลา: {minutes:00}:{seconds:00}";
            timerText.color = (timeToDisplay <= 15f && timeToDisplay > 0) ? Color.red : Color.white;
        }
    }

    public void GoToNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
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
