using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

// คลาสจัดการภารกิจ Triage Room - รอบที่ 1 (สำรวจโรงงาน)
public class TriageMissionManager : MonoBehaviour
{
    public static TriageMissionManager Instance;

    // *******************************************************************
    // *** NEW: TeleportData - สำหรับการส่งข้อมูลจุดเกิดข้าม Scene ***
    // *******************************************************************
    // ใช้ static string เพื่อเก็บชื่อจุดเกิดปลายทางใน Scene TriageRoom
    // เมื่อโหลด Scene แล้ว SpawnPointManager.cs จะมาอ่านค่านี้นำไปใช้
    private static string destinationPointName = "SpawnPointA"; 

    public static void SetDestinationPointName(string pointName)
    {
        destinationPointName = pointName;
    }

    public static string GetDestinationPointName()
    {
        return destinationPointName;
    }
    // *******************************************************************

    [Header("--- Mission Settings ---")]
    [Tooltip("ชื่อ Scene ถัดไปเมื่อจบภารกิจ (Pick_Item)")]
    public string nextSceneName = "Pick_Item";
    [Tooltip("เวลาจำกัดสำหรับภารกิจสำรวจจุดเสี่ยง (หน่วยเป็นวินาที)")]
    public float timeLimitSeconds = 360f; // 6 นาที
    
    [Header("--- UI & Interaction ---")]
    [Tooltip("GameObject ของ UI อธิบายภารกิจ (แสดงที่ NPC)")]
    public GameObject missionStartUI;
    [Tooltip("TextMeshProUGUI สำหรับแสดงเวลาที่เหลือ")]
    public TextMeshProUGUI timerText; 
    [Tooltip("GameObject ของ UI สรุปคะแนน")]
    public GameObject scoreSummaryUI; 
    [Tooltip("TextMeshProUGUI สำหรับแสดงคะแนนรวมในหน้าสรุป")]
    public TextMeshProUGUI totalScoreText;
    [Tooltip("TextMeshProUGUI สำหรับแสดงเวลาที่ใช้ไปในหน้าสรุป")]
    public TextMeshProUGUI timeTakenText;
    [Tooltip("GameObject ของปุ่ม Finish (แสดงที่ NPC)")]
    public GameObject finishButton; 
    [Tooltip("จุดวาร์ปผู้เล่นเมื่อหมดเวลาหรือจบภารกิจ (หน้า NPC)")]
    public Transform warpPointBeforeNPC; 
    [Tooltip("Transform ของ Player Rig")]
    public Transform playerRig; // ต้องลาก XR Rig มาใส่

    // สถานะ
    private bool missionActive = false;
    private float currentTime;
    private int collectedDangerPoints = 0;
    private int totalDangerPoints;
    private List<string> collectedPointNames = new List<string>();


    void Awake()
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
    }

    void Start()
    {
        // 1. ค้นหาจุดเสี่ยงทั้งหมดและนับจำนวน
        DangerPointButton[] allPoints = FindObjectsOfType<DangerPointButton>();
        totalDangerPoints = allPoints.Length;
        
        // 2. ซ่อน UI สรุปคะแนนและปุ่ม Finish ไว้ก่อน
        scoreSummaryUI.SetActive(false);
        if (finishButton != null) 
        {
            finishButton.SetActive(false);
            // ตรวจสอบว่าปุ่ม Finish ถูกเชื่อมต่อกับ EndMission() แล้วหรือยัง
            // (การเชื่อมต่อนี้ต้องทำใน Unity Editor)
        }
        
        // 3. แสดง UI เริ่มภารกิจ
        if (missionStartUI != null) missionStartUI.SetActive(true);
        
        // 4. ตั้งค่าเวลาเริ่มต้น
        UpdateTimerUI(timeLimitSeconds);
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
                // เวลาหมด
                currentTime = 0;
                EndMission(false); // จบภารกิจเพราะหมดเวลา
            }
            
            // ตรวจสอบเงื่อนไขจบภารกิจด้วยการเก็บครบ
            if (collectedDangerPoints >= totalDangerPoints)
            {
                // Note: การจบภารกิจเมื่อเก็บครบจะทำผ่านการแสดงปุ่ม Finish
                // และให้ผู้เล่นกด EndMission(true) เอง
            }
        }
    }

    // ฟังก์ชันสำหรับปุ่ม 'Continue' (เริ่มภารกิจ)
    public void StartMission()
    {
        if (missionStartUI != null) missionStartUI.SetActive(false);
        missionActive = true;
        Debug.Log("Mission Started! Time limit: " + timeLimitSeconds + " seconds.");
        
        // เปิดใช้งานปุ่ม Danger Point ทั้งหมด
        foreach(DangerPointButton dp in FindObjectsOfType<DangerPointButton>())
        {
            dp.SetButtonActive(true);
        }
    }
    
    // ฟังก์ชันเรียกจาก DangerPointButton.cs เมื่อถูกคลิก
    public void ReportDangerPointCollected(string pointName)
    {
        if (!collectedPointNames.Contains(pointName))
        {
            collectedDangerPoints++;
            collectedPointNames.Add(pointName);
            Debug.Log($"Collected: {pointName}. Total Collected: {collectedDangerPoints}/{totalDangerPoints}");

            // ตรวจสอบว่าเก็บครบแล้วหรือยัง
            if (collectedDangerPoints >= totalDangerPoints)
            {
                // ถ้าเก็บครบแล้ว ให้แสดงปุ่ม Finish ที่ NPC ทันที
                if (finishButton != null) finishButton.SetActive(true);
            }
        }
    }

    // ฟังก์ชันเรียกเมื่อกดปุ่ม 'Finish' (จาก UI)
    public void FinishMissionButton()
    {
         EndMission(true); // จบภารกิจโดยผู้เล่นกดปุ่ม
    }

    // ฟังก์ชันหลักที่ถูกเรียกเมื่อกดปุ่ม 'Finish' หรือหมดเวลา (ภายใน Update)
    private void EndMission(bool completedByPlayer)
    {
        if (!missionActive) return; // ป้องกันการเรียกซ้ำ
        
        missionActive = false;
        
        // 1. คำนวณเวลาและคะแนน
        float timeTaken = timeLimitSeconds - currentTime;
        
        // 2. จัดการผู้เล่นเมื่อหมดเวลา (วาร์ป)
        if (!completedByPlayer && playerRig != null && warpPointBeforeNPC != null)
        {
            // วาร์ปผู้เล่นมาหน้า NPC ทันที
            playerRig.position = warpPointBeforeNPC.position;
            Debug.Log("Time's up! Player warped to NPC.");
        }
        
        // 3. ปิดการทำงานของปุ่มทั้งหมด
        foreach(DangerPointButton dp in FindObjectsOfType<DangerPointButton>())
        {
            dp.SetButtonActive(false);
        }
        if (finishButton != null) finishButton.SetActive(false);

        // 4. แสดง Score Summary
        DisplayScoreSummary(timeTaken, collectedDangerPoints);
    }
    
    private void DisplayScoreSummary(float timeTaken, int score)
    {
        scoreSummaryUI.SetActive(true);
        totalScoreText.text = $"คะแนนรวม: {score}/{totalDangerPoints} คะแนน";
        timeTakenText.text = $"เวลาที่ใช้ไป: {FormatTime(timeTaken)}";
    }

    // ฟังก์ชันสำหรับปุ่ม 'Next' ใน UI สรุปผล
    public void GoToNextScene()
    {
        // 1. บันทึกข้อมูลว่าผู้เล่นจะไปเกิดที่จุด B ในซีน TriageRoom (สำหรับรอบที่ 2)
        SetDestinationPointName("SpawnPointB"); 
        
        // 2. โหลด Scene ใหม่
        Debug.Log("Loading next scene: " + nextSceneName);
        SceneManager.LoadScene(nextSceneName);
    }
    
    // ฟังก์ชันช่วยในการอัพเดท UI เวลา
    private void UpdateTimerUI(float timeToDisplay)
    {
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);  
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        if (timerText != null)
        {
            timerText.text = $"เวลา: {minutes:00}:{seconds:00}";
            timerText.color = (timeToDisplay <= 60f && timeToDisplay > 0) ? Color.red : Color.white;
        }
    }

    // ฟังก์ชันช่วยในการแปลงวินาทีเป็นรูปแบบ MM:SS
    private string FormatTime(float timeInSeconds)
    {
        float minutes = Mathf.FloorToInt(timeInSeconds / 60);
        float seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
