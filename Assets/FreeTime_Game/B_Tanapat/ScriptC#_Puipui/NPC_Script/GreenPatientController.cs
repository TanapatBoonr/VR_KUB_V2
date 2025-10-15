using UnityEngine;
using UnityEngine.AI; 

// ตรวจสอบว่าคุณได้สร้างไฟล์ TriageEnums.cs แล้ว และ TriageColor ถูกประกาศในนั้น

public class GreenPatientController : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent navMeshAgent;

    private const string PARAM_MOVE = "Move";

    // การตั้งค่าใน Inspector (สำคัญ: ต้องลากมาใส่ใน Unity Inspector)
    public Transform greenTreatmentArea; // จุดหมายปลายทางสุดท้าย (Plane_G)
    public Transform playerTransform; // ลาก XR Rig/Player's Camera มาใส่
    public float walkTowardsPlayerDistance = 3f; // NPC จะหยุดยืนห่างจาก Player กี่เมตร

    // สถานะของ NPC
    private bool hasStoodUp = false; // เคยลุกขึ้นยืนแล้ว
    private bool isTagged = false; // ติด Tag แล้ว
    private bool isMovingToTreatment = false; // กำลังเดินไป Plane_G

    void Start()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = false;
            animator.Play("Sitting");
        }
    }

    void OnEnable()
    {
        MegaphoneController.OnMegaphoneStateChanged += OnMegaphoneStateChanged;
    }

    void OnDisable()
    {
        MegaphoneController.OnMegaphoneStateChanged -= OnMegaphoneStateChanged;
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
        if (isTagged) return; 

        if (isActive) // โทรโข่งเริ่มพูด
        {
            if (!hasStoodUp)
            {
                StartStandingUpSequence();
            }
            else
            {
                StartInitialMovement(); 
            }
        }
        else // โทรโข่งหยุดพูด
        {
            if (hasStoodUp)
            {
                StopMovementAndGoIdle();
            }
        }
    }

    private void StartStandingUpSequence()
    {
        animator.Play("Standing Up");
        hasStoodUp = true;
        Invoke("StartInitialMovement", 1.5f);
    }

    private void StartInitialMovement()
    {
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
    // 5. Player แปะบัตรสีเขียว -> 6. เดินไป Plane_G (ส่วนที่แก้ปัญหา "สีแดง")
    // -----------------------------------------------------------
    public void ReceiveTriageTag(string tagReceived)
    {
        // NPC สีเขียวต้องถูกติด Tag สีเขียวเท่านั้น
        if (tagReceived != TriageColor.Green.ToString())
        {
            return;
        }

        if (isTagged) return;

        isTagged = true;
        isMovingToTreatment = true; // ตั้งค่าสถานะการเดินไปจุดหมายสุดท้าย

        Debug.Log("Player ได้คะแนนจากการติด Green Tag");

        // 6. เดินไปที่ Plane สีเขียว
        if (greenTreatmentArea != null)
        {
            if (navMeshAgent != null)
            {
                CancelInvoke();
                navMeshAgent.enabled = true;
                navMeshAgent.isStopped = false;

                Vector3 destination = greenTreatmentArea.position;

                // พยายามหาตำแหน่งที่ใกล้ที่สุดบน NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(destination, out hit, 1.0f, NavMesh.AllAreas))
                {
                    destination = hit.position;
                }
                else
                {
                    // !!! ERROR A: จุดหมายไม่อยู่บน NavMesh
                    Debug.LogError("!!! (DEBUG A) ตำแหน่ง Green Treatment Area ไม่อยู่บน NavMesh ที่ Bake ไว้ !!!");
                    animator.SetBool(PARAM_MOVE, false);
                    isMovingToTreatment = false;
                    return;
                }

                // 2. สั่งให้เดินไปยังจุดหมายที่อยู่บน NavMesh
                if (navMeshAgent.SetDestination(destination))
                {
                    // SUCCESS: กำลังเดิน
                    animator.SetBool(PARAM_MOVE, true);
                    Debug.Log(gameObject.name + " (DEBUG: SUCCESS) เริ่มเดินไป Green Treatment Area แล้ว");
                }
                else
                {
                    // !!! ERROR B: คำนวณเส้นทางไม่ได้
                    Debug.LogError("!!! (DEBUG B) NavMesh Agent คำนวณเส้นทางไป Green Treatment Area ไม่ได้ !!!");
                    animator.SetBool(PARAM_MOVE, false);
                    isMovingToTreatment = false;
                }
            }
        }
        else
        {
             // !!! ERROR C: ลืมลาก GameObject
             Debug.LogError("!!! (DEBUG C) Green Treatment Area (Plane_G) ไม่ได้ถูกกำหนดใน Inspector !!!");
        }
    }
}