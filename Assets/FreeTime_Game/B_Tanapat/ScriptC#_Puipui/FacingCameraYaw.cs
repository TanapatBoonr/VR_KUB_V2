using UnityEngine;

public class FacingCameraYaw : MonoBehaviour
{
    [Tooltip("ลาก Camera Offset หรือ Main Camera มาใส่")]
    public Transform cameraTransform;

    [Tooltip("ระยะห่างจาก Player ที่ Socket ควรอยู่ (สำหรับปรับตำแหน่ง)")]
    public float followDistance = 0.5f;

    [Tooltip("ความสูงสัมพัทธ์ของ Socket (สำหรับปรับความสูง)")]
    public float yOffset = -0.3f; 
    
    // *** NEW: ตำแหน่งเริ่มต้นของ Socket (ซ้าย/ขวา) ***
    private Vector3 initialLocalPosition; 

    void Start()
    {
        // บันทึกตำแหน่ง Local Position เดิมของ Socket
        // เราจะใช้ค่า X (ซ้าย/ขวา) ของมันเป็นตัวกำหนด Lateral Offset
        initialLocalPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        if (cameraTransform == null)
        {
            Debug.LogError("Camera Transform is not assigned to FacingCameraYaw on " + gameObject.name);
            return;
        }

        // ********** 1. จัดการการหมุน (Rotation) **********
        
        // ใช้การหมุนแกน Y (Yaw) ของกล้องเท่านั้น
        Quaternion cameraYaw = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        transform.rotation = cameraYaw;

        // ********** 2. จัดการตำแหน่ง (Position) **********

        // 2.1 คำนวณตำแหน่งศูนย์กลางของ Rig (ตำแหน่งที่ Player ยืนอยู่)
        Vector3 targetPosition = cameraTransform.parent.position; 
        
        // 2.2 เพิ่ม Y Offset (ความสูง)
        targetPosition.y += yOffset;
        
        // 2.3 คำนวณตำแหน่งด้านหน้า (ตามทิศทางที่ Socket หันอยู่)
        //     - ใช้ transform.forward ที่คำนวณจาก cameraYaw (ไม่ก้ม/เงย)
        targetPosition += transform.forward * followDistance;
        
        // *** NEW: เพิ่มการชดเชยซ้าย/ขวา (Lateral Offset) ***
        //     - ใช้ transform.right (ทิศทางขวาของ Socket)
        //     - คูณด้วยค่า X จากตำแหน่ง Local Position เดิม (ค่าบวกสำหรับ R, ค่าลบสำหรับ L)
        targetPosition += transform.right * initialLocalPosition.x;

        // 2.4 กำหนดตำแหน่งสุดท้าย
        transform.position = targetPosition;
    }
}