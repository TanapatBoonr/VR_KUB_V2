using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using System.Linq;

// ใช้สำหรับผู้บาดเจ็บประเภท Yellow (Delayed) และ Red (Immediate)
public class EmergencyPatientController : MonoBehaviour
{
    private Animator animator;
    
    // ตั้งชื่อ Animation Clip ของการบาดเจ็บ
    private const string ANIM_PAIN = "In Pain"; // ใช้ชื่อกลางๆ ที่ใช้ได้ทั้ง Red/Yellow
    // ตั้งชื่อ Animation Clip หลังถูก Tag/รักษา (สำหรับการยกร่าง)
    private const string ANIM_CURED = "Cured"; 

    [Header("--- Triage Configuration ---")]
    [Tooltip("กำหนดสีเริ่มต้นของผู้บาดเจ็บรายนี้ (Red หรือ Yellow)")]
    public TriageColor patientColor = TriageColor.Red;

    [Tooltip("เวลาทั้งหมดที่ผู้เล่นมีในการช่วยเหลือ (วินาที)")]
    public float maxTimeLimit = 120f; // 2 นาทีสำหรับ Red, 3 นาที (180s) สำหรับ Yellow

    [Tooltip("สี Triage ที่จะเปลี่ยนไปเมื่อหมดเวลา (เช่น Red -> Black, Yellow -> Red)")]
    public TriageColor criticalColor = TriageColor.Black;
    
    [Tooltip("ชื่อ Tag ของบัตร Triage ที่ถูกต้องสำหรับผู้บาดเจ็บรายนี้ (เช่น 'Red_Tag-Triage' หรือ 'Yellow_Tag-Triage')")]
    public string requiredTagName = "Red_Tag-Triage"; 

    [Header("--- External References ---")]
    [Tooltip("ลาก XRSocketInteractor ที่รับ Tag มาใส่")]
    public XRSocketInteractor tagSocket; 

    [Tooltip("GameObject ที่เป็นเปลหาม (Stretcher) ที่จะแสดงเมื่อรักษา/Tag เสร็จแล้ว")]
    public GameObject stretcherObject; 

    [Header("Wound Treatment Configuration")]
    [Tooltip("GameObject ที่มีสคริปต์ WoundController อยู่ (ถ้ามีหลายบาดแผล)")]
    public WoundController[] allWounds; 

    // สถานะ
    private bool isTagged = false;
    private bool isWoundsTreated = false; 
    private float timeRemaining; 
    private bool isCritical = false; // สถานะวิกฤตเมื่อหมดเวลา

    void Start()
    {
        animator = GetComponent<Animator>();
        timeRemaining = maxTimeLimit;
        
        // 1. ตรวจสอบและเริ่มต้น Animation
        if (animator != null)
        {
            animator.Play(ANIM_PAIN);
        }
        
        // 2. ตั้งค่า Event สำหรับการติด Tag ผ่าน Socket (วิธีการหลัก)
        if (tagSocket != null)
        {
            // ใช้ OnTagAttached เป็นชื่อเดียวกับใน RedPatientController
            tagSocket.selectEntered.AddListener(OnTagAttached);
        }

        // 3. ตั้งค่า Event สำหรับ Wound Controller
        foreach (var wound in allWounds)
        {
            if (wound != null)
            {
                wound.OnWoundTreated += OnWoundTreated;
            }
        }

        // 4. ซ่อนเปลหามตอนเริ่มต้น
        if (stretcherObject != null)
        {
            stretcherObject.SetActive(false);
        }
    }

    void Update()
    {
        // Logic นับเวลา: นับเมื่อยังไม่ถูก Tag และยังไม่วิกฤต
        if (!isTagged && !isCritical)
        {
            timeRemaining -= Time.deltaTime;

            if (timeRemaining <= 0)
            {
                ChangeToCriticalState();
                isCritical = true;
            }
        }
    }

    // --------------------------------------------------------------------
    // 1. ฟังก์ชันใหม่: ถูกเรียกจาก TriageTagHandler (เพื่อป้องกัน Error)
    // --------------------------------------------------------------------
    /// <summary>
    /// ถูกเรียกจาก TriageTagHandler เมื่อมี Tag มาชน/ติด (ใช้สำหรับจัดการคะแนน)
    /// </summary>
    /// <param name="receivedTagColor">สีของ Tag ที่มาติด (เช่น "Yellow", "Red")</param>
    public void ReceiveTriageTag(string receivedTagColor)
    {
        if (isTagged) return;

        // ตรวจสอบว่า Tag ที่ได้รับตรงกับสีที่คาดหวังหรือไม่
        if (receivedTagColor.Equals(patientColor.ToString(), System.StringComparison.OrdinalIgnoreCase)) 
        {
            // ถ้า Tag ถูกต้อง: ไม่ต้องทำอะไรมาก เพราะ Logic การ Tagging ที่แท้จริงจะเกิดจาก OnTagAttached (Socket)
            Debug.Log(gameObject.name + ": Correct Triage Tag detected by Handler.");
        }
        else
        {
            // ถ้า Tag ผิดสี: ควรมี Logic หักคะแนนที่นี่
             Debug.LogWarning(gameObject.name + $": Wrong Triage Tag ({receivedTagColor}) used. Expected: {patientColor}.");
             // *TODO: เพิ่ม Logic หักคะแนน (ScoringManager.Instance.AddScore(-penalty, "Mis-Triage"))*
        }
    }


    // --------------------------------------------------------------------
    // 2. Logic การติด Tag ผ่าน Socket
    // --------------------------------------------------------------------
    private void OnTagAttached(SelectEnterEventArgs args)
    {
        if (isTagged) return;

        // โค้ดเดิมที่ใช้ Socket ในการตรวจสอบ Tag
        // Note: เราเปรียบเทียบ Tag ของ GameObject ที่ถูก Interacted (บัตร Triage)
        if (args.interactableObject.transform.CompareTag(requiredTagName))
        {
            isTagged = true;
            Debug.Log(gameObject.name + $": {patientColor} Triage Tag attached via Socket. Patient is now tagged.");

            // หลัง Tagging สำเร็จ: ตรวจสอบสถานะการรักษา
            CheckIfComplete();
            
            // ลบ Listener เพื่อไม่ให้ติด Tag ซ้ำ
            tagSocket.selectEntered.RemoveListener(OnTagAttached);
        }
        else
        {
             // ถ้าติด Tag ผิดสีผ่าน Socket, ให้ Socket ปล่อย Tag ออก
             tagSocket.interactionManager.SelectExit(tagSocket, args.interactableObject);
             Debug.LogWarning(gameObject.name + ": Wrong Triage Tag used in Socket. Please use: " + requiredTagName);
        }
    }
    
    // --------------------------------------------------------------------
    // 3. Logic การรักษาบาดแผล
    // --------------------------------------------------------------------
    public void OnWoundTreated()
    {
        // ตรวจสอบว่าบาดแผลทั้งหมดถูกรักษาหมดหรือยัง
        isWoundsTreated = allWounds.All(w => w.isTreated);
        Debug.Log(gameObject.name + $": Wound treated. All wounds treated? {isWoundsTreated}");
        
        // ตรวจสอบสถานะว่าภารกิจสำเร็จหรือไม่
        CheckIfComplete();
    }
    
    // --------------------------------------------------------------------
    // 4. Logic ตรวจสอบความสำเร็จ (ทั้ง Tag และ Wound)
    // --------------------------------------------------------------------
    private void CheckIfComplete()
    {
        // ถ้าผู้ป่วยเป็นสี Yellow จะต้อง: 1. Tagged และ 2. Wounds Treated
        // ถ้าผู้ป่วยเป็นสี Red จะต้อง: 1. Tagged
        
        bool requiresWoundTreatment = allWounds.Length > 0;
        bool isConditionMet = isTagged && (!requiresWoundTreatment || isWoundsTreated);

        if (isConditionMet)
        {
            Debug.Log(gameObject.name + ": Mission complete for this patient! Changing to Cured State.");
            
            // *TODO: เพิ่ม Logic ให้คะแนน (ScoringManager.Instance.AddScore(100, "Patient Triage & Treatment Complete"))*

            ChangeToCuredState();
        }
    }

    // --------------------------------------------------------------------
    // 5. Logic การเปลี่ยน Animation และสถานะสุดท้าย
    // --------------------------------------------------------------------
    private void ChangeToCuredState()
    {
        // 1. เปลี่ยน Animation
        if (animator != null)
        {
             animator.Play(ANIM_CURED); 
        }

        // 2. แสดงเปลหาม (Army Stretcher)
        if (stretcherObject != null)
        {
            stretcherObject.SetActive(true);
        }
    }

    // --------------------------------------------------------------------
    // 6. Logic การเปลี่ยนสถานะวิกฤตเมื่อหมดเวลา
    // --------------------------------------------------------------------
    private void ChangeToCriticalState()
    {
        Debug.LogWarning(gameObject.name + $": Time limit expired. Patient changed from {patientColor} to {criticalColor}!");

        // *TODO: เพิ่ม Logic หักคะแนนสำหรับการจัดการช้าเกินไป*
        
        // *ตัวเลือก: เปลี่ยน Tag, เปลี่ยน Animation*
    }
}