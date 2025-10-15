using UnityEngine;

public class PatientAnimatorController : MonoBehaviour
{
    private Animator animator;
    public Transform playerTransform; // ลาก XR Rig ของ Player มาใส่
    public float proximityRange = 3f; // ระยะห่างที่ผู้เล่นต้องเข้ามาใกล้ (3 เมตร)

    // พารามิเตอร์ที่ใช้ใน Animator Controller
    private const string PARAM_MOVE = "Move"; // ตัวแปร Bool สำหรับ Walk/Idle

    private void Start()
    {
        animator = GetComponent<Animator>();

        // ตรวจสอบว่าเริ่มต้นที่ท่านั่ง (G2 Sitting Pose)
        animator.Play("Sitting");
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // คำนวณระยะห่างระหว่างผู้เล่นกับ NPC
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // 1. เงื่อนไข: เมื่อ Player เข้าใกล้
        if (distance <= proximityRange)
        {
            // เรียกฟังก์ชันสำหรับเปลี่ยนท่าทาง
            StartInteractionSequence();
        }
    }

    public void StartInteractionSequence()
    {
        // 1. เปลี่ยนจาก Sitting ไป Standing Up (ใช้ Trigger หรือ Play)
        // ถ้าคุณตั้ง Transition จาก Sitting > Standing Up ใน Animator ให้ใช้
        // animator.SetTrigger("StandUp"); 
        
        // สำหรับตอนนี้ ใช้ Play โดยตรง เพราะเราต้องการควบคุมลำดับ
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Sitting"))
        {
             // 1. Standing Up
             animator.Play("Standing Up"); 
             Invoke("StartWalkSequence", 2f); // หน่วงเวลา 2 วินาทีเพื่อให้แอนิเมชัน Standing Up จบ
        }
    }

    private void StartWalkSequence()
    {
        // 2. เริ่มต้น Walk
        animator.SetBool(PARAM_MOVE, true);
        animator.Play("Walk");
        
        // (คุณจะต้องมีเงื่อนไขในโค้ดอื่น หรือใน Animator เพื่อเปลี่ยนจาก Walk > IDEL > Walk)
        // หรือถ้าต้องการให้เดินไป IDEL ทันทีหลังยืน:
        Invoke("StartIdleSequence", 5f); // เดิน 5 วินาที
    }
    
    private void StartIdleSequence()
    {
        // 3. เปลี่ยนเป็น IDEL
        animator.SetBool(PARAM_MOVE, false);
        animator.Play("IDEL");
    }

    // ฟังก์ชันนี้สามารถถูกเรียกใช้โดยปุ่ม 'พูดคุย' ในภายหลังได้
    public void PlayerInitiatedTalk()
    {
        // ตัวอย่างการเปลี่ยน Animation เมื่อผู้เล่นกดปุ่มพูดคุย
        // animator.SetBool("Talk", true);
    }
}