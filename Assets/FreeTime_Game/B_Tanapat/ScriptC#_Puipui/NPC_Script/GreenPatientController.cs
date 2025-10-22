using UnityEngine;
using UnityEngine.AI; 
using UnityEngine.XR.Interaction.Toolkit; // <<< เพิ่มสำหรับ Socket

// ตรวจสอบว่าคุณได้สร้างไฟล์ TriageEnums.cs แล้ว และ TriageColor ถูกประกาศในนั้น
// (สมมติว่า TriageColor.Green.ToString() ส่งค่ากลับมาเป็น "Green")

public class GreenPatientController : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private const string PARAM_MOVE = "Move";

    // การตั้งค่าใน Inspector (สำคัญ: ต้องลากมาใส่ใน Unity Inspector)
    public Transform greenTreatmentArea; // จุดหมายปลายทางสุดท้าย (Plane_G)
    public Transform playerTransform; // ลาก XR Rig/Player's Camera มาใส่
    public float walkTowardsPlayerDistance = 3f; // NPC จะหยุดยืนห่างจาก Player กี่เมตร
    public float standUpDuration = 6.0f; 

    [Header("Triage Socket")]
    public XRSocketInteractor tagSocket; // <<< NEW: ลาก XR Socket บน NPC มาใส่

    // สถานะของ NPC
    private bool hasStoodUp = false; // เคยลุกขึ้นยืนแล้ว
    private bool isTagged = false; // ติด Tag แล้ว
    private bool isMovingToTreatment = false; // กำลังเดินไป Plane_G
    private bool isMegaphoneActive = false; 
    private bool isFirstMegaphoneCheckDone = false; // <<< NEW: ป้องกันการยืนเองตอนเริ่มต้น

    void Start()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = false;
            animator.Play("G2 Sitting Pose");
        }
    }

    void OnEnable()
    {
        MegaphoneController.OnMegaphoneStateChanged += OnMegaphoneStateChanged;
        
        // 1. NEW: สมัครรับ Event เมื่อมีวัตถุถูกแปะเข้า Socket สำเร็จ
        if (tagSocket != null)
        {
            tagSocket.selectEntered.AddListener(OnTagSocketed);
        }
    }

    void OnDisable()
    {
        MegaphoneController.OnMegaphoneStateChanged -= OnMegaphoneStateChanged;
        
        // 2. NEW: ยกเลิกการสมัครรับ Event
        if (tagSocket != null)
        {
            tagSocket.selectEntered.RemoveListener(OnTagSocketed);
        }
    }

    // ตรวจสอบสถานะการหยุดระหว่างการเดิน
    void Update()
    {
        // ถ้าอยู่ในขั้นตอนเดินหา Player และยังไม่ติด Tag
        if (hasStoodUp && !isTagged && !isMovingToTreatment && navMeshAgent.enabled && navMeshAgent.hasPath)
        {
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                if (navMeshAgent.velocity.sqrMagnitude < 0.1f)
                {
                    StopMovementAndGoIdle();
                }
            }
        }

        // ถ้ากำลังเดินไป Plane_G (isMovingToTreatment)
        if (isMovingToTreatment && navMeshAgent.enabled)
        {
            // ตรวจสอบว่าถึงจุดหมายสุดท้ายแล้วหรือยัง
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance &&
                navMeshAgent.velocity.sqrMagnitude < 0.1f)
            {
                Debug.Log(gameObject.name + " ถึง Green Treatment Area แล้ว");
                animator.SetBool(PARAM_MOVE, false);
                isMovingToTreatment = false;
            }
        }
    }

    private void OnMegaphoneStateChanged(bool isActive)
    {
        // Debug Check เพื่อตรวจสอบว่า Event ถูกยิงตอนไหน
        Debug.Log(gameObject.name + " >> Megaphone State Changed. IsActive: " + isActive + " Time: " + Time.time); 

        if (isTagged) return; 

        isMegaphoneActive = isActive;
        
        // 1. ถ้าโทรโข่งเปิด (isActive = true)
        if (isActive) 
        {
            isFirstMegaphoneCheckDone = true; // ยืนยันว่ามีการเปิดโทรโข่งแล้วจริง
            
            if (!hasStoodUp)
            {
                StartStandingUpSequence();
            }
            else
            {
                StartInitialMovement(); 
            }
        }
        // 2. ถ้าโทรโข่งปิด (isActive = false)
        else 
        {
            // ถ้าเป็นการเปลี่ยนแปลงสถานะครั้งแรก (เกมเพิ่งเริ่ม) ให้เพิกเฉย
            if (!isFirstMegaphoneCheckDone)
            {
                return;
            }

            // ถ้าไม่ใช่การเปลี่ยนแปลงครั้งแรก (ผู้เล่นกดปิดโทรโข่ง)
            if (hasStoodUp)
            {
                StopMovementAndGoIdle();
            }
        }
    }

    private void StartStandingUpSequence()
    {
        // ป้องกันการยืน หาก Megaphone ถูกปิดไปแล้ว
        if (!isMegaphoneActive) 
        {
            Debug.Log(gameObject.name + ": Megaphone is not active. Cancelling stand-up sequence.");
            return;
        }
        
        // **[แก้ไข]: ใช้ Trigger แทน Play**
    	animator.SetTrigger("StandUp");
        hasStoodUp = true;
    	Invoke("StartInitialMovement", standUpDuration);
    }

    private void StartInitialMovement()
    {
        // ... (โค้ด StartInitialMovement() เดิม) ...
        if (navMeshAgent != null && playerTransform != null)
        {
            navMeshAgent.enabled = true;
            navMeshAgent.isStopped = false;

            Vector3 targetPosition = playerTransform.position;
            Vector3 directionToPlayer = (targetPosition - transform.position).normalized;
            Vector3 walkDestination = targetPosition - (directionToPlayer * walkTowardsPlayerDistance);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(walkDestination, out hit, 1.0f, NavMesh.AllAreas))
            {
                navMeshAgent.SetDestination(hit.position);
                animator.SetBool(PARAM_MOVE, true);
            }
            else
            {
                Debug.LogError("ไม่พบตำแหน่งที่ถูกต้องบน NavMesh ใกล้ Player!");
                StopMovementAndGoIdle();
            }
        }
        else if (playerTransform == null)
        {
            Debug.LogError("Player Transform is not assigned in the Inspector for " + gameObject.name);
        }
    }

    private void StopMovementAndGoIdle()
    {
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            animator.SetBool(PARAM_MOVE, false);
        }
    }
    
    // -----------------------------------------------------------
    // *** NEW: ถูกเรียกเมื่อมีบัตรถูกวางใน Socket ***
    // -----------------------------------------------------------
    private void OnTagSocketed(SelectEnterEventArgs args)
    {
        // ดึง Tag ของบัตรที่ถูกแปะ (เช่น "Green" หรือ "Red")
        // เราใช้ Tag ในการตรวจสอบ เพราะอาจมีวัตถุอื่นมาติด Socket ได้
        string tagColorString = args.interactableObject.transform.gameObject.tag; 

        // เรียก Logic การ Tag เดิม
        ReceiveTriageTag(tagColorString);

        // หมายเหตุ: การแปะบัตรสำเร็จแล้ว (บัตรติดอยู่กับ NPC)
    }

    // -----------------------------------------------------------
    // 5. Logic การ Tag (ถูกเรียกโดย Socket Event)
    // -----------------------------------------------------------
    public void ReceiveTriageTag(string tagReceived)
    {
        // ตรวจสอบว่าเป็น Tag ที่ถูกต้องสำหรับ NPC ตัวนี้หรือไม่
        // (สมมติว่า TriageColor.Green.ToString() == "Green" และ Tag ของบัตรคือ "Green")
        if (tagReceived != "Green") 
        {
            return;
        }

        if (isTagged) return;

        isTagged = true;
        isMovingToTreatment = true; 
    
        Debug.Log("Green Tag Socketed successfully. NPC is preparing to move.");
    
        // 1. หยุดการเคลื่อนไหวทั้งหมดและแอนิเมชันทันที
        CancelInvoke(); 
        StopMovementAndGoIdle(); 
    
        // 2. หน่วงเวลา 0.5 วินาที ก่อนเริ่มเดิน (Acknowledgment Pause)
        Invoke("StartWalkingToTreatmentArea", 0.5f);
    }
    
    // *** StartWalkingToTreatmentArea() (ส่วนการเดินไป Plane_G) ***
    private void StartWalkingToTreatmentArea()
    {
        if (greenTreatmentArea != null)
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = true;
                navMeshAgent.isStopped = false;

                Vector3 destination = greenTreatmentArea.position;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(destination, out hit, 1.0f, NavMesh.AllAreas))
                {
                    destination = hit.position;
                }
                else
                {
                    Debug.LogError("!!! (DEBUG A) ตำแหน่ง Green Treatment Area ไม่อยู่บน NavMesh ที่ Bake ไว้ !!!");
                    animator.SetBool(PARAM_MOVE, false);
                    isMovingToTreatment = false;
                    return;
                }

                if (navMeshAgent.SetDestination(destination))
                {
                    animator.SetBool(PARAM_MOVE, true);
                    Debug.Log(gameObject.name + " (DEBUG: SUCCESS) เริ่มเดินไป Green Treatment Area แล้ว");
                }
                else
                {
                    Debug.LogError("!!! (DEBUG B) NavMesh Agent คำนวณเส้นทางไป Green Treatment Area ไม่ได้ !!!");
                    animator.SetBool(PARAM_MOVE, false);
                    isMovingToTreatment = false;
                }
            }
        }
        else
        {
             Debug.LogError("!!! (DEBUG C) Green Treatment Area (Plane_G) ไม่ได้ถูกกำหนดใน Inspector !!!");
        }
    }
}