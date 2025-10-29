using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro; 

public class ScoringManager : MonoBehaviour
{
    public static ScoringManager Instance; // Singleton
    
    [Header("--- Scene & Time Settings ---")]
    [Tooltip("ชื่อ Scene ปัจจุบัน (สำหรับปุ่ม Try Again)")]
    public string currentSceneName; // NEW: ชื่อซีนปัจจุบัน
    
    [Tooltip("เวลาทั้งหมดในซีนนี้ (หน่วยเป็นวินาที)")]
    public float timeLimitSeconds = 60f; 
    
    [Tooltip("ชื่อ Scene ถัดไปเมื่อจบด่าน")]
    public string nextSceneName = "TriageRoom";

    [Header("--- UI Elements (XR UI Canvas)")]
    [Tooltip("Text UI สำหรับแสดงเวลาที่เหลือ")]
    public TextMeshProUGUI timerText; 

    [Tooltip("GameObject ที่เป็น Canvas หลักสำหรับแสดง Score Summary")]
    public GameObject scoreSummaryUI; 

    [Tooltip("TextMeshProUGUI สำหรับแสดงคะแนนรวม")]
    public TextMeshProUGUI totalScoreText; 
    
    [Tooltip("NEW: TextMeshProUGUI สำหรับแสดงเวลาที่ใช้ไป")]
    public TextMeshProUGUI timeTakenText; // NEW: ตัวแปรใหม่

    [Tooltip("GameObject ที่ใช้สำหรับแสดงรายการถูก/ผิด (Container)")]
    public Transform scoreListContainer; 

    [Tooltip("Prefab ของ Text UI สำหรับแสดงแต่ละรายการคะแนน (ถูก/ผิด)")]
    public TextMeshProUGUI scoreListItemPrefab; 
    
    [Header("--- Player & Trigger Settings ---")]
    [Tooltip("GameObject ของกระเป๋า Player (ที่ติด PlayerInventory.cs)")]
    public GameObject playerBag; 

    [Tooltip("GameObject ของ NPC (Mr.T suit 1) ที่มี UI 'เสร็จสิ้น'")]
    public GameObject npcObject; 

    [Header("--- Special Action Settings ---")]
    [Tooltip("คะแนนสำหรับการสวมใส่ PPE Lv.C")]
    public int ppeScoreValue = 15;
    private bool ppeWorn = false;
    
    [Tooltip("AudioClip เสียงรูดซิปเมื่อสวม PPE")]
    public AudioClip ppeSoundClip;
    private AudioSource audioSource;

    private float currentTime;
    private bool timerIsRunning = false;
    private int currentTotalScore = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        currentTime = timeLimitSeconds;
        timerIsRunning = true;
        
        // NEW: เก็บชื่อซีนปัจจุบันไว้สำหรับปุ่ม Try Again
        currentSceneName = SceneManager.GetActiveScene().name;
        
        // ตรวจสอบ AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 1. ตรวจสอบกระเป๋า Player
        if (playerBag == null || playerBag.GetComponent<PlayerInventory>() == null)
        {
            Debug.LogError("Player Bag หรือ PlayerInventory Script ไม่ได้ตั้งค่า/แนบอย่างถูกต้อง");
            scoreSummaryUI.SetActive(false);
            return;
        }

        // 2. ตั้งค่า UI 'เสร็จสิ้น' ที่ NPC 
        SetupNpcInteraction();
        
        // ปิด UI สรุปคะแนนไว้ก่อน
        scoreSummaryUI.SetActive(false);
    }

    void Update()
    {
        if (timerIsRunning)
        {
            if (currentTime > 0)
            {
                currentTime -= Time.deltaTime;
                UpdateTimerUI(currentTime);
            }
            else
            {
                // เวลาหมด
                currentTime = 0;
                timerIsRunning = false;
                UpdateTimerUI(0);
                EndPhaseAndShowScore();
            }
        }
    }

    // ฟังก์ชันช่วยในการอัพเดท UI เวลา (Timer)
    private void UpdateTimerUI(float timeToDisplay)
    {
        timeToDisplay += 1;
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);  
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        if (timerText != null)
        {
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
    
    // ฟังก์ชันช่วยในการแปลงวินาทีเป็นรูปแบบ MM:SS
    private string FormatTime(float timeInSeconds)
    {
        float minutes = Mathf.FloorToInt(timeInSeconds / 60);
        float seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // ฟังก์ชันสำหรับเรียกเมื่อกดปุ่ม 'สวมใส่ PPE Lv.C'
    public void WearPPE()
    {
        if (!ppeWorn)
        {
            ppeWorn = true;
            currentTotalScore += ppeScoreValue;
            
            // เล่นเสียง
            if (ppeSoundClip != null)
            {
                audioSource.PlayOneShot(ppeSoundClip);
            }
            
            Debug.Log("PPE Lv.C สวมใส่แล้ว! ได้รับ " + ppeScoreValue + " คะแนน");
        }
    }

    // ----------------------------------------------------
    // --- NPC INTERACTION & SCORE SUMMARY ---
    // ----------------------------------------------------

    // สร้างปุ่ม 'เสร็จสิ้น' ที่ NPC (คำเตือน: การตั้งค่านี้ควรทำใน Editor)
    private void SetupNpcInteraction()
    {
        if (npcObject == null)
        {
            Debug.LogWarning("NPC Object ไม่ได้ถูกกำหนด! ผู้เล่นไม่สามารถจบด่านได้.");
        }
        else
        {
            Debug.Log("Interaction point on NPC is ready. Ensure the 'Finish' UI Button is active and linked to EndPhaseAndShowScore().");
        }
    }

    // ฟังก์ชันหลักที่ถูกเรียกเมื่อ Player กด 'เสร็จสิ้น' หรือหมดเวลา
    public void EndPhaseAndShowScore()
    {
        timerIsRunning = false;
        
        // 1. คำนวณเวลาที่ใช้ไป (NEW)
        float timeTaken = timeLimitSeconds - currentTime;
        
        // 2. เคลียร์รายการเดิม
        List<GameObject> childrenToDestroy = new List<GameObject>();
        for (int i = scoreListContainer.childCount - 1; i >= 0; i--)
        {
            if (scoreListItemPrefab == null || scoreListContainer.GetChild(i).gameObject != scoreListItemPrefab.gameObject)
            {
                childrenToDestroy.Add(scoreListContainer.GetChild(i).gameObject);
            }
            else
            {
                 scoreListContainer.GetChild(i).gameObject.SetActive(false);
            }
        }
        childrenToDestroy.ForEach(Destroy);


        // 3. ประมวลผลไอเทมในกระเป๋า (โค้ดเดิม)
        PlayerInventory inventory = playerBag.GetComponent<PlayerInventory>();
        currentTotalScore = ppeWorn ? ppeScoreValue : 0; 
        
        List<GameObject> itemsInBag = inventory.GetAllItemsInSockets();
        
        // 4. แสดงรายการคะแนน (โค้ดเดิม)
        foreach (GameObject item in itemsInBag)
        {
            ScorableItem scorable = item.GetComponent<ScorableItem>();
            string resultText = item.name;
            string colorHex = "#AAAAAA"; 
            int itemScore = 0; 

            if (scorable != null)
            {
                if (scorable.IsCorrectItem)
                {
                    itemScore = scorable.ScoreValue;
                    currentTotalScore += itemScore;
                    resultText = scorable.ItemDisplayName + " (ถูกต้อง! +" + itemScore + " คะแนน)";
                    colorHex = "#32CD32"; 
                }
                else
                {
                    resultText = scorable.ItemDisplayName + " (ไม่ถูกต้อง/ไม่จำเป็น)";
                    colorHex = "#FF4500"; 
                }
            }
            else
            {
                 resultText = item.name + " (ไอเทมทั่วไป)";
            }

            TextMeshProUGUI listItem = Instantiate(scoreListItemPrefab, scoreListContainer);
            listItem.gameObject.SetActive(true); 
            listItem.text = $"<color={colorHex}>• {resultText}</color>";
        }
        
        // 5. เพิ่มรายการ PPE (โค้ดเดิม)
        string ppeResultText = ppeWorn ? $"PPE Lv.C (สวมใส่ถูกต้อง! +{ppeScoreValue} คะแนน)" : "PPE Lv.C (ไม่ได้สวมใส่)";
        string ppeColor = ppeWorn ? "#32CD32" : "#FF0000";
        TextMeshProUGUI ppeListItem = Instantiate(scoreListItemPrefab, scoreListContainer);
        ppeListItem.gameObject.SetActive(true); 
        ppeListItem.text = $"<color={ppeColor}>• {ppeResultText}</color>";

        // 6. แสดงคะแนนรวม (โค้ดเดิม)
        totalScoreText.text = "คะแนนรวม: " + currentTotalScore.ToString() + " คะแนน";
        
        // 7. แสดงเวลาที่ใช้ไป (NEW)
        if (timeTakenText != null)
        {
             timeTakenText.text = "เวลาที่ใช้ไป: " + FormatTime(timeTaken);
        }
        
        // 8. บังคับให้ Layout Group อัปเดตทันที
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)scoreListContainer);

        // 9. แสดง UI สรุปผล
        scoreSummaryUI.SetActive(true);
    }

    // ฟังก์ชันสำหรับปุ่ม 'ต่อไป' ใน UI สรุปผล (โค้ดเดิม)
    public void GoToNextScene()
    {
        Debug.Log("Loading next scene: " + nextSceneName);
        SceneManager.LoadScene(nextSceneName);
    }
    
    // NEW: ฟังก์ชันสำหรับปุ่ม 'Try Again' 
    public void TryAgain()
    {
        Debug.Log("Restarting current scene: " + currentSceneName);
        SceneManager.LoadScene(currentSceneName);
    }
}
