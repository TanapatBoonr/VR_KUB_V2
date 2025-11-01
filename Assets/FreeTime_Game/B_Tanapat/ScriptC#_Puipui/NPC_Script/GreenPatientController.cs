using UnityEngine;
using UnityEngine.AI; 
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic; // เพิ่มเข้ามาสำหรับอนาคต

// ตรวจสอบว่าคุณได้สร้างไฟล์ TriageEnums.cs แล้ว และ TriageColor ถูกประกาศในนั้น

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

    // สถานะของ NPC
    private bool hasStoodUp = false; 
    private bool isTagged = false; 
    private bool isMovingToTreatment = false; 
    private bool isMegaphoneActive = false;
    private bool isWalkingToPlayer = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = true;
            navMeshAgent.isStopped = true;
        }

        // ตรวจสอบค่า Player Rig
        if (playerTransform == null)
        {
            Debug.LogError(gameObject.name + ": Player Transform ไม่ได้ถูกกำหนดใน Inspector!");
        }
    }

    void OnEnable()
    {
        // Subscribe Event จาก MegaphoneController
        MegaphoneController.OnMegaphoneStateChanged += OnMegaphoneStateChanged;

        // Subscribe Event เมื่อมีการติด Tag
        if (tagSocket != null)
        {
            tagSocket.selectEntered.AddListener(OnGreenTagAttached);
        }
    }

    void OnDisable()
    {
        // Unsubscribe Event เพื่อป้องกัน Error
        MegaphoneController.OnMegaphoneStateChanged -= OnMegaphoneStateChanged;
        
        if (tagSocket != null)
        {
            tagSocket.selectEntered.RemoveListener(OnGreenTagAttached);
        }
    }

    void Update()
    {
        // 1. ถ้าติด Tag แล้ว หรือ กำลังเดินไปจุดรักษา ให้ข้าม Update
        if (isTagged || isMovingToTreatment) return;

        // 2. ถ้ากำลังเดินหาผู้เล่น (isWalkingToPlayer) ให้จัดการเส้นทาง
        if (isWalkingToPlayer && playerTransform != null)
        {
            Vector3 targetPosition = playerTransform.position;
            
            // คำนวณระยะห่าง
            float distanceToPlayer = Vector3.Distance(transform.position, targetPosition);

            if (distanceToPlayer > walkTowardsPlayerDistance)
            {
                // เดินเข้าหา
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(targetPosition);
                animator.SetBool(PARAM_MOVE, true);
            }
            else
            {
                // หยุดยืน
                navMeshAgent.isStopped = true;
                animator.SetBool(PARAM_MOVE, false);
                transform.LookAt(playerTransform); // หันหน้าเข้าหาผู้เล่น
            }
        }
    }

    // --------------------------------------------------------------------
    // Event Handlers
    // --------------------------------------------------------------------

    // ถูกเรียกเมื่อมีการใช้โทรโข่ง
    private void OnMegaphoneStateChanged(bool isActive)
    {
        if (isTagged || isMovingToTreatment) return;

        isMegaphoneActive = isActive;
        
        if (isActive)
        {
            StandUpAndWalkToPlayer();
        }
        else
        {
            // ถ้าโทรโข่งหยุดทำงาน ให้หยุดเดิน
            isWalkingToPlayer = false;
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = true;
            }
            // อาจจะสั่งให้กลับไป Idle ท่าเดิม หรือ รอการเรียกใหม่
            if (animator != null)
            {
                 animator.SetBool(PARAM_MOVE, false);
            }
        }
    }
    
    // ถูกเรียกเมื่อมีการติด Tag
    public void ReceiveTriageTag(string tagColorName)
    {
        // ตรวจสอบว่า Tag ที่มาชนเป็นสีเขียวจริง ๆ
        if (tagColorName == TriageColor.Green.ToString())
        {
            isTagged = true;
            Debug.Log(gameObject.name + ": ได้รับ Tag " + tagColorName + " แล้ว!");

            // 1. หยุดการตอบสนองต่อผู้เล่น/โทรโข่งทันที
            isWalkingToPlayer = false;
            MegaphoneController.OnMegaphoneStateChanged -= OnMegaphoneStateChanged;
            
            // 2. เริ่มเดินไปจุดรักษา
            MoveToTreatmentArea();
        }
        else
        {
            // ถูกเรียกจาก TriageTagHandler.cs แต่เป็น Tag สีอื่น (Yellow, Red)
            Debug.LogWarning(gameObject.name + ": ได้รับ Tag ผิดสี (" + tagColorName + ") ไม่ทำอะไร");
        }
    }
    
    // ถูกเรียกเมื่อ Socket ตรวจจับว่ามี Item มาติด
    private void OnGreenTagAttached(SelectEnterEventArgs args)
    {
        // ตรวจสอบว่า Tag ที่มาติดมีสคริปต์ TriageTagHandler หรือไม่
        if (args.interactableObject.transform.TryGetComponent<TriageTagHandler>(out TriageTagHandler tagHandler))
        {
             // เราไม่จำเป็นต้องทำอะไรที่นี่ เพราะ TriageTagHandler จะเป็นคนเรียก ReceiveTriageTag(string)
             // โค้ดนี้มีไว้เพื่อ Handle การลากมาติดโดยตรง (แต่เราใช้ ReceiveTriageTag เป็นหลัก)
             // เพื่อให้ Logic สะอาด เราจะใช้ ReceiveTriageTag(string) เท่านั้น
        }
    }

    // --------------------------------------------------------------------
    // Movement Logic
    // --------------------------------------------------------------------

    private void StandUpAndWalkToPlayer()
    {
        if (hasStoodUp)
        {
            // ถ้าเคยยืนแล้ว ก็เริ่มเดินหา Player เลย
            isWalkingToPlayer = true;
            return;
        }

        // 1. สั่งให้ลุกยืน (ใช้ Trigger)
        if (animator != null)
        {
            // เรียก Trigger ให้ Animator เปลี่ยนจากท่านั่ง/นอน ไปเป็นท่า Stand Up
            animator.SetTrigger(PARAM_STANDUP_TRIGGER);
            hasStoodUp = true;
            
            // 2. หน่วงเวลา: เมื่อลุกยืนจบแล้ว ให้เริ่มเดิน
            // (ต้องตั้งเวลา standUpDuration ให้ตรงกับความยาวของ Animation "Stand Up" ของโมเดล)
            Invoke(nameof(StartWalkingToPlayer), standUpDuration);
        }
    }

    private void StartWalkingToPlayer()
    {
        isWalkingToPlayer = true;
        navMeshAgent.isStopped = false;
        // การตั้งค่าจุดหมายปลายทางจะเกิดขึ้นใน Update()
        Debug.Log(gameObject.name + ": เริ่มเดินหา Player แล้ว");
    }

    private void MoveToTreatmentArea()
    {
        isMovingToTreatment = true;
        isWalkingToPlayer = false; // หยุดเดินหา Player
        
        // ** ต้องตรวจสอบ NavMesh และ SetDestination เหมือนโค้ดเดิม
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
             Debug.LogError(gameObject.name + ": Green Treatment Area หรือ NavMeshAgent ไม่ได้ถูกกำหนด!");
        }
    }
}
