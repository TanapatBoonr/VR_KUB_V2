using UnityEngine;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private int score = 0;
    public int totalObjects = 10;

    [Header("Mission Complete UI")]
    public GameObject missionCompleteUI;   // Popup UI
    public TextMeshProUGUI missionText;    // ข้อความ Mission Complete / You Lose
    public Transform playerHead;           // XR Camera
    public float uiDistance = 2f;          // ระยะห่างจากผู้เล่น

    [Header("Timer Settings")]
    public float totalTime = 60f;          // เวลาทั้งหมด (วินาที)
    private float remainingTime;
    private bool timerStarted = false;
    private bool missionEnded = false;     // ป้องกันซ้ำ
    private Coroutine timerCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 🕹️ ซ่อน UI ตอนเริ่มเกม
        if (missionCompleteUI != null)
            missionCompleteUI.SetActive(false);
    }

    public void AddScore()
    {
        if (!timerStarted)
        {
            StartTimer();
        }

        score++;
        Debug.Log($"คะแนนปัจจุบัน: {score}/{totalObjects}");

        if (score >= totalObjects && !missionEnded)
        {
            missionEnded = true;
            StopTimer();
            MissionComplete();
        }
    }

    // ⏱️ เริ่มจับเวลา
    private void StartTimer()
    {
        remainingTime = totalTime;
        timerStarted = true;
        timerCoroutine = StartCoroutine(TimerCountdown());
    }

    // ⏳ นับถอยหลัง
    private IEnumerator TimerCountdown()
    {
        while (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        if (!missionEnded)
        {
            missionEnded = true;
            TimeUp();
        }
    }

    // ✋ หยุดเวลา
    private void StopTimer()
    {
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
        timerStarted = false;
    }

    // ✅ ภารกิจสำเร็จ
    private void MissionComplete()
    {
        Debug.Log("🎉 ภารกิจสำเร็จ!");
        ShowMissionUI($"Mission Complete!\n{score}/{totalObjects}");
    }

    // ❌ ภารกิจล้มเหลว
    private void TimeUp()
    {
        Debug.Log("หมดเวลา! ภารกิจล้มเหลว!");
        ShowMissionUI($"You Lose!\nScore: {score}/{totalObjects}");
    }

    // 🪟 แสดง UI หน้าผู้เล่น
    private void ShowMissionUI(string message)
    {
        if (missionCompleteUI != null)
        {
            if (playerHead != null)
            {
                Vector3 forward = playerHead.forward;
                forward.y = 0;
                missionCompleteUI.transform.position = playerHead.position + forward.normalized * uiDistance;
                missionCompleteUI.transform.LookAt(playerHead);
                missionCompleteUI.transform.Rotate(0, 180, 0);
            }

            missionCompleteUI.SetActive(true);

            if (missionText != null)
                missionText.text = message;
        }
    }

    public void CloseMissionUI()
    {
        if (missionCompleteUI != null)
            missionCompleteUI.SetActive(false);
    }

    public int GetScore()
    {
        return score;
    }
}
