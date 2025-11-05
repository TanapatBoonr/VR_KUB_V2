using UnityEngine;
using UnityEngine.AI; 
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections; // เพิ่มเข้ามาสำหรับ Coroutine
using System.Collections.Generic; 

// ***************************************************************
// TriageColor ถูกดึงมาจาก TriageEnums.cs
// ***************************************************************

public class GreenPatientController : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent navMeshAgent;

    // พารามิเตอร์ของ Animator (ใช้ชื่อกลางๆ ที่ใช้กับทุกโมเดล)
    private const string PARAM_STANDUP_TRIGGER = "StandUp"; // Trigger สำหรับเริ่มลุกยืน
    private const string PARAM_MOVE = "Move";              // Bool สำหรับ Walk/Idle

    // การตั้งค่าใน Inspector (สำคัญ: ต้องลากมาใส่ใน Unity Inspector)
    [Header("Triage & Movement Settings")]
    [Tooltip("จุดหมายปลายทางสุดท้าย (Plane_G)")]
    public Transform greenTreatmentArea; 
    
    [Tooltip("ลาก XR Rig/Player's Camera มาใส่")]
    public Transform playerTransform; 
    
    [Tooltip("NPC จะหยุดยืนห่างจาก Player กี่เมตร")]
    public float walkTowardsPlayerDistance = 3f; 
    
    [Tooltip("ระยะเวลาที่ใช้ในการลุกขึ้นยืน (ใช้สำหรับการหน่วงเวลา)")]
    public float standUpDuration = 6.0f; 

    [Header("Triage Socket")]
    [Tooltip("ลาก XR Socket บน NPC มาใส่")]
    public XRSocketInteractor tagSocket; 

    // *** ตัวแปรนี้จำเป็นสำหรับการอ้างอิง Tag (แต่ไม่ถูกใช้ใน TriageTagHandler ที่แก้ไขแล้ว) ***
    [Tooltip("ชื่อ Tag ของบัตร Triage ที่ถูกต้องสำหรับผู้ป่วยรายนี้ (สำหรับอ้างอิง)")]
    public string requiredTagName = "Green_Tag-Triage"; 
    
    // สถานะ
    private bool isLyingDown = true;
    private bool isWalkingToPlayer = false; 
    private bool isMovingToTreatment = false;
    private bool isTagged = false; // สถานะใหม่เพื่อตรวจสอบว่าถูก Tag แล้วหรือยัง

    void Start()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        // Note: Logic การเดินเข้าหา Player จะถูกเรียกใน Update()
    }

    void Update()
    {
        // ตรวจสอบว่ายังนอนอยู่และยังไม่ถูก Tag
        if (isLyingDown && !isTagged)
        {
            // ตรรกะ: ตรวจสอบระยะห่างจาก Player และเรียก StandUp()
            if (playerTransform != null && !isWalkingToPlayer)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                if (distance < 5f) // สมมติว่าระยะ 5 เมตร (อาจจะปรับให้ยืนทันทีที่ Scene โหลด)
                {
                    // ถ้ายังไม่ถูก Tag ให้ลุกขึ้นยืนแล้วเดินหา Player
                    StandUp();
                }
            }
        }
        else if (isWalkingToPlayer)
        {
            // ตรรกะ: เดินเข้าหา Player จนถึงระยะที่กำหนด
            if (navMeshAgent.enabled && playerTransform != null)
            {
                float remainingDistance = Vector3.Distance(transform.position, playerTransform.position);
                
                if (remainingDistance > walkTowardsPlayerDistance)
                {
                    navMeshAgent.SetDestination(playerTransform.position);
                }
                else
                {
                    // ถึงระยะแล้ว: หยุดเดิน
                    navMeshAgent.isStopped = true;
                    animator.SetBool(PARAM_MOVE, false);
                    isWalkingToPlayer = false;
                    Debug.Log(gameObject.name + ": Stopped walking towards player.");
                }
            }
        }
    }
    
    // ***************************************************************
    // Logic การรับ Tag จาก TriageTagHandler
    // ***************************************************************
    /// <summary>
    /// ถูกเรียกจาก TriageTagHandler เมื่อมี Tag มาชน/ติด
    /// </summary>
    /// <param name="receivedTagColor">สีของ Tag ที่มาติด (เช่น "Green", "Red")</param>
    public void ReceiveTriageTag(string receivedTagColor)
    {
        if (isTagged) return;

        // ตรวจสอบว่า Tag ที่ได้รับคือ "Green" หรือไม่
        if (receivedTagColor.Equals(TriageColor.Green.ToString(), System.StringComparison.OrdinalIgnoreCase)) 
        {
            isTagged = true;
            Debug.Log(gameObject.name + ": Correct Triage Tag (Green) received. Starting movement to Treatment Area.");
            
            // 1. ถ้ายังนอนอยู่ ให้ลุกขึ้นยืนแล้วค่อยเดินไป Treatment
            if (isLyingDown)
            {
                 StandUp();
                 StartCoroutine(MoveToTreatmentAfterStandUp());
            }
            else
            {
                 // ถ้าลุกขึ้นยืนอยู่แล้ว (เดินหา Player อยู่) ให้เดินไป Treatment เลย
                 MoveToTreatmentArea();
            }

            // 2. ถ้าต้องการให้ Tag ติดกับตัว NPC ทันที
            // *หมายเหตุ: ต้องหา TriageTagHandler ที่เรียกฟังก์ชันนี้เพื่อดึง transform ของ Tag
            // แต่เนื่องจาก TriageTagHandler เองก็มีฟังก์ชัน AttachTagToPatient
            // การทำให้ Tag ติดกับตัว NPC ควรทำใน TriageTagHandler หรือใช้ Socket อย่าง Red/Yellow
            // หากคุณต้องการให้ Tag ติด ให้แน่ใจว่า TriageTagHandler ได้เรียก AttachTagToPatient
        }
        else
        {
             Debug.LogWarning(gameObject.name + $": Wrong Triage Tag ({receivedTagColor}) used. This patient is Green.");
        }
    }
    // ***************************************************************

    private void StandUp()
    {
        if (!isLyingDown) return;

        isLyingDown = false;
        animator.SetTrigger(PARAM_STANDUP_TRIGGER);
        Debug.Log(gameObject.name + ": Starting stand up sequence.");
        
        // หยุดเดินหา Player ชั่วคราว (ถ้ามีการเริ่มเดินแล้ว)
        isWalkingToPlayer = false;
    }

    private IEnumerator MoveToTreatmentAfterStandUp()
    {
        // รอให้ Animation ลุกขึ้นยืนจบก่อน (ใช้ค่า standUpDuration)
        yield return new WaitForSeconds(standUpDuration); 
        
        // ตรวจสอบอีกครั้งเพื่อความปลอดภัย
        if (isTagged)
        {
            MoveToTreatmentArea();
        }
    }

    private void MoveToTreatmentArea()
    {
        // หยุดเดินหา Player ชั่วคราว (ถ้ามีการเริ่มเดินแล้ว)
        if (isWalkingToPlayer)
        {
            isWalkingToPlayer = false; 
        }
        
        isMovingToTreatment = true;
        
        // ** ต้องตรวจสอบ NavMesh และ SetDestination
        if (greenTreatmentArea != null && navMeshAgent != null)
        {
            navMeshAgent.enabled = true;
            navMeshAgent.isStopped = false;
            animator.SetBool(PARAM_MOVE, true);

            Vector3 destination = greenTreatmentArea.position;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(destination, out hit, 1.0f, NavMesh.AllAreas))
            {
                destination = hit.position;
            }
            else
            {
                Debug.LogError(gameObject.name + ": ตำแหน่ง Green Treatment Area ไม่อยู่บน NavMesh!");
                animator.SetBool(PARAM_MOVE, false);
                isMovingToTreatment = false;
                return;
            }

            if (!navMeshAgent.SetDestination(destination))
            {
                Debug.LogError(gameObject.name + ": NavMesh Agent คำนวณเส้นทางไป Green Treatment Area ไม่ได้!");
                animator.SetBool(PARAM_MOVE, false);
                isMovingToTreatment = false;
            }
            // เมื่อเดินไปถึง NavMeshAgent จะหยุดเอง
        }
        else
        {
             Debug.LogError(gameObject.name + ": Green Treatment Area หรือ NavMeshAgent ไม่ถูกกำหนดค่า!");
        }
    }
}