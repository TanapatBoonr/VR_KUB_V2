using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class GreenPatientController : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;

    private const string PARAM_STANDUP_TRIGGER = "StandUp";
    private const string PARAM_MOVE = "Move";

    [Header("Movement Settings")]
    public Transform playerTransform;
    public Transform greenTreatmentArea;
    public float walkTowardsPlayerDistance = 3f;
    public float standUpDuration = 6f;

    [Header("Triage Tag Settings")]
    [Tooltip("ลาก XR Socket Interactor ที่ติดอยู่กับตัว NPC มาใส่")]
    public XRSocketInteractor tagSocket;

    [Tooltip("ลาก Prefab ของบัตรสีเขียวที่ถูกต้องมาใส่ในช่องนี้")]
    public GameObject correctTagPrefab; // ✅ Prefab ของบัตรที่ถูกต้อง

    private bool isLying = true;
    private bool isWalkingToPlayer = false;
    private bool isTagged = false;
    private bool megaphoneActive = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        // ฟัง Event จาก Megaphone
        MegaphoneController.OnMegaphoneStateChanged += OnMegaphoneStateChanged;

        // ฟัง Event จาก XR Socket (เมื่อมีบัตรถูกวางลงใน Socket)
        if (tagSocket != null)
            tagSocket.selectEntered.AddListener(OnTagPlaced);
    }

    void OnDestroy()
    {
        MegaphoneController.OnMegaphoneStateChanged -= OnMegaphoneStateChanged;

        if (tagSocket != null)
            tagSocket.selectEntered.RemoveListener(OnTagPlaced);
    }

    // เมื่อมีบัตรถูกวางลงใน Socket
    private void OnTagPlaced(SelectEnterEventArgs args)
    {
        GameObject tagObj = args.interactableObject.transform.gameObject;

        // ✅ ตรวจชื่อ prefab แทน PrefabUtility (ใช้ได้ใน Runtime)
        if (correctTagPrefab != null && tagObj.name.Contains(correctTagPrefab.name))
        {
            Debug.Log($"{name}: ได้รับบัตร Prefab ที่ถูกต้องแล้ว!");
            ReceiveTriageTag("Green");
        }
        else
        {
            Debug.LogWarning($"{name}: บัตรที่ใส่มาไม่ตรงกับ Prefab ที่กำหนดไว้!");
        }
    }

    // -------------------------------------------------------
    // โทรโข่ง
    // -------------------------------------------------------
    private void OnMegaphoneStateChanged(bool state)
    {
        megaphoneActive = state;

        if (megaphoneActive && !isTagged)
        {
            if (isLying)
            {
                StartCoroutine(StandUpAndWalkToPlayer());
            }
            else
            {
                StartWalkingToPlayer();
            }
        }
        else if (!megaphoneActive && !isTagged)
        {
            StopWalking();
        }
    }

    private IEnumerator StandUpAndWalkToPlayer()
    {
        if (!isLying) yield break;
        isLying = false;

        animator.SetTrigger(PARAM_STANDUP_TRIGGER);
        yield return new WaitForSeconds(standUpDuration);

        StartWalkingToPlayer();
    }

    private void StartWalkingToPlayer()
    {
        if (playerTransform == null) return;

        isWalkingToPlayer = true;
        agent.enabled = true;
        agent.isStopped = false;
        animator.SetBool(PARAM_MOVE, true);
    }

    private void StopWalking()
    {
        if (!isWalkingToPlayer) return;

        agent.isStopped = true;
        animator.SetBool(PARAM_MOVE, false);
        isWalkingToPlayer = false;
    }

    void Update()
    {
        if (isWalkingToPlayer && !isTagged && playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance > walkTowardsPlayerDistance)
            {
                if (agent.isOnNavMesh)
                    agent.SetDestination(playerTransform.position);
            }
            else
            {
                StopWalking();
                Debug.Log($"{name}: ถึงระยะกับผู้เล่นแล้ว");
            }
        }
    }

    // -------------------------------------------------------
    // เมื่อได้รับบัตร (Prefab ที่ถูกต้อง)
    // -------------------------------------------------------
    public void ReceiveTriageTag(string receivedTagColor) // ✅ เปลี่ยนเป็น public
    {
        if (isTagged) return;

        if (receivedTagColor.Equals("Green", System.StringComparison.OrdinalIgnoreCase))
        {
            isTagged = true;
            Debug.Log($"{name}: ได้รับบัตร {receivedTagColor} แล้ว กำลังไปจุดรักษา...");

            StopWalking();
            StartCoroutine(MoveToTreatmentAfterStandUp());
        }
        else
        {
            Debug.LogWarning($"{name}: บัตรไม่ถูกต้อง ({receivedTagColor})");
        }
    }

    private IEnumerator MoveToTreatmentAfterStandUp()
    {
        if (isLying)
        {
            animator.SetTrigger(PARAM_STANDUP_TRIGGER);
            yield return new WaitForSeconds(standUpDuration + 0.5f);
        }

        MoveToTreatmentArea();
    }

    private void MoveToTreatmentArea()
    {
        if (greenTreatmentArea == null)
        {
            Debug.LogError($"{name}: ยังไม่ได้ตั้งค่า GreenTreatmentArea!");
            return;
        }

        NavMeshHit hit;
        if (!NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name}: NPC ยังไม่อยู่บน NavMesh จะรอ 0.5 วินาทีแล้วลองใหม่...");
            StartCoroutine(WaitAndRetryMove());
            return;
        }

        agent.enabled = true;
        agent.isStopped = false;
        animator.SetBool(PARAM_MOVE, true);

        if (!agent.SetDestination(greenTreatmentArea.position))
        {
            Debug.LogWarning($"{name}: ไม่สามารถ SetDestination ได้");
        }
    }

    private IEnumerator WaitAndRetryMove()
    {
        yield return new WaitForSeconds(0.5f);
        MoveToTreatmentArea();
    }
}
