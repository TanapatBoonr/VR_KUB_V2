using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using System.Linq; // ต้องเพิ่ม

public class RedPatientController : MonoBehaviour
{
    private Animator animator;
    
    // ตั้งชื่อ Animation Clip ของการบาดเจ็บ
    private const string ANIM_PAIN = "Red01 In Pain";
    // ตั้งชื่อ Animation Clip หลังถูก Tag/รักษา
    private const string ANIM_CURED = "Red01 Cured"; 

    [Header("Triage Tag Configuration")]
    [Tooltip("ลาก XRSocketInteractor ที่รับ Red Tag มาใส่")]
    public XRSocketInteractor redTagSocket;
    
    // ชื่อของ Tag ที่ถูกต้องสำหรับ Socket นี้ (ควรเป็น Red_Tag-Triage)
    public string requiredTagName = "Red_Tag-Triage"; 

    // สถานะ
    private bool isTagged = false;
    private bool isWoundsTreated = false; // NEW: สถานะการรักษาบาดแผล

    [Header("Wound Treatment Configuration")]
    [Tooltip("GameObject ที่มีสคริปต์ WoundController อยู่")]
    public WoundController[] allWounds; 
    
    // *** ลบ: private RedPatientBodyPart[] bodyParts; (เพราะคลาสนี้ยังไม่ได้สร้าง) ***

    void Start()
    {
        animator = GetComponent<Animator>();
        
        // 1. ตรวจสอบและเริ่มต้น Animation
        if (animator != null)
        {
            animator.Play(ANIM_PAIN);
        }
        
        // 2. ปิด Socket ไว้ก่อน (จะเปิดเมื่อรักษาบาดแผลเสร็จ)
        if (redTagSocket != null)
        {
            redTagSocket.gameObject.SetActive(false); 
        }

        // *** ลบ: 3. หาส่วนของร่างกาย (ถ้ามี) ***
        // bodyParts = GetComponentsInChildren<RedPatientBodyPart>();

        // 4. หาสคริปต์ WoundController ที่แนบกับ NPC หรือ Child ของ NPC
        if (allWounds == null || allWounds.Length == 0)
        {
            allWounds = GetComponentsInChildren<WoundController>();
        }

        // 5. เชื่อมต่อ Event การรักษา
        foreach (var wound in allWounds)
        {
            // ตรวจสอบ null เพื่อป้องกันข้อผิดพลาดหากลืมแนบ
            if (wound != null) 
            {
                wound.OnWoundTreated += CheckAllWoundsTreated;
            }
        }

        // 6. เชื่อมต่อ Event การติด Tag
        if (redTagSocket != null)
        {
            redTagSocket.selectEntered.AddListener(OnRedTagAttached);
        }
        
        // 7. ตรวจสอบสถานะเริ่มต้นของบาดแผล
        CheckAllWoundsTreated();
    }
    
    // --------------------------------------------------------------------
    // 1. Logic การรักษาบาดแผล (Wound Treatment)
    // --------------------------------------------------------------------

    private void CheckAllWoundsTreated()
    {
        if (isWoundsTreated) return;

        bool allTreated = true;
        foreach (var wound in allWounds)
        {
            if (wound != null && !wound.isTreated)
            {
                allTreated = false;
                break;
            }
        }

        if (allTreated)
        {
            isWoundsTreated = true;
            Debug.Log(gameObject.name + ": All critical wounds treated. Ready for Triage Tag.");
            
            if (redTagSocket != null)
            {
                redTagSocket.gameObject.SetActive(true);
            }
        }
    }

    // --------------------------------------------------------------------
    // 2. Logic การติด Tag (Tag Attachment)
    // --------------------------------------------------------------------
    private void OnRedTagAttached(SelectEnterEventArgs args)
    {
        if (isTagged) return;

        // ตรวจสอบชื่อ Tag ให้ถูกต้อง (ใช้ CompareTag ใน Interactable Object)
        // เนื่องจาก args.interactableObject เป็น IXRSelectInteractable 
        // เราต้องเข้าถึง GameObject ผ่าน Transform
        if (args.interactableObject.transform.CompareTag(requiredTagName))
        {
            isTagged = true;
            Debug.Log(gameObject.name + ": Red Triage Tag attached. Mission Complete for this NPC.");

            ChangeToCuredState();
            
            redTagSocket.selectEntered.RemoveListener(OnRedTagAttached);
        }
        else
        {
             // ถ้าติด Tag ผิดประเภท ให้ปล่อยออกมา
             redTagSocket.interactionManager.SelectExit(redTagSocket, args.interactableObject);
             Debug.LogWarning(gameObject.name + ": Wrong Triage Tag used. Please use: " + requiredTagName);
        }
    }

    // --------------------------------------------------------------------
    // 3. Logic การเปลี่ยน Animation (Final State)
    // --------------------------------------------------------------------
    private void ChangeToCuredState()
    {
        if (animator != null)
        {
             animator.Play(ANIM_CURED);
        }
    }
}