using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using System.Linq; 

public class RedPatientController : MonoBehaviour
{
    private Animator animator;
    
    private const string ANIM_PAIN = "Red01 In Pain";
    private const string ANIM_CURED = "Red01 Cured"; 

    [Header("Triage Tag Configuration")]
    [Tooltip("ลาก XRSocketInteractor ที่รับ Red Tag มาใส่")]
    public XRSocketInteractor redTagSocket;
    
    // ชื่อของ Tag ที่ถูกต้องสำหรับ Socket นี้ (ควรเป็น Red_Tag-Triage)
    public string requiredTagName = "Red_Tag-Triage"; 

    // สถานะ
    private bool isTagged = false;
    private bool isWoundsTreated = false; 

    [Header("Wound Treatment Configuration")]
    [Tooltip("GameObject ที่มีสคริปต์ WoundController อยู่")]
    public WoundController[] allWounds; 

    void Start()
    {
        animator = GetComponent<Animator>();
        
        // 1. ตรวจสอบและเริ่มต้น Animation
        if (animator != null)
        {
            animator.Play(ANIM_PAIN);
        }
        
        // 2. ตั้งค่า Event สำหรับการติด Tag
        if (redTagSocket != null)
        {
            redTagSocket.selectEntered.AddListener(OnRedTagAttached);
        }
    }
    
    // --------------------------------------------------------------------
    // NEW: ฟังก์ชันที่ TriageTagHandler จะใช้เรียก
    // --------------------------------------------------------------------
    public void ReceiveTriageTag(string tagColorName)
    {
        // ตรวจสอบว่า Tag ที่เข้ามาชนเป็นสีแดง (Red) ตามที่ต้องการหรือไม่
        if (tagColorName == TriageColor.Red.ToString()) 
        {
            if (!isTagged)
            {
                isTagged = true;
                Debug.Log(gameObject.name + ": ได้รับ Tag Red จาก TriageTagHandler แล้ว.");
                
                // สั่งเปลี่ยนท่าทางทันที (หรือรอดูการรักษาแผลก่อนก็ได้)
                ChangeToCuredState();
            }
        }
        else
        {
            // ถูกเรียกจาก Tag สีอื่น (Green/Yellow/Black)
            Debug.LogWarning(gameObject.name + ": ได้รับ Tag ผิดสี (" + tagColorName + ") จาก TriageTagHandler.");
        }
    }

    // --------------------------------------------------------------------
    // 2. Logic การติด Tag (Tag Attachment)
    // --------------------------------------------------------------------
    private void OnRedTagAttached(SelectEnterEventArgs args)
    {
        if (isTagged) return;

        // โค้ดเดิมที่ใช้ Socket ในการตรวจสอบ Tag
        if (args.interactableObject.transform.CompareTag(requiredTagName))
        {
            // Note: ReceiveTriageTag(string) จะถูกเรียกซ้ำจาก TriageTagHandler ด้วย 
            // แต่เรามี Guard Clause (if (!isTagged)) เพื่อป้องกันการทำงานซ้ำ
            
            isTagged = true;
            Debug.Log(gameObject.name + ": Red Triage Tag attached via Socket. Mission Complete for this NPC.");

            ChangeToCuredState();
            
            redTagSocket.selectEntered.RemoveListener(OnRedTagAttached);
        }
        else
        {
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
